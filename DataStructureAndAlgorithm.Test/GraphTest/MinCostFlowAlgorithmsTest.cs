using DataStructureAndAlgorithm.Graph;

namespace DataStructureAndAlgorithm.Test.GraphTest;

/// <summary>
/// 最小费用最大流的示例与不变量测试。
/// </summary>
public class MinCostFlowAlgorithmsTest
{
    [Fact]
    public void MinimumCostMaximumFlow_UsesReverseResidualEdgeToRepairAnEarlyChoice()
    {
        var network = new MinCostFlowNetwork<string>(StringComparer.Ordinal);
        network.AddEdge("source", "vehicle-a", capacity: 1, unitCost: 0);
        network.AddEdge("source", "vehicle-b", capacity: 1, unitCost: 0);
        network.AddEdge("vehicle-a", "order-x", capacity: 1, unitCost: 1);
        network.AddEdge("vehicle-a", "order-y", capacity: 1, unitCost: 2);
        network.AddEdge("vehicle-b", "order-x", capacity: 1, unitCost: 1);
        network.AddEdge("order-x", "sink", capacity: 1, unitCost: 0);
        network.AddEdge("order-y", "sink", capacity: 1, unitCost: 0);

        var result = MinCostFlowAlgorithms.MinimumCostMaximumFlow(network, "source", "sink");

        Assert.Equal(2, result.Flow);
        Assert.Equal(3, result.Cost);
        Assert.Equal(
            1,
            Assert.Single(result.Edges, edge => edge.From == "vehicle-a" && edge.To == "order-y").Flow);
        Assert.Equal(
            1,
            Assert.Single(result.Edges, edge => edge.From == "vehicle-b" && edge.To == "order-x").Flow);

        // 第一条最短增广路会选择 a -> x；要得到最终最优解，第二轮必须经过 x -> a 的反向残量边撤销它。
        // 若实现只做“按边成本从小到大贪心”，要么只能分配一个订单，要么会得到错误组合。
        Assert.Equal(
            0,
            Assert.Single(result.Edges, edge => edge.From == "vehicle-a" && edge.To == "order-x").Flow);
    }

    [Fact]
    public void MinimumCostMaximumFlow_PreservesCapacityConservationAndDoesNotMutateInput()
    {
        var network = new MinCostFlowNetwork<string>(StringComparer.Ordinal);
        network.AddEdge("s", "a", capacity: 3, unitCost: 0);
        network.AddEdge("s", "b", capacity: 2, unitCost: 0);
        network.AddEdge("a", "b", capacity: 2, unitCost: 1.5);
        network.AddEdge("a", "t", capacity: 2, unitCost: 4);
        network.AddEdge("b", "t", capacity: 4, unitCost: 2);

        var first = MinCostFlowAlgorithms.MinimumCostMaximumFlow(network, "s", "t");
        var second = MinCostFlowAlgorithms.MinimumCostMaximumFlow(network, "s", "t");

        Assert.Equal(5, first.Flow);
        Assert.Equal(15, first.Cost);
        Assert.Equal(first.Flow, second.Flow);
        Assert.Equal(first.Cost, second.Cost);
        Assert.True(first.Edges.SequenceEqual(second.Edges));
        Assert.All(first.Edges, edge => Assert.InRange(edge.Flow, 0, edge.Capacity));

        var vertices = first.Edges.SelectMany(edge => new[] { edge.From, edge.To })
            .Distinct(StringComparer.Ordinal);
        foreach (var vertex in vertices.Where(vertex => vertex is not ("s" or "t")))
        {
            var inflow = first.Edges.Where(edge => edge.To == vertex).Sum(edge => edge.Flow);
            var outflow = first.Edges.Where(edge => edge.From == vertex).Sum(edge => edge.Flow);
            Assert.Equal(inflow, outflow);
        }

        Assert.Equal(first.Flow, first.Edges.Where(edge => edge.From == "s").Sum(edge => edge.Flow));
        Assert.Equal(first.Flow, first.Edges.Where(edge => edge.To == "t").Sum(edge => edge.Flow));
    }

    [Fact]
    public void MinimumCostMaximumFlow_FlowLimitReturnsTheCheapestRequestedPrefix()
    {
        var network = new MinCostFlowNetwork<int>();
        network.AddEdge(0, 1, capacity: 1, unitCost: 8);
        network.AddEdge(0, 2, capacity: 1, unitCost: 3);
        network.AddEdge(1, 3, capacity: 1, unitCost: 0);
        network.AddEdge(2, 3, capacity: 1, unitCost: 0);

        var result = MinCostFlowAlgorithms.MinimumCostMaximumFlow(network, 0, 3, flowLimit: 1);

        Assert.Equal(1, result.Flow);
        Assert.Equal(3, result.Cost);
        Assert.Equal(1, Assert.Single(result.Edges, edge => edge.From == 0 && edge.To == 2).Flow);
    }

