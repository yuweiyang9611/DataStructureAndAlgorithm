using DataStructureAndAlgorithm.LeetCode.Models;
using DataStructureAndAlgorithm.PatternMatching;

namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>P3 高级图、字符串和动态规划专题题；题号均不在原有 100 题中。</summary>
public static class AdvancedTopicProblems
{
    /// <summary>
    /// 312 - Burst Balloons。最后戳破区间内的气球时，两侧边界已经确定，子问题因此相互独立。
    /// 时间 O(n³)，空间 O(n²)。
    /// </summary>
    public static long MaximumCoins(IReadOnlyList<int> balloons)
    {
        ArgumentNullException.ThrowIfNull(balloons);
        if (balloons.Any(value => value < 0)) throw new ArgumentException("Balloon values must be non-negative.", nameof(balloons));
        var values = new int[balloons.Count + 2];
        values[0] = values[^1] = 1;
        for (var index = 0; index < balloons.Count; index++) values[index + 1] = balloons[index];
        var best = new long[values.Length, values.Length];

        for (var length = 2; length < values.Length; length++)
            for (var left = 0; left + length < values.Length; left++)
            {
                var right = left + length;
                for (var last = left + 1; last < right; last++)
                {
                    var coins = checked(best[left, last] + best[last, right] +
                                        (long)values[left] * values[last] * values[right]);
                    best[left, right] = Math.Max(best[left, right], coins);
                }
            }

        return best[0, values.Length - 1];
    }

    /// <summary>
    /// 332 - Reconstruct Itinerary。Hierholzer 在走到无出边顶点时逆序加入答案，
    /// 优先队列保证每次取字典序最小的剩余边。所有票必须恰好使用一次。
    /// </summary>
    public static IReadOnlyList<string> ReconstructItinerary(
        IEnumerable<(string From, string To)> tickets,
        string origin = "JFK")
    {
        ArgumentNullException.ThrowIfNull(tickets);
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        var adjacency = new Dictionary<string, PriorityQueue<string, string>>(StringComparer.Ordinal);
        var ticketCount = 0;
        foreach (var (from, to) in tickets)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(from);
            ArgumentException.ThrowIfNullOrWhiteSpace(to);
            if (!adjacency.TryGetValue(from, out var destinations))
            {
                destinations = new PriorityQueue<string, string>(StringComparer.Ordinal);
                adjacency.Add(from, destinations);
            }

            destinations.Enqueue(to, to);
            ticketCount++;
        }

        var stack = new Stack<string>();
        var reversed = new List<string>(ticketCount + 1);
        stack.Push(origin);
        while (stack.TryPeek(out var airport))
        {
            if (adjacency.TryGetValue(airport, out var destinations) && destinations.TryDequeue(out var next, out _))
            {
                stack.Push(next);
            }
            else
            {
                reversed.Add(stack.Pop());
            }
        }

        if (reversed.Count != ticketCount + 1)
        {
            throw new ArgumentException("All tickets must belong to one Eulerian itinerary from the origin.", nameof(tickets));
        }

