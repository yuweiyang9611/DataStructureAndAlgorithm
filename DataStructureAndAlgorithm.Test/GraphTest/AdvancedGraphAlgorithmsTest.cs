using DataStructureAndAlgorithm.Graph;

namespace DataStructureAndAlgorithm.Test.GraphTest;

public class AdvancedGraphAlgorithmsTest
{
    [Fact]
    public void BellmanFord_ShouldSupportNegativeEdgesAndRestorePath()
    {
        var graph = new WeightedGraph<string>();
        graph.AddEdge("S", "A", 4);
        graph.AddEdge("S", "B", 5);
        graph.AddEdge("A", "B", -2);
        graph.AddEdge("B", "T", 3);

        var result = AdvancedGraphAlgorithms.BellmanFord(graph, "S");

        Assert.Equal(5, result.Distances["T"]);
        Assert.Equal(["S", "A", "B", "T"], result.GetPathTo("T"));
    }

    [Fact]
    public void BellmanFord_ShouldDetectReachableNegativeCycle()
    {
        var graph = new WeightedGraph<int>();
        graph.AddEdge(1, 2, 1);
        graph.AddEdge(2, 3, -2);
        graph.AddEdge(3, 2, -2);

        Assert.Throws<InvalidOperationException>(() => AdvancedGraphAlgorithms.BellmanFord(graph, 1));
    }

    [Fact]
    public void Kruskal_ShouldReturnMinimumSpanningForestForDisconnectedGraph()
    {
        var graph = new WeightedGraph<string>(isDirected: false);
        graph.AddEdge("A", "B", 1);
        graph.AddEdge("A", "C", 5);
        graph.AddEdge("B", "C", 2);
        graph.AddEdge("D", "E", 4);

        var result = AdvancedGraphAlgorithms.KruskalMinimumSpanningForest(graph);

        Assert.Equal(2, result.ComponentCount);
        Assert.Equal(3, result.Edges.Count);
        Assert.Equal(7, result.TotalWeight);
    }
}
