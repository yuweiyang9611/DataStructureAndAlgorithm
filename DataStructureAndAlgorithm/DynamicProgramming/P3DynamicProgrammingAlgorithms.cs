namespace DataStructureAndAlgorithm.DynamicProgramming;

/// <summary>区间、树形与状态压缩动态规划的代表算法。</summary>
public static class P3DynamicProgrammingAlgorithms
{
    /// <summary>
    /// 矩阵链乘法：dimensions[i] x dimensions[i+1] 描述第 i 个矩阵。
    /// 枚举区间最后一次合并的位置，时间 O(n³)、空间 O(n²)。
    /// </summary>
    public static long MinimumMatrixChainMultiplications(IReadOnlyList<int> dimensions)
    {
        ArgumentNullException.ThrowIfNull(dimensions);
        if (dimensions.Count < 2) throw new ArgumentException("At least two dimensions are required.", nameof(dimensions));
        if (dimensions.Any(value => value <= 0)) throw new ArgumentException("Dimensions must be positive.", nameof(dimensions));
        var matrixCount = dimensions.Count - 1;
        var costs = new long[matrixCount, matrixCount];
        for (var length = 2; length <= matrixCount; length++)
            for (var left = 0; left + length <= matrixCount; left++)
            {
                var right = left + length - 1;
                costs[left, right] = long.MaxValue;
                for (var split = left; split < right; split++)
                {
                    var candidate = checked(costs[left, split] + costs[split + 1, right] +
                                            (long)dimensions[left] * dimensions[split + 1] * dimensions[right + 1]);
                    costs[left, right] = Math.Min(costs[left, right], candidate);
                }
            }
        return matrixCount == 1 ? 0 : costs[0, matrixCount - 1];
    }

    /// <summary>
    /// 树上最大权独立集：每个顶点返回“选它”和“不选它”两种状态。
    /// 选择父节点时不能选择子节点；不选父节点时子节点可取两种状态的较大值。时间 O(n)。
    /// </summary>
    public static long MaximumWeightIndependentSetOnTree<TVertex>(
        IReadOnlyDictionary<TVertex, long> weights,
        IEnumerable<(TVertex First, TVertex Second)> edges,
        IEqualityComparer<TVertex>? comparer = null)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(edges);
        if (weights.Count == 0)
        {
            if (edges.Any()) throw new ArgumentException("Edges cannot exist without vertices.", nameof(edges));
            return 0;
        }
        var adjacency = weights.Keys.ToDictionary(vertex => vertex, _ => new List<TVertex>(), comparer);
        var edgeCount = 0;
        foreach (var (first, second) in edges)
        {
            if (!adjacency.ContainsKey(first) || !adjacency.ContainsKey(second))
                throw new ArgumentException("Every edge endpoint must have a weight.", nameof(edges));
            if (adjacency.Comparer.Equals(first, second)) throw new ArgumentException("Tree edges cannot be self-loops.", nameof(edges));
            adjacency[first].Add(second);
            adjacency[second].Add(first);
            edgeCount++;
        }
        if (edgeCount != weights.Count - 1) throw new ArgumentException("Edges must form a tree.", nameof(edges));

        var root = weights.Keys.First();
        var parent = new Dictionary<TVertex, TVertex>(adjacency.Comparer) { [root] = root };
        var order = new List<TVertex>(weights.Count);
        var stack = new Stack<TVertex>();
        stack.Push(root);
        while (stack.TryPop(out var vertex))
        {
            order.Add(vertex);
            foreach (var neighbor in adjacency[vertex])
            {
                if (parent.ContainsKey(neighbor)) continue;
                parent[neighbor] = vertex;
                stack.Push(neighbor);
            }
        }
        if (order.Count != weights.Count) throw new ArgumentException("Edges must form one connected tree.", nameof(edges));

        var states = new Dictionary<TVertex, (long Skip, long Take)>(adjacency.Comparer);
        for (var index = order.Count - 1; index >= 0; index--)
        {
            var vertex = order[index];
            var skip = 0L;
            var take = weights[vertex];
            foreach (var child in adjacency[vertex])
            {
                if (!adjacency.Comparer.Equals(parent[child], vertex)) continue;
                var childState = states[child];
                skip = checked(skip + Math.Max(childState.Skip, childState.Take));
                take = checked(take + childState.Skip);
            }
            states[vertex] = (skip, take);
        }
        return Math.Max(states[root].Skip, states[root].Take);
    }

    /// <summary>
    /// Held-Karp 状态压缩 DP 求从 start 出发并回到 start 的最短 Hamilton 回路。
    /// 状态 (mask,last) 表示已访问 mask 且停在 last，时间 O(n²2^n)、空间 O(n2^n)。
    /// </summary>
    public static double TravelingSalesperson(double[,] distances, int start = 0)
    {
        ArgumentNullException.ThrowIfNull(distances);
        var count = distances.GetLength(0);
        if (count != distances.GetLength(1)) throw new ArgumentException("Distance matrix must be square.", nameof(distances));
        if (count == 0) return 0;
        if ((uint)start >= (uint)count) throw new ArgumentOutOfRangeException(nameof(start));
        if (count > 18) throw new ArgumentOutOfRangeException(nameof(distances), "At most 18 vertices are supported.");
        foreach (var distance in distances)
        {
            if (double.IsNaN(distance) || distance < 0)
                throw new ArgumentException("Distances must be non-negative numbers or positive infinity.", nameof(distances));
        }

        var stateCount = 1 << count;
        var best = new double[stateCount, count];
        for (var mask = 0; mask < stateCount; mask++)
            for (var last = 0; last < count; last++) best[mask, last] = double.PositiveInfinity;
        best[1 << start, start] = 0;

        for (var mask = 0; mask < stateCount; mask++)
        {
            if ((mask & (1 << start)) == 0) continue;
            for (var last = 0; last < count; last++)
            {
                if (double.IsPositiveInfinity(best[mask, last])) continue;
                for (var next = 0; next < count; next++)
                {
                    if ((mask & (1 << next)) != 0) continue;
                    var nextMask = mask | (1 << next);
                    best[nextMask, next] = Math.Min(best[nextMask, next], best[mask, last] + distances[last, next]);
                }
            }
        }

        var allVisited = stateCount - 1;
        var answer = double.PositiveInfinity;
        for (var last = 0; last < count; last++)
            answer = Math.Min(answer, best[allVisited, last] + distances[last, start]);
        return answer;
    }
}
