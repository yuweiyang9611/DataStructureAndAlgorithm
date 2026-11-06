// 加权图、最短路与最小生成森林：Dijkstra 要求非负权，Bellman-Ford 允许负权边，Kruskal 不关心方向。
// 把某条边的权改成负数再跑，看 Dijkstra 和 Bellman-Ford 谁还给出正确答案。
using DataStructureAndAlgorithm.Graph;

public static class Demo
{
    public static void Run()
    {
        var graph = new WeightedGraph<string>(isDirected: true);
        graph.AddEdge("A", "B", 5);
        graph.AddEdge("A", "C", 1);
        graph.AddEdge("C", "B", 2);
        graph.AddEdge("B", "D", 1);
        graph.AddEdge("C", "D", 5);
        var dijkstra = WeightedGraphAlgorithms.Dijkstra(graph, "A");
        Console.WriteLine($"Dijkstra 从 A 出发：到 D 的距离 {dijkstra.Distances["D"]}，路径 {string.Join(" -> ", dijkstra.GetPathTo("D"))}");
        Console.WriteLine($"这张图是 DAG，拓扑序：{string.Join(" ", WeightedGraphAlgorithms.TopologicalSort(graph))}");

        var negative = new WeightedGraph<string>(isDirected: true);
        negative.AddEdge("S", "A", 4);
        negative.AddEdge("S", "B", 2);
        negative.AddEdge("B", "A", -3);
        negative.AddEdge("A", "T", 1);
        var bellmanFord = AdvancedGraphAlgorithms.BellmanFord(negative, "S");
        Console.WriteLine($"Bellman-Ford 允许负权边：到 T 的距离 {bellmanFord.Distances["T"]}，路径 {string.Join(" -> ", bellmanFord.GetPathTo("T"))}");

        var roads = new WeightedGraph<string>(isDirected: false);
        roads.AddEdge("A", "B", 4);
        roads.AddEdge("A", "C", 1);
        roads.AddEdge("B", "C", 2);
        roads.AddEdge("B", "D", 5);
        roads.AddEdge("C", "D", 8);
        roads.AddEdge("E", "F", 3);
        var forest = AdvancedGraphAlgorithms.KruskalMinimumSpanningForest(roads);
        Console.WriteLine($"Kruskal 最小生成森林：总权 {forest.TotalWeight}，{forest.ComponentCount} 个连通分量");
        foreach (var edge in forest.Edges) Console.WriteLine($"  {edge.First}-{edge.Second}（权 {edge.Weight}）");
    }
}
