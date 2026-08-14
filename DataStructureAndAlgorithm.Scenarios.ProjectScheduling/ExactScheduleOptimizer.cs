using System.Globalization;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Hashing;
using DataStructureAndAlgorithm.Range;

namespace DataStructureAndAlgorithm.Scenarios.ProjectScheduling;

/// <summary>
/// 为小规模资源受限项目执行确定性分支限界搜索。
/// </summary>
/// <remarks>
/// 该类型故意保持为场景程序集的内部实现：调用方只需要通过 <see cref="ProjectScheduler"/> 获得
/// 经过统一校验的对比结果，不应依赖搜索器的递归顺序等实现细节。
/// </remarks>
internal sealed class ExactScheduleOptimizer
{
    private const string TraceAlgorithm = "ProjectScheduling";
    private const long DefaultSearchNodeLimit = 2_000_000;
    private static readonly StringComparer IdComparer = StringComparer.Ordinal;

    private readonly int _capacity;
    private readonly IAlgorithmTraceSink? _trace;
    private readonly long _searchNodeLimit;

    internal ExactScheduleOptimizer(
        int capacity,
        IAlgorithmTraceSink? trace,
        long searchNodeLimit = DefaultSearchNodeLimit)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(searchNodeLimit);
        _capacity = capacity;
        _trace = trace;
        _searchNodeLimit = searchNodeLimit;
    }

    /// <summary>
    /// 在创建指数级搜索状态前检查输入规模。
    /// </summary>
    internal static void ValidateScale(IReadOnlyList<WorkItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count > ProjectScheduler.ExactWorkItemLimit)
        {
            throw new ArgumentException(
                $"精确调度最多支持 {ProjectScheduler.ExactWorkItemLimit} 个工作项，实际为 {items.Count}。" +
                "更大项目请使用 CreateSchedule 的确定性贪心计划。",
                nameof(items));
        }

        long horizon = 0;
        foreach (var item in items)
        {
            horizon += item.Duration;
            if (horizon > ProjectScheduler.ExactHorizonLimit)
            {
                throw new ArgumentException(
                    $"精确调度的持续时间总和最多为 {ProjectScheduler.ExactHorizonLimit}，实际至少为 {horizon}。" +
                    "更长时间轴请使用 CreateSchedule，或先按业务事件压缩时间刻度。",
                    nameof(items));
            }
        }
    }

    internal ProjectScheduleOptimalityComparison Compare(
        IReadOnlyList<WorkItem> items,
        ProjectSchedule greedySchedule)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(greedySchedule);

        var dependencyLowerBound = CalculateDependencyLowerBound(items, greedySchedule);
        var resourceLowerBound = CalculateResourceLowerBound(items);
        if (_trace is not null)
        {
            _trace.Record(
                TraceAlgorithm,
                "ExactSearchStarted",
                "以贪心计划为可行上界，并用关键路径与资源工作量建立分支限界下界。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["dependencyLowerBound"] = Format(dependencyLowerBound),
                    ["greedyMakespan"] = Format(greedySchedule.Makespan),
                    ["resourceLowerBound"] = Format(resourceLowerBound),
                    ["workItemCount"] = Format(items.Count)
                });
        }

        if (items.Count == 0)
        {
            var emptyComparison = new ProjectScheduleOptimalityComparison(
                greedySchedule,
                greedySchedule,
                ExploredNodeCount: 1,
                PrunedBranchCount: 0,
                dependencyLowerBound,
                resourceLowerBound);
            RecordCompleted(emptyComparison);
            return emptyComparison;
        }

        var registry = new OpenAddressingHashTable<string, WorkItem>(comparer: IdComparer);
        var orderIndex = new OpenAddressingHashTable<string, int>(comparer: IdComparer);
        foreach (var item in items)
        {
            // 工作项已经由 ProjectScheduler 统一校验；这里的 TryAdd 失败只可能表示内部调用契约损坏。
            if (!registry.TryAdd(item.Id, item))
            {
                throw new InvalidOperationException($"内部错误：精确搜索收到重复工作项 '{item.Id}'。");
            }
        }

        var orderedItems = new WorkItem[greedySchedule.TopologicalOrder.Count];
        for (var index = 0; index < orderedItems.Length; index++)
        {
            var id = greedySchedule.TopologicalOrder[index];
            orderedItems[index] = registry[id];
            orderIndex.TryAdd(id, index);
        }

        // 把字符串依赖提前解析为拓扑序下标。递归热路径只访问数组，既减少哈希查找，也让
        // “每个依赖下标都小于当前下标”的拓扑不变量一目了然。
        var dependencyIndices = new int[orderedItems.Length][];
        for (var index = 0; index < orderedItems.Length; index++)
        {
            var item = orderedItems[index];
            dependencyIndices[index] = new int[item.Dependencies.Count];
            for (var dependencyIndex = 0; dependencyIndex < item.Dependencies.Count; dependencyIndex++)
            {
                var predecessorIndex = orderIndex[item.Dependencies[dependencyIndex]];
                if (predecessorIndex >= index)
                {
                    throw new InvalidOperationException("内部错误：精确搜索收到的顺序不是合法拓扑序。");
                }

                dependencyIndices[index][dependencyIndex] = predecessorIndex;
            }
        }

        var greedyById = greedySchedule.WorkItems.ToDictionary(item => item.Id, IdComparer);
        var criticalRemaining = new int[orderedItems.Length];
        var bestStarts = new int[orderedItems.Length];
        for (var index = 0; index < orderedItems.Length; index++)
        {
            var greedyItem = greedyById[orderedItems[index].Id];
            criticalRemaining[index] = greedyItem.CriticalRemaining;
            bestStarts[index] = greedyItem.Start;
        }

        var horizon = items.Sum(item => item.Duration);
        var usageMaximum = new LazyRangeAddMaxSegmentTree(new long[horizon]);
        var currentStarts = new int[orderedItems.Length];
        var currentEnds = new int[orderedItems.Length];
        var bestMakespan = greedySchedule.Makespan;
        long exploredNodeCount = 0;
        long prunedBranchCount = 0;

        // 下界达到上界时，贪心解已经被证明最优，根节点即可剪枝；否则枚举所有可能改进上界的开始时间。
        Search(
            index: 0,
            currentMakespan: 0,
            lowerBound: Math.Max(dependencyLowerBound, resourceLowerBound));

        var optimalSchedule = bestMakespan == greedySchedule.Makespan
            ? greedySchedule
            : BuildSchedule(
                orderedItems,
                bestStarts,
                criticalRemaining,
                greedySchedule,
                bestMakespan,
                horizon);
        var comparison = new ProjectScheduleOptimalityComparison(
            greedySchedule,
            optimalSchedule,
            exploredNodeCount,
            prunedBranchCount,
            dependencyLowerBound,
            resourceLowerBound);
        RecordCompleted(comparison);
        return comparison;

        void Search(int index, int currentMakespan, int lowerBound)
        {
            exploredNodeCount++;
            if (exploredNodeCount > _searchNodeLimit)
            {
                RecordAborted(exploredNodeCount, bestMakespan);
                throw new InvalidOperationException(
                    $"精确调度已访问超过 {_searchNodeLimit:N0} 个状态节点。" +
                    "为避免教学进程失控，搜索已中止；请缩小输入或使用 CreateSchedule。");
            }

            // makespan 是正则目标：下界等于当前可行上界时也不必继续，因为我们已经拥有同样好的完整计划。
            if (lowerBound >= bestMakespan)
            {
                prunedBranchCount++;
                return;
            }

            if (index == orderedItems.Length)
            {
                var previousBest = bestMakespan;
                bestMakespan = currentMakespan;
                Array.Copy(currentStarts, bestStarts, currentStarts.Length);
                RecordImproved(previousBest, bestMakespan, orderedItems, bestStarts);
                return;
            }

            var item = orderedItems[index];
            var earliestStart = 0;
            foreach (var predecessorIndex in dependencyIndices[index])
            {
                earliestStart = Math.Max(earliestStart, currentEnds[predecessorIndex]);
            }

            // 只寻找严格优于当前上界的计划。整数时间下 end < bestMakespan，
            // 等价于 start <= bestMakespan - duration - 1。
            var latestStart = bestMakespan - item.Duration - 1;
            var foundFeasibleBranch = false;
            for (var start = earliestStart; start <= latestStart; start++)
            {
                var end = checked(start + item.Duration);
                if (end >= bestMakespan ||
                    usageMaximum.QueryMax(start, end) > _capacity - item.ResourceDemand)
                {
                    continue;
                }

                foundFeasibleBranch = true;
                currentStarts[index] = start;
                currentEnds[index] = end;
                usageMaximum.RangeAdd(start, end, item.ResourceDemand);

                // 若当前项位于一条从自身到终点的最长后继链上，那么它结束后至少还需要
                // CriticalRemaining - Duration 个时间单位。该下界使用真实 end，通常比静态关键路径更强。
                var branchLowerBound = Math.Max(
                    lowerBound,
                    checked(end + criticalRemaining[index] - item.Duration));
                Search(index + 1, Math.Max(currentMakespan, end), branchLowerBound);

                // 回溯必须与预订完全对称。区间加法支持负增量，所以撤销仍为 O(log H)，
                // 无需复制整棵树，也不会把一个分支的资源状态泄漏给下一个分支。
                usageMaximum.RangeAdd(start, end, -item.ResourceDemand);
            }

            if (!foundFeasibleBranch)
            {
                prunedBranchCount++;
            }
        }
    }

    private ProjectSchedule BuildSchedule(
        IReadOnlyList<WorkItem> orderedItems,
        IReadOnlyList<int> starts,
        IReadOnlyList<int> criticalRemaining,
        ProjectSchedule greedySchedule,
        int makespan,
        int horizon)
    {
        var criticalIds = new OpenAddressingHashTable<string, byte>(comparer: IdComparer);
        foreach (var id in greedySchedule.CriticalPath)
        {
            criticalIds.TryAdd(id, 0);
        }

        var usageMaximum = new LazyRangeAddMaxSegmentTree(new long[horizon]);
        var scheduled = new List<ScheduledWorkItem>(orderedItems.Count);
        for (var index = 0; index < orderedItems.Count; index++)
        {
            var item = orderedItems[index];
            var start = starts[index];
            var end = checked(start + item.Duration);
            usageMaximum.RangeAdd(start, end, item.ResourceDemand);
            scheduled.Add(new ScheduledWorkItem(
                item.Id,
                start,
                end,
                item.ResourceDemand,
                criticalRemaining[index],
                criticalIds.ContainsKey(item.Id)));
        }

        // OrderBy 是稳定排序，因此同一开始时间下仍保持确定性拓扑序；排序只改变展示顺序，不改变计划含义。
        var orderedResult = scheduled.OrderBy(item => item.Start).ToArray();
        var peakResourceUsage = checked((int)usageMaximum.QueryMax(0, horizon));
        return new ProjectSchedule(
            Array.AsReadOnly(orderedResult),
            greedySchedule.TopologicalOrder,
            greedySchedule.CriticalPath,
            makespan,
            peakResourceUsage,
            _capacity);
    }

    private static int CalculateDependencyLowerBound(
        IReadOnlyList<WorkItem> items,
        ProjectSchedule greedySchedule)
    {
        if (items.Count == 0)
        {
            return 0;
        }

        var durationById = items.ToDictionary(item => item.Id, item => item.Duration, IdComparer);
        return greedySchedule.CriticalPath.Sum(id => durationById[id]);
    }

    private int CalculateResourceLowerBound(IReadOnlyList<WorkItem> items)
    {
        long resourceWork = 0;
        foreach (var item in items)
        {
            resourceWork = checked(resourceWork + (long)item.Duration * item.ResourceDemand);
        }

        return checked((int)((resourceWork + _capacity - 1) / _capacity));
    }

    private void RecordImproved(
        int previousMakespan,
        int makespan,
        IReadOnlyList<WorkItem> orderedItems,
        IReadOnlyList<int> starts)
    {
        if (_trace is null)
        {
            return;
        }

        _trace.Record(
            TraceAlgorithm,
            "OptimalIncumbentImproved",
            "一个完整可行计划缩短了当前 makespan 上界，后续分支将使用更强的剪枝条件。",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["makespan"] = Format(makespan),
                ["previousMakespan"] = Format(previousMakespan),
                ["starts"] = string.Join(
                    ", ",
                    orderedItems.Select((item, index) => $"{item.Id}@{Format(starts[index])}"))
            });
    }

    private void RecordAborted(long exploredNodeCount, int bestMakespan)
    {
        if (_trace is null)
        {
            return;
        }

        _trace.Record(
            TraceAlgorithm,
            "ExactSearchAborted",
            "搜索节点预算已耗尽；为保证返回值不会冒充最优解，本次调用以异常结束。",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bestMakespan"] = Format(bestMakespan),
                ["exploredNodeCount"] = Format(exploredNodeCount),
                ["nodeLimit"] = Format(_searchNodeLimit)
            });
    }

    private void RecordCompleted(ProjectScheduleOptimalityComparison comparison)
    {
        if (_trace is null)
        {
            return;
        }

        _trace.Record(
            TraceAlgorithm,
            "ExactSearchCompleted",
            "分支限界搜索已经穷尽所有可能改进当前上界的分支，最优 makespan 得到证明。",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["exploredNodeCount"] = Format(comparison.ExploredNodeCount),
                ["greedyIsOptimal"] = comparison.GreedyIsOptimal ? "true" : "false",
                ["greedyMakespan"] = Format(comparison.GreedySchedule.Makespan),
                ["makespanImprovement"] = Format(comparison.MakespanImprovement),
                ["optimalMakespan"] = Format(comparison.OptimalSchedule.Makespan),
                ["prunedBranchCount"] = Format(comparison.PrunedBranchCount)
            });
    }

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Format(long value) => value.ToString(CultureInfo.InvariantCulture);
}
