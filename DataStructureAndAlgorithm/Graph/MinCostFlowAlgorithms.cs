using DataStructureAndAlgorithm.Heap;

namespace DataStructureAndAlgorithm.Graph;

/// <summary>
/// 最小费用流网络中的一条原始有向边。
/// </summary>
/// <param name="From">边的起点。</param>
/// <param name="To">边的终点。</param>
/// <param name="Capacity">输入容量。</param>
/// <param name="Flow">算法最终在该边上发送的流量。</param>
/// <param name="UnitCost">每发送一个单位流量需要支付的成本。</param>
public readonly record struct MinCostFlowEdgeResult<TVertex>(
    TVertex From,
    TVertex To,
    long Capacity,
    long Flow,
    double UnitCost)
    where TVertex : notnull;

/// <summary>
/// 最小费用最大流结果。
/// </summary>
/// <param name="Flow">从源点到汇点能够发送的最大流量。</param>
/// <param name="Cost">在达到 <paramref name="Flow"/> 的所有方案中最小的总成本。</param>
/// <param name="Edges">按输入顺序返回的原始边及其最终流量；反向残量边不会暴露给调用者。</param>
public sealed record MinimumCostMaximumFlowResult<TVertex>(
    long Flow,
    double Cost,
    IReadOnlyList<MinCostFlowEdgeResult<TVertex>> Edges)
    where TVertex : notnull;

/// <summary>
/// 容量为整数、单位成本为非负实数的最小费用流网络。
/// </summary>
/// <remarks>
/// <para>
/// 容量使用 <see cref="long"/>，因为“一个订单”“一件货物”等离散资源不应承受浮点累计误差；
/// 成本使用 <see cref="double"/>，以便直接表达分钟、距离或金额等可能带小数的业务指标。
/// </para>
/// <para>
/// 原始边要求非负成本。这使第一次最短路可以从全零势能开始；算法在残量网络中自动创建负成本反向边，
/// 从而可以撤销早先并非全局最优的选择。若业务必须直接输入负成本边，应先做等价成本平移，
/// 或改用 Bellman-Ford 初始化顶点势能。
/// </para>
/// </remarks>
public sealed class MinCostFlowNetwork<TVertex> where TVertex : notnull
{
    internal sealed class Edge(
        TVertex from,
        TVertex to,
        long capacity,
        double unitCost,
        int reverseIndex,
        bool isOriginal)
    {
        public TVertex From { get; } = from;
        public TVertex To { get; } = to;
        public long ResidualCapacity { get; set; } = capacity;
        public long OriginalCapacity { get; } = capacity;
        public double UnitCost { get; } = unitCost;
        public int ReverseIndex { get; } = reverseIndex;
        public bool IsOriginal { get; } = isOriginal;
    }

    private readonly Dictionary<TVertex, List<Edge>> _adjacency;
    private readonly List<Edge> _originalEdges = [];

    /// <summary>
    /// 创建空网络。
    /// </summary>
    /// <param name="comparer">顶点的相等比较器；省略时使用 <see cref="EqualityComparer{T}.Default"/>。</param>
    public MinCostFlowNetwork(IEqualityComparer<TVertex>? comparer = null)
    {
        _adjacency = new Dictionary<TVertex, List<Edge>>(comparer);
    }

    /// <summary>网络使用的顶点相等比较器。</summary>
    public IEqualityComparer<TVertex> Comparer => _adjacency.Comparer;

    /// <summary>按加入顺序枚举顶点。</summary>
    public IEnumerable<TVertex> Vertices => _adjacency.Keys;

    internal IReadOnlyDictionary<TVertex, List<Edge>> Adjacency => _adjacency;
    internal IReadOnlyList<Edge> OriginalEdges => _originalEdges;

    /// <summary>
    /// 添加一个顶点；若等价顶点已存在则返回 <see langword="false"/>。
    /// </summary>
    public bool AddVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        return _adjacency.TryAdd(vertex, []);
    }

    /// <summary>
    /// 添加一条有向边，并同步创建一条容量为零、成本相反的反向残量边。
    /// </summary>
    /// <remarks>
    /// 反向边是最小费用流正确性的关键：若后续发现更便宜的组合，算法会沿反向边退回此前发送的流量，
    /// 而不是被第一次局部选择永久锁死。允许平行边，但拒绝自环，因为自环不能增加源汇流量，
    /// 只会制造没有教学价值的残量索引特例。
    /// </remarks>
    public void AddEdge(TVertex from, TVertex to, long capacity, double unitCost)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "容量不能为负数。");
        }

        if (!double.IsFinite(unitCost) || unitCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitCost), unitCost, "单位成本必须是非负有限数。");
        }

        if (Comparer.Equals(from, to))
        {
            throw new ArgumentException("最小费用流的边必须连接两个不同顶点。", nameof(to));
        }

        AddVertex(from);
        AddVertex(to);

        var forwardIndex = _adjacency[from].Count;
        var reverseIndex = _adjacency[to].Count;
        var forward = new Edge(from, to, capacity, unitCost, reverseIndex, isOriginal: true);
        var reverse = new Edge(to, from, 0, -unitCost, forwardIndex, isOriginal: false);

        _adjacency[from].Add(forward);
        _adjacency[to].Add(reverse);
        _originalEdges.Add(forward);
    }

    internal MinCostFlowNetwork<TVertex> Clone()
    {
        var clone = new MinCostFlowNetwork<TVertex>(Comparer);
        foreach (var vertex in Vertices)
        {
            clone.AddVertex(vertex);
        }

        foreach (var edge in _originalEdges)
        {
            clone.AddEdge(edge.From, edge.To, edge.OriginalCapacity, edge.UnitCost);
        }

        return clone;
    }
}

