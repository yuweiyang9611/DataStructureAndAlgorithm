using System;
using System.Linq;
using DataStructureAndAlgorithm.DynamicProgramming;
using DataStructureAndAlgorithm.Tree;
using Xunit;

namespace DataStructureAndAlgorithm.Test.TreeTest;

/// <summary>
/// BK-tree 测试既验证集合结果，也验证公开的确定性排序契约。
/// </summary>
public sealed class BkTreeTests
{
    [Fact]
    public void Search_ReturnsEveryWordInsideRadiusInDeterministicOrder()
    {
        var tree = new BkTree(AdvancedDynamicProgrammingAlgorithms.EditDistance);
        foreach (var word in new[] { "book", "books", "cake", "boo", "cape", "boon", "cook" })
        {
            Assert.True(tree.Add(word));
        }

        var matches = tree.Search("bok", maximumDistance: 2);

        // 输出先按距离、再按序号字典序排列。测试不依赖节点入栈顺序，
        // 因而以后即使把 List 边改成别的自实现容器，公开结果也不能漂移。
        Assert.Equal(
            [
                new BkTreeMatch("boo", 1),
                new BkTreeMatch("book", 1),
                new BkTreeMatch("books", 2),
                new BkTreeMatch("boon", 2),
                new BkTreeMatch("cook", 2)
            ],
            matches);
    }

    [Fact]
    public void Add_RejectsDuplicatesAndEmptyTreeSearchIsSafe()
    {
        var tree = new BkTree(AdvancedDynamicProgrammingAlgorithms.EditDistance);

        Assert.Empty(tree.Search("anything", maximumDistance: 3));
        Assert.True(tree.Add("search"));
        Assert.False(tree.Add("search"));
        Assert.Equal(1, tree.Count);
        Assert.Equal(new BkTreeMatch("search", 0), Assert.Single(tree.Search("search", 0)));
    }

    [Fact]
    public void Search_RejectsNegativeRadiusAndNegativeMetricResult()
    {
        var tree = new BkTree((_, _) => -1);
        tree.Add("root");

        Assert.Throws<ArgumentOutOfRangeException>(() => tree.Search("root", -1));
        Assert.Throws<InvalidOperationException>(() => tree.Search("query", 1));
    }
}
