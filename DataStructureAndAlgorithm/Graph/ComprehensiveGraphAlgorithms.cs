using DataStructureAndAlgorithm.Heap;

namespace DataStructureAndAlgorithm.Graph;

/// <summary>最小生成树、全源最短路、连通性、启发式寻路和网络流算法。</summary>
public static class ComprehensiveGraphAlgorithms
{
    /// <summary>
    /// 使用 Prim 算法构造最小生成森林。每次选择连接已访问集合与未访问集合的最轻边。
    /// 使用索引堆后复杂度为 O((V + E) log V)，空间 O(V)。
    /// </summary>
    public static MinimumSpanningForestResult<TVertex> PrimMinimumSpanningForest<TVertex>(
        WeightedGraph<TVertex> graph)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (graph.IsDirected) throw new InvalidOperationException("Prim's algorithm requires an undirected graph.");

        var visited = new HashSet<TVertex>(graph.Comparer);
        var bestWeight = new Dictionary<TVertex, double>(graph.Comparer);
        var parent = new Dictionary<TVertex, TVertex>(graph.Comparer);
        var queue = new IndexedPriorityQueue<TVertex, double>(graph.Comparer);
        var selected = new List<ForestEdge<TVertex>>();
        var totalWeight = 0d;
        var componentCount = 0;

        foreach (var vertex in graph.Vertices) bestWeight[vertex] = double.PositiveInfinity;

        foreach (var start in graph.Vertices)
        {
            if (visited.Contains(start)) continue;
            componentCount++;
            bestWeight[start] = 0;
            queue.EnqueueOrDecrease(start, 0);

            while (queue.TryDequeue(out var current, out var weight))
            {
                if (!visited.Add(current)) continue;
                if (parent.TryGetValue(current, out var from))
                {
                    selected.Add(new ForestEdge<TVertex>(from, current, weight));
                    totalWeight += weight;
                }

                foreach (var edge in graph.GetOutgoingEdges(current))
                {
                    if (visited.Contains(edge.To) || edge.Weight >= bestWeight[edge.To]) continue;
                    bestWeight[edge.To] = edge.Weight;
                    parent[edge.To] = current;
                    queue.EnqueueOrDecrease(edge.To, edge.Weight);
                }
            }
        }

