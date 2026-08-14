using DataStructureAndAlgorithm.Graph;

namespace DataStructureAndAlgorithm.Test.GraphTest;

public class ComprehensiveGraphAlgorithmsTest
{
    [Fact]
    public void Prim_MatchesKruskalForADisconnectedGraph()
    {
        var graph = new WeightedGraph<string>(isDirected: false);
        graph.AddEdge("A", "B", 4);
        graph.AddEdge("A", "C", 1);
        graph.AddEdge("B", "C", 2);
        graph.AddEdge("B", "D", 5);
        graph.AddEdge("C", "D", 3);
        graph.AddEdge("X", "Y", -2);

        var prim = ComprehensiveGraphAlgorithms.PrimMinimumSpanningForest(graph);
        var kruskal = AdvancedGraphAlgorithms.KruskalMinimumSpanningForest(graph);

        Assert.Equal(2, prim.ComponentCount);
        Assert.Equal(graph.VertexCount - prim.ComponentCount, prim.Edges.Count);
        Assert.Equal(kruskal.TotalWeight, prim.TotalWeight);
    }

    [Fact]
    public void FloydWarshall_SupportsNegativeEdgesAndRestoresPaths()
    {
        var graph = new WeightedGraph<string>();
        graph.AddEdge("A", "B", 4);
        graph.AddEdge("A", "C", 10);
        graph.AddEdge("B", "C", -2);
        graph.AddEdge("C", "D", 3);
        graph.AddVertex("X");

        var result = ComprehensiveGraphAlgorithms.FloydWarshall(graph);

        Assert.Equal(5, result.GetDistance("A", "D"));
        Assert.True(result.GetPath("A", "D").SequenceEqual(["A", "B", "C", "D"]));
        Assert.True(double.IsPositiveInfinity(result.GetDistance("D", "A")));
        Assert.Empty(result.GetPath("A", "X"));
    }

    [Fact]
    public void FloydWarshall_RejectsNegativeCycles()
    {
        var graph = new WeightedGraph<int>();
        graph.AddEdge(1, 2, -2);
        graph.AddEdge(2, 1, 1);

        Assert.Throws<InvalidOperationException>(() => ComprehensiveGraphAlgorithms.FloydWarshall(graph));
    }

    [Fact]
    public void Tarjan_FindsEveryStronglyConnectedComponent()
    {
        var graph = new WeightedGraph<int>();
        graph.AddEdge(1, 2, 1);
        graph.AddEdge(2, 3, 1);
        graph.AddEdge(3, 1, 1);
        graph.AddEdge(3, 4, 1);
        graph.AddEdge(4, 5, 1);
        graph.AddEdge(5, 4, 1);
        graph.AddEdge(5, 6, 1);

        var components = ComprehensiveGraphAlgorithms.TarjanStronglyConnectedComponents(graph)
            .Select(component => string.Join(",", component.Order()))
            .Order()
            .ToArray();

        Assert.True(components.SequenceEqual(["1,2,3", "4,5", "6"]));
    }

    [Fact]
    public void LowLinkAnalysis_FindsBridgesAndArticulationPoints()
    {
        var graph = new WeightedGraph<int>(isDirected: false);
        graph.AddEdge(1, 2, 1);
        graph.AddEdge(2, 3, 1);
        graph.AddEdge(3, 1, 1);
        graph.AddEdge(2, 4, 1);
        graph.AddEdge(4, 5, 1);

        var result = ComprehensiveGraphAlgorithms.AnalyzeUndirectedConnectivity(graph);
        var bridges = result.Bridges
            .Select(edge => (Math.Min(edge.First, edge.Second), Math.Max(edge.First, edge.Second)))
            .Order()
            .ToArray();

        Assert.True(bridges.SequenceEqual([(2, 4), (4, 5)]));
        Assert.True(result.ArticulationPoints.Order().SequenceEqual([2, 4]));
    }

    [Fact]
    public void AStar_UsesAnAdmissibleHeuristicAndReturnsAnOptimalRoute()
    {
        var graph = new WeightedGraph<string>();
        graph.AddEdge("S", "A", 2);
        graph.AddEdge("S", "B", 1);
        graph.AddEdge("B", "C", 1);
        graph.AddEdge("C", "T", 5);
        graph.AddEdge("A", "T", 2);
        var heuristic = new Dictionary<string, double>
        {
            ["S"] = 4,
            ["A"] = 2,
            ["B"] = 3,
            ["C"] = 2,
            ["T"] = 0
        };

        var result = ComprehensiveGraphAlgorithms.AStar(graph, "S", "T", vertex => heuristic[vertex]);

        Assert.Equal(4, result.Distances["T"]);
        Assert.True(result.GetPathTo("T").SequenceEqual(["S", "A", "T"]));
    }

    [Fact]
    public void Dinic_ComputesMaximumFlowWithoutMutatingTheInputNetwork()
    {
        // CLRS 的经典网络最大流为 23。
        var network = new FlowNetwork<string>();
        network.AddEdge("s", "v1", 16);
        network.AddEdge("s", "v2", 13);
        network.AddEdge("v1", "v2", 10);
        network.AddEdge("v2", "v1", 4);
        network.AddEdge("v1", "v3", 12);
        network.AddEdge("v3", "v2", 9);
        network.AddEdge("v2", "v4", 14);
        network.AddEdge("v4", "v3", 7);
        network.AddEdge("v3", "t", 20);
        network.AddEdge("v4", "t", 4);

        var first = ComprehensiveGraphAlgorithms.DinicMaximumFlow(network, "s", "t");
        var second = ComprehensiveGraphAlgorithms.DinicMaximumFlow(network, "s", "t");

        Assert.Equal(23, first.Value);
        Assert.Equal(first.Value, second.Value);
        Assert.All(first.Edges, edge => Assert.InRange(edge.Flow, 0, edge.Capacity));
        Assert.Equal(first.Value, first.Edges.Where(edge => edge.From == "s").Sum(edge => edge.Flow));
        Assert.Equal(first.Value, first.Edges.Where(edge => edge.To == "t").Sum(edge => edge.Flow));
    }

    [Fact]
    public void Dinic_PreservesPositiveCapacitiesBelowPreviousEpsilon()
    {
        const double capacity = 1e-13;
        var network = new FlowNetwork<string>();
        network.AddEdge("source", "sink", capacity);

        var result = ComprehensiveGraphAlgorithms.DinicMaximumFlow(network, "source", "sink");

        Assert.Equal(capacity, result.Value);
        Assert.Equal(capacity, Assert.Single(result.Edges).Flow);
    }

    [Fact]
    public void FlowNetwork_RejectsSelfLoopsAndInvalidCapacities()
    {
        var network = new FlowNetwork<int>();

        Assert.Throws<ArgumentException>(() => network.AddEdge(1, 1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.AddEdge(1, 2, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.AddEdge(1, 2, double.NaN));
    }
}
