using System.Collections.ObjectModel;

namespace DataStructureAndAlgorithm.Graph;

/// <summary>二分图匹配中的一条左右顶点配对。</summary>
public readonly record struct BipartiteMatch<TLeft, TRight>(TLeft Left, TRight Right)
    where TLeft : notnull
    where TRight : notnull;

/// <summary>Hopcroft-Karp 最大匹配结果。</summary>
public sealed record BipartiteMatchingResult<TLeft, TRight>(
    IReadOnlyList<BipartiteMatch<TLeft, TRight>> Matches)
    where TLeft : notnull
    where TRight : notnull
{
    public int Count => Matches.Count;
}

/// <summary>稀疏图上的全源最短路、二分图匹配与欧拉路径算法。</summary>
public static class P3GraphAlgorithms
{
    private const int ReweightingToleranceInUlps = 8;

    /// <summary>
    /// Johnson 全源最短路先用 Bellman-Ford 势能把所有边重标为非负，再从每个顶点运行 Dijkstra。
    /// 它支持负边但拒绝负环；使用二叉堆时复杂度 O(VE + V(E+V)logV)，适合稀疏图。
    /// </summary>
    public static AllPairsShortestPathResult<TVertex> JohnsonAllPairsShortestPaths<TVertex>(
        WeightedGraph<TVertex> graph)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        var vertices = graph.Vertices.ToArray();
        var indexes = new Dictionary<TVertex, int>(graph.Comparer);
        for (var index = 0; index < vertices.Length; index++) indexes.Add(vertices[index], index);

        // 假想超级源点以 0 权边连接每个顶点，所以 Bellman-Ford 初始势能都为 0。
        var potential = vertices.ToDictionary(vertex => vertex, _ => 0d, graph.Comparer);
        for (var iteration = 1; iteration < vertices.Length; iteration++)
        {
            var changed = false;
            foreach (var from in vertices)
                foreach (var edge in graph.GetOutgoingEdges(from))
                {
                    var candidate = potential[from] + edge.Weight;
                    if (candidate >= potential[edge.To]) continue;
                    potential[edge.To] = candidate;
                    changed = true;
                }

            if (!changed) break;
        }

        foreach (var from in vertices)
            foreach (var edge in graph.GetOutgoingEdges(from))
            {
                if (potential[from] + edge.Weight < potential[edge.To])
                    throw new InvalidOperationException("The graph contains a negative-weight cycle.");
            }

        var distances = new double[vertices.Length, vertices.Length];
        var next = new int[vertices.Length, vertices.Length];
        for (var sourceIndex = 0; sourceIndex < vertices.Length; sourceIndex++)
        {
            var source = vertices[sourceIndex];
            var reweightedDistances = vertices.ToDictionary(vertex => vertex, _ => double.PositiveInfinity, graph.Comparer);
            var firstHop = new Dictionary<TVertex, TVertex>(graph.Comparer);
            var queue = new PriorityQueue<TVertex, double>();
            reweightedDistances[source] = 0;
            queue.Enqueue(source, 0);

            while (queue.TryDequeue(out var current, out var queuedDistance))
            {
                if (queuedDistance > reweightedDistances[current]) continue;
                foreach (var edge in graph.GetOutgoingEdges(current))
                {
                    var reweighted = edge.Weight + potential[current] - potential[edge.To];
                    var reweightingTolerance = GetReweightingTolerance(
                        edge.Weight, potential[current], potential[edge.To]);
                    if (reweighted < -reweightingTolerance)
                        throw new InvalidOperationException("Johnson reweighting produced a negative edge.");
                    reweighted = Math.Max(0, reweighted);
                    var candidate = reweightedDistances[current] + reweighted;
                    if (candidate >= reweightedDistances[edge.To]) continue;
                    reweightedDistances[edge.To] = candidate;
                    firstHop[edge.To] = graph.Comparer.Equals(current, source) ? edge.To : firstHop[current];
                    queue.Enqueue(edge.To, candidate);
                }
            }

            for (var targetIndex = 0; targetIndex < vertices.Length; targetIndex++)
            {
                var target = vertices[targetIndex];
                if (double.IsPositiveInfinity(reweightedDistances[target]))
                {
                    distances[sourceIndex, targetIndex] = double.PositiveInfinity;
                    next[sourceIndex, targetIndex] = -1;
                    continue;
                }

                distances[sourceIndex, targetIndex] = reweightedDistances[target] - potential[source] + potential[target];
                next[sourceIndex, targetIndex] = sourceIndex == targetIndex ? targetIndex : indexes[firstHop[target]];
            }
        }

