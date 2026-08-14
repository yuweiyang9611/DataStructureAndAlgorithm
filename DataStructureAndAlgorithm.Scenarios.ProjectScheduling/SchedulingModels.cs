namespace DataStructureAndAlgorithm.Scenarios.ProjectScheduling;

/// <summary>
/// 一个需要调度的工作项。
/// </summary>
/// <param name="Id">项目内唯一、区分大小写的标识。</param>
/// <param name="Duration">连续占用资源的时间单位数，必须大于 0。</param>
/// <param name="ResourceDemand">执行期间同时占用的资源单位数，必须大于 0。</param>
/// <param name="Dependencies">必须先完成的工作项标识。</param>
/// <remarks>
/// 示例故意使用离散整数时间，而不是 <see cref="DateTime"/>：调度算法首先需要讲清楚
/// 半开区间、资源容量和依赖不变量，时区、节假日与工作日历应由真实应用的适配层处理。
/// </remarks>
public sealed record WorkItem(
    string Id,
    int Duration,
    int ResourceDemand,
    IReadOnlyList<string> Dependencies);

/// <summary>
/// 工作项在资源受限计划中的最终位置。
/// </summary>
/// <param name="Id">工作项标识。</param>
/// <param name="Start">开始时间，属于占用区间。</param>
/// <param name="End">结束时间，不属于占用区间；因此持续时间为 <c>End - Start</c>。</param>
/// <param name="ResourceDemand">区间 <c>[Start, End)</c> 内的资源占用量。</param>
/// <param name="CriticalRemaining">从该项开始到依赖图终点的最长剩余时长。</param>
/// <param name="IsOnCriticalPath">是否属于只考虑依赖关系时的一条确定性关键路径。</param>
public sealed record ScheduledWorkItem(
    string Id,
    int Start,
    int End,
    int ResourceDemand,
    int CriticalRemaining,
    bool IsOnCriticalPath);

/// <summary>
/// 一次调度的完整、只读结果。
/// </summary>
/// <param name="WorkItems">按开始时间稳定排序的计划；开始时间相同时保留调度顺序。</param>
/// <param name="TopologicalOrder">按标识打破平局的确定性拓扑序。</param>
/// <param name="CriticalPath">只考虑依赖、不考虑资源争用的一条最长路径。</param>
/// <param name="Makespan">所有工作项完成的最早计划结束时刻。</param>
/// <param name="PeakResourceUsage">任意一个时间槽内的最大资源占用量。</param>
/// <param name="Capacity">本次调度使用的总资源容量。</param>
public sealed record ProjectSchedule(
    IReadOnlyList<ScheduledWorkItem> WorkItems,
    IReadOnlyList<string> TopologicalOrder,
    IReadOnlyList<string> CriticalPath,
    int Makespan,
    int PeakResourceUsage,
    int Capacity);
