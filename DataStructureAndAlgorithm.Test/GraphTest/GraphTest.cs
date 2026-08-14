using DataStructureAndAlgorithm.Graph;
using Xunit.Abstractions;

namespace DataStructureAndAlgorithm.Test.GraphTest;

public class GraphTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void GraphWithAdjacencyMatrixCurdTest()
    {
        var vertices = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
        var edges = new int[][]
        {
            new[] { 0, 5 },
            new[] { 0, 3 },
            new[] { 0, 2 },
            new[] { 0, 1 },
            new[] { 3, 4 },
            new[] { 3, 6 },
            new[] { 1, 4 }
        };
        var graph = new GraphWithAdjacencyMatrix<int>(vertices, edges);
        testOutputHelper.WriteLine($"顶点个数 = {graph.Size.ToString()}");
        testOutputHelper.WriteLine(graph.Print());
        graph.AddEdge(5, 6);
        graph.RemoveVertex(0);
        graph.RemoveEdge(1, 4);
        testOutputHelper.WriteLine($"顶点个数：{graph.Size.ToString()}");
        testOutputHelper.WriteLine(graph.Print());
    }

    [Fact]
    public void GraphWithAdjacencyListTest()
    {
        // 注意：只有结构体能这么赋值，如果结构体中的值全部相等则结构体相等；
        // 对于类，只有引用相等才相等，即使两个类具有相同值如果这两个类的引用不同也不相等
        var edges = new Vertex<int>[][]
        {
            new[] { new Vertex<int>(1), new Vertex<int>(2) },
            new[] { new Vertex<int>(1), new Vertex<int>(3) },
            new[] { new Vertex<int>(2), new Vertex<int>(3) },
            new[] { new Vertex<int>(3), new Vertex<int>(4) },
            new[] { new Vertex<int>(3), new Vertex<int>(5) },
        };

        var graph = new GraphWithAdjacencyList<int>(edges);
        testOutputHelper.WriteLine(graph.Print());
    }

    [Fact]
    public void GraphBfsTest()
    {
        var edges = new Vertex<int>[][]
        {
            new[] { new Vertex<int>(0), new Vertex<int>(1) },
            new[] { new Vertex<int>(1), new Vertex<int>(2) },
            new[] { new Vertex<int>(3), new Vertex<int>(4) },
            new[] { new Vertex<int>(4), new Vertex<int>(5) },
            new[] { new Vertex<int>(6), new Vertex<int>(7) },
            new[] { new Vertex<int>(7), new Vertex<int>(8) },
            new[] { new Vertex<int>(0), new Vertex<int>(3) },
            new[] { new Vertex<int>(3), new Vertex<int>(6) },
            new[] { new Vertex<int>(1), new Vertex<int>(4) },
            new[] { new Vertex<int>(4), new Vertex<int>(7) },
            new[] { new Vertex<int>(2), new Vertex<int>(5) },
            new[] { new Vertex<int>(5), new Vertex<int>(8) },
        };

        var graph = new GraphWithAdjacencyList<int>(edges);
        testOutputHelper.WriteLine(graph.Print());

        var graphBfs = graph.GraphBfs(edges[0][0],
            vet => { testOutputHelper.WriteLine($"当前节点：{vet.Value}"); });
        var result = string.Join(", ", graphBfs.Select(item => item.Value));
        testOutputHelper.WriteLine(result);
    }

    [Fact]
    public void GraphDfsTest()
    {
        var edges = new Vertex<int>[][]
        {
            new[] { new Vertex<int>(1), new Vertex<int>(2) },
            new[] { new Vertex<int>(1), new Vertex<int>(3) },
            new[] { new Vertex<int>(2), new Vertex<int>(4) },
            new[] { new Vertex<int>(2), new Vertex<int>(5) },
            new[] { new Vertex<int>(3), new Vertex<int>(6) },
            new[] { new Vertex<int>(3), new Vertex<int>(7) },
        };

        var graph = new GraphWithAdjacencyList<int>(edges);
        testOutputHelper.WriteLine(graph.Print());

        var graphDfs = graph.GraphDfs(edges[0][0],
            vet => { testOutputHelper.WriteLine($"当前节点：{vet.Value}"); });
        var resultDfs = string.Join(", ", graphDfs.Select(item => item.Value));
        testOutputHelper.WriteLine(resultDfs);

        var graphBfs = graph.GraphBfs(edges[0][0],
            vet => { testOutputHelper.WriteLine($"当前节点：{vet.Value}"); });
        var resultBfs = string.Join(", ", graphBfs.Select(item => item.Value));
        testOutputHelper.WriteLine(resultBfs);
    }

    [Fact]
    public void AdjacencyList_ShouldIgnoreDuplicateEdgesAndValidateTraversalStart()
    {
        var first = new Vertex<int>(1);
        var second = new Vertex<int>(2);
        var graph = new GraphWithAdjacencyList<int>([]);
        graph.AddVertex(first);
        graph.AddVertex(second);

        graph.AddEdge(first, second);
        graph.AddEdge(first, second);

        Assert.Equal(new[] { second }, graph.GetNeighbors(first));
        Assert.Equal(new[] { first }, graph.GetNeighbors(second));
        Assert.Throws<ArgumentException>(() => graph.GraphBfs(new Vertex<int>(999)));
        Assert.Throws<ArgumentException>(() => graph.GraphDfs(new Vertex<int>(999)));
    }

    [Fact]
    public void AdjacencyList_ShouldNotExposeMutableInternalCollections()
    {
        var first = new Vertex<int>(1);
        var second = new Vertex<int>(2);
        var graph = new GraphWithAdjacencyList<int>([[first, second]]);

        var exposedMap = Assert.IsAssignableFrom<
            IDictionary<Vertex<int>, IReadOnlyList<Vertex<int>>>>(graph.AdjacencyList);
        var exposedNeighbors = Assert.IsAssignableFrom<IList<Vertex<int>>>(graph.GetNeighbors(first));

        Assert.Throws<NotSupportedException>(() => exposedMap.Clear());
        Assert.Throws<NotSupportedException>(() => exposedNeighbors.Clear());
        Assert.Equal([second], graph.GetNeighbors(first));
    }

    [Fact]
    public void GraphConstructors_ShouldRejectMalformedEdges()
    {
        Assert.Throws<ArgumentException>(() =>
            new GraphWithAdjacencyList<int>([new[] { new Vertex<int>(1) }]));
        Assert.Throws<ArgumentException>(() =>
            new GraphWithAdjacencyMatrix<int>([1, 2], [new[] { 0 }]));
    }

    [Fact]
    public void AdjacencyMatrix_ShouldExposeVerticesAndValidateIndexes()
    {
        var graph = new GraphWithAdjacencyMatrix<int>([10, 20, 30], [new[] { 0, 1 }]);

        Assert.Equal(new[] { 10, 20, 30 }, graph.Vertices);
        Assert.True(graph.HasEdge(0, 1));
        Assert.True(graph.HasEdge(1, 0));
        Assert.False(graph.HasEdge(0, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.RemoveVertex(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.AddEdge(0, 99));
        Assert.Throws<ArgumentException>(() => graph.AddEdge(1, 1));
    }
}
