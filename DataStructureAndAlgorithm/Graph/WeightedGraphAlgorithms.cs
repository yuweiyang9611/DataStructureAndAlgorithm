using DataStructureAndAlgorithm.Diagnostics;

namespace DataStructureAndAlgorithm.Graph;

/// <summary>带权图和有向无环图上的经典算法。</summary>
public static class WeightedGraphAlgorithms
{
    /// <summary>
    /// 使用 Dijkstra 算法计算一个源点到所有顶点的最短路径。
    /// </summary>
    /// <remarks>
    /// 只适用于非负权边。使用二叉优先队列时复杂度为 O((V + E) log V)。
    /// </remarks>
    public static ShortestPathResult<TVertex> Dijkstra<TVertex>(
        WeightedGraph<TVertex> graph,
        TVertex source,
        IAlgorithmTraceSink? trace = null)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (!graph.ContainsVertex(source))
        {
            throw new ArgumentException("The source vertex does not exist in the graph.", nameof(source));
        }

        var distances = new Dictionary<TVertex, double>(graph.Comparer);
        var previous = new Dictionary<TVertex, TVertex>(graph.Comparer);
        var queue = new PriorityQueue<TVertex, double>();

        foreach (var vertex in graph.Vertices)
        {
            distances.Add(vertex, double.PositiveInfinity);
        }

        distances[source] = 0;
        queue.Enqueue(source, 0);
        trace?.Record("Dijkstra", "Initialize", "将源点距离设为 0 并加入优先队列。",
            new Dictionary<string, string> { ["source"] = source.ToString() ?? string.Empty });

        while (queue.TryDequeue(out var current, out var dequeuedDistance))
        {
            trace?.Record("Dijkstra", "Dequeue", "取出当前队列中距离最小的候选顶点。",
                new Dictionary<string, string>
                {
                    ["vertex"] = current.ToString() ?? string.Empty,
                    ["distance"] = dequeuedDistance.ToString("G17")
                });

            // PriorityQueue 没有 decrease-key。发现更短路径时会再次入队，旧条目在这里跳过。
            if (dequeuedDistance > distances[current])
            {
                trace?.Record("Dijkstra", "SkipStale", "该条目是 decrease-key 替代策略留下的过期候选。",
                    new Dictionary<string, string> { ["vertex"] = current.ToString() ?? string.Empty });
                continue;
            }

            foreach (var edge in graph.GetOutgoingEdges(current))
            {
                if (edge.Weight < 0)
                {
                    throw new InvalidOperationException("Dijkstra's algorithm does not support negative edge weights.");
                }

                var candidateDistance = distances[current] + edge.Weight;
                if (candidateDistance >= distances[edge.To])
                {
                    continue;
                }

                distances[edge.To] = candidateDistance;
                previous[edge.To] = current;
                queue.Enqueue(edge.To, candidateDistance);
                trace?.Record("Dijkstra", "Relax", "发现更短路径，更新距离与前驱并重新入队。",
                    new Dictionary<string, string>
                    {
                        ["from"] = current.ToString() ?? string.Empty,
                        ["to"] = edge.To.ToString() ?? string.Empty,
                        ["distance"] = candidateDistance.ToString("G17")
                    });
            }
        }

        return new ShortestPathResult<TVertex>(source, distances, previous, graph.Comparer);
    }

    /// <summary>
    /// 使用 Kahn 算法对有向无环图进行拓扑排序。
    /// </summary>
    /// <exception cref="InvalidOperationException">图是无向图或包含环时抛出。</exception>
    public static IReadOnlyList<TVertex> TopologicalSort<TVertex>(WeightedGraph<TVertex> graph)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (!graph.IsDirected)
        {
            throw new InvalidOperationException("Topological sorting is defined only for directed graphs.");
        }

        var inDegrees = new Dictionary<TVertex, int>(graph.Comparer);

        foreach (var vertex in graph.Vertices)
        {
            inDegrees.Add(vertex, 0);
        }

        foreach (var vertex in graph.Vertices)
        {
            foreach (var edge in graph.GetOutgoingEdges(vertex))
            {
                inDegrees[edge.To]++;
            }
        }

        var zeroInDegreeVertices = new Queue<TVertex>(
            inDegrees.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        var result = new List<TVertex>(graph.VertexCount);

        while (zeroInDegreeVertices.TryDequeue(out var vertex))
        {
            result.Add(vertex);

            foreach (var edge in graph.GetOutgoingEdges(vertex))
            {
                inDegrees[edge.To]--;
                if (inDegrees[edge.To] == 0)
                {
                    zeroInDegreeVertices.Enqueue(edge.To);
                }
            }
        }

        // 环上的顶点入度永远不会降到 0，因此处理数量不足就表示存在环。
        if (result.Count != graph.VertexCount)
        {
            throw new InvalidOperationException("The graph contains a cycle and cannot be topologically sorted.");
        }

        return result;
    }
}
