using System.Globalization;
using System.Text.Json;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Scenarios.CityDelivery;
using FsCheck.Xunit;

namespace DataStructureAndAlgorithm.Test.IntegratedProjectsTest;

/// <summary>
/// 城市配送综合项目的性质测试。
/// </summary>
/// <remarks>
/// 样例测试回答“这个例子对不对”，性质测试回答“所有小规模合法快照都必须遵守哪些规则”。
/// 这里故意用简单的链式道路和穷举匹配作为独立判定器：测试判定器越朴素，越不容易复制生产算法的同一个错误。
/// </remarks>
public class CityDeliveryPlannerPropertyTests
{
    private static readonly DateTimeOffset PlanningTime =
        new(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);

    [Property(MaxTest = 60)]
    public bool Plan_AlwaysProducesAnExecutableMaximumMatchingAndCompleteOrderPartition(int[]? source)
    {
        var scenario = CreateConnectedScenario(source);
        var plan = new CityDeliveryPlanner().Plan(scenario);

        var assignedOrderIds = plan.Assignments.Select(item => item.OrderId).ToArray();
        var unassignedOrderIds = plan.UnassignedOrders.Select(item => item.OrderId).ToArray();

        // 一车一单要求两侧都不能重复；“已分配 + 未分配”还必须恰好覆盖输入订单，不能丢单或凭空造单。
        if (assignedOrderIds.Distinct(StringComparer.Ordinal).Count() != assignedOrderIds.Length ||
            plan.Assignments.Select(item => item.VehicleId).Distinct(StringComparer.Ordinal).Count() != plan.Assignments.Count ||
            assignedOrderIds.Concat(unassignedOrderIds).Order(StringComparer.Ordinal)
                .SequenceEqual(scenario.Orders.Select(item => item.Id).Order(StringComparer.Ordinal), StringComparer.Ordinal) is false)
        {
            return false;
        }

        var vehicles = scenario.Vehicles.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var orders = scenario.Orders.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var roadWeights = BuildDirectedRoadWeights(scenario.Roads);

        foreach (var assignment in plan.Assignments)
        {
            var vehicle = vehicles[assignment.VehicleId];
            var order = orders[assignment.OrderId];
            if (vehicle.Capacity < order.Volume || !HasRequiredSkill(vehicle, order) ||
                assignment.Path.Count == 0 || assignment.Path[0] != vehicle.StartLocationId ||
                assignment.Path[^1] != order.DestinationId)
            {
                return false;
            }

            var independentlySummedMinutes = 0d;
            for (var index = 1; index < assignment.Path.Count; index++)
            {
                if (!roadWeights.TryGetValue((assignment.Path[index - 1], assignment.Path[index]), out var minutes))
                {
                    return false;
                }

                independentlySummedMinutes += minutes;
            }

            if (Math.Abs(independentlySummedMinutes - assignment.TravelMinutes) > 1e-9 ||
                assignment.EstimatedArrival != scenario.PlanningTime + TimeSpan.FromMinutes(assignment.TravelMinutes) ||
                assignment.EstimatedArrival > assignment.Deadline)
            {
                return false;
            }
        }

        // 最多 5 辆车、6 个订单，穷举所有“一车匹配零或一个订单”的选择仍很小，适合作为最大匹配的独立 oracle。
        return plan.Assignments.Count == BruteForceMaximumMatchingSize(scenario);
    }

    [Property(MaxTest = 60)]
    public bool MaintenanceForest_OnAChainAlwaysHasVerticesMinusComponentsEdges(int[]? source)
    {
        var values = Normalize(source);
        var locationCount = 1 + ValueAt(values, 0) % 8;
        var locations = Enumerable.Range(0, locationCount)
            .Select(index => new DeliveryLocation($"L{index}"))
            .ToArray();
        var roads = Enumerable.Range(0, Math.Max(0, locationCount - 1))
            .Select(index => new DeliveryRoad(
                $"L{index}",
                $"L{index + 1}",
                TravelMinutes: 1 + ValueAt(values, index + 1) % 5,
                MaintenanceCost: 1 + ValueAt(values, index + 11) % 9,
                IsOpen: ValueAt(values, index + 21) % 2 == 0))
            .ToArray();

        var scenario = new CityDeliveryScenario(1, PlanningTime, locations, roads, [], []);
        var forest = new CityDeliveryPlanner().Plan(scenario).MaintenanceForest;
        var openRoads = roads.Where(road => road.IsOpen).ToArray();

        // 输入本身是一条链的子图，不可能含环，所以每条开放道路都必须出现在最小生成森林中。
        // 这同时验证森林恒等式 |E| = |V| - components，以及总成本没有遗漏或重复。
        return forest.ComponentCount == locationCount - openRoads.Length &&
               forest.Connections.Count == locationCount - forest.ComponentCount &&
               Math.Abs(forest.TotalCost - openRoads.Sum(road => road.MaintenanceCost)) < 1e-9;
    }

    [Property(MaxTest = 60)]
    public bool OptionalTrace_NeverChangesThePlanAndAlwaysRecordsStablePipelineStages(int[]? source)
    {
        var scenario = CreateConnectedScenario(source);
        var withoutTrace = new CityDeliveryPlanner().Plan(scenario);
        var trace = new CollectingAlgorithmTraceSink();
        var withTrace = new CityDeliveryPlanner(trace: trace).Plan(scenario);

        // JSON 仅在测试中充当深比较器：所有公开结果字段都被比较，同时无需让领域模型为了测试实现额外相等逻辑。
        if (!StringComparer.Ordinal.Equals(JsonSerializer.Serialize(withoutTrace), JsonSerializer.Serialize(withTrace)))
        {
            return false;
        }

        var operations = trace.Events.Select(item => item.Operation).ToArray();
        var expectedOperations = new List<string>
        {
            "Start", "BuildGraph", "EvaluateCandidates", "MaximumMatching", "CostOptimization"
        };
        expectedOperations.AddRange(Enumerable.Repeat("Dispatch", withTrace.Assignments.Count));
        expectedOperations.Add("MaintenanceForest");
        expectedOperations.Add("Complete");

        return trace.Events.Select(item => item.Step).SequenceEqual(Enumerable.Range(1, trace.Events.Count)) &&
               operations.SequenceEqual(expectedOperations, StringComparer.Ordinal);
    }