/// <summary>
/// 最小费用最大流算法。
/// </summary>
public static class MinCostFlowAlgorithms
{
    private const int ReducedCostToleranceInUlps = 8;

    /// <summary>
    /// 使用“连续最短增广路 + Johnson 势能”求最小费用最大流。
    /// </summary>
    /// <param name="network">不会被修改的输入网络。</param>
    /// <param name="source">源点。</param>
    /// <param name="sink">汇点。</param>
    /// <param name="flowLimit">
    /// 最多发送的流量。默认值 <see cref="long.MaxValue"/> 表示一直增广到残量网络中不存在源汇路径，
    /// 即得到真正的最大流；传入较小值时得到该流量上限内的最低成本方案。
    /// </param>
    /// <remarks>
    /// <para>
    /// 每轮 Dijkstra 寻找当前最便宜的增广路。反向残量边可能具有负成本，因此不能直接对原成本运行 Dijkstra；
    /// 顶点势能把残量边转换成非负“约化成本”，同时保持任意两条源汇路径之间的成本次序不变。
    /// </para>
    /// <para>
    /// 若最大流为 F，复杂度约为 O(F · E log V)。一次可以发送多单位瓶颈流量，所以这里的 F 更准确地说是
    /// 增广轮数的上界。相同输入顺序下，邻接边顺序和优先队列序号共同保证成本相同时仍得到确定结果。
    /// </para>
    /// </remarks>
    public static MinimumCostMaximumFlowResult<TVertex> MinimumCostMaximumFlow<TVertex>(
        MinCostFlowNetwork<TVertex> network,
        TVertex source,
        TVertex sink,
        long flowLimit = long.MaxValue)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(network);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sink);

        if (flowLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(flowLimit), flowLimit, "流量上限不能为负数。");
        }

        var residual = network.Clone();
        if (!residual.Adjacency.ContainsKey(source))
        {
            throw new ArgumentException("源点不存在于网络中。", nameof(source));
        }

        if (!residual.Adjacency.ContainsKey(sink))
        {
            throw new ArgumentException("汇点不存在于网络中。", nameof(sink));
        }

        if (residual.Comparer.Equals(source, sink))
        {
            throw new ArgumentException("源点和汇点必须不同。", nameof(sink));
        }

        // 原始成本均非负，所以初始全零势能已经满足“所有约化成本非负”的 Dijkstra 前提。
        var potential = new Dictionary<TVertex, double>(residual.Comparer);
        foreach (var vertex in residual.Vertices)
        {
            potential[vertex] = 0;
        }

        var totalFlow = 0L;
        while (totalFlow < flowLimit)
        {
            var distance = new Dictionary<TVertex, double>(residual.Comparer)
            {
                [source] = 0
            };
            var previous = new Dictionary<TVertex, MinCostFlowNetwork<TVertex>.Edge>(residual.Comparer);

            // 复用项目自实现的二叉最小堆，让 Dijkstra 的优先队列不变量也真实参与综合算法。
            // 递增序号只负责稳定打破等距队列项的平局，不改变最短路含义。
            var queue = new BinaryMinHeap<QueueEntry<TVertex>>(QueueEntryComparer<TVertex>.Instance);
            var sequence = 0L;
            queue.Enqueue(new QueueEntry<TVertex>(source, Distance: 0, sequence++));

            while (queue.Count > 0)
            {
                var queued = queue.Dequeue();
                var current = queued.Vertex;
                if (!distance.TryGetValue(current, out var currentDistance) ||
                    queued.Distance > currentDistance)
                {
                    // 这棵教学堆没有 decrease-key；同一顶点可能以旧距离多次入队，弹出时忽略过期项即可。
                    continue;
                }

                foreach (var edge in residual.Adjacency[current])
                {
                    if (edge.ResidualCapacity == 0)
                    {
                        continue;
                    }

                    var reducedCost = edge.UnitCost + potential[current] - potential[edge.To];
                    var reducedCostTolerance = GetReducedCostTolerance(
                        edge.UnitCost,
                        potential[current],
                        potential[edge.To]);
                    if (reducedCost < -reducedCostTolerance)
                    {
                        throw new InvalidOperationException("顶点势能不变量被破坏：发现负约化成本边。");
                    }

                    // 理论上的 0 可能因大数吞掉低位而成为小负数；只在表达式的 ULP 误差预算内钳制为 0。
                    reducedCost = Math.Max(0, reducedCost);
                    var candidateDistance = currentDistance + reducedCost;
                    if (!double.IsFinite(candidateDistance))
                    {
                        throw new OverflowException("最短路成本超出 double 可表示范围。");
                    }

                    if (distance.TryGetValue(edge.To, out var knownDistance) &&
                        candidateDistance >= knownDistance)
                    {
                        // 只有实际不更小时才保留先发现路径。不能在这里使用 epsilon：
                        // 5e-11 与 0 虽然很接近，却仍是公开成本契约中的真实差异；把它们当平局会返回较贵方案。
                        // Epsilon 只用于容忍势函数约化成本的舍入噪声，不能改变最短路的目标函数。
                        continue;
                    }

                    distance[edge.To] = candidateDistance;
                    previous[edge.To] = edge;
                    queue.Enqueue(new QueueEntry<TVertex>(edge.To, candidateDistance, sequence++));
                }
            }

            if (!previous.ContainsKey(sink))
            {
                // 不存在增广路时，当前流量根据最大流增广路定理已经达到最大值。
                break;
            }

            foreach (var (vertex, shortestDistance) in distance)
            {
                potential[vertex] += shortestDistance;
            }

            var sent = flowLimit - totalFlow;
            for (var vertex = sink; !residual.Comparer.Equals(vertex, source);)
            {
                var edge = previous[vertex];
                sent = Math.Min(sent, edge.ResidualCapacity);
                vertex = edge.From;
            }

            for (var vertex = sink; !residual.Comparer.Equals(vertex, source);)
            {
                var edge = previous[vertex];
                edge.ResidualCapacity -= sent;
                residual.Adjacency[edge.To][edge.ReverseIndex].ResidualCapacity += sent;
                vertex = edge.From;
            }

            totalFlow = checked(totalFlow + sent);
        }

        var edgeResults = residual.OriginalEdges
            .Select(edge => new MinCostFlowEdgeResult<TVertex>(
                edge.From,
                edge.To,
                edge.OriginalCapacity,
                edge.OriginalCapacity - edge.ResidualCapacity,
                edge.UnitCost))
            .ToArray();
        var totalCost = edgeResults.Sum(edge => edge.Flow * edge.UnitCost);
        if (!double.IsFinite(totalCost))
        {
            throw new OverflowException("最小费用最大流的总成本超出 double 可表示范围。");
        }

        return new MinimumCostMaximumFlowResult<TVertex>(totalFlow, totalCost, edgeResults);
    }

    /// <summary>
    /// 根据约化成本表达式三个操作数的实际二进制尺度，估算可接受的舍入误差。
    /// </summary>
    private static double GetReducedCostTolerance(double edgeCost, double fromPotential, double toPotential)
    {
        var scale = Math.Max(
            Math.Abs(edgeCost),
            Math.Max(Math.Abs(fromPotential), Math.Abs(toPotential)));
        if (scale == 0)
        {
            return 0;
        }

        // 固定的十进制 epsilon 只在某个偶然尺度上有效。例如 1 + 1e20 会舍入为 1e20，
        // 随后的反向边约化成本可能得到 -1；它远小于 1e20 附近相邻 double 的间距 16384，
        // 因而是表示能力造成的噪声，而不是一条真实的负成本边。
        //
        // ULP（unit in the last place）会随数值尺度变化。这里给三项加减和势能累计留下 8 ULP
        // 的保守预算；这仍只覆盖最后几位的舍入不确定性，不会像宽泛的相对 epsilon 那样
        // 把所有小业务成本都视为 0。最短路松弛仍使用严格 double 比较，真实可表示的差异不会被吞掉。
        var incremented = Math.BitIncrement(scale);
        var unitInLastPlace = double.IsFinite(incremented)
            ? incremented - scale
            : scale - Math.BitDecrement(scale);
        return ReducedCostToleranceInUlps * unitInLastPlace;
    }

    private readonly record struct QueueEntry<TVertex>(TVertex Vertex, double Distance, long Sequence)
        where TVertex : notnull;

    private sealed class QueueEntryComparer<TVertex> : IComparer<QueueEntry<TVertex>>
        where TVertex : notnull
    {
        public static QueueEntryComparer<TVertex> Instance { get; } = new();

        public int Compare(QueueEntry<TVertex> left, QueueEntry<TVertex> right)
        {
            var byDistance = left.Distance.CompareTo(right.Distance);
            if (byDistance != 0)
            {
                return byDistance;
            }

            // Vertex 不必可比较：相同距离只按全局入队序号决定先后，既兼容任意顶点类型，
            // 又让结果稳定跟随调用方显式的建边顺序。
            return left.Sequence.CompareTo(right.Sequence);
        }
    }
}
