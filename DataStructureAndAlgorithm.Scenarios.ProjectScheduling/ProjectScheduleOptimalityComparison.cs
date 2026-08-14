namespace DataStructureAndAlgorithm.Scenarios.ProjectScheduling;

/// <summary>
/// 确定性贪心计划与分支限界最优计划的对比结果。
/// </summary>
/// <param name="GreedySchedule">现有列表调度算法生成的可行上界。</param>
/// <param name="OptimalSchedule">精确搜索证明具有最小 makespan 的计划。</param>
/// <param name="ExploredNodeCount">搜索访问的状态节点数量；包括根节点与完整解节点。</param>
/// <param name="PrunedBranchCount">因为下界不可能优于当前上界而被跳过的分支数量。</param>
/// <param name="DependencyLowerBound">只考虑依赖关系时，由关键路径给出的 makespan 下界。</param>
/// <param name="ResourceLowerBound">由总资源工作量除以容量并向上取整得到的 makespan 下界。</param>
/// <remarks>
/// 两种下界都可以独立于搜索结果重新计算。如果任一下界已经等于贪心 makespan，搜索器可立即证明
/// 贪心计划最优；否则分支限界会继续枚举可能改进上界的离散开始时间。
/// </remarks>
public sealed record ProjectScheduleOptimalityComparison(
    ProjectSchedule GreedySchedule,
    ProjectSchedule OptimalSchedule,
    long ExploredNodeCount,
    long PrunedBranchCount,
    int DependencyLowerBound,
    int ResourceLowerBound)
{
    /// <summary>精确计划比贪心计划缩短的时间单位数；0 表示贪心已经最优。</summary>
    public int MakespanImprovement => GreedySchedule.Makespan - OptimalSchedule.Makespan;

    /// <summary>贪心计划是否已经被精确搜索证明最优。</summary>
    public bool GreedyIsOptimal => MakespanImprovement == 0;
}
