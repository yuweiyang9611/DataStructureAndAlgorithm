using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Graph;

namespace DataStructureAndAlgorithm.Test.QualityTest;

public class P3TracingTest
{
    [Fact]
    public void GcdAndMatrixChain_ExposeOrderedInvariantSteps()
    {
        var trace = new CollectingAlgorithmTraceSink();

        Assert.Equal(21UL, P3TraceScenarios.GreatestCommonDivisor(1_071, 462, trace));
        Assert.Equal(26_000, P3TraceScenarios.MinimumMatrixChainMultiplications([40, 20, 30, 10, 30], trace));
        Assert.True(trace.Events.Select(item => item.Step).SequenceEqual(Enumerable.Range(1, trace.Events.Count)));
        Assert.Contains(trace.Events, item => item.Operation == "Remainder");
        Assert.Contains(trace.Events, item => item.Operation == "ChooseSplit");
    }

    [Fact]
    public void TracedDinic_RecoversMinCutAndRendersMermaid()
    {
        var network = new FlowNetwork<string>();
        network.AddEdge("s", "a", 3);
        network.AddEdge("s", "b", 2);
        network.AddEdge("a", "t", 2);
        network.AddEdge("b", "t", 2);
        var trace = new CollectingAlgorithmTraceSink();

        var result = P3FlowAlgorithms.DinicMaximumFlowWithMinimumCut(network, "s", "t", trace);
        var sourceSide = result.SourceSideMinimumCut.ToHashSet();
        var cutCapacity = result.Edges
            .Where(edge => sourceSide.Contains(edge.From) && !sourceSide.Contains(edge.To))
            .Sum(edge => edge.Capacity);
        var mermaid = MermaidTraceRenderer.Render(trace.Events);

        Assert.Equal(4, result.Value);
        Assert.Equal(result.Value, cutCapacity);
        Assert.Contains(trace.Events, item => item.Operation == "MinimumCut");
        Assert.StartsWith("flowchart TD", mermaid);
        Assert.Contains("BuildLevelGraph", mermaid);
    }
}
