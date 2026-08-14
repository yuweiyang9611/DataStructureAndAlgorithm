using System.Globalization;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Graph;
using DataStructureAndAlgorithm.Hashing;
using DataStructureAndAlgorithm.Heap;
using DataStructureAndAlgorithm.Range;

namespace DataStructureAndAlgorithm.Scenarios.ProjectScheduling;

/// <summary>
/// 为带依赖和统一资源容量的项目生成确定性计划。
/// </summary>
/// <remarks>
/// <para>
/// 这是教学型“列表调度”实现，而不是保证全局最优的通用约束求解器。算法先在 DAG 上计算
/// 每个工作项的最长剩余路径，再让剩余路径更长的就绪项优先寻找可用时间段。这一启发式通常会
/// 优先保护瓶颈链，但资源受限调度本身是困难问题，因此结果不宣称一定具有最小 makespan。
/// </para>
/// <para>
/// 核心状态使用项目自己的 <see cref="OpenAddressingHashTable{TKey,TValue}"/>；拓扑排序和就绪队列
/// 使用 <see cref="BinaryMinHeap{T}"/>；区间资源上限使用 <see cref="LazyRangeAddMaxSegmentTree"/>。
/// 没有直接用 BCL Dictionary 和 PriorityQueue 代替这些结构，是为了让哈希探测、堆不变量与区间聚合
/// 真正参与一个完整应用，而不是只停留在孤立示例中。输入快照和结果排序仍可使用 List/数组/LINQ，
/// 因为它们不是本场景要重复实现的核心数据结构。
/// </para>
/// </remarks>
public sealed class ProjectScheduler
{
    private const string TraceAlgorithm = "ProjectScheduling";

    /// <summary>
    /// 精确求解允许的最大工作项数量。
    /// </summary>
    /// <remarks>
    /// 精确搜索的状态空间会随工作项数指数增长；公开限制能让调用方在进入搜索前就理解成本，
    /// 也防止教学 API 被误当成可无限扩展的生产级约束求解器。
    /// </remarks>
    public const int ExactWorkItemLimit = 8;

    /// <summary>
    /// 精确求解允许的最大离散时间轴长度。
    /// </summary>
    public const int ExactHorizonLimit = 32;

    private static readonly StringComparer IdComparer = StringComparer.Ordinal;
    private readonly int _capacity;
    private readonly IAlgorithmTraceSink? _trace;

