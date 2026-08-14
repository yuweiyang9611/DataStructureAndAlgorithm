using DataStructureAndAlgorithm.Set;

namespace DataStructureAndAlgorithm.Graph;

/// <summary>包含负权最短路和最小生成森林的进阶图算法。</summary>
public static class AdvancedGraphAlgorithms
{
    /// <summary>
    /// 使用 Bellman-Ford 计算单源最短路，并检测从源点可达的负权环。
    /// </summary>
    /// <remarks>支持负权边，时间 O(VE)，空间 O(V)。</remarks>
    public static ShortestPathResult<TVertex> BellmanFord<TVertex>(
        WeightedGraph<TVertex> graph,
        TVertex source)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (!graph.ContainsVertex(source))
        {
            throw new ArgumentException("The source vertex does not exist in the graph.", nameof(source));
        }

        var distances = new Dictionary<TVertex, double>(graph.Comparer);
        var previous = new Dictionary<TVertex, TVertex>(graph.Comparer);
        var edges = new List<(TVertex From, TVertex To, double Weight)>();

        foreach (var vertex in graph.Vertices)
        {
            distances.Add(vertex, double.PositiveInfinity);
            edges.AddRange(graph.GetOutgoingEdges(vertex).Select(edge => (vertex, edge.To, edge.Weight)));
        }

        distances[source] = 0;

        // 不含环的最短路径最多使用 V-1 条边，因此最多需要 V-1 轮松弛。
        for (var round = 1; round < graph.VertexCount; round++)
        {
            var changed = false;

            foreach (var (from, to, weight) in edges)
            {
                if (double.IsPositiveInfinity(distances[from]))
                {
                    continue;
                }

                var candidate = distances[from] + weight;
                if (candidate >= distances[to])
                {
                    continue;
                }

                distances[to] = candidate;
                previous[to] = from;
                changed = true;
            }

            // 一整轮没有更新说明已经收敛，可提前结束。
            if (!changed)
            {
                break;
            }
        }

        foreach (var (from, to, weight) in edges)
        {
            if (!double.IsPositiveInfinity(distances[from]) && distances[from] + weight < distances[to])
            {
                throw new InvalidOperationException("The graph contains a negative-weight cycle reachable from the source.");
            }
        }

        return new ShortestPathResult<TVertex>(source, distances, previous, graph.Comparer);
    }

    /// <summary>
    /// 使用 Kruskal 算法构造无向图的最小生成森林。
    /// </summary>
    /// <remarks>
    /// 边排序是主要成本，复杂度 O(E log E)。图不连通时返回每个连通分量的最小生成树集合。
    /// </remarks>
    public static MinimumSpanningForestResult<TVertex> KruskalMinimumSpanningForest<TVertex>(
        WeightedGraph<TVertex> graph)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (graph.IsDirected)
        {
            throw new InvalidOperationException("Kruskal's algorithm requires an undirected graph.");
        }

        var vertices = graph.Vertices.ToList();
        var vertexIndexes = new Dictionary<TVertex, int>(graph.Comparer);
        var disjointSet = new DisjointSet<TVertex>(graph.Comparer);

        for (var index = 0; index < vertices.Count; index++)
        {
            vertexIndexes.Add(vertices[index], index);
            disjointSet.Add(vertices[index]);
        }

        var edges = new List<ForestEdge<TVertex>>();
        foreach (var from in vertices)
        {
            foreach (var edge in graph.GetOutgoingEdges(from))
            {
                // 无向边存了两个方向，只保留顶点序号较小的一份。
                if (vertexIndexes[from] < vertexIndexes[edge.To])
                {
                    edges.Add(new ForestEdge<TVertex>(from, edge.To, edge.Weight));
                }
            }
        }

        edges.Sort((left, right) => left.Weight.CompareTo(right.Weight));
        var selected = new List<ForestEdge<TVertex>>();
        double totalWeight = 0;

        foreach (var edge in edges)
        {
            if (!disjointSet.Union(edge.First, edge.Second))
            {
                // 两端已经连通，加入该边会形成环。
                continue;
            }

            selected.Add(edge);
            totalWeight += edge.Weight;
        }

        return new MinimumSpanningForestResult<TVertex>(selected, totalWeight, disjointSet.SetCount);
    }
}