        return new AllPairsShortestPathResult<TVertex>(vertices, distances, next, graph.Comparer);
    }

    private static double GetReweightingTolerance(double edgeWeight, double fromPotential, double toPotential)
    {
        var scale = Math.Max(
            Math.Abs(edgeWeight),
            Math.Max(Math.Abs(fromPotential), Math.Abs(toPotential)));
        if (scale == 0)
        {
            return 0;
        }

        var incremented = Math.BitIncrement(scale);
        var unitInLastPlace = double.IsFinite(incremented)
            ? incremented - scale
            : scale - Math.BitDecrement(scale);
        return ReweightingToleranceInUlps * unitInLastPlace;
    }

    /// <summary>
    /// Hopcroft-Karp 每轮 BFS 同时寻找最短增广路的层次，再由 DFS 批量增广，复杂度 O(E sqrt(V))。
    /// </summary>
    public static BipartiteMatchingResult<TLeft, TRight> HopcroftKarpMaximumMatching<TLeft, TRight>(
        IEnumerable<(TLeft Left, TRight Right)> edges,
        IEqualityComparer<TLeft>? leftComparer = null,
        IEqualityComparer<TRight>? rightComparer = null)
        where TLeft : notnull
        where TRight : notnull
    {
        ArgumentNullException.ThrowIfNull(edges);
        var adjacencySets = new Dictionary<TLeft, HashSet<TRight>>(leftComparer);
        foreach (var (left, right) in edges)
        {
            if (!adjacencySets.TryGetValue(left, out var neighbors))
            {
                neighbors = new HashSet<TRight>(rightComparer);
                adjacencySets.Add(left, neighbors);
            }
            neighbors.Add(right);
        }

        var pairLeft = new Dictionary<TLeft, TRight>(adjacencySets.Comparer);
        var pairRight = new Dictionary<TRight, TLeft>(rightComparer);
        var distance = new Dictionary<TLeft, int>(adjacencySets.Comparer);

        bool BuildLayers()
        {
            distance.Clear();
            var queue = new Queue<TLeft>();
            foreach (var left in adjacencySets.Keys)
            {
                if (pairLeft.ContainsKey(left)) continue;
                distance[left] = 0;
                queue.Enqueue(left);
            }

            var foundFreeRight = false;
            while (queue.TryDequeue(out var left))
            {
                foreach (var right in adjacencySets[left])
                {
                    if (!pairRight.TryGetValue(right, out var pairedLeft)) foundFreeRight = true;
                    else if (!distance.ContainsKey(pairedLeft))
                    {
                        distance[pairedLeft] = distance[left] + 1;
                        queue.Enqueue(pairedLeft);
                    }
                }
            }
            return foundFreeRight;
        }

        bool Augment(TLeft left)
        {
            foreach (var right in adjacencySets[left])
            {
                if (!pairRight.TryGetValue(right, out var pairedLeft) ||
                    distance.TryGetValue(pairedLeft, out var nextDistance) &&
                    nextDistance == distance[left] + 1 && Augment(pairedLeft))
                {
                    pairLeft[left] = right;
                    pairRight[right] = left;
                    return true;
                }
            }
            distance[left] = int.MaxValue;
            return false;
        }

        while (BuildLayers())
        {
            foreach (var left in adjacencySets.Keys)
            {
                if (!pairLeft.ContainsKey(left)) Augment(left);
            }
        }

        var matches = pairLeft.Select(pair => new BipartiteMatch<TLeft, TRight>(pair.Key, pair.Value)).ToArray();
        return new BipartiteMatchingResult<TLeft, TRight>(Array.AsReadOnly(matches));
    }

    /// <summary>
    /// Hierholzer 算法恢复有向多重图的欧拉路径，使用每条边恰好一次，时间 O(V+E)。
    /// </summary>
    public static IReadOnlyList<TVertex> DirectedEulerianTrail<TVertex>(
        IEnumerable<(TVertex From, TVertex To)> edges,
        IEqualityComparer<TVertex>? comparer = null)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(edges);
        var materialized = edges.ToArray();
        if (materialized.Length == 0) return [];
        var adjacency = new Dictionary<TVertex, Stack<TVertex>>(comparer);
        var inDegree = new Dictionary<TVertex, int>(comparer);
        var outDegree = new Dictionary<TVertex, int>(comparer);
        foreach (var (from, to) in materialized)
        {
            if (!adjacency.TryGetValue(from, out var destinations)) adjacency[from] = destinations = new Stack<TVertex>();
            adjacency.TryAdd(to, new Stack<TVertex>());
            destinations.Push(to);
            outDegree[from] = outDegree.GetValueOrDefault(from) + 1;
            outDegree.TryAdd(to, 0);
            inDegree[to] = inDegree.GetValueOrDefault(to) + 1;
            inDegree.TryAdd(from, 0);
        }

        // 先使用真实顶点初始化，避免把 default(TVertex) 当作“尚未赋值”的哨兵值。
        // 若存在出度比入度多 1 的顶点，下面的度数检查会把它覆盖为唯一合法起点。
        TVertex start = materialized[0].From;
        var startCount = 0;
        var endCount = 0;
        foreach (var vertex in adjacency.Keys)
        {
            var difference = outDegree[vertex] - inDegree[vertex];
            if (difference == 1) { start = vertex; startCount++; }
            else if (difference == -1) endCount++;
            else if (difference != 0) throw new ArgumentException("The directed graph has no Eulerian trail.", nameof(edges));
        }
        if (!((startCount == 1 && endCount == 1) || (startCount == 0 && endCount == 0)))
            throw new ArgumentException("The directed graph has no Eulerian trail.", nameof(edges));

        var traversal = new Stack<TVertex>();
        var reversed = new List<TVertex>(materialized.Length + 1);
        traversal.Push(start);
        while (traversal.TryPeek(out var current))
        {
            if (adjacency[current].TryPop(out var nextVertex)) traversal.Push(nextVertex);
            else reversed.Add(traversal.Pop());
        }
        if (reversed.Count != materialized.Length + 1)
            throw new ArgumentException("All edges must belong to one connected Eulerian component.", nameof(edges));
        reversed.Reverse();
        return new ReadOnlyCollection<TVertex>(reversed);
    }
}