    /// <summary>创建一个具有固定并行资源容量的调度器。</summary>
    /// <param name="capacity">任意时间槽可以同时使用的资源总量。</param>
    /// <param name="trace">
    /// 可选的教学追踪接收器。默认不追踪，避免生产式调用为日志分配额外对象；
    /// 注入后只记录阶段边界和调度决策，不逐个扫描时间槽制造噪声。
    /// </param>
    public ProjectScheduler(int capacity, IAlgorithmTraceSink? trace = null)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "资源容量必须大于 0。");
        }

        _capacity = capacity;
        _trace = BestEffortAlgorithmTraceSink.Wrap(trace);
    }

    public int Capacity => _capacity;

    /// <summary>
    /// 创建确定性资源计划。
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 工作项字段非法，或存在重复标识、重复依赖、未知依赖、自依赖、单项资源需求超限时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">依赖图包含环时抛出。</exception>
    /// <remarks>
    /// 若 n 为工作项数量、e 为依赖数量、H 为所有持续时间之和，则建图与 DAG 动态规划为
    /// O((n + e) log n)，区间寻找的最坏复杂度为 O(nH log H)，每次预订通过懒标记区间加降为 O(log H)。
    /// 仍逐个尝试候选开始时间，是为了清晰展示“查询窗口峰值再预订”的不变量；若 H 很大，
    /// 可进一步用事件压缩或维护可行窗口跳表减少候选扫描。
    /// </remarks>
    public ProjectSchedule CreateSchedule(IEnumerable<WorkItem> workItems)
    {
        ArgumentNullException.ThrowIfNull(workItems);

        // 只枚举输入一次，并复制依赖列表。这样惰性 IEnumerable 或调用方随后修改原列表，
        // 都不会让一次调度在执行中途看到不同的数据。
        var items = SnapshotAndValidateWorkItems(workItems);
        if (_trace is not null)
        {
            _trace.Record(
                TraceAlgorithm,
                "ScheduleStarted",
                "已取得不可变输入快照，准备建立依赖图。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["capacity"] = Format(_capacity),
                    ["dependencyCount"] = Format(items.Sum(item => item.Dependencies.Count)),
                    ["workItemCount"] = Format(items.Count)
                });
        }

        if (items.Count == 0)
        {
            _trace?.Record(
                TraceAlgorithm,
                "ScheduleCompleted",
                "空项目无需建立时间轴，直接返回空计划。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["makespan"] = "0",
                    ["peakResourceUsage"] = "0",
                    ["scheduledCount"] = "0"
                });
            return new ProjectSchedule([], [], [], 0, 0, _capacity);
        }

        var registry = CreateRegistry(items);
        var graph = new WeightedGraph<string>(isDirected: true, comparer: IdComparer);
        var inDegrees = new OpenAddressingHashTable<string, int>(comparer: IdComparer);

        foreach (var item in items)
        {
            graph.AddVertex(item.Id); // 孤立工作项也必须出现在拓扑序中。
            inDegrees.TryAdd(item.Id, 0);
        }

        AddAndValidateDependencies(items, registry, graph, inDegrees);
        if (_trace is not null)
        {
            _trace.Record(
                TraceAlgorithm,
                "GraphBuilt",
                "依赖已按“前置项 -> 消费者”方向加入有向图。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["edgeCount"] = Format(items.Sum(item => item.Dependencies.Count)),
                    ["vertexCount"] = Format(items.Count)
                });
        }

        var topologicalOrder = DeterministicTopologicalSort(items, graph, inDegrees);
        if (_trace is not null)
        {
            _trace.Record(
                TraceAlgorithm,
                "TopologicalSortCompleted",
                "Kahn 算法完成；最小堆用标识打破多个零入度顶点之间的平局。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["order"] = string.Join(" -> ", topologicalOrder)
                });
        }

        var (criticalRemaining, criticalPath) = CalculateCriticalRemaining(
            topologicalOrder,
            registry,
            graph,
            inDegrees);
        if (_trace is not null)
        {
            _trace.Record(
                TraceAlgorithm,
                "CriticalPathCalculated",
                "反向拓扑动态规划得到仅考虑依赖关系时的一条确定性最长路径。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["duration"] = Format(criticalPath.Sum(id => registry[id].Duration)),
                    ["path"] = string.Join(" -> ", criticalPath)
                });
        }

        var scheduledItems = BuildResourceConstrainedSchedule(
            items,
            registry,
            graph,
            inDegrees,
            criticalRemaining,
            criticalPath,
            out var makespan,
            out var peakResourceUsage);

        if (_trace is not null)
        {
            _trace.Record(
                TraceAlgorithm,
                "ScheduleCompleted",
                "所有工作项均满足依赖与资源上限，并已放入离散时间轴。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["makespan"] = Format(makespan),
                    ["peakResourceUsage"] = Format(peakResourceUsage),
                    ["scheduledCount"] = Format(scheduledItems.Count)
                });
        }

        return new ProjectSchedule(
            scheduledItems,
            Array.AsReadOnly(topologicalOrder.ToArray()),
            criticalPath,
            makespan,
            peakResourceUsage,
            _capacity);
    }

    /// <summary>
    /// 对受控的小规模输入返回可证明最短工期的计划。
    /// </summary>
    /// <exception cref="ArgumentException">输入非法或超过公开规模边界时抛出。</exception>
    /// <exception cref="InvalidOperationException">
    /// 依赖图有环，或分支限界在安全节点预算内未能穷尽证明空间时抛出；后一种情况不会返回一个冒充最优的计划。
    /// </exception>
    /// <remarks>
    /// 该方法故意复用 <see cref="CompareWithOptimalSchedule"/>，因为“最优计划”只有和可行上界、
    /// 依赖/资源下界以及搜索统计放在一起时，才容易解释它为何确实最优。只取计划的调用方可用此便捷入口。
    /// </remarks>
    public ProjectSchedule CreateOptimalSchedule(IEnumerable<WorkItem> workItems) =>
        CompareWithOptimalSchedule(workItems).OptimalSchedule;

    /// <summary>
    /// 同时运行确定性贪心调度和小规模分支限界精确搜索，返回可核验的最优性比较。
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 输入字段非法，或工作项数量/离散时间轴超过精确搜索的公开安全限制时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// 依赖图包含环，或搜索访问超过内部状态节点预算、尚未完成最优性证明时抛出。
    /// </exception>
    /// <remarks>
    /// 先生成贪心计划有两个原因：它既是实际可用结果，也是精确搜索的初始上界。分支限界随后用
    /// 依赖最长路径下界与总资源工作量下界剪枝；当所有未搜索分支的下界都不小于当前最好上界时，
    /// “没有更短计划”就由搜索过程持续证明。若访问超过 2,000,000 个状态仍未穷尽，算法明确失败；
    /// 启用追踪时还会记录 ExactSearchAborted，而不会把当前最好上界包装成最优结果。
    /// </remarks>
    public ProjectScheduleOptimalityComparison CompareWithOptimalSchedule(IEnumerable<WorkItem> workItems)
    {
        ArgumentNullException.ThrowIfNull(workItems);

        // 先取得稳定快照，避免惰性枚举让贪心与精确算法看到两组不同输入。CreateSchedule 作为公开边界会
        // 防御性地再次校验这个小规模快照；这里接受少量重复工作，换取所有公开入口始终执行同一套领域校验，
        // 而不提供一个可能被误用的“跳过验证”旁路。
        var items = SnapshotAndValidateWorkItems(workItems);
        ExactScheduleOptimizer.ValidateScale(items);
        var greedySchedule = CreateSchedule(items);
        return new ExactScheduleOptimizer(_capacity, _trace).Compare(items, greedySchedule);
    }

    private List<WorkItem> SnapshotAndValidateWorkItems(IEnumerable<WorkItem> workItems)
    {
        var result = new List<WorkItem>();

        foreach (var item in workItems)
        {
            if (item is null)
            {
                throw new ArgumentException("工作项集合不能包含 null。", nameof(workItems));
            }

            if (string.IsNullOrWhiteSpace(item.Id))
            {
                throw new ArgumentException("工作项标识不能为空或空白。", nameof(workItems));
            }

            if (item.Duration <= 0)
            {
                throw new ArgumentException($"工作项 '{item.Id}' 的持续时间必须大于 0。", nameof(workItems));
            }

            if (item.ResourceDemand <= 0)
            {
                throw new ArgumentException($"工作项 '{item.Id}' 的资源需求必须大于 0。", nameof(workItems));
            }

            if (item.ResourceDemand > _capacity)
            {
                throw new ArgumentException(
                    $"工作项 '{item.Id}' 需要 {item.ResourceDemand} 个资源单位，超过总容量 {_capacity}。",
                    nameof(workItems));
            }

            if (item.Dependencies is null)
            {
                throw new ArgumentException($"工作项 '{item.Id}' 的依赖集合不能为 null。", nameof(workItems));
            }

            var dependencies = new string[item.Dependencies.Count];
            for (var index = 0; index < item.Dependencies.Count; index++)
            {
                var dependency = item.Dependencies[index];
                if (string.IsNullOrWhiteSpace(dependency))
                {
                    throw new ArgumentException($"工作项 '{item.Id}' 包含空依赖标识。", nameof(workItems));
                }

                dependencies[index] = dependency;
            }

            result.Add(item with { Dependencies = Array.AsReadOnly(dependencies) });
        }

        return result;
    }

    private static OpenAddressingHashTable<string, WorkItem> CreateRegistry(IReadOnlyList<WorkItem> items)
    {
        var registry = new OpenAddressingHashTable<string, WorkItem>(comparer: IdComparer);
        foreach (var item in items)
        {
            if (!registry.TryAdd(item.Id, item))
            {
                throw new ArgumentException($"工作项标识 '{item.Id}' 重复。", nameof(items));
            }
        }

        return registry;
    }

    private static void AddAndValidateDependencies(
        IReadOnlyList<WorkItem> items,
        OpenAddressingHashTable<string, WorkItem> registry,
        WeightedGraph<string> graph,
        OpenAddressingHashTable<string, int> inDegrees)
    {
        foreach (var item in items)
        {
            // 每个工作项用一个开放寻址表检测重复依赖；若直接交给 WeightedGraph.AddEdge，
            // 虽然也会失败，但这里可以给出包含工作项标识的领域错误。
            var seenDependencies = new OpenAddressingHashTable<string, byte>(comparer: IdComparer);
            foreach (var dependency in item.Dependencies)
            {
                if (IdComparer.Equals(item.Id, dependency))
                {
                    throw new ArgumentException($"工作项 '{item.Id}' 不能依赖自身。", nameof(items));
                }

                if (!registry.ContainsKey(dependency))
                {
                    throw new ArgumentException(
                        $"工作项 '{item.Id}' 依赖未知工作项 '{dependency}'。",
                        nameof(items));
                }

                if (!seenDependencies.TryAdd(dependency, 0))
                {
                    throw new ArgumentException(
                        $"工作项 '{item.Id}' 重复声明依赖 '{dependency}'。",
                        nameof(items));
                }

                // 边方向必须是“前置项 -> 消费者”，这样入度为 0 才表示可以立即执行。
                // 拓扑排序不读取权重，这里使用 1 仅满足 WeightedGraph 的通用边模型。
                graph.AddEdge(dependency, item.Id, 1);
                inDegrees[item.Id] = checked(inDegrees[item.Id] + 1);
            }
        }
    }

    private static List<string> DeterministicTopologicalSort(
        IReadOnlyList<WorkItem> items,
        WeightedGraph<string> graph,
        OpenAddressingHashTable<string, int> inDegrees)
    {
        var remainingInDegrees = CopyIntegerTable(items, inDegrees);
        var zeroInDegree = new BinaryMinHeap<string>(IdComparer);

        foreach (var item in items)
        {
            if (remainingInDegrees[item.Id] == 0)
            {
                zeroInDegree.Enqueue(item.Id);
            }
        }

        var order = new List<string>(items.Count);
        while (zeroInDegree.Count > 0)
        {
            var current = zeroInDegree.Dequeue();
            order.Add(current);

            foreach (var edge in graph.GetOutgoingEdges(current))
            {
                var nextDegree = remainingInDegrees[edge.To] - 1;
                remainingInDegrees[edge.To] = nextDegree;
                if (nextDegree == 0)
                {
                    zeroInDegree.Enqueue(edge.To);
                }
            }
        }

        // Kahn 算法的不变量：每次只删除入度为 0 的顶点。若最终仍有顶点未删除，
        // 那些顶点之间必然构成环，因而不存在合法项目计划。
        if (order.Count != items.Count)
        {
            throw new InvalidOperationException("工作项依赖图包含环，无法生成拓扑序和项目计划。");
        }

        return order;
    }

    private static (
        OpenAddressingHashTable<string, int> CriticalRemaining,
        IReadOnlyList<string> CriticalPath) CalculateCriticalRemaining(
        IReadOnlyList<string> topologicalOrder,
        OpenAddressingHashTable<string, WorkItem> registry,
        WeightedGraph<string> graph,
        OpenAddressingHashTable<string, int> inDegrees)
    {
        var criticalRemaining = new OpenAddressingHashTable<string, int>(comparer: IdComparer);
        var nextOnCriticalPath = new OpenAddressingHashTable<string, string?>(comparer: IdComparer);

        // 反向拓扑 DP 不变量：处理 current 时，它的所有后继都已经有答案。
        // dp[current] = duration[current] + max(dp[child])。
        for (var index = topologicalOrder.Count - 1; index >= 0; index--)
        {
            var current = topologicalOrder[index];
            var longestChild = 0;
            string? selectedChild = null;

            foreach (var edge in graph.GetOutgoingEdges(current))
            {
                var candidate = criticalRemaining[edge.To];
                if (candidate > longestChild ||
                    candidate == longestChild &&
                    (selectedChild is null || IdComparer.Compare(edge.To, selectedChild) < 0))
                {
                    longestChild = candidate;
                    selectedChild = edge.To;
                }
            }

            var remaining = checked(registry[current].Duration + longestChild);
            criticalRemaining.TryAdd(current, remaining);
            nextOnCriticalPath.TryAdd(current, selectedChild);
        }

        // 关键路径应从一个源点开始。持续时间均为正，因此最长源路径也是整个 DAG 的最长路径。
        string? criticalStart = null;
        var longestPath = -1;
        foreach (var current in topologicalOrder)
        {
            if (inDegrees[current] != 0) continue;
            var candidate = criticalRemaining[current];
            if (candidate > longestPath ||
                candidate == longestPath &&
                (criticalStart is null || IdComparer.Compare(current, criticalStart) < 0))
            {
                longestPath = candidate;
                criticalStart = current;
            }
        }

        if (criticalStart is null)
        {
            throw new InvalidOperationException("内部错误：非空 DAG 没有源点。");
        }

        var path = new List<string>();
        var cursor = criticalStart;
        while (cursor is not null)
        {
            path.Add(cursor);
            nextOnCriticalPath.TryGetValue(cursor, out cursor);
        }

        return (criticalRemaining, Array.AsReadOnly(path.ToArray()));
    }

    private List<ScheduledWorkItem> BuildResourceConstrainedSchedule(
        IReadOnlyList<WorkItem> items,
        OpenAddressingHashTable<string, WorkItem> registry,
        WeightedGraph<string> graph,
        OpenAddressingHashTable<string, int> inDegrees,
        OpenAddressingHashTable<string, int> criticalRemaining,
        IReadOnlyList<string> criticalPath,
        out int makespan,
        out int peakResourceUsage)
    {
        var horizon = 0;
        foreach (var item in items)
        {
            // 所有任务串行执行一定可行，所以持续时间之和是安全的离散时间轴上界。
            horizon = checked(horizon + item.Duration);
        }

        var usageMaximum = new LazyRangeAddMaxSegmentTree(new long[horizon]);
        var remainingDependencies = CopyIntegerTable(items, inDegrees);
        var startTimes = new OpenAddressingHashTable<string, int>(comparer: IdComparer);
        var endTimes = new OpenAddressingHashTable<string, int>(comparer: IdComparer);
        var criticalIds = new OpenAddressingHashTable<string, byte>(comparer: IdComparer);
        foreach (var id in criticalPath) criticalIds.TryAdd(id, 0);

        var ready = new BinaryMinHeap<ReadyWorkItem>(ReadyWorkItemComparer.Instance);
        foreach (var item in items)
        {
            if (remainingDependencies[item.Id] == 0)
            {
                ready.Enqueue(new ReadyWorkItem(item.Id, criticalRemaining[item.Id]));
            }
        }

        var result = new List<ScheduledWorkItem>(items.Count);
        makespan = 0;

        while (ready.Count > 0)
        {
            var entry = ready.Dequeue();
            var item = registry[entry.Id];

            // 前置完成时间给出依赖意义上的最早开始时间。由于一个工作项只有在所有前置项
            // 都已被安排后才进入 ready 堆，这里的每次 endTimes 查找都必然成功。
            var earliestStart = 0;
            foreach (var dependency in item.Dependencies)
            {
                earliestStart = Math.Max(earliestStart, endTimes[dependency]);
            }

            var start = FindEarliestCapacityWindow(
                usageMaximum,
                earliestStart,
                item.Duration,
                item.ResourceDemand,
                horizon);
            var end = checked(start + item.Duration);
            ReserveCapacity(usageMaximum, start, end, item.ResourceDemand);

            startTimes.TryAdd(item.Id, start);
            endTimes.TryAdd(item.Id, end);
            makespan = Math.Max(makespan, end);
            result.Add(new ScheduledWorkItem(
                item.Id,
                start,
                end,
                item.ResourceDemand,
                entry.CriticalRemaining,
                criticalIds.ContainsKey(item.Id)));

            if (_trace is not null)
            {
                // 只记录最终选中的窗口，而不记录 FindEarliestCapacityWindow 检查过的每个时间槽。
                // 前者能解释“为什么任务落在这里”，后者会让学习轨迹被大量重复事件淹没。
                _trace.Record(
                    TraceAlgorithm,
                    "TaskScheduled",
                    "就绪堆取出的工作项已预订最早可行资源窗口。",
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["criticalRemaining"] = Format(entry.CriticalRemaining),
                        ["demand"] = Format(item.ResourceDemand),
                        ["earliestByDependencies"] = Format(earliestStart),
                        ["end"] = Format(end),
                        ["id"] = item.Id,
                        ["start"] = Format(start)
                    });
            }

            // 第二次 Kahn 过程用于维护“所有前置项都已经排入时间轴”的 ready 不变量。
            foreach (var edge in graph.GetOutgoingEdges(item.Id))
            {
                var remaining = remainingDependencies[edge.To] - 1;
                remainingDependencies[edge.To] = remaining;
                if (remaining == 0)
                {
                    ready.Enqueue(new ReadyWorkItem(edge.To, criticalRemaining[edge.To]));
                }
            }
        }

        if (result.Count != items.Count)
        {
            throw new InvalidOperationException("内部错误：合法 DAG 中仍有工作项未被调度。");
        }

        peakResourceUsage = checked((int)usageMaximum.QueryMax(0, horizon));

        // LINQ OrderBy 是稳定排序：相同开始时间保留 ready 堆产生的确定性调度顺序。
        // 排序仅用于呈现，不参与调度核心，因此不会隐藏需要学习的数据结构。
        return result.OrderBy(item => item.Start).ToList();
    }

    private int FindEarliestCapacityWindow(
        LazyRangeAddMaxSegmentTree usageMaximum,
        int earliestStart,
        int duration,
        int demand,
        int horizon)
    {
        var latestStart = horizon - duration;
        for (var start = earliestStart; start <= latestStart; start++)
        {
            // 懒标记树保存每个时间槽的占用量，并以最大值聚合；查询前会把祖先的延迟增量下推。
            // 只要窗口峰值 <= capacity - demand，整段增加 demand 后仍不会超过容量。
            if (usageMaximum.QueryMax(start, start + duration) <= _capacity - demand)
            {
                return start;
            }
        }

        // 每个单项需求都已验证不超过容量，把所有项串行放置必然可行；若走到这里，说明实现不变量损坏。
        throw new InvalidOperationException("内部错误：在安全时间轴上没有找到可行资源区间。");
    }

    private static void ReserveCapacity(LazyRangeAddMaxSegmentTree usageMaximum, int start, int end, int demand)
    {
        // 半开区间 [start, end) 与计划模型一致；一次区间加替代逐时间槽更新，复杂度从 O(duration log H) 降到 O(log H)。
        usageMaximum.RangeAdd(start, end, demand);
    }

    private static OpenAddressingHashTable<string, int> CopyIntegerTable(
        IReadOnlyList<WorkItem> items,
        OpenAddressingHashTable<string, int> source)
    {
        var copy = new OpenAddressingHashTable<string, int>(comparer: IdComparer);
        foreach (var item in items)
        {
            copy.TryAdd(item.Id, source[item.Id]);
        }

        return copy;
    }

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    private readonly record struct ReadyWorkItem(string Id, int CriticalRemaining);

    private sealed class ReadyWorkItemComparer : IComparer<ReadyWorkItem>
    {
        public static ReadyWorkItemComparer Instance { get; } = new();

        public int Compare(ReadyWorkItem first, ReadyWorkItem second)
        {
            // BinaryMinHeap 会先取“较小”元素，所以交换比较方向，让剩余关键路径更长的项先出队。
            var byCriticalRemaining = second.CriticalRemaining.CompareTo(first.CriticalRemaining);
            return byCriticalRemaining != 0
                ? byCriticalRemaining
                : IdComparer.Compare(first.Id, second.Id);
        }
    }
}