        return new MinimumSpanningForestResult<TVertex>(selected, totalWeight, componentCount);
    }

    /// <summary>
    /// Floyd-Warshall 动态规划：逐个允许顶点作为中转点，计算所有顶点对最短路。
    /// 时间 O(V³)，空间 O(V²)，适合顶点数较小、需要大量两点查询的图；支持负边但拒绝负环。
    /// </summary>
    public static AllPairsShortestPathResult<TVertex> FloydWarshall<TVertex>(WeightedGraph<TVertex> graph)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        var vertices = graph.Vertices.ToList();
        var indexes = new Dictionary<TVertex, int>(graph.Comparer);
        for (var index = 0; index < vertices.Count; index++) indexes.Add(vertices[index], index);

        var distances = new double[vertices.Count, vertices.Count];
        var next = new int[vertices.Count, vertices.Count];
        for (var from = 0; from < vertices.Count; from++)
        {
            for (var to = 0; to < vertices.Count; to++)
            {
                distances[from, to] = from == to ? 0 : double.PositiveInfinity;
                next[from, to] = from == to ? to : -1;
            }

            foreach (var edge in graph.GetOutgoingEdges(vertices[from]))
            {
                var to = indexes[edge.To];
                if (edge.Weight < distances[from, to])
                {
                    distances[from, to] = edge.Weight;
                    next[from, to] = to;
                }
            }
        }

        for (var intermediate = 0; intermediate < vertices.Count; intermediate++)
        {
            for (var from = 0; from < vertices.Count; from++)
            {
                if (double.IsPositiveInfinity(distances[from, intermediate])) continue;
                for (var to = 0; to < vertices.Count; to++)
                {
                    var candidate = distances[from, intermediate] + distances[intermediate, to];
                    if (candidate >= distances[from, to]) continue;
                    distances[from, to] = candidate;
                    next[from, to] = next[from, intermediate];
                }
            }
        }

        for (var vertex = 0; vertex < vertices.Count; vertex++)
        {
            if (distances[vertex, vertex] < 0)
            {
                throw new InvalidOperationException("The graph contains a negative-weight cycle.");
            }
        }

        return new AllPairsShortestPathResult<TVertex>(vertices, distances, next, graph.Comparer);
    }

    /// <summary>
    /// Tarjan 强连通分量：DFS 中用 discovery 和 low-link 值识别仍在栈中的环。
    /// 每个顶点和每条边只处理常数次，时间 O(V+E)，空间 O(V)。
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<TVertex>> TarjanStronglyConnectedComponents<TVertex>(
        WeightedGraph<TVertex> graph)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (!graph.IsDirected) throw new InvalidOperationException("Strongly connected components require a directed graph.");

        var discovery = new Dictionary<TVertex, int>(graph.Comparer);
        var low = new Dictionary<TVertex, int>(graph.Comparer);
        var onStack = new HashSet<TVertex>(graph.Comparer);
        var stack = new Stack<TVertex>();
        var result = new List<IReadOnlyList<TVertex>>();
        var time = 0;

        void Visit(TVertex vertex)
        {
            discovery[vertex] = low[vertex] = time++;
            stack.Push(vertex);
            onStack.Add(vertex);

            foreach (var edge in graph.GetOutgoingEdges(vertex))
            {
                if (!discovery.ContainsKey(edge.To))
                {
                    Visit(edge.To);
                    low[vertex] = Math.Min(low[vertex], low[edge.To]);
                }
                else if (onStack.Contains(edge.To))
                {
                    // 只使用仍在当前 DFS 栈中的祖先；已完成分量不能影响当前 low-link。
                    low[vertex] = Math.Min(low[vertex], discovery[edge.To]);
                }
            }

            if (low[vertex] != discovery[vertex]) return;
            var component = new List<TVertex>();
            TVertex current;
            do
            {
                current = stack.Pop();
                onStack.Remove(current);
                component.Add(current);
            } while (!graph.Comparer.Equals(current, vertex));
            result.Add(component);
        }

        foreach (var vertex in graph.Vertices)
        {
            if (!discovery.ContainsKey(vertex)) Visit(vertex);
        }

        return result;
    }

    /// <summary>
    /// 一次 DFS 同时求无向图的桥和割点。子树无法通过返祖边回到当前顶点以上时，树边是桥；
    /// 非根顶点若存在 low[child] &gt;= discovery[parent] 的子树就是割点。复杂度 O(V+E)。
    /// </summary>
    public static ConnectivityAnalysisResult<TVertex> AnalyzeUndirectedConnectivity<TVertex>(
        WeightedGraph<TVertex> graph)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (graph.IsDirected) throw new InvalidOperationException("Bridge and articulation analysis requires an undirected graph.");

        var discovery = new Dictionary<TVertex, int>(graph.Comparer);
        var low = new Dictionary<TVertex, int>(graph.Comparer);
        var articulation = new HashSet<TVertex>(graph.Comparer);
        var bridges = new List<ForestEdge<TVertex>>();
        var time = 0;

        void Visit(TVertex vertex, TVertex parent, bool hasParent)
        {
            discovery[vertex] = low[vertex] = time++;
            var children = 0;
            foreach (var edge in graph.GetOutgoingEdges(vertex))
            {
                if (hasParent && graph.Comparer.Equals(edge.To, parent)) continue;
                if (discovery.ContainsKey(edge.To))
                {
                    low[vertex] = Math.Min(low[vertex], discovery[edge.To]);
                    continue;
                }

                children++;
                Visit(edge.To, vertex, hasParent: true);
                low[vertex] = Math.Min(low[vertex], low[edge.To]);
                if (low[edge.To] > discovery[vertex])
                {
                    bridges.Add(new ForestEdge<TVertex>(vertex, edge.To, edge.Weight));
                }

                if (hasParent && low[edge.To] >= discovery[vertex]) articulation.Add(vertex);
            }

            if (!hasParent && children > 1) articulation.Add(vertex);
        }

        foreach (var vertex in graph.Vertices)
        {
            if (!discovery.ContainsKey(vertex)) Visit(vertex, default!, hasParent: false);
        }

        return new ConnectivityAnalysisResult<TVertex>(bridges, articulation);
    }

    /// <summary>
    /// A* 用 g(n)+h(n) 选择最有希望的顶点。边权必须非负，启发函数必须有限且非负；
    /// h 不高估真实剩余距离时可保证最优。h 恒为 0 时退化为 Dijkstra。
    /// </summary>
    public static ShortestPathResult<TVertex> AStar<TVertex>(
        WeightedGraph<TVertex> graph,
        TVertex source,
        TVertex target,
        Func<TVertex, double> heuristic)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(heuristic);
        if (!graph.ContainsVertex(source)) throw new ArgumentException("The source vertex does not exist.", nameof(source));
        if (!graph.ContainsVertex(target)) throw new ArgumentException("The target vertex does not exist.", nameof(target));

        var distances = new Dictionary<TVertex, double>(graph.Comparer);
        var previous = new Dictionary<TVertex, TVertex>(graph.Comparer);
        foreach (var vertex in graph.Vertices) distances[vertex] = double.PositiveInfinity;
        distances[source] = 0;

        var queue = new IndexedPriorityQueue<TVertex, double>(graph.Comparer);
        queue.EnqueueOrDecrease(source, ValidateHeuristic(heuristic(source)));

        while (queue.TryDequeue(out var current, out _))
        {
            if (graph.Comparer.Equals(current, target)) break;
            foreach (var edge in graph.GetOutgoingEdges(current))
            {
                if (edge.Weight < 0) throw new InvalidOperationException("A* does not support negative edge weights.");
                var candidate = distances[current] + edge.Weight;
                if (candidate >= distances[edge.To]) continue;
                distances[edge.To] = candidate;
                previous[edge.To] = current;
                queue.EnqueueOrDecrease(edge.To, candidate + ValidateHeuristic(heuristic(edge.To)));
            }
        }

        return new ShortestPathResult<TVertex>(source, distances, previous, graph.Comparer);
    }

    /// <summary>
    /// Dinic 最大流：BFS 构造残量网络分层图，DFS 在分层图上反复发送阻塞流。
    /// 一般网络复杂度 O(V²E)；算法在克隆的残量网络上运行，不修改调用者的容量定义。
    /// </summary>
    public static MaximumFlowResult<TVertex> DinicMaximumFlow<TVertex>(
        FlowNetwork<TVertex> network,
        TVertex source,
        TVertex sink)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(network);
        var residual = network.Clone();
        if (!residual.Adjacency.ContainsKey(source)) throw new ArgumentException("The source vertex does not exist.", nameof(source));
        if (!residual.Adjacency.ContainsKey(sink)) throw new ArgumentException("The sink vertex does not exist.", nameof(sink));
        if (residual.Comparer.Equals(source, sink)) throw new ArgumentException("Source and sink must be different.");

        var level = new Dictionary<TVertex, int>(residual.Comparer);

        bool BuildLevelGraph()
        {
            level.Clear();
            var queue = new Queue<TVertex>();
            level[source] = 0;
            queue.Enqueue(source);
            while (queue.TryDequeue(out var current))
            {
                foreach (var edge in residual.Adjacency[current])
                {
                    if (edge.ResidualCapacity <= 0 || level.ContainsKey(edge.To)) continue;
                    level[edge.To] = level[current] + 1;
                    queue.Enqueue(edge.To);
                }
            }

            return level.ContainsKey(sink);
        }

        double Send(TVertex current, double available, Dictionary<TVertex, int> nextEdge)
        {
            if (residual.Comparer.Equals(current, sink)) return available;
            var edges = residual.Adjacency[current];
            while (nextEdge[current] < edges.Count)
            {
                var edge = edges[nextEdge[current]];
                if (edge.ResidualCapacity > 0 &&
                    level.TryGetValue(edge.To, out var nextLevel) && nextLevel == level[current] + 1)
                {
                    var sent = Send(edge.To, Math.Min(available, edge.ResidualCapacity), nextEdge);
                    if (sent > 0)
                    {
                        edge.ResidualCapacity -= sent;
                        residual.Adjacency[edge.To][edge.ReverseIndex].ResidualCapacity += sent;
                        return sent;
                    }
                }

                nextEdge[current]++;
            }

            return 0;
        }

        var maximumFlow = 0d;
        while (BuildLevelGraph())
        {
            var nextEdge = residual.Vertices.ToDictionary(vertex => vertex, _ => 0, residual.Comparer);
            double sent;
            while ((sent = Send(source, double.PositiveInfinity, nextEdge)) > 0) maximumFlow += sent;
        }

        var flows = residual.Adjacency.Values
            .SelectMany(edges => edges)
            .Where(edge => edge.IsOriginal)
            .Select(edge => new FlowEdgeResult<TVertex>(
                edge.From, edge.To, edge.OriginalCapacity, edge.OriginalCapacity - edge.ResidualCapacity))
            .ToArray();
        return new MaximumFlowResult<TVertex>(maximumFlow, flows);
    }

    private static double ValidateHeuristic(double value)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Heuristic values must be finite and non-negative.");
        }

        return value;
    }
}
