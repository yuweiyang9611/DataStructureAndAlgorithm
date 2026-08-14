using System.Collections.ObjectModel;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Graph;
using DataStructureAndAlgorithm.Sorting;
using DataStructureAndAlgorithm.Tree;

namespace DataStructureAndAlgorithm.Test.QualityTest;

public class AlgorithmTracingTest
{
    [Fact]
    public void TracedAlgorithms_ExposeOrderedLearningStepsWithoutChangingResults()
    {
        var trace = new CollectingAlgorithmTraceSink();
        int[] values = [3, -1, 2, int.MinValue, int.MaxValue];
        NonComparisonSortAlgorithms.RadixSort(values, trace);

        var tree = new RedBlackTree<int>(trace: trace);
        foreach (var value in new[] { 3, 1, 2 }) tree.Add(value);

        var huffman = new HuffmanTree<char>();
        huffman.CalculateHuffmanCode(new Dictionary<char, uint> { ['a'] = 2, ['b'] = 1 }, trace);

        Assert.True(values.SequenceEqual([int.MinValue, -1, 2, 3, int.MaxValue]));
        Assert.True(tree.HasValidInvariants());
        Assert.True(trace.Events.Select(item => item.Step).SequenceEqual(Enumerable.Range(1, trace.Events.Count)));
        Assert.Contains(trace.Events, item => item.Operation == "Pass");
        Assert.Contains(trace.Events, item => item.Operation.StartsWith("Rotate", StringComparison.Ordinal));
        Assert.Contains(trace.Events, item => item.Operation == "Merge");
    }

    [Fact]
    public void DijkstraTrace_RecordsRelaxationsAndKeepsTheShortestPath()
    {
        var graph = new WeightedGraph<string>();
        graph.AddEdge("A", "B", 10);
        graph.AddEdge("A", "C", 1);
        graph.AddEdge("C", "B", 2);
        var trace = new CollectingAlgorithmTraceSink();

        var result = WeightedGraphAlgorithms.Dijkstra(graph, "A", trace);

        Assert.Equal(3, result.Distances["B"]);
        Assert.True(result.GetPathTo("B").SequenceEqual(["A", "C", "B"]));
        Assert.Equal(3, trace.Events.Count(item => item.Operation == "Relax"));
    }

    [Fact]
    public void CollectingTrace_CapturesReadOnlyStateSnapshots()
    {
        var trace = new CollectingAlgorithmTraceSink();
        var mutableState = new Dictionary<string, string> { ["value"] = "before" };

        trace.Record("Test", "Snapshot", "验证快照所有权。", mutableState);
        mutableState["value"] = "after";

        Assert.Equal("before", trace.Events[0].State["value"]);
        Assert.IsAssignableFrom<ReadOnlyDictionary<string, string>>(trace.Events[0].State);
        Assert.IsAssignableFrom<ReadOnlyCollection<AlgorithmTraceEvent>>(trace.Events);
    }

    [Fact]
    public void MermaidRenderer_SortsStateKeysAndProducesDeterministicOutput()
    {
        var trace = new CollectingAlgorithmTraceSink();

        // 故意按相反顺序插入 key，证明渲染结果由内容决定，而不是依赖 Dictionary 的插入顺序。
        // 这对学习文档尤其重要：同一组状态每次都应产生相同文本，Git 才不会出现噪声差异。
        trace.Record(
            "Demo",
            "Compare",
            "验证稳定顺序。",
            new Dictionary<string, string>
            {
                ["zeta"] = "last",
                ["alpha"] = "first",
            });

        var firstRender = MermaidTraceRenderer.Render(trace.Events);
        var secondRender = MermaidTraceRenderer.Render(trace.Events);

        Assert.Equal(firstRender, secondRender);
        Assert.True(
            firstRender.IndexOf("alpha=first", StringComparison.Ordinal)
            < firstRender.IndexOf("zeta=last", StringComparison.Ordinal));
    }

    [Fact]
    public void MermaidRenderer_EscapesQuotesNewLinesAndBackslashes()
    {
        var trace = new CollectingAlgorithmTraceSink();

        // Mermaid 节点使用双引号包裹标签，因此用户可控的追踪文本必须先转义。
        // 同时保留换行的可读含义（转换为 <br/>），并把 Windows 路径中的反斜线稳定保留下来。
        trace.Record(
            @"Path\Finder",
            "Read\"Value",
            "第一行\r\n第二行",
            new Dictionary<string, string>
            {
                ["path"] = @"C:\Temp\trace.json",
                ["quoted"] = "\"value\"",
            });

        var mermaid = MermaidTraceRenderer.Render(trace.Events);

        Assert.Contains(@"Path\\Finder/Read&quot;Value", mermaid, StringComparison.Ordinal);
        Assert.Contains("第一行<br/>第二行", mermaid, StringComparison.Ordinal);
        Assert.Contains(@"path=C:\\Temp\\trace.json", mermaid, StringComparison.Ordinal);
        Assert.Contains("quoted=&quot;value&quot;", mermaid, StringComparison.Ordinal);
        Assert.DoesNotContain("第一行\r", mermaid, StringComparison.Ordinal);
        Assert.DoesNotContain("第一行\n", mermaid, StringComparison.Ordinal);
    }
}
