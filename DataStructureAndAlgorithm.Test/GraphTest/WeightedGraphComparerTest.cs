using DataStructureAndAlgorithm.Graph;

namespace DataStructureAndAlgorithm.Test.GraphTest;

public class WeightedGraphComparerTest
{
    [Fact]
    public void Dijkstra_ShouldUseGraphComparerForEveryVertexLookup()
    {
        var graph = new WeightedGraph<string>(comparer: StringComparer.OrdinalIgnoreCase);
        graph.AddEdge("A", "B", 2);

        var result = WeightedGraphAlgorithms.Dijkstra(graph, "a");

        Assert.Equal(2, result.Distances["b"]);
        Assert.Equal(["a", "b"], result.GetPathTo("b"));
    }
}
