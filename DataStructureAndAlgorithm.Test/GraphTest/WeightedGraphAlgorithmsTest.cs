using DataStructureAndAlgorithm.Graph;

namespace DataStructureAndAlgorithm.Test.GraphTest;

public class WeightedGraphAlgorithmsTest
{
    [Fact]
    public void Dijkstra_ShouldCalculateDistancesAndRestorePath()
    {
        var graph = new WeightedGraph<string>();
        graph.AddEdge("A", "B", 4);
        graph.AddEdge("A", "C", 1);
        graph.AddEdge("C", "B", 2);
        graph.AddEdge("B", "D", 1);
        graph.AddEdge("C", "D", 5);
        graph.AddVertex("unreachable");

        var result = WeightedGraphAlgorithms.Dijkstra(graph, "A");

        Assert.Equal(4, result.Distances["D"]);
        Assert.Equal(["A", "C", "B", "D"], result.GetPathTo("D"));
        Assert.Empty(result.GetPathTo("unreachable"));
    }

    [Fact]
    public void Dijkstra_WhenGraphContainsNegativeEdge_ShouldThrow()
    {
        var graph = new WeightedGraph<int>();
        graph.AddEdge(1, 2, -1);

        Assert.Throws<InvalidOperationException>(
            () => WeightedGraphAlgorithms.Dijkstra(graph, 1));
    }

    [Fact]
    public void OutgoingEdges_ShouldNotAllowCallersToMutateTheGraph()
    {
        var graph = new WeightedGraph<string>();
        graph.AddEdge("A", "B", 1);

        var exposedEdges = Assert.IsAssignableFrom<IList<WeightedEdge<string>>>(
            graph.GetOutgoingEdges("A"));

        Assert.Throws<NotSupportedException>(() => exposedEdges.Clear());
        Assert.Equal(1, WeightedGraphAlgorithms.Dijkstra(graph, "A").Distances["B"]);
    }

    [Fact]
    public void TopologicalSort_ShouldPlaceEveryDependencyBeforeItsConsumer()
    {
        var graph = new WeightedGraph<string>();
        graph.AddEdge("foundation", "linear", 1);
        graph.AddEdge("linear", "tree", 1);
        graph.AddEdge("linear", "graph", 1);
        graph.AddEdge("tree", "dynamic-programming", 1);
        graph.AddEdge("graph", "dynamic-programming", 1);

        var order = WeightedGraphAlgorithms.TopologicalSort(graph);

        AssertComesBefore(order, "foundation", "linear");
        AssertComesBefore(order, "linear", "tree");
        AssertComesBefore(order, "linear", "graph");
        AssertComesBefore(order, "tree", "dynamic-programming");
        AssertComesBefore(order, "graph", "dynamic-programming");
    }

    [Fact]
    public void TopologicalSort_WhenGraphContainsCycle_ShouldThrow()
    {
        var graph = new WeightedGraph<int>();
        graph.AddEdge(1, 2, 1);
        graph.AddEdge(2, 1, 1);

        Assert.Throws<InvalidOperationException>(
            () => WeightedGraphAlgorithms.TopologicalSort(graph));
    }

    private static void AssertComesBefore<T>(IReadOnlyList<T> values, T first, T second)
    {
        Assert.True(
            values.ToList().IndexOf(first) < values.ToList().IndexOf(second),
            $"Expected {first} to appear before {second}.");
    }
}
