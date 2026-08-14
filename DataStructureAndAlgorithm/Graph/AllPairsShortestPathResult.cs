namespace DataStructureAndAlgorithm.Graph;

/// <summary>Floyd-Warshall 的全源最短距离和路径恢复结果。</summary>
public sealed class AllPairsShortestPathResult<TVertex> where TVertex : notnull
{
    private readonly IReadOnlyList<TVertex> _vertices;
    private readonly Dictionary<TVertex, int> _indexes;
    private readonly double[,] _distances;
    private readonly int[,] _next;

    internal AllPairsShortestPathResult(
        IReadOnlyList<TVertex> vertices,
        double[,] distances,
        int[,] next,
        IEqualityComparer<TVertex> comparer)
    {
        _vertices = vertices;
        _distances = distances;
        _next = next;
        _indexes = new Dictionary<TVertex, int>(comparer);
        for (var index = 0; index < vertices.Count; index++) _indexes.Add(vertices[index], index);
    }

    public double GetDistance(TVertex from, TVertex to) =>
        _distances[GetIndex(from), GetIndex(to)];

    public IReadOnlyList<TVertex> GetPath(TVertex from, TVertex to)
    {
        var current = GetIndex(from);
        var target = GetIndex(to);
        if (_next[current, target] < 0) return [];

        var path = new List<TVertex> { _vertices[current] };
        // 每一步至少向终点推进一条边；保护上限可在内部状态损坏时避免无限循环。
        for (var step = 0; current != target && step <= _vertices.Count; step++)
        {
            current = _next[current, target];
            path.Add(_vertices[current]);
        }

        if (current != target) throw new InvalidOperationException("全源最短路的路径矩阵不一致。");
        return path;
    }

    private int GetIndex(TVertex vertex) => _indexes.TryGetValue(vertex, out var index)
        ? index
        : throw new KeyNotFoundException("The specified vertex does not exist in the graph.");
}
