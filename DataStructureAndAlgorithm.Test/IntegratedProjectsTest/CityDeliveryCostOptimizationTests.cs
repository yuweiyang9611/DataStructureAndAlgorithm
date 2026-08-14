using System.Globalization;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Scenarios.CityDelivery;
using FsCheck.Xunit;

namespace DataStructureAndAlgorithm.Test.IntegratedProjectsTest;

/// <summary>
/// CityDelivery V2 的成本最优性测试。
/// </summary>
/// <remarks>
/// 最大匹配只回答“最多能送几单”，不能回答“这些最大方案中哪一个总行程最短”。
/// 本组测试用会诱骗局部贪心的固定反例和独立穷举 oracle，分别验证典型路径与大量小规模随机快照。
/// </remarks>
public class CityDeliveryCostOptimizationTests
{
    private static readonly DateTimeOffset PlanningTime =
        new(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Plan_MaximizesAssignmentCountBeforeMinimizingTotalTravelMinutes()
    {
        var scenario = new CityDeliveryScenario(
            MapVersion: 2,
            PlanningTime,
            Locations:
            [
                new DeliveryLocation("A"),
                new DeliveryLocation("B"),
                new DeliveryLocation("X"),
                new DeliveryLocation("Y")
            ],
            Roads:
            [
                new DeliveryRoad("A", "X", TravelMinutes: 1, MaintenanceCost: 1, IsBidirectional: false),
                new DeliveryRoad("A", "Y", TravelMinutes: 2, MaintenanceCost: 1, IsBidirectional: false),
                new DeliveryRoad("B", "X", TravelMinutes: 1, MaintenanceCost: 1, IsBidirectional: false)
            ],
            Vehicles:
            [
                new DeliveryVehicle("V-A", "A", Capacity: 1, Skills: []),
                new DeliveryVehicle("V-B", "B", Capacity: 1, Skills: [])
            ],
            Orders:
            [
                new DeliveryOrder("O-X", "X", Volume: 1, Deadline: PlanningTime.AddMinutes(10)),
                new DeliveryOrder("O-Y", "Y", Volume: 1, Deadline: PlanningTime.AddMinutes(10))
            ]);

        var plan = new CityDeliveryPlanner().Plan(scenario);

        Assert.Equal(2, plan.Assignments.Count);
        Assert.Equal(3, plan.TotalTravelMinutes);
        Assert.Equal("V-B", Assert.Single(plan.Assignments, assignment => assignment.OrderId == "O-X").VehicleId);
        Assert.Equal("V-A", Assert.Single(plan.Assignments, assignment => assignment.OrderId == "O-Y").VehicleId);

        // 若先贪心选择最短的 V-A -> O-X，就会堵住只能送 O-X 的 V-B；残量反向边必须撤销该局部选择。
        Assert.DoesNotContain(
            plan.Assignments,
            assignment => assignment.VehicleId == "V-A" && assignment.OrderId == "O-X");
    }

    [Fact]
    public void Plan_TraceSeparatesMaximumCardinalityFromCostOptimization()
    {
        var trace = new CollectingAlgorithmTraceSink();
        var scenario = CreateGeneratedScenario([1, 4, 2, 9, 3, 8, 5, 7]);

        var plan = new CityDeliveryPlanner(trace: trace).Plan(scenario);

        var maximumMatching = Assert.Single(trace.Events, item => item.Operation == "MaximumMatching");
        var costOptimization = Assert.Single(trace.Events, item => item.Operation == "CostOptimization");
        Assert.Equal(plan.Assignments.Count.ToString(CultureInfo.InvariantCulture), maximumMatching.State["matches"]);
        Assert.Equal(plan.Assignments.Count.ToString(CultureInfo.InvariantCulture), costOptimization.State["matches"]);
        Assert.Equal(
            plan.TotalTravelMinutes.ToString("R", CultureInfo.InvariantCulture),
            costOptimization.State["totalTravelMinutes"]);
        Assert.True(maximumMatching.Step < costOptimization.Step);
        Assert.All(trace.Events, item => Assert.Equal("CityDelivery", item.Algorithm));
    }

    [Property(MaxTest = 80)]
    public bool Plan_MatchesBruteForceLexicographicOptimumAndPreservesAssignmentInvariants(int[]? source)
    {
        var scenario = CreateGeneratedScenario(source);
        var plan = new CityDeliveryPlanner().Plan(scenario);
        var expected = BruteForceLexicographicOptimum(scenario);

        if (plan.Assignments.Count != expected.Count ||
            Math.Abs(plan.TotalTravelMinutes - expected.Cost) > 1e-9)
        {
            return false;
        }

        // 一车一单是流网络四层结构的容量不变量；总成本还必须等于每条最终路线的独立求和。
        return plan.Assignments.Select(assignment => assignment.VehicleId)
                   .Distinct(StringComparer.Ordinal).Count() == plan.Assignments.Count &&
               plan.Assignments.Select(assignment => assignment.OrderId)
                   .Distinct(StringComparer.Ordinal).Count() == plan.Assignments.Count &&
               Math.Abs(plan.TotalTravelMinutes - plan.Assignments.Sum(assignment => assignment.TravelMinutes)) < 1e-9 &&
               plan.Assignments.All(assignment =>
                   assignment.Path.Count == 2 &&
                   assignment.EstimatedArrival <= assignment.Deadline);
    }

    private static CityDeliveryScenario CreateGeneratedScenario(int[]? source)
    {
        var values = source is { Length: > 0 } ? source : [0];
        var vehicleCount = 1 + ValueAt(values, 0) % 4;
        var orderCount = 1 + ValueAt(values, 1) % 4;

        var vehicleLocations = Enumerable.Range(0, vehicleCount)
            .Select(index => new DeliveryLocation($"V{index}"))
            .ToArray();
        var orderLocations = Enumerable.Range(0, orderCount)
            .Select(index => new DeliveryLocation($"O{index}"))
            .ToArray();
        var vehicles = Enumerable.Range(0, vehicleCount)
            .Select(index => new DeliveryVehicle($"vehicle-{index}", $"V{index}", Capacity: 1, Skills: []))
            .ToArray();
        var orders = Enumerable.Range(0, orderCount)
            .Select(index => new DeliveryOrder(
                $"order-{index}",
                $"O{index}",
                Volume: 1,
                Deadline: PlanningTime.AddMinutes(100)))
            .ToArray();

        var roads = new List<DeliveryRoad>();
        for (var vehicleIndex = 0; vehicleIndex < vehicleCount; vehicleIndex++)
        {
            for (var orderIndex = 0; orderIndex < orderCount; orderIndex++)
            {
                var valueIndex = 2 + vehicleIndex * orderCount + orderIndex;
                // 锚点保证至少有一条边；其余边随机缺失，让性质覆盖稀疏图、竞争订单和无法分配订单。
                if ((vehicleIndex != 0 || orderIndex != 0) && ValueAt(values, valueIndex + 19) % 3 == 0)
                {
                    continue;
                }

                roads.Add(new DeliveryRoad(
                    $"V{vehicleIndex}",
                    $"O{orderIndex}",
                    TravelMinutes: 1 + ValueAt(values, valueIndex) % 20,
                    MaintenanceCost: 1,
                    IsBidirectional: false));
            }
        }

        return new CityDeliveryScenario(
            MapVersion: 1L + ValueAt(values, 37),
            PlanningTime,
            [.. vehicleLocations, .. orderLocations],
            roads,
            vehicles,
            orders);
    }

    private static (int Count, double Cost) BruteForceLexicographicOptimum(CityDeliveryScenario scenario)
    {
        var roadCosts = scenario.Roads.ToDictionary(
            road => (road.From, road.To),
            road => road.TravelMinutes);
        var usedOrders = new bool[scenario.Orders.Count];

        return Search(vehicleIndex: 0);

        (int Count, double Cost) Search(int vehicleIndex)
        {
            if (vehicleIndex == scenario.Vehicles.Count)
            {
                return (0, 0);
            }

            var best = Search(vehicleIndex + 1);
            var vehicle = scenario.Vehicles[vehicleIndex];
            for (var orderIndex = 0; orderIndex < scenario.Orders.Count; orderIndex++)
            {
                var order = scenario.Orders[orderIndex];
                if (usedOrders[orderIndex] ||
                    !roadCosts.TryGetValue((vehicle.StartLocationId, order.DestinationId), out var cost))
                {
                    continue;
                }

                usedOrders[orderIndex] = true;
                var suffix = Search(vehicleIndex + 1);
                usedOrders[orderIndex] = false;

                var candidate = (Count: suffix.Count + 1, Cost: suffix.Cost + cost);
                if (candidate.Count > best.Count ||
                    candidate.Count == best.Count && candidate.Cost < best.Cost)
                {
                    best = candidate;
                }
            }

            return best;
        }
    }

    private static int ValueAt(IReadOnlyList<int> values, int index) => values[index % values.Count] & int.MaxValue;
}
