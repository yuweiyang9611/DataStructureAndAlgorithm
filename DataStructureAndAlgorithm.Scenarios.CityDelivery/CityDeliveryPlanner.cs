using System.Globalization;
using DataStructureAndAlgorithm.Caching;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Graph;
using DataStructureAndAlgorithm.Hashing;
using DataStructureAndAlgorithm.Heap;
using DataStructureAndAlgorithm.Set;
using DataStructureAndAlgorithm.Sorting;

namespace DataStructureAndAlgorithm.Scenarios.CityDelivery;

/// <summary>
/// 城市即时配送综合算法示例。
/// </summary>
/// <remarks>
/// 该规划器把多个单独学习过的数据结构和算法串成一条完整数据流：
/// <list type="number">
/// <item><description>开放寻址哈希表负责地点、车辆和订单索引；</description></item>
/// <item><description>带权图和 A* 负责路线计算；A* 的启发值取 0，保证在任意非负旅行时间图上都不会高估；</description></item>
/// <item><description>并查集先做忽略道路方向的弱连通预筛，但最终可达性仍以 A* 为准；</description></item>
/// <item><description>LRU 缓存避免重复计算同一地图版本中的路线；</description></item>
/// <item><description>最小费用最大流先最大化成功分配数，再在所有最大匹配中最小化总行程时间；</description></item>
/// <item><description>二叉最小堆按截止时间输出确定性的执行顺序；</description></item>
/// <item><description>Prim 算法给出当前开放道路的最低维护森林。</description></item>
/// </list>
/// </remarks>
public sealed class CityDeliveryPlanner
{
    private const string TraceAlgorithmName = "CityDelivery";
    private const string NoFeasibleVehicleReason = "没有同时满足容量、技能、道路可达性与截止时间的车辆。";
    private const string MatchingCompetitionReason = "存在可行车辆，但在本轮一车一单的最小费用最大流中未被选中。";

    private readonly LruCache<RouteCacheKey, CachedRoute> _routeCache;
    private readonly IAlgorithmTraceSink? _trace;

    /// <summary>
    /// 创建规划器。
    /// </summary>
    /// <param name="routeCacheCapacity">最多缓存的起终点路线数量。</param>
    /// <param name="trace">
    /// 可选的教学追踪接收器。默认不追踪，避免让生产路径为学习功能承担额外分配；
    /// 注入后只记录稳定的业务阶段，而不暴露图、堆等实现细节。
    /// </param>
    public CityDeliveryPlanner(int routeCacheCapacity = 128, IAlgorithmTraceSink? trace = null)
    {
        if (routeCacheCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(routeCacheCapacity), routeCacheCapacity, "路线缓存容量必须为正数。");
        }

