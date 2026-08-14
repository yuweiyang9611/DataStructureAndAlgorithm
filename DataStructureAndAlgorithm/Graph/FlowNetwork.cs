namespace DataStructureAndAlgorithm.Graph;

/// <summary>最大流算法使用的有向容量网络。</summary>
public sealed class FlowNetwork<TVertex> where TVertex : notnull
{
    internal sealed class Edge(TVertex from, TVertex to, double capacity, int reverseIndex, bool isOriginal)
    {
        public TVertex From { get; } = from;
        public TVertex To { get; } = to;
        public double ResidualCapacity { get; set; } = capacity;
        public double OriginalCapacity { get; } = capacity;
        public int ReverseIndex { get; } = reverseIndex;
        public bool IsOriginal { get; } = isOriginal;
    }

    private readonly Dictionary<TVertex, List<Edge>> _adjacency;

    public FlowNetwork(IEqualityComparer<TVertex>? comparer = null)
    {
        _adjacency = new Dictionary<TVertex, List<Edge>>(comparer);
    }

    public IEqualityComparer<TVertex> Comparer => _adjacency.Comparer;
    public IEnumerable<TVertex> Vertices => _adjacency.Keys;
    internal IReadOnlyDictionary<TVertex, List<Edge>> Adjacency => _adjacency;

    public bool AddVertex(TVertex vertex) => _adjacency.TryAdd(vertex, []);

    /// <summary>
    /// 添加一条容量非负的有向边，同时添加容量为零的反向残量边；反向边使算法可以撤销早先的流量选择。
    /// </summary>
    public void AddEdge(TVertex from, TVertex to, double capacity)
    {
        if (!double.IsFinite(capacity) || capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be finite and non-negative.");
        }

        if (Comparer.Equals(from, to))
        {
            // 自环不能增加 s-t 流量，却会让正向边与反向边的索引关系产生没有教学价值的特殊分支。
            throw new ArgumentException("Flow-network edges must connect two different vertices.", nameof(to));
        }

        AddVertex(from);
        AddVertex(to);
        var forwardIndex = _adjacency[from].Count;
        var reverseIndex = _adjacency[to].Count;
        _adjacency[from].Add(new Edge(from, to, capacity, reverseIndex, isOriginal: true));
        _adjacency[to].Add(new Edge(to, from, 0, forwardIndex, isOriginal: false));
    }

    internal FlowNetwork<TVertex> Clone()
    {
        var clone = new FlowNetwork<TVertex>(Comparer);
        foreach (var vertex in Vertices) clone.AddVertex(vertex);
        foreach (var edges in _adjacency.Values)
        {
            foreach (var edge in edges.Where(item => item.IsOriginal))
            {
                clone.AddEdge(edge.From, edge.To, edge.OriginalCapacity);
            }
        }

        return clone;
    }
}

public readonly record struct FlowEdgeResult<TVertex>(TVertex From, TVertex To, double Capacity, double Flow)
    where TVertex : notnull;

public sealed record MaximumFlowResult<TVertex>(
    double Value,
    IReadOnlyList<FlowEdgeResult<TVertex>> Edges)
    where TVertex : notnull;
