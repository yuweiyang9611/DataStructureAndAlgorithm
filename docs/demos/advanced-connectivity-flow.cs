// 全源最短路、低链接、A* 与费用流：一张小图上跑完 Floyd-Warshall、Tarjan、桥与割点、A* 和两种流。
// 给网格加一堵墙（删掉几条边），看 A* 绕路后的步数。
using DataStructureAndAlgorithm.Graph;

public static class Demo
{
    public static void Run()
    {
        var directed = new WeightedGraph<string>(isDirected: true);
        foreach (var (from, to, weight) in new[] { ("A", "B", 3.0), ("B", "C", 1.0), ("C", "A", 2.0), ("C", "D", 7.0), ("D", "E", 1.0), ("E", "D", 1.0) })
        {
            directed.AddEdge(from, to, weight);
        }
        var allPairs = ComprehensiveGraphAlgorithms.FloydWarshall(directed);
        Console.WriteLine($"Floyd-Warshall：A->E = {allPairs.GetDistance("A", "E")}，路径 {string.Join("->", allPairs.GetPath("A", "E"))}；E->A = {allPairs.GetDistance("E", "A")}（不可达）");
        var components = ComprehensiveGraphAlgorithms.TarjanStronglyConnectedComponents(directed);
        Console.WriteLine($"Tarjan 强连通分量：{string.Join(" | ", components.Select(component => string.Join(",", component.OrderBy(vertex => vertex, StringComparer.Ordinal))))}");

        var undirected = new WeightedGraph<string>(isDirected: false);
        foreach (var (from, to) in new[] { ("A", "B"), ("B", "C"), ("C", "A"), ("C", "D"), ("D", "E") }) undirected.AddEdge(from, to, 1);
        var connectivity = ComprehensiveGraphAlgorithms.AnalyzeUndirectedConnectivity(undirected);
        Console.WriteLine($"桥：{string.Join(", ", connectivity.Bridges.Select(bridge => $"{bridge.First}-{bridge.Second}"))}；割点：{string.Join(", ", connectivity.ArticulationPoints.OrderBy(vertex => vertex, StringComparer.Ordinal))}");

        var grid = new WeightedGraph<(int X, int Y)>(isDirected: false);
        for (var x = 0; x < 4; x++)
        {
            for (var y = 0; y < 4; y++)
            {
                if (x < 3) grid.AddEdge((x, y), (x + 1, y), 1);
                if (y < 3) grid.AddEdge((x, y), (x, y + 1), 1);
            }
        }
        var start = (X: 0, Y: 0);
        var goal = (X: 3, Y: 3);
        Func<(int X, int Y), double> manhattan = vertex => Math.Abs(goal.X - vertex.X) + Math.Abs(goal.Y - vertex.Y);
        var aStar = ComprehensiveGraphAlgorithms.AStar(grid, start, goal, manhattan);
        Console.WriteLine($"A* 在 4×4 网格上 {start} -> {goal}：{aStar.Distances[goal]} 步，{string.Join(" ", aStar.GetPathTo(goal))}");

        var network = new FlowNetwork<string>();
        network.AddEdge("s", "a", 3);
        network.AddEdge("s", "b", 2);
        network.AddEdge("a", "b", 1);
        network.AddEdge("a", "t", 2);
        network.AddEdge("b", "t", 3);
        var flow = ComprehensiveGraphAlgorithms.DinicMaximumFlow(network, "s", "t");
        Console.WriteLine($"Dinic 最大流 = {flow.Value}，用到 {flow.Edges.Count(edge => edge.Flow > 0)} 条边");

        var costNetwork = new MinCostFlowNetwork<string>();
        costNetwork.AddEdge("s", "a", capacity: 2, unitCost: 1);
        costNetwork.AddEdge("s", "b", capacity: 2, unitCost: 3);
        costNetwork.AddEdge("a", "b", capacity: 1, unitCost: 1);
        costNetwork.AddEdge("a", "t", capacity: 1, unitCost: 2);
        costNetwork.AddEdge("b", "t", capacity: 2, unitCost: 1);
        var cheapest = MinCostFlowAlgorithms.MinimumCostMaximumFlow(costNetwork, "s", "t");
        Console.WriteLine($"最小费用最大流：流量 {cheapest.Flow}，费用 {cheapest.Cost}");
    }
}