        _routeCache = new LruCache<RouteCacheKey, CachedRoute>(routeCacheCapacity);
        _trace = BestEffortAlgorithmTraceSink.Wrap(trace);
    }

    /// <summary>
    /// 为输入快照生成一次“一车一单”、分配数最大且总行程时间最小的配送计划。
    /// </summary>
    /// <remarks>
    /// 优化目标采用严格的字典序：第一目标是尽可能多地分配订单；只有分配数相同，才比较总行程时间。
    /// 这不是“给最大匹配补一个排序”：局部选择一条最短边可能堵住另一辆车的唯一订单，因此必须允许残量反向边
    /// 撤销早先选择。最小费用最大流正好同时表达“一车一单”的单位容量和这两个分层目标。
    /// </remarks>
    public CityDeliveryPlan Plan(CityDeliveryScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ValidateScenarioCollections(scenario);

        if (_trace is not null)
        {
            RecordTrace(
                "Start",
                "开始处理城市配送快照。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["mapVersion"] = scenario.MapVersion.ToString(CultureInfo.InvariantCulture),
                    ["locations"] = scenario.Locations.Count.ToString(CultureInfo.InvariantCulture),
                    ["roads"] = scenario.Roads.Count.ToString(CultureInfo.InvariantCulture),
                    ["vehicles"] = scenario.Vehicles.Count.ToString(CultureInfo.InvariantCulture),
                    ["orders"] = scenario.Orders.Count.ToString(CultureInfo.InvariantCulture)
                });
        }

        var locations = BuildLocationIndex(scenario.Locations);
        var vehicles = BuildVehicleIndex(scenario.Vehicles, locations);
        var orders = BuildOrderIndex(scenario.Orders, locations);
        var (travelGraph, weakConnectivity, maintenanceGraph) = BuildGraphs(scenario, locations);

        if (_trace is not null)
        {
            RecordTrace(
                "BuildGraph",
                "完成有向通行图、弱连通并查集和无向维护图的构建。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["locations"] = scenario.Locations.Count.ToString(CultureInfo.InvariantCulture),
                    ["openRoads"] = scenario.Roads.Count(road => road.IsOpen).ToString(CultureInfo.InvariantCulture)
                });
        }

        var cacheHits = 0;
        var cacheMisses = 0;
        var candidateAssignments = new List<CandidateAssignment>();
        var feasibleOrders = new OpenAddressingHashTable<string, bool>(comparer: StringComparer.Ordinal);

        // 流网络只包含真正可行的车辆—订单边。这样最终每一单位流都可以直接转成可执行配送任务。
        foreach (var vehicle in scenario.Vehicles)
        {
            foreach (var order in scenario.Orders)
            {
                if (!HasCapacityAndSkill(vehicle, order))
                {
                    continue;
                }

                // 并查集忽略单行道方向，只能快速排除“无向意义下也不连通”的情况。
                // 即使这里连通，仍必须运行 A* 验证从车辆位置到订单目的地的有向可达性。
                if (!weakConnectivity.AreConnected(vehicle.StartLocationId, order.DestinationId))
                {
                    continue;
                }

                var route = GetRoute(
                    scenario.MapVersion,
                    travelGraph,
                    vehicle.StartLocationId,
                    order.DestinationId,
                    ref cacheHits,
                    ref cacheMisses);

                if (!route.IsReachable || scenario.PlanningTime + TimeSpan.FromMinutes(route.TravelMinutes) > order.Deadline)
                {
                    continue;
                }

                candidateAssignments.Add(new CandidateAssignment(vehicle.Id, order.Id, route.TravelMinutes));
                feasibleOrders[order.Id] = true;
            }
        }

        if (_trace is not null)
        {
            RecordTrace(
                "EvaluateCandidates",
                "完成容量、技能、可达性与截止时间过滤，得到最小费用最大流的候选边。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["candidateEdges"] = candidateAssignments.Count.ToString(CultureInfo.InvariantCulture),
                    ["feasibleOrders"] = feasibleOrders.Count.ToString(CultureInfo.InvariantCulture),
                    ["routeCacheHits"] = cacheHits.ToString(CultureInfo.InvariantCulture),
                    ["routeCacheMisses"] = cacheMisses.ToString(CultureInfo.InvariantCulture)
                });
        }

        var source = AssignmentFlowNode.Source;
        var sink = AssignmentFlowNode.Sink;
        var assignmentNetwork = new MinCostFlowNetwork<AssignmentFlowNode>();
        assignmentNetwork.AddVertex(source);
        assignmentNetwork.AddVertex(sink);

        // 源点 -> 车辆和订单 -> 汇点的容量都为 1，直接编码“一车一单”。显式按稳定标识排序后建边，
        // 让多个方案的总成本完全相同时仍有可重复结果，而不依赖调用者集合或哈希表的枚举顺序。
        foreach (var vehicleId in scenario.Vehicles.Select(vehicle => vehicle.Id).Order(StringComparer.Ordinal))
        {
            assignmentNetwork.AddEdge(source, AssignmentFlowNode.Vehicle(vehicleId), capacity: 1, unitCost: 0);
        }

        SortAlgorithms.MergeSort(candidateAssignments, CandidateAssignmentComparer.Instance);
        foreach (var candidate in candidateAssignments)
        {
            assignmentNetwork.AddEdge(
                AssignmentFlowNode.Vehicle(candidate.VehicleId),
                AssignmentFlowNode.Order(candidate.OrderId),
                capacity: 1,
                unitCost: candidate.TravelMinutes);
        }

        foreach (var orderId in scenario.Orders.Select(order => order.Id).Order(StringComparer.Ordinal))
        {
            assignmentNetwork.AddEdge(AssignmentFlowNode.Order(orderId), sink, capacity: 1, unitCost: 0);
        }

        // 不传流量上限，算法会一直增广到没有源汇路径，所以先得到最大分配数；每轮又选择最低成本增广路，
        // 因而在这个最大分配数下总行程时间最小。输入网络会被克隆，后续教学展示可以安全复用它。
        var optimalFlow = MinCostFlowAlgorithms.MinimumCostMaximumFlow(assignmentNetwork, source, sink);
        var selectedAssignments = optimalFlow.Edges
            .Where(edge => edge is
            {
                Flow: 1,
                From.Kind: AssignmentFlowNodeKind.Vehicle,
                To.Kind: AssignmentFlowNodeKind.Order
            })
            .Select(edge => new CandidateAssignment(edge.From.Id, edge.To.Id, edge.UnitCost))
            .ToList();

        if (_trace is not null)
        {
            RecordTrace(
                "MaximumMatching",
                "最小费用最大流已证明本轮一车一单约束下的最大分配数。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["matches"] = optimalFlow.Flow.ToString(CultureInfo.InvariantCulture)
                });

            RecordTrace(
                "CostOptimization",
                "在所有最大分配方案中选择总行程时间最小的方案。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["matches"] = optimalFlow.Flow.ToString(CultureInfo.InvariantCulture),
                    ["totalTravelMinutes"] = optimalFlow.Cost.ToString("R", CultureInfo.InvariantCulture)
                });
        }

        var matchedOrders = new OpenAddressingHashTable<string, bool>(comparer: StringComparer.Ordinal);
        var dispatchQueue = new BinaryMinHeap<DispatchEntry>(DispatchEntryComparer.Instance);

        foreach (var selected in selectedAssignments)
        {
            var vehicle = vehicles[selected.VehicleId];
            var order = orders[selected.OrderId];
            var route = GetRoute(
                scenario.MapVersion,
                travelGraph,
                vehicle.StartLocationId,
                order.DestinationId,
                ref cacheHits,
                ref cacheMisses);

            // 这里再次读取路线会命中 LRU。重复读取不是算法必需，而是刻意演示“候选生成”和“结果物化”
            // 可以通过缓存共享昂贵的最短路结果。
            dispatchQueue.Enqueue(new DispatchEntry(vehicle, order, route));
            matchedOrders[order.Id] = true;
        }

        var assignments = new List<DeliveryAssignment>(selectedAssignments.Count);
        while (dispatchQueue.Count > 0)
        {
            var entry = dispatchQueue.Dequeue();
            var assignment = new DeliveryAssignment(
                entry.Vehicle.Id,
                entry.Order.Id,
                entry.Route.Path,
                entry.Route.TravelMinutes,
                scenario.PlanningTime + TimeSpan.FromMinutes(entry.Route.TravelMinutes),
                entry.Order.Deadline);
            assignments.Add(assignment);

            // 只在最终确定的出车顺序上记录事件。候选边可能很多，逐边追踪会淹没真正值得学习的决策，
            // 还会把最小费用流的内部增广顺序误当成公开契约。
            if (_trace is not null)
            {
                RecordTrace(
                    "Dispatch",
                    "按截止时间、紧急度和稳定标识顺序生成一条配送任务。",
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["vehicleId"] = assignment.VehicleId,
                        ["orderId"] = assignment.OrderId,
                        ["travelMinutes"] = assignment.TravelMinutes.ToString("R", CultureInfo.InvariantCulture),
                        ["path"] = string.Join(" -> ", assignment.Path)
                    });
            }
        }

        var unassignedOrders = BuildUnassignedOrders(scenario.Orders, feasibleOrders, matchedOrders);
        var maintenanceForest = BuildMaintenanceForest(maintenanceGraph);

        if (_trace is not null)
        {
            RecordTrace(
                "MaintenanceForest",
                "Prim 已生成当前开放道路的最低维护森林。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["components"] = maintenanceForest.ComponentCount.ToString(CultureInfo.InvariantCulture),
                    ["connections"] = maintenanceForest.Connections.Count.ToString(CultureInfo.InvariantCulture),
                    ["totalCost"] = maintenanceForest.TotalCost.ToString("R", CultureInfo.InvariantCulture)
                });

            RecordTrace(
                "Complete",
                "城市配送计划生成完成。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["assignments"] = assignments.Count.ToString(CultureInfo.InvariantCulture),
                    ["totalTravelMinutes"] = assignments.Sum(assignment => assignment.TravelMinutes)
                        .ToString("R", CultureInfo.InvariantCulture),
                    ["unassignedOrders"] = unassignedOrders.Count.ToString(CultureInfo.InvariantCulture),
                    ["routeCacheHits"] = cacheHits.ToString(CultureInfo.InvariantCulture),
                    ["routeCacheMisses"] = cacheMisses.ToString(CultureInfo.InvariantCulture)
                });
        }

        return new CityDeliveryPlan(
            scenario.MapVersion,
            scenario.PlanningTime,
            assignments.AsReadOnly(),
            unassignedOrders,
            maintenanceForest,
            cacheHits,
            cacheMisses);
    }

    private void RecordTrace(
        string operation,
        string description,
        IReadOnlyDictionary<string, string>? state = null)
    {
        // 空条件访问让“不开追踪”成为真正的默认路径：调用者无需创建空对象，算法也不会创建事件快照。
        _trace?.Record(TraceAlgorithmName, operation, description, state);
    }

    private static void ValidateScenarioCollections(CityDeliveryScenario scenario)
    {
        if (scenario.MapVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scenario), "地图版本不能为负数。");
        }

        ArgumentNullException.ThrowIfNull(scenario.Locations);
        ArgumentNullException.ThrowIfNull(scenario.Roads);
        ArgumentNullException.ThrowIfNull(scenario.Vehicles);
        ArgumentNullException.ThrowIfNull(scenario.Orders);
    }

    private static OpenAddressingHashTable<string, DeliveryLocation> BuildLocationIndex(
        IReadOnlyList<DeliveryLocation> source)
    {
        var result = new OpenAddressingHashTable<string, DeliveryLocation>(
            Math.Max(8, source.Count * 2), StringComparer.Ordinal);

        foreach (var location in source)
        {
            ArgumentNullException.ThrowIfNull(location);
            ValidateIdentifier(location.Id, "地点标识");
            if (!result.TryAdd(location.Id, location))
            {
                throw new ArgumentException($"地点标识重复：{location.Id}", nameof(source));
            }
        }

        return result;
    }

    private static OpenAddressingHashTable<string, DeliveryVehicle> BuildVehicleIndex(
        IReadOnlyList<DeliveryVehicle> source,
        OpenAddressingHashTable<string, DeliveryLocation> locations)
    {
        var result = new OpenAddressingHashTable<string, DeliveryVehicle>(
            Math.Max(8, source.Count * 2), StringComparer.Ordinal);

        foreach (var vehicle in source)
        {
            ArgumentNullException.ThrowIfNull(vehicle);
            ValidateIdentifier(vehicle.Id, "车辆标识");
            ValidateIdentifier(vehicle.StartLocationId, "车辆起点");
            if (vehicle.Capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(source), vehicle.Capacity, "车辆容量不能为负数。");
            }

            ArgumentNullException.ThrowIfNull(vehicle.Skills);
            foreach (var skill in vehicle.Skills)
            {
                ValidateIdentifier(skill, "车辆技能");
            }

            if (!locations.ContainsKey(vehicle.StartLocationId))
            {
                throw new ArgumentException(
                    $"车辆 {vehicle.Id} 的起点不存在：{vehicle.StartLocationId}", nameof(source));
            }

            if (!result.TryAdd(vehicle.Id, vehicle))
            {
                throw new ArgumentException($"车辆标识重复：{vehicle.Id}", nameof(source));
            }
        }

        return result;
    }

    private static OpenAddressingHashTable<string, DeliveryOrder> BuildOrderIndex(
        IReadOnlyList<DeliveryOrder> source,
        OpenAddressingHashTable<string, DeliveryLocation> locations)
    {
        var result = new OpenAddressingHashTable<string, DeliveryOrder>(
            Math.Max(8, source.Count * 2), StringComparer.Ordinal);

        foreach (var order in source)
        {
            ArgumentNullException.ThrowIfNull(order);
            ValidateIdentifier(order.Id, "订单标识");
            ValidateIdentifier(order.DestinationId, "订单目的地");
            if (order.Volume <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(source), order.Volume, "订单体积必须为正数。");
            }

            if (order.Urgency < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(source), order.Urgency, "订单紧急度不能为负数。");
            }

            if (order.RequiredSkill is not null)
            {
                ValidateIdentifier(order.RequiredSkill, "订单所需技能");
            }

            if (!locations.ContainsKey(order.DestinationId))
            {
                throw new ArgumentException(
                    $"订单 {order.Id} 的目的地不存在：{order.DestinationId}", nameof(source));
            }

            if (!result.TryAdd(order.Id, order))
            {
                throw new ArgumentException($"订单标识重复：{order.Id}", nameof(source));
            }
        }

        return result;
    }

    private static (
        WeightedGraph<string> TravelGraph,
        DisjointSet<string> WeakConnectivity,
        WeightedGraph<string> MaintenanceGraph) BuildGraphs(
        CityDeliveryScenario scenario,
        OpenAddressingHashTable<string, DeliveryLocation> locations)
    {
        var travelGraph = new WeightedGraph<string>(isDirected: true, StringComparer.Ordinal);
        var maintenanceGraph = new WeightedGraph<string>(isDirected: false, StringComparer.Ordinal);
        var weakConnectivity = new DisjointSet<string>(StringComparer.Ordinal);

        foreach (var location in scenario.Locations)
        {
            travelGraph.AddVertex(location.Id);
            maintenanceGraph.AddVertex(location.Id);
            weakConnectivity.Add(location.Id);
        }

        var directedRoads = new OpenAddressingHashTable<DirectedRoadKey, bool>();
        var physicalRoads = new OpenAddressingHashTable<UndirectedRoadKey, bool>();

        foreach (var road in scenario.Roads)
        {
            ArgumentNullException.ThrowIfNull(road);
            ValidateRoad(road, locations);
            if (!road.IsOpen)
            {
                continue;
            }

            // 第一版把输入中的每条记录定义成唯一物理道路，因而同一无向端点对不能重复。
            // 这既避免维护图出现平行边，也让演示数据的含义保持清晰。
            var physicalKey = UndirectedRoadKey.Create(road.From, road.To);
            if (!physicalRoads.TryAdd(physicalKey, true))
            {
                throw new ArgumentException(
                    $"开放道路重复：{physicalKey.First} <-> {physicalKey.Second}", nameof(scenario));
            }

            AddDirectedRoad(travelGraph, directedRoads, road.From, road.To, road.TravelMinutes);
            if (road.IsBidirectional)
            {
                AddDirectedRoad(travelGraph, directedRoads, road.To, road.From, road.TravelMinutes);
            }

            // 并查集只建立弱连通关系，所以单行道也会合并两端；真正方向仍由 A* 判断。
            weakConnectivity.Union(road.From, road.To);
            maintenanceGraph.AddEdge(road.From, road.To, road.MaintenanceCost);
        }

        return (travelGraph, weakConnectivity, maintenanceGraph);
    }

    private static void AddDirectedRoad(
        WeightedGraph<string> graph,
        OpenAddressingHashTable<DirectedRoadKey, bool> roadIndex,
        string from,
        string to,
        double travelMinutes)
    {
        var key = new DirectedRoadKey(from, to);
        if (!roadIndex.TryAdd(key, true))
        {
            throw new ArgumentException($"有向道路重复：{from} -> {to}");
        }

        graph.AddEdge(from, to, travelMinutes);
    }

    private static void ValidateRoad(
        DeliveryRoad road,
        OpenAddressingHashTable<string, DeliveryLocation> locations)
    {
        ValidateIdentifier(road.From, "道路起点");
        ValidateIdentifier(road.To, "道路终点");
        if (StringComparer.Ordinal.Equals(road.From, road.To))
        {
            throw new ArgumentException($"道路不能连接同一个地点：{road.From}", nameof(road));
        }

        if (!locations.ContainsKey(road.From) || !locations.ContainsKey(road.To))
        {
            throw new ArgumentException($"道路端点不存在：{road.From} -> {road.To}", nameof(road));
        }

        if (!double.IsFinite(road.TravelMinutes) || road.TravelMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(road), road.TravelMinutes, "通行时间必须是有限非负数。");
        }

        if (!double.IsFinite(road.MaintenanceCost) || road.MaintenanceCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(road), road.MaintenanceCost, "维护成本必须是有限非负数。");
        }
    }

    private CachedRoute GetRoute(
        long mapVersion,
        WeightedGraph<string> graph,
        string from,
        string to,
        ref int cacheHits,
        ref int cacheMisses)
    {
        var key = new RouteCacheKey(mapVersion, from, to);
        if (_routeCache.TryGetValue(key, out var cached))
        {
            cacheHits++;
            return cached;
        }

        cacheMisses++;

        // h = 0 永远不会高估剩余旅行时间，因此无需地理坐标也能保证最优性。
        // A* 此时等价于使用仓库自实现 IndexedPriorityQueue 的 Dijkstra。
        var shortestPath = ComprehensiveGraphAlgorithms.AStar(graph, from, to, static _ => 0d);
        var path = shortestPath.GetPathTo(to);
        var route = path.Count == 0
            ? CachedRoute.Unreachable
            : new CachedRoute(path, shortestPath.Distances[to]);

        _routeCache.Set(key, route);
        return route;
    }

    private static bool HasCapacityAndSkill(DeliveryVehicle vehicle, DeliveryOrder order)
    {
        if (vehicle.Capacity < order.Volume)
        {
            return false;
        }

        return order.RequiredSkill is null ||
               vehicle.Skills.Any(skill => StringComparer.Ordinal.Equals(skill, order.RequiredSkill));
    }

    private static IReadOnlyList<UnassignedDeliveryOrder> BuildUnassignedOrders(
        IReadOnlyList<DeliveryOrder> orders,
        OpenAddressingHashTable<string, bool> feasibleOrders,
        OpenAddressingHashTable<string, bool> matchedOrders)
    {
        var result = new List<UnassignedDeliveryOrder>();
        foreach (var order in orders)
        {
            if (matchedOrders.ContainsKey(order.Id))
            {
                continue;
            }

            result.Add(new UnassignedDeliveryOrder(
                order.Id,
                feasibleOrders.ContainsKey(order.Id) ? MatchingCompetitionReason : NoFeasibleVehicleReason));
        }

        // 哈希表与残量流网络都不承诺业务所需的展示顺序。对外结果显式稳定排序，确保示例、测试和文档输出可重复。
        SortAlgorithms.MergeSort(result, UnassignedOrderComparer.Instance);
        return result.AsReadOnly();
    }

    private static MaintenanceForestPlan BuildMaintenanceForest(WeightedGraph<string> maintenanceGraph)
    {
        var forest = ComprehensiveGraphAlgorithms.PrimMinimumSpanningForest(maintenanceGraph);
        var connections = forest.Edges
            .Select(edge =>
            {
                var firstComesFirst = string.CompareOrdinal(edge.First, edge.Second) <= 0;
                return new MaintenanceConnection(
                    firstComesFirst ? edge.First : edge.Second,
                    firstComesFirst ? edge.Second : edge.First,
                    edge.Weight);
            })
            .ToList();

        SortAlgorithms.MergeSort(connections, MaintenanceConnectionComparer.Instance);
        return new MaintenanceForestPlan(connections.AsReadOnly(), forest.TotalWeight, forest.ComponentCount);
    }

    private static void ValidateIdentifier(string value, string description)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{description}不能为空。", nameof(value));
        }

        if (!StringComparer.Ordinal.Equals(value, value.Trim()))
        {
            throw new ArgumentException($"{description}不能包含首尾空白：'{value}'。", nameof(value));
        }
    }

    private readonly record struct DirectedRoadKey(string From, string To);

    private readonly record struct UndirectedRoadKey(string First, string Second)
    {
        public static UndirectedRoadKey Create(string first, string second) =>
            string.CompareOrdinal(first, second) <= 0
                ? new UndirectedRoadKey(first, second)
                : new UndirectedRoadKey(second, first);
    }

    private readonly record struct RouteCacheKey(long MapVersion, string From, string To);

    private sealed record CachedRoute(IReadOnlyList<string> Path, double TravelMinutes)
    {
        public static CachedRoute Unreachable { get; } = new([], double.PositiveInfinity);

        public bool IsReachable => Path.Count > 0;
    }

    private enum AssignmentFlowNodeKind
    {
        Source,
        Vehicle,
        Order,
        Sink
    }

    /// <summary>
    /// 使用节点种类隔离业务标识：即使某辆车或订单恰好叫 Source，也不会与网络源点发生键冲突。
    /// </summary>
    private readonly record struct AssignmentFlowNode(AssignmentFlowNodeKind Kind, string Id)
    {
        public static AssignmentFlowNode Source { get; } = new(AssignmentFlowNodeKind.Source, string.Empty);
        public static AssignmentFlowNode Sink { get; } = new(AssignmentFlowNodeKind.Sink, string.Empty);

        public static AssignmentFlowNode Vehicle(string id) => new(AssignmentFlowNodeKind.Vehicle, id);
        public static AssignmentFlowNode Order(string id) => new(AssignmentFlowNodeKind.Order, id);
    }

    private sealed record CandidateAssignment(string VehicleId, string OrderId, double TravelMinutes);

    private sealed class CandidateAssignmentComparer : IComparer<CandidateAssignment>
    {
        public static CandidateAssignmentComparer Instance { get; } = new();

        public int Compare(CandidateAssignment? left, CandidateAssignment? right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left is null) return -1;
            if (right is null) return 1;

            var comparison = string.CompareOrdinal(left.VehicleId, right.VehicleId);
            return comparison != 0 ? comparison : string.CompareOrdinal(left.OrderId, right.OrderId);
        }
    }

    private sealed record DispatchEntry(
        DeliveryVehicle Vehicle,
        DeliveryOrder Order,
        CachedRoute Route);

    private sealed class DispatchEntryComparer : IComparer<DispatchEntry>
    {
        public static DispatchEntryComparer Instance { get; } = new();

        public int Compare(DispatchEntry? left, DispatchEntry? right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left is null) return -1;
            if (right is null) return 1;

            var comparison = left.Order.Deadline.CompareTo(right.Order.Deadline);
            if (comparison != 0) return comparison;

            // 最小堆会先弹出“较小”元素，所以紧急度越高，比较结果应越小。
            comparison = right.Order.Urgency.CompareTo(left.Order.Urgency);
            if (comparison != 0) return comparison;

            comparison = string.CompareOrdinal(left.Order.Id, right.Order.Id);
            return comparison != 0
                ? comparison
                : string.CompareOrdinal(left.Vehicle.Id, right.Vehicle.Id);
        }
    }

    private sealed class UnassignedOrderComparer : IComparer<UnassignedDeliveryOrder>
    {
        public static UnassignedOrderComparer Instance { get; } = new();

        public int Compare(UnassignedDeliveryOrder? left, UnassignedDeliveryOrder? right) =>
            string.CompareOrdinal(left?.OrderId, right?.OrderId);
    }

    private sealed class MaintenanceConnectionComparer : IComparer<MaintenanceConnection>
    {
        public static MaintenanceConnectionComparer Instance { get; } = new();

        public int Compare(MaintenanceConnection? left, MaintenanceConnection? right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left is null) return -1;
            if (right is null) return 1;

            var comparison = left.Cost.CompareTo(right.Cost);
            if (comparison != 0) return comparison;

            comparison = string.CompareOrdinal(left.First, right.First);
            return comparison != 0 ? comparison : string.CompareOrdinal(left.Second, right.Second);
        }
    }
}