    [Fact]
    public void MinimumCostMaximumFlow_DoesNotTreatARealSubEpsilonDifferenceAsATie()
    {
        var network = new MinCostFlowNetwork<string>(StringComparer.Ordinal);
        network.AddEdge("source", "slightly-expensive", capacity: 1, unitCost: 5e-11);
        network.AddEdge("source", "free", capacity: 1, unitCost: 0);
        network.AddEdge("slightly-expensive", "sink", capacity: 1, unitCost: 0);
        network.AddEdge("free", "sink", capacity: 1, unitCost: 0);

        var result = MinCostFlowAlgorithms.MinimumCostMaximumFlow(network, "source", "sink", flowLimit: 1);

        // 较贵边故意先加入。若松弛条件用 1e-10 容差判断平局，5e-11 会压过后发现的真实零成本路径。
        Assert.Equal(1, result.Flow);
        Assert.Equal(0, result.Cost);
        Assert.Equal(1, Assert.Single(result.Edges, edge => edge.From == "source" && edge.To == "free").Flow);
        Assert.Equal(
            0,
            Assert.Single(result.Edges, edge => edge.From == "source" && edge.To == "slightly-expensive").Flow);
    }

    [Fact]
    public void MinimumCostMaximumFlow_ScalesReducedCostToleranceToLargePotentials()
    {
        var network = new MinCostFlowNetwork<string>(StringComparer.Ordinal);
        network.AddEdge("source", "u", capacity: 1, unitCost: 1);
        network.AddEdge("u", "v", capacity: 1, unitCost: 1e20);
        network.AddEdge("source", "v", capacity: 1, unitCost: 1.000000000000001e20);
        network.AddEdge("v", "sink", capacity: 2, unitCost: 0);

        var result = MinCostFlowAlgorithms.MinimumCostMaximumFlow(network, "source", "sink");

        // 第一轮走 source -> u -> v。数学上的 v 势能是 1 + 1e20，但 double 会把低位的 1 吞掉。
        // 第二轮从 v 检查反向边 v -> u 时，按已存 double 计算会得到 -1。固定 1e-10 会误报不变量损坏；
        // 1e20 附近一个 ULP 是 16384，所以 -1 应被识别为尺度相关的表示误差。
        Assert.Equal(2, result.Flow);
        Assert.Equal(1, Assert.Single(result.Edges, edge => edge.From == "source" && edge.To == "u").Flow);
        Assert.Equal(1, Assert.Single(result.Edges, edge => edge.From == "u" && edge.To == "v").Flow);
        Assert.Equal(1, Assert.Single(result.Edges, edge => edge.From == "source" && edge.To == "v").Flow);
        Assert.Equal(2, Assert.Single(result.Edges, edge => edge.From == "v" && edge.To == "sink").Flow);
    }

    [Fact]
    public void MinimumCostMaximumFlow_LargeScaleStillPreservesOneUlpCostDifferences()
    {
        const double cheaperCost = 1e20;
        var moreExpensiveCost = Math.BitIncrement(cheaperCost);
        var network = new MinCostFlowNetwork<string>(StringComparer.Ordinal);

        // 故意先加入较贵路径，证明算法依赖严格成本比较，而不是邻接顺序或尺度容差制造的“平局”。
        network.AddEdge("source", "expensive", capacity: 1, unitCost: moreExpensiveCost);
        network.AddEdge("source", "cheap", capacity: 1, unitCost: cheaperCost);
        network.AddEdge("expensive", "sink", capacity: 1, unitCost: 0);
        network.AddEdge("cheap", "sink", capacity: 1, unitCost: 0);

        var result = MinCostFlowAlgorithms.MinimumCostMaximumFlow(
            network,
            "source",
            "sink",
            flowLimit: 1);

        // 一 ULP 是该尺度下最小的可表示差异。它必须继续属于目标函数，不能被约化成本容差吞掉。
        Assert.Equal(cheaperCost, result.Cost);
        Assert.Equal(1, Assert.Single(result.Edges, edge => edge.From == "source" && edge.To == "cheap").Flow);
        Assert.Equal(
            0,
            Assert.Single(result.Edges, edge => edge.From == "source" && edge.To == "expensive").Flow);
    }

    [Fact]
    public void MinCostFlowNetwork_RejectsInvalidInputAtTheBoundary()
    {
        var network = new MinCostFlowNetwork<string>();

        Assert.Throws<ArgumentException>(() => network.AddEdge("same", "same", 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.AddEdge("a", "b", -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.AddEdge("a", "b", 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.AddEdge("a", "b", 1, double.NaN));

        network.AddVertex("s");
        network.AddVertex("t");
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MinCostFlowAlgorithms.MinimumCostMaximumFlow(network, "s", "t", flowLimit: -1));
        Assert.Throws<ArgumentException>(
            () => MinCostFlowAlgorithms.MinimumCostMaximumFlow(network, "missing", "t"));
        Assert.Throws<ArgumentException>(
            () => MinCostFlowAlgorithms.MinimumCostMaximumFlow(network, "s", "s"));
    }
}
