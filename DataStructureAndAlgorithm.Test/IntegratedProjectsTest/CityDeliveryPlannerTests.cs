using DataStructureAndAlgorithm.Scenarios.CityDelivery;

namespace DataStructureAndAlgorithm.Test.IntegratedProjectsTest;

public class CityDeliveryPlannerTests
{
    private static readonly DateTimeOffset PlanningTime =
        new(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Plan_ShouldMaximizeAssignmentsWithoutBreakingCapacityOrSkillConstraints()
    {
        var scenario = new CityDeliveryScenario(
            MapVersion: 1,
            PlanningTime,
            Locations:
            [
                new DeliveryLocation("Depot"),
                new DeliveryLocation("A"),
                new DeliveryLocation("B")
            ],
            Roads:
            [
                new DeliveryRoad("Depot", "A", TravelMinutes: 1, MaintenanceCost: 4),
                new DeliveryRoad("Depot", "B", TravelMinutes: 2, MaintenanceCost: 3),
                new DeliveryRoad("A", "B", TravelMinutes: 1, MaintenanceCost: 1)
            ],
            Vehicles:
            [
                // 冷链车容量只有 2，能接冷链单，但不能接体积为 3 的普通单。
                new DeliveryVehicle("V-cold", "Depot", Capacity: 2, Skills: ["ColdChain"]),
                // 普通车容量足够，但没有冷链技能。两个约束共同迫使最大匹配选择唯一可行组合。
                new DeliveryVehicle("V-plain", "Depot", Capacity: 3, Skills: [])
            ],
            Orders:
            [
                new DeliveryOrder(
                    "O-cold", "A", Volume: 2, Deadline: PlanningTime.AddMinutes(5),
                    Urgency: 5, RequiredSkill: "ColdChain"),
                new DeliveryOrder(
                    "O-normal", "B", Volume: 3, Deadline: PlanningTime.AddMinutes(10), Urgency: 2),
                new DeliveryOrder(
                    "O-too-large", "B", Volume: 99, Deadline: PlanningTime.AddMinutes(20), Urgency: 1)
            ]);

        var plan = new CityDeliveryPlanner(routeCacheCapacity: 8).Plan(scenario);

        Assert.Equal(2, plan.Assignments.Count);
        // 二叉最小堆按截止时间发布结果，不能依赖最小费用流残量网络中边的插入或枚举顺序。
        Assert.True(plan.Assignments.Select(item => item.OrderId).SequenceEqual(["O-cold", "O-normal"]));

        var coldAssignment = Assert.Single(plan.Assignments, item => item.OrderId == "O-cold");
        Assert.Equal("V-cold", coldAssignment.VehicleId);
        var normalAssignment = Assert.Single(plan.Assignments, item => item.OrderId == "O-normal");
        Assert.Equal("V-plain", normalAssignment.VehicleId);
        Assert.All(plan.Assignments, item =>
        {
            Assert.NotEmpty(item.Path);
            Assert.True(item.EstimatedArrival <= item.Deadline);
        });

        var unassigned = Assert.Single(plan.UnassignedOrders);
        Assert.Equal("O-too-large", unassigned.OrderId);
        Assert.Contains("容量", unassigned.Reason, StringComparison.Ordinal);

        // 候选生成计算两条路线，物化匹配结果时再次读取并命中 LRU，证明两个阶段共享缓存。
        Assert.Equal(2, plan.RouteCacheMisses);
        Assert.Equal(2, plan.RouteCacheHits);
        Assert.Equal(1, plan.MaintenanceForest.ComponentCount);
        Assert.Equal(4, plan.MaintenanceForest.TotalCost);
    }

    [Fact]
    public void Plan_WeakConnectivityMustNotBeTreatedAsDirectedReachability()
    {
        var scenario = new CityDeliveryScenario(
            MapVersion: 1,
            PlanningTime,
            Locations: [new DeliveryLocation("A"), new DeliveryLocation("B")],
            Roads:
            [
                // 并查集会把 A、B 合并，但道路只允许 A -> B，位于 B 的车辆不能反向到达 A。
                new DeliveryRoad(
                    "A", "B", TravelMinutes: 1, MaintenanceCost: 1,
                    IsBidirectional: false)
            ],
            Vehicles: [new DeliveryVehicle("V", "B", Capacity: 1, Skills: [])],
            Orders: [new DeliveryOrder("O", "A", Volume: 1, Deadline: PlanningTime.AddMinutes(10))]);

        var plan = new CityDeliveryPlanner().Plan(scenario);

        Assert.Empty(plan.Assignments);
        Assert.Equal("O", Assert.Single(plan.UnassignedOrders).OrderId);
        // 维护图按物理道路看作无向图，仍然连通；这进一步证明弱连通不等于有向可达。
        Assert.Equal(1, plan.MaintenanceForest.ComponentCount);
        Assert.Equal(1, plan.RouteCacheMisses);
    }

    [Fact]
    public void Plan_MapVersionMustIsolateCachedRoutes()
    {
        var planner = new CityDeliveryPlanner(routeCacheCapacity: 8);
        var locations = new[]
        {
            new DeliveryLocation("S"),
            new DeliveryLocation("M"),
            new DeliveryLocation("T")
        };
        var vehicles = new[] { new DeliveryVehicle("V", "S", Capacity: 1, Skills: []) };
        var orders = new[]
        {
            new DeliveryOrder("O", "T", Volume: 1, Deadline: PlanningTime.AddMinutes(30))
        };

        var first = planner.Plan(new CityDeliveryScenario(
            MapVersion: 1,
            PlanningTime,
            locations,
            Roads:
            [
                new DeliveryRoad("S", "T", TravelMinutes: 1, MaintenanceCost: 1),
                new DeliveryRoad("S", "M", TravelMinutes: 1, MaintenanceCost: 2),
                new DeliveryRoad("M", "T", TravelMinutes: 1, MaintenanceCost: 2)
            ],
            vehicles,
            orders));

        var second = planner.Plan(new CityDeliveryScenario(
            MapVersion: 2,
            PlanningTime,
            locations,
            Roads:
            [
                // 新版本把直达道路改成 10 分钟，最优路线应切换到 S -> M -> T。
                new DeliveryRoad("S", "T", TravelMinutes: 10, MaintenanceCost: 1),
                new DeliveryRoad("S", "M", TravelMinutes: 1, MaintenanceCost: 2),
                new DeliveryRoad("M", "T", TravelMinutes: 1, MaintenanceCost: 2)
            ],
            vehicles,
            orders));

        Assert.True(Assert.Single(first.Assignments).Path.SequenceEqual(["S", "T"]));
        Assert.True(Assert.Single(second.Assignments).Path.SequenceEqual(["S", "M", "T"]));
        Assert.Equal(2, Assert.Single(second.Assignments).TravelMinutes);
        // 如果缓存键遗漏 MapVersion，第二轮会错误地以 0 次 miss 返回旧的直达路线。
        Assert.Equal(1, second.RouteCacheMisses);
        Assert.Equal(1, second.RouteCacheHits);
    }

    [Fact]
    public void Plan_ShouldRejectDuplicateIdentifiersAndInvalidRoadWeights()
    {
        var planner = new CityDeliveryPlanner();
        var duplicateLocationScenario = new CityDeliveryScenario(
            MapVersion: 1,
            PlanningTime,
            Locations: [new DeliveryLocation("A"), new DeliveryLocation("A")],
            Roads: [],
            Vehicles: [],
            Orders: []);

        // 哈希索引依赖键唯一；若默默覆盖，后续图顶点与业务对象会失去一一对应关系。
        Assert.Throws<ArgumentException>(() => planner.Plan(duplicateLocationScenario));

        var negativeWeightScenario = new CityDeliveryScenario(
            MapVersion: 1,
            PlanningTime,
            Locations: [new DeliveryLocation("A"), new DeliveryLocation("B")],
            Roads: [new DeliveryRoad("A", "B", TravelMinutes: -1, MaintenanceCost: 1)],
            Vehicles: [],
            Orders: []);

        // A* 的正确性要求边权非负，因此必须在场景边界拒绝非法通行时间，而不是得到错误路线后再补救。
        Assert.Throws<ArgumentOutOfRangeException>(() => planner.Plan(negativeWeightScenario));
    }
}
