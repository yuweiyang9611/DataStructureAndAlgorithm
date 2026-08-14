namespace DataStructureAndAlgorithm.Graph;

public static class GraphWithAdjacencyListExtension
{
    /// <summary>
    /// 广度优先搜索(层序遍历)
    /// </summary>
    /// <param name="graph">图(邻接表表示)</param>
    /// <param name="startVet">从哪个顶点开始搜索</param>
    /// <param name="operation">委托：用于对顶点的操作</param>
    /// <returns>顶点遍历序列</returns>
    public static List<Vertex<TValue>> GraphBfs<TValue>(this GraphWithAdjacencyList<TValue> graph,
        Vertex<TValue> startVet, Action<Vertex<TValue>>? operation = null)
        where TValue : notnull
    {
        ValidateStartVertex(graph, startVet);

        // 动态数组：用于存储顶点遍历序列
        var result = new List<Vertex<TValue>>(graph.Size);
        // Hash表：用于记录已经被访问过的顶点
        var visited = new HashSet<Vertex<TValue>>();
        // 队列：用于辅助进行广度优先遍历(类比层序遍历)
        var queue = new Queue<Vertex<TValue>>();
        queue.Enqueue(startVet);
        visited.Add(startVet);
        // 以顶点startVet为起点，循环直至访问完所有顶点
        while (queue.Count > 0)
        {
            // 队首顶点出队
            var vertex = queue.Dequeue();
            // 加入遍历序列
            result.Add(vertex);
            // 对顶点的操作
            operation?.Invoke(vertex);
            // 搜索该顶点连接了哪些顶点
            foreach (var adjVertex in graph.GetNeighbors(vertex))
            {
                // Add同时完成查询和标记，避免对HashSet进行两次查找
                if (visited.Add(adjVertex)) queue.Enqueue(adjVertex);
            }
        }

        // 返回顶点遍历序列
        return result;
    }

    /// <summary>
    /// 深度优先搜索(根-右-左)
    /// </summary>
    /// <param name="graph">图(邻接表表示)</param>
    /// <param name="startVet">从哪个顶点开始搜索</param>
    /// <param name="operation">委托：用于对顶点的操作</param>
    /// <returns>顶点遍历序列</returns>
    public static List<Vertex<TValue>> GraphDfs<TValue>(this GraphWithAdjacencyList<TValue> graph,
        Vertex<TValue> startVet, Action<Vertex<TValue>>? operation = null)
        where TValue : notnull
    {
        ValidateStartVertex(graph, startVet);

        // 动态数组：用于存储顶点遍历序列
        var result = new List<Vertex<TValue>>(graph.Size);
        // Hash表：用于记录已经被访问过的顶点
        var visited = new HashSet<Vertex<TValue>>();
        // 栈：用于辅助进行深度优先遍历(类比层序遍历)
        var stack = new Stack<Vertex<TValue>>();
        // 首顶点入栈
        stack.Push(startVet);
        // 标记该顶点已经被访问
        visited.Add(startVet);
        // 以顶点startVet为起点，循环直至访问完所有顶点
        while (stack.Count > 0)
        {
            var vertex = stack.Pop();
            // 加入遍历序列
            result.Add(vertex);
            // 对顶点的操作
            operation?.Invoke(vertex);
            foreach (var adjVet in graph.GetNeighbors(vertex))
            {
                // Add同时完成查询和标记，避免对HashSet进行两次查找
                if (visited.Add(adjVet)) stack.Push(adjVet);
            }
        }

        return result;
    }

    private static void ValidateStartVertex<TValue>(GraphWithAdjacencyList<TValue> graph,
        Vertex<TValue> startVertex)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (!graph.ContainsVertex(startVertex))
        {
            throw new ArgumentException("The start vertex does not exist in the graph.", nameof(startVertex));
        }
    }
}