        reversed.Reverse();
        return reversed.AsReadOnly();
    }

    /// <summary>337 - House Robber III。每个节点同时返回“抢”和“不抢”两种状态，时间 O(n)。</summary>
    public static long RobTree(TreeNode? root)
    {
        var (skip, take) = Visit(root);
        return Math.Max(skip, take);

        static (long Skip, long Take) Visit(TreeNode? node)
        {
            if (node is null) return (0, 0);
            var left = Visit(node.Left);
            var right = Visit(node.Right);
            var take = checked(node.Value + left.Skip + right.Skip);
            var skip = checked(Math.Max(left.Skip, left.Take) + Math.Max(right.Skip, right.Take));
            return (skip, take);
        }
    }

    /// <summary>516 - Longest Palindromic Subsequence。一维区间 DP，时间 O(n²)、空间 O(n)。</summary>
    public static int LongestPalindromicSubsequence(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var best = new int[text.Length];
        for (var left = text.Length - 1; left >= 0; left--)
        {
            best[left] = 1;
            var previousDiagonal = 0;
            for (var right = left + 1; right < text.Length; right++)
            {
                var oldAbove = best[right];
                best[right] = text[left] == text[right]
                    ? previousDiagonal + 2
                    : Math.Max(best[right], best[right - 1]);
                previousDiagonal = oldAbove;
            }
        }

        return text.Length == 0 ? 0 : best[^1];
    }

    /// <summary>
    /// 698 - Partition to K Equal Sum Subsets。先放大数以尽早失败，并跳过同层等价桶。
    /// 这是指数级搜索；约束剪枝只减少实际分支，不改变最坏复杂度。
    /// </summary>
    public static bool CanPartitionKEqualSumSubsets(IReadOnlyList<int> numbers, int subsetCount)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        if (subsetCount <= 0) throw new ArgumentOutOfRangeException(nameof(subsetCount));
        if (numbers.Any(value => value <= 0)) throw new ArgumentException("Numbers must be positive.", nameof(numbers));
        if (subsetCount > numbers.Count) return false;
        var total = numbers.Sum(value => (long)value);
        if (total % subsetCount != 0) return false;
        var target = total / subsetCount;
        var ordered = numbers.OrderDescending().ToArray();
        if (ordered.Length == 0 || ordered[0] > target) return false;
        var buckets = new long[subsetCount];

        bool Place(int index)
        {
            if (index == ordered.Length) return true;
            for (var bucket = 0; bucket < buckets.Length; bucket++)
            {
                if (buckets[bucket] + ordered[index] > target) continue;
                if (bucket > 0 && buckets[bucket] == buckets[bucket - 1]) continue;
                buckets[bucket] += ordered[index];
                if (Place(index + 1)) return true;
                buckets[bucket] -= ordered[index];
                if (buckets[bucket] == 0) break; // 所有空桶等价，只尝试第一个。
            }

            return false;
        }

        return Place(0);
    }

    /// <summary>
    /// 847 - Shortest Path Visiting All Nodes。BFS 状态由“当前顶点 + 已访问集合位掩码”组成，
    /// 从每个顶点同时出发避免预先猜起点。时间和空间 O(n * 2^n)。
    /// </summary>
    public static int ShortestPathVisitingAllNodes(int[][] graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (graph.Length == 0) return 0;
        if (graph.Length > 20) throw new ArgumentOutOfRangeException(nameof(graph), "At most 20 vertices are supported.");
        if (graph.Any(neighbors => neighbors is null || neighbors.Any(vertex => vertex < 0 || vertex >= graph.Length)))
        {
            throw new ArgumentException("Every neighbor must be a valid vertex index.", nameof(graph));
        }

        var allVisited = (1 << graph.Length) - 1;
        var seen = new bool[graph.Length, 1 << graph.Length];
        var queue = new Queue<(int Vertex, int Mask, int Distance)>();
        for (var vertex = 0; vertex < graph.Length; vertex++)
        {
            var mask = 1 << vertex;
            seen[vertex, mask] = true;
            queue.Enqueue((vertex, mask, 0));
        }

        while (queue.TryDequeue(out var state))
        {
            if (state.Mask == allVisited) return state.Distance;
            foreach (var next in graph[state.Vertex])
            {
                var nextMask = state.Mask | (1 << next);
                if (seen[next, nextMask]) continue;
                seen[next, nextMask] = true;
                queue.Enqueue((next, nextMask, state.Distance + 1));
            }
        }

        throw new ArgumentException("The graph has no walk that can visit every vertex.", nameof(graph));
    }

    /// <summary>1044 - Longest Duplicate Substring。后缀数组中最长重复串必来自相邻后缀的最大 LCP。</summary>
    public static string LongestDuplicateSubstring(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var suffixArray = SuffixArrayAlgorithms.Build(text);
        var bestOrder = 0;
        for (var order = 1; order < suffixArray.LongestCommonPrefixes.Count; order++)
        {
            if (suffixArray.LongestCommonPrefixes[order] > suffixArray.LongestCommonPrefixes[bestOrder]) bestOrder = order;
        }

        var length = suffixArray.LongestCommonPrefixes.Count == 0
            ? 0
            : suffixArray.LongestCommonPrefixes[bestOrder];
        return length == 0 ? string.Empty : text.Substring(suffixArray.Suffixes[bestOrder], length);
    }

    /// <summary>
    /// 1192 - Critical Connections。Tarjan low-link 判断子树是否存在绕过父边的返祖路径，时间 O(V+E)。
    /// 使用边编号而不是只跳过父顶点，因此平行边也不会被误判为桥。
    /// </summary>
    public static IReadOnlyList<(int From, int To)> CriticalConnections(
        int vertexCount,
        IEnumerable<(int From, int To)> connections)
    {
        if (vertexCount < 0) throw new ArgumentOutOfRangeException(nameof(vertexCount));
        ArgumentNullException.ThrowIfNull(connections);
        var adjacency = Enumerable.Range(0, vertexCount).Select(_ => new List<(int To, int Edge)>()).ToArray();
        var edgeIndex = 0;
        foreach (var (from, to) in connections)
        {
            if ((uint)from >= (uint)vertexCount || (uint)to >= (uint)vertexCount)
                throw new ArgumentException("Connection endpoints must be valid vertices.", nameof(connections));
            adjacency[from].Add((to, edgeIndex));
            adjacency[to].Add((from, edgeIndex));
            edgeIndex++;
        }

        var discovery = Enumerable.Repeat(-1, vertexCount).ToArray();
        var low = new int[vertexCount];
        var time = 0;
        var bridges = new List<(int From, int To)>();

        void Visit(int vertex, int parentEdge)
        {
            discovery[vertex] = low[vertex] = time++;
            foreach (var (next, edge) in adjacency[vertex])
            {
                if (edge == parentEdge) continue;
                if (discovery[next] < 0)
                {
                    Visit(next, edge);
                    low[vertex] = Math.Min(low[vertex], low[next]);
                    if (low[next] > discovery[vertex]) bridges.Add((Math.Min(vertex, next), Math.Max(vertex, next)));
                }
                else
                {
                    low[vertex] = Math.Min(low[vertex], discovery[next]);
                }
            }
        }

        for (var vertex = 0; vertex < vertexCount; vertex++)
        {
            if (discovery[vertex] < 0) Visit(vertex, -1);
        }

        bridges.Sort();
        return bridges.AsReadOnly();
    }
}
