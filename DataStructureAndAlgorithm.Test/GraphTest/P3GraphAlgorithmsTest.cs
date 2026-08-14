using DataStructureAndAlgorithm.Graph;

namespace DataStructureAndAlgorithm.Test.GraphTest;

public class P3GraphAlgorithmsTest
{
    [Fact]
    public void Johnson_MatchesFloydWarshallOnSparseGraphWithNegativeEdges()
    {
        var graph = new WeightedGraph<string>();
        graph.AddEdge("A", "B", 4);
        graph.AddEdge("A", "C", 10);
        graph.AddEdge("B", "C", -2);
        graph.AddEdge("C", "D", 3);
        graph.AddEdge("D", "B", 1);
        graph.AddVertex("X");

        var johnson = P3GraphAlgorithms.JohnsonAllPairsShortestPaths(graph);
        var floyd = ComprehensiveGraphAlgorithms.FloydWarshall(graph);

        foreach (var from in graph.Vertices)
            foreach (var to in graph.Vertices)
                Assert.Equal(floyd.GetDistance(from, to), johnson.GetDistance(from, to), 8);
        Assert.True(johnson.GetPath("A", "D").SequenceEqual(["A", "B", "C", "D"]));
    }

    [Fact]
    public void Johnson_RejectsNegativeCycles()
    {
        var graph = new WeightedGraph<int>();
        graph.AddEdge(1, 2, -2);
        graph.AddEdge(2, 1, 1);
        Assert.Throws<InvalidOperationException>(() => P3GraphAlgorithms.JohnsonAllPairsShortestPaths(graph));
    }

    [Fact]
    public void Johnson_RejectsTinyNegativeSelfLoop()
    {
        var graph = new WeightedGraph<string>();
        graph.AddEdge("vertex", "vertex", -1e-13);

        Assert.Throws<InvalidOperationException>(() => P3GraphAlgorithms.JohnsonAllPairsShortestPaths(graph));
    }

    [Fact]
    public void Dinic_PreservesPositiveCapacitiesBelowPreviousEpsilon()
    {
        const double capacity = 1e-13;
        var network = new FlowNetwork<string>();
        network.AddEdge("source", "sink", capacity);

        var result = P3FlowAlgorithms.DinicMaximumFlowWithMinimumCut(network, "source", "sink");

        Assert.Equal(capacity, result.Value);
        Assert.Equal(capacity, Assert.Single(result.Edges).Flow);
        Assert.Equal(new[] { "source" }, result.SourceSideMinimumCut);
    }

    [Fact]
    public void HopcroftKarp_FindsMaximumMatching()
    {
        var result = P3GraphAlgorithms.HopcroftKarpMaximumMatching([
            ("L1", "R1"), ("L1", "R2"), ("L2", "R1"), ("L3", "R2"), ("L3", "R3")]);

        Assert.Equal(3, result.Count);
        Assert.Equal(3, result.Matches.Select(match => match.Left).Distinct().Count());
        Assert.Equal(3, result.Matches.Select(match => match.Right).Distinct().Count());
    }

    [Fact]
    public void DirectedEulerianTrail_UsesEveryEdgeOnce()
    {
        (string From, string To)[] edges = [("A", "B"), ("B", "C"), ("C", "A"), ("A", "D")];
        var trail = P3GraphAlgorithms.DirectedEulerianTrail(edges);

        Assert.Equal(edges.Length + 1, trail.Count);
        Assert.Equal("A", trail[0]);
        Assert.Equal("D", trail[^1]);
        var used = trail.Zip(trail.Skip(1)).Select(pair => (pair.First, pair.Second)).ToArray();
        Assert.All(edges, edge => Assert.Contains(edge, used));
    }

    [Fact]
    public void DirectedEulerianTrail_ValueTypeCircuitDoesNotUseDefaultAsASentinel()
    {
        var trail = P3GraphAlgorithms.DirectedEulerianTrail<int>([(5, 6), (6, 5)]);

        Assert.Equal([5, 6, 5], trail);
    }

    [Fact]
    public void BinaryLifting_AnswersLcaDistanceAndAncestors()
    {
        var tree = new BinaryLiftingTree<int>(1, [(1, 2), (1, 3), (2, 4), (2, 5), (3, 6)]);

        Assert.Equal(2, tree.LowestCommonAncestor(4, 5));
        Assert.Equal(1, tree.LowestCommonAncestor(4, 6));
        Assert.Equal(4, tree.Distance(4, 6));
        Assert.True(tree.TryGetKthAncestor(5, 2, out var ancestor));
        Assert.Equal(1, ancestor);
        Assert.False(tree.TryGetKthAncestor(5, 3, out _));
    }
}