    private static CityDeliveryScenario CreateConnectedScenario(int[]? source)
    {
        var values = Normalize(source);
        var locationCount = 2 + ValueAt(values, 0) % 5;
        var locations = Enumerable.Range(0, locationCount)
            .Select(index => new DeliveryLocation($"L{index}"))
            .ToArray();
        var roads = Enumerable.Range(0, locationCount - 1)
            .Select(index => new DeliveryRoad(
                $"L{index}",
                $"L{index + 1}",
                TravelMinutes: 1 + ValueAt(values, index + 1) % 5,
                MaintenanceCost: 1 + ValueAt(values, index + 7) % 9))
            .ToArray();

        var vehicleCount = 1 + ValueAt(values, 13) % 5;
        var vehicles = Enumerable.Range(0, vehicleCount)
            .Select(index => new DeliveryVehicle(
                $"V{index}",
                $"L{ValueAt(values, index + 17) % locationCount}",
                Capacity: index == 0 ? 10 : 1 + ValueAt(values, index + 23) % 5,
                Skills: index == 0 || ValueAt(values, index + 29) % 2 == 0 ? ["ColdChain"] : []))
            .ToArray();

        var orderCount = 1 + ValueAt(values, 37) % 6;
        var orders = Enumerable.Range(0, orderCount)
            .Select(index => new DeliveryOrder(
                $"O{index}",
                $"L{ValueAt(values, index + 41) % locationCount}",
                Volume: index == 0 ? 1 : 1 + ValueAt(values, index + 47) % 7,
                Deadline: PlanningTime.AddMinutes(index == 0 ? 100 : 1 + ValueAt(values, index + 53) % 20),
                Urgency: ValueAt(values, index + 59) % 6,
                RequiredSkill: index != 0 && ValueAt(values, index + 67) % 3 == 0 ? "ColdChain" : null))
            .ToArray();

        // V0/O0 是一个始终可行的锚点，保证追踪性质每次都能验证 Dispatch 阶段；其余数据仍由 FsCheck 种子变化。
        // 先提升到 long 再加一；若种子归一化后恰为 int.MaxValue，留在 int 中相加会溢出成负数。
        return new CityDeliveryScenario(1L + ValueAt(values, 73), PlanningTime, locations, roads, vehicles, orders);
    }

    private static int BruteForceMaximumMatchingSize(CityDeliveryScenario scenario)
    {
        var edges = scenario.Vehicles
            .Select(vehicle => scenario.Orders
                .Select((order, index) => (order, index))
                .Where(candidate => vehicle.Capacity >= candidate.order.Volume &&
                                    HasRequiredSkill(vehicle, candidate.order) &&
                                    IndependentChainDistance(scenario, vehicle.StartLocationId, candidate.order.DestinationId) <=
                                    (candidate.order.Deadline - scenario.PlanningTime).TotalMinutes)
                .Select(candidate => candidate.index)
                .ToArray())
            .ToArray();
        var usedOrders = new bool[scenario.Orders.Count];

        return Search(0);

        int Search(int vehicleIndex)
        {
            if (vehicleIndex == edges.Length)
            {
                return 0;
            }

            var best = Search(vehicleIndex + 1);
            foreach (var orderIndex in edges[vehicleIndex])
            {
                if (usedOrders[orderIndex])
                {
                    continue;
                }

                usedOrders[orderIndex] = true;
                best = Math.Max(best, 1 + Search(vehicleIndex + 1));
                usedOrders[orderIndex] = false;
            }

            return best;
        }
    }

    private static double IndependentChainDistance(
        CityDeliveryScenario scenario,
        string from,
        string to)
    {
        var first = int.Parse(from[1..], CultureInfo.InvariantCulture);
        var second = int.Parse(to[1..], CultureInfo.InvariantCulture);
        var lower = Math.Min(first, second);
        var upper = Math.Max(first, second);
        return scenario.Roads.Skip(lower).Take(upper - lower).Sum(road => road.TravelMinutes);
    }

    private static Dictionary<(string From, string To), double> BuildDirectedRoadWeights(
        IReadOnlyList<DeliveryRoad> roads)
    {
        var result = new Dictionary<(string From, string To), double>();
        foreach (var road in roads.Where(item => item.IsOpen))
        {
            result[(road.From, road.To)] = road.TravelMinutes;
            if (road.IsBidirectional)
            {
                result[(road.To, road.From)] = road.TravelMinutes;
            }
        }

        return result;
    }

    private static bool HasRequiredSkill(DeliveryVehicle vehicle, DeliveryOrder order) =>
        order.RequiredSkill is null || vehicle.Skills.Contains(order.RequiredSkill, StringComparer.Ordinal);

    private static int[] Normalize(int[]? source) => source is { Length: > 0 } ? source : [0];

    // 位掩码把 int.MinValue 也安全映射到非负数；Math.Abs(int.MinValue) 会溢出，不适合生成器归一化。
    private static int ValueAt(IReadOnlyList<int> values, int index) => values[index % values.Count] & int.MaxValue;
}