/// <summary>倍增表：预处理 O(n log n)，最近公共祖先、距离和 k 级祖先查询 O(log n)。</summary>
public sealed class BinaryLiftingTree<TVertex> where TVertex : notnull
{
    private readonly TVertex[] _vertices;
    private readonly Dictionary<TVertex, int> _indexes;
    private readonly int[][] _ancestors;
    private readonly int[] _depth;

    public BinaryLiftingTree(TVertex root, IEnumerable<(TVertex First, TVertex Second)> edges,
        IEqualityComparer<TVertex>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(edges);
        var adjacency = new Dictionary<TVertex, List<TVertex>>(comparer) { [root] = [] };
        var edgeCount = 0;
        foreach (var (first, second) in edges)
        {
            if (adjacency.Comparer.Equals(first, second)) throw new ArgumentException("Tree edges cannot be self-loops.", nameof(edges));
            if (!adjacency.TryGetValue(first, out var firstNeighbors)) adjacency[first] = firstNeighbors = [];
            if (!adjacency.TryGetValue(second, out var secondNeighbors)) adjacency[second] = secondNeighbors = [];
            firstNeighbors.Add(second);
            secondNeighbors.Add(first);
            edgeCount++;
        }
        if (edgeCount != adjacency.Count - 1) throw new ArgumentException("Edges must form a tree.", nameof(edges));

        _vertices = adjacency.Keys.ToArray();
        _indexes = new Dictionary<TVertex, int>(adjacency.Comparer);
        for (var index = 0; index < _vertices.Length; index++) _indexes.Add(_vertices[index], index);
        _depth = new int[_vertices.Length];
        var parent = Enumerable.Repeat(-1, _vertices.Length).ToArray();
        var rootIndex = _indexes[root];
        parent[rootIndex] = rootIndex;
        var queue = new Queue<TVertex>();
        queue.Enqueue(root);
        var visitedCount = 0;
        while (queue.TryDequeue(out var current))
        {
            visitedCount++;
            var currentIndex = _indexes[current];
            foreach (var neighbor in adjacency[current])
            {
                var neighborIndex = _indexes[neighbor];
                if (parent[neighborIndex] >= 0) continue;
                parent[neighborIndex] = currentIndex;
                _depth[neighborIndex] = _depth[currentIndex] + 1;
                queue.Enqueue(neighbor);
            }
        }
        if (visitedCount != _vertices.Length) throw new ArgumentException("Edges must form one connected tree.", nameof(edges));

        var levels = 1;
        while ((1L << levels) <= _vertices.Length) levels++;
        _ancestors = new int[levels][];
        _ancestors[0] = parent;
        for (var level = 1; level < levels; level++)
        {
            _ancestors[level] = new int[_vertices.Length];
            for (var vertex = 0; vertex < _vertices.Length; vertex++)
                _ancestors[level][vertex] = _ancestors[level - 1][_ancestors[level - 1][vertex]];
        }
    }

    public TVertex LowestCommonAncestor(TVertex first, TVertex second)
    {
        var left = GetIndex(first);
        var right = GetIndex(second);
        if (_depth[left] < _depth[right]) (left, right) = (right, left);
        left = Lift(left, _depth[left] - _depth[right]);
        if (left == right) return _vertices[left];
        for (var level = _ancestors.Length - 1; level >= 0; level--)
        {
            if (_ancestors[level][left] == _ancestors[level][right]) continue;
            left = _ancestors[level][left];
            right = _ancestors[level][right];
        }
        return _vertices[_ancestors[0][left]];
    }

    public int Distance(TVertex first, TVertex second)
    {
        var ancestor = GetIndex(LowestCommonAncestor(first, second));
        return _depth[GetIndex(first)] + _depth[GetIndex(second)] - 2 * _depth[ancestor];
    }

    public bool TryGetKthAncestor(TVertex vertex, int steps, out TVertex ancestor)
    {
        if (steps < 0) throw new ArgumentOutOfRangeException(nameof(steps));
        var index = GetIndex(vertex);
        if (steps > _depth[index]) { ancestor = default!; return false; }
        ancestor = _vertices[Lift(index, steps)];
        return true;
    }

    private int Lift(int vertex, int steps)
    {
        for (var level = 0; steps > 0; level++, steps >>= 1)
            if ((steps & 1) != 0) vertex = _ancestors[level][vertex];
        return vertex;
    }

    private int GetIndex(TVertex vertex) => _indexes.TryGetValue(vertex, out var index)
        ? index
        : throw new KeyNotFoundException("The specified vertex does not belong to the tree.");
}
