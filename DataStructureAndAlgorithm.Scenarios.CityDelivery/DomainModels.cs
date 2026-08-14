namespace DataStructureAndAlgorithm.Scenarios.CityDelivery;

/// <summary>
/// 配送地图中的一个路口或配送站。
/// </summary>
/// <param name="Id">
/// 位置的稳定标识。第一版刻意使用区分大小写的标识，避免不同数据源的隐式大小写规则造成歧义。
/// </param>
public sealed record DeliveryLocation(string Id);

/// <summary>
/// 一条物理道路。交通图可以把它展开成一条或两条有向边，维护图始终把它看作一条无向边。
/// </summary>
/// <param name="From">道路起点。</param>
/// <param name="To">道路终点。</param>
/// <param name="TravelMinutes">当前地图版本下通过道路所需的分钟数。</param>
/// <param name="MaintenanceCost">把道路纳入基础维护网络的成本。</param>
/// <param name="IsBidirectional">是否允许双向通行。</param>
/// <param name="IsOpen">当前地图版本中道路是否开放。</param>
public sealed record DeliveryRoad(
    string From,
    string To,
    double TravelMinutes,
    double MaintenanceCost,
    bool IsBidirectional = true,
    bool IsOpen = true);

/// <summary>
/// 一辆当前可参与一轮调度的配送车辆。
/// </summary>
/// <remarks>
/// 第一版遵循“一车一单”：每辆车在一次 <see cref="CityDeliveryPlanner.Plan"/> 调用中至多匹配一个订单。
/// 这让二分图匹配的约束与业务含义完全一致，不会把多站路线问题伪装成简单匹配问题。
/// </remarks>
public sealed record DeliveryVehicle(
    string Id,
    string StartLocationId,
    int Capacity,
    IReadOnlyList<string> Skills);

/// <summary>
/// 一个待配送订单。
/// </summary>
/// <param name="Id">订单稳定标识。</param>
/// <param name="DestinationId">配送目的地。</param>
/// <param name="Volume">占用的车辆容量。</param>
/// <param name="Deadline">期望送达的绝对时间。</param>
/// <param name="Urgency">非负紧急度；值越大，同截止时间下越优先。</param>
/// <param name="RequiredSkill">可选技能，例如 ColdChain；为 <see langword="null"/> 时表示无技能要求。</param>
public sealed record DeliveryOrder(
    string Id,
    string DestinationId,
    int Volume,
    DateTimeOffset Deadline,
    int Urgency = 0,
    string? RequiredSkill = null);

/// <summary>
/// 城市配送场景的一次不可变输入快照。
/// </summary>
/// <param name="MapVersion">
/// 道路开放状态或权重变化时必须递增。路线缓存把版本放入键中，从而不会跨版本复用旧路线。
/// </param>
/// <param name="PlanningTime">本轮调度的统一起算时间，避免在算法中多次读取系统时钟而产生非确定结果。</param>
public sealed record CityDeliveryScenario(
    long MapVersion,
    DateTimeOffset PlanningTime,
    IReadOnlyList<DeliveryLocation> Locations,
    IReadOnlyList<DeliveryRoad> Roads,
    IReadOnlyList<DeliveryVehicle> Vehicles,
    IReadOnlyList<DeliveryOrder> Orders);

/// <summary>
/// 已分配订单及其实际道路路径。
/// </summary>
public sealed record DeliveryAssignment(
    string VehicleId,
    string OrderId,
    IReadOnlyList<string> Path,
    double TravelMinutes,
    DateTimeOffset EstimatedArrival,
    DateTimeOffset Deadline);

/// <summary>
/// 未分配订单及面向学习者的原因说明。
/// </summary>
public sealed record UnassignedDeliveryOrder(string OrderId, string Reason);

/// <summary>
/// 最小维护森林中的一条道路。
/// </summary>
public sealed record MaintenanceConnection(string First, string Second, double Cost);

/// <summary>
/// 使用 Prim 算法得到的城市道路最低维护森林摘要。
/// </summary>
/// <remarks>
/// 当 <see cref="ComponentCount"/> 大于 1 时结果是森林而不是单棵生成树；这代表当前开放道路并不完全连通。
/// </remarks>
public sealed record MaintenanceForestPlan(
    IReadOnlyList<MaintenanceConnection> Connections,
    double TotalCost,
    int ComponentCount);

/// <summary>
/// 一轮城市配送调度的完整结果。
/// </summary>
public sealed record CityDeliveryPlan(
    long MapVersion,
    DateTimeOffset PlanningTime,
    IReadOnlyList<DeliveryAssignment> Assignments,
    IReadOnlyList<UnassignedDeliveryOrder> UnassignedOrders,
    MaintenanceForestPlan MaintenanceForest,
    int RouteCacheHits,
    int RouteCacheMisses)
{
    /// <summary>
    /// 所有已分配任务的总行程时间，也是 V2 成本优化阶段使用的目标值。
    /// </summary>
    /// <remarks>
    /// 这是由 <see cref="Assignments"/> 推导出的只读属性，而不是额外存储的状态，避免调用者手工构造计划时
    /// 传入与任务明细矛盾的总数。把属性添加在主构造函数之外，也保持现有七参数构造调用完全兼容。
    /// </remarks>
    public double TotalTravelMinutes => Assignments.Sum(assignment => assignment.TravelMinutes);
}
