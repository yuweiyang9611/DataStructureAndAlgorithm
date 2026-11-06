// 城市即时配送 V2：最短路算到每单的送达时间，最大匹配把订单分给车，最小生成森林规划道路维护。
// 把 O-003 的体积改成 2，或者给 V-02 加上 ColdChain 技能，看分派结果怎么变。
using DataStructureAndAlgorithm.Scenarios.CityDelivery;

public static class Demo
{
    public static void Run()
    {
        var planningTime = new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);
        var scenario = new CityDeliveryScenario(
            MapVersion: 1,
            PlanningTime: planningTime,
            Locations: [new("Depot"), new("A"), new("B"), new("C")],
            Roads:
            [
                new("Depot", "A", TravelMinutes: 4, MaintenanceCost: 6),
                new("Depot", "B", TravelMinutes: 2, MaintenanceCost: 5),
                new("B", "A", TravelMinutes: 1, MaintenanceCost: 2),
                new("A", "C", TravelMinutes: 3, MaintenanceCost: 4),
                new("B", "C", TravelMinutes: 7, MaintenanceCost: 3)
            ],
            Vehicles:
            [
                new("V-01", "Depot", Capacity: 5, Skills: ["ColdChain"]),
                new("V-02", "B", Capacity: 3, Skills: [])
            ],
            Orders:
            [
                new("O-001", "C", Volume: 2, Deadline: planningTime.AddMinutes(12), Urgency: 5, RequiredSkill: "ColdChain"),
                new("O-002", "A", Volume: 1, Deadline: planningTime.AddMinutes(6), Urgency: 3),
                new("O-003", "C", Volume: 99, Deadline: planningTime.AddMinutes(30), Urgency: 1)
            ]);

        var plan = new CityDeliveryPlanner(routeCacheCapacity: 16).Plan(scenario);
        Console.WriteLine($"分派 {plan.Assignments.Count} 单，总行驶 {plan.TotalTravelMinutes} 分钟，路线缓存命中 {plan.RouteCacheHits} 次、未命中 {plan.RouteCacheMisses} 次");
        foreach (var assignment in plan.Assignments)
        {
            Console.WriteLine($"  {assignment.VehicleId} 送 {assignment.OrderId}：{string.Join(" -> ", assignment.Path)}，{assignment.TravelMinutes} 分钟，{assignment.EstimatedArrival:HH:mm} 到达（截止 {assignment.Deadline:HH:mm}）");
        }
        foreach (var unassigned in plan.UnassignedOrders) Console.WriteLine($"  未分派 {unassigned.OrderId}：{unassigned.Reason}");
        var forest = plan.MaintenanceForest;
        Console.WriteLine($"道路维护最小生成森林：总成本 {forest.TotalCost}，{forest.ComponentCount} 个分量，{string.Join(", ", forest.Connections.Select(connection => $"{connection.First}-{connection.Second}"))}");
    }
}
