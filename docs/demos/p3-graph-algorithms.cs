// Johnson、匹配、欧拉路径、LCA 与最大流：五个 P3 图算法各跑一个小例子。
// 给二分图多加一条边，看最大匹配能不能从 3 变成 4。
using DataStructureAndAlgorithm.Graph;

public static class Demo
{
    public static void Run()
    {
        var graph = new WeightedGraph<string>(isDirected: true);
        graph.AddEdge("A", "B", -2);
        graph.AddEdge("B", "C", 3);
        graph.AddEdge("A", "C", 4);
        graph.AddEdge("C", "D", 1);
        graph.AddEdge("D", "B", 2);
        var johnson = P3GraphAlgorithms.JohnsonAllPairsShortestPaths(graph);
        Console.WriteLine($"Johnson 全源最短路（含负权边）：A->D = {johnson.GetDistance("A", "D")}，路径 {string.Join("->", johnson.GetPath("A", "D"))}；B->A = {johnson.GetDistance("B", "A")}");

        var matching = P3GraphAlgorithms.HopcroftKarpMaximumMatching(new[] { ("张三", "前端"), ("张三", "后端"), ("李四", "后端"), ("王五", "后端"), ("王五", "测试") });
        Console.WriteLine($"Hopcroft-Karp 最大匹配 {matching.Count} 对：{string.Join(", ", matching.Matches.Select(match => $"{match.Left}-{match.Right}"))}");

        var trail = P3GraphAlgorithms.DirectedEulerianTrail(new[] { ("JFK", "SFO"), ("JFK", "ATL"), ("SFO", "ATL"), ("ATL", "JFK"), ("ATL", "SFO") });
        Console.WriteLine($"有向欧拉路径（每条边恰好走一次）：{string.Join(" -> ", trail)}");

        var tree = new BinaryLiftingTree<string>("root", new[] { ("root", "a"), ("root", "b"), ("a", "c"), ("a", "d"), ("d", "e") });
        var ancestor = tree.TryGetKthAncestor("e", 2, out var found) ? found : "无";
        Console.WriteLine($"倍增 LCA(c, e) = {tree.LowestCommonAncestor("c", "e")}，距离 {tree.Distance("c", "e")}；e 的第 2 级祖先 {ancestor}");

        var network = new FlowNetwork<string>();
        network.AddEdge("s", "a", 3);
        network.AddEdge("s", "b", 2);
        network.AddEdge("a", "b", 1);
        network.AddEdge("a", "t", 2);
        network.AddEdge("b", "t", 3);
        var cut = P3FlowAlgorithms.DinicMaximumFlowWithMinimumCut(network, "s", "t");
        Console.WriteLine($"Dinic 最大流 {cut.Value}，最小割的源侧顶点：{string.Join(", ", cut.SourceSideMinimumCut)}");
    }
}
