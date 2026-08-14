namespace DataStructureAndAlgorithm.Graph;

/// <summary>Dijkstra 算法产生的距离表和前驱表。</summary>
public sealed class ShortestPathResult<TVertex> where TVertex : notnull
{
    private readonly IReadOnlyDictionary<TVertex, TVertex> _previous;
    private readonly IEqualityComparer<TVertex> _comparer;

    internal ShortestPathResult(
        TVertex source,
        IReadOnlyDictionary<TVertex, double> distances,
        IReadOnlyDictionary<TVertex, TVertex> previous,
        IEqualityComparer<TVertex> comparer)
    {
        Source = source;
        Distances = distances;
        _previous = previous;
        _comparer = comparer;
    }

    public TVertex Source { get; }

    /// <summary>源点到每个顶点的距离；不可达顶点的距离为正无穷。</summary>
    public IReadOnlyDictionary<TVertex, double> Distances { get; }

    /// <summary>恢复源点到目标点的路径；不可达时返回空数组。</summary>
    public IReadOnlyList<TVertex> GetPathTo(TVertex target)
    {
        if (!Distances.TryGetValue(target, out var distance))
        {
            throw new KeyNotFoundException("The target vertex does not exist in the graph.");
        }

        if (double.IsPositiveInfinity(distance))
        {
            return [];
        }

        var path = new List<TVertex> { target };
        var current = target;

        while (!_comparer.Equals(current, Source))
        {
            current = _previous[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
