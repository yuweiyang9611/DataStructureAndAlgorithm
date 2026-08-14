namespace DataStructureAndAlgorithm.Graph;

/// <summary>带权图中的一条有向邻接边。</summary>
public readonly record struct WeightedEdge<TVertex>(TVertex To, double Weight)
    where TVertex : notnull;

/// <summary>
/// 使用邻接表存储的带权图。
/// </summary>
/// <remarks>
/// 邻接表的空间复杂度为 O(V + E)，适合边数远小于 V² 的稀疏图。
/// 无向边在内部存成方向相反的两条邻接边。
/// </remarks>
public sealed class WeightedGraph<TVertex> where TVertex : notnull
{
    private sealed class AdjacencyBucket
    {
        public AdjacencyBucket()
        {
            Edges = MutableEdges.AsReadOnly();
        }

        public List<WeightedEdge<TVertex>> MutableEdges { get; } = [];

        public IReadOnlyList<WeightedEdge<TVertex>> Edges { get; }
    }

    private readonly Dictionary<TVertex, AdjacencyBucket> _adjacencyList;

    public WeightedGraph(bool isDirected = true, IEqualityComparer<TVertex>? comparer = null)
    {
        IsDirected = isDirected;
        _adjacencyList = new Dictionary<TVertex, AdjacencyBucket>(comparer);
    }

    public bool IsDirected { get; }

    /// <summary>Gets the equality comparer used to identify vertices.</summary>
    public IEqualityComparer<TVertex> Comparer => _adjacencyList.Comparer;

    public int VertexCount => _adjacencyList.Count;

    public IEnumerable<TVertex> Vertices => _adjacencyList.Keys;

    /// <summary>添加顶点。顶点已存在时不做任何修改。</summary>
    public bool AddVertex(TVertex vertex)
    {
        return _adjacencyList.TryAdd(vertex, new AdjacencyBucket());
    }

    /// <summary>
    /// 添加边。端点不存在时会自动创建；同一方向上的重复边会被拒绝。
    /// </summary>
    public void AddEdge(TVertex from, TVertex to, double weight)
    {
        if (double.IsNaN(weight) || double.IsInfinity(weight))
        {
            throw new ArgumentOutOfRangeException(nameof(weight), weight, "Weight must be a finite number.");
        }

        AddVertex(from);
        AddVertex(to);

        if (_adjacencyList[from].MutableEdges.Any(edge => Comparer.Equals(edge.To, to)))
        {
            throw new ArgumentException("An edge between the specified vertices already exists.");
        }

        _adjacencyList[from].MutableEdges.Add(new WeightedEdge<TVertex>(to, weight));

        if (!IsDirected)
        {
            _adjacencyList[to].MutableEdges.Add(new WeightedEdge<TVertex>(from, weight));
        }
    }

    /// <summary>获取某个顶点的只读出边视图。</summary>
    public IReadOnlyList<WeightedEdge<TVertex>> GetOutgoingEdges(TVertex vertex)
    {
        if (!_adjacencyList.TryGetValue(vertex, out var edges))
        {
            throw new KeyNotFoundException("The specified vertex does not exist in the graph.");
        }

        return edges.Edges;
    }

    public bool ContainsVertex(TVertex vertex) => _adjacencyList.ContainsKey(vertex);
}
