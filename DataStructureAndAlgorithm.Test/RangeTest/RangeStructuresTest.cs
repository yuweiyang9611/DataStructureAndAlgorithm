using DataStructureAndAlgorithm.Range;

namespace DataStructureAndAlgorithm.Test.RangeTest;

public class RangeStructuresTest
{
    [Fact]
    public void FenwickTree_ShouldSupportPrefixAndRangeSumsAfterUpdates()
    {
        var tree = new FenwickTree([1, 2, 3, 4, 5]);

        Assert.Equal(6, tree.PrefixSum(3));
        Assert.Equal(9, tree.RangeSum(1, 4));

        tree.Add(2, 10);
        Assert.Equal(19, tree.RangeSum(1, 4));
    }

    [Fact]
    public void SegmentTree_ShouldSupportSumAndNonCommutativeCombination()
    {
        var sumTree = new SegmentTree<int>([1, 2, 3, 4, 5], (left, right) => left + right, 0);
        Assert.Equal(9, sumTree.Query(1, 4));

        sumTree.Update(2, 10);
        Assert.Equal(16, sumTree.Query(1, 4));

        var textTree = new SegmentTree<string>(["a", "b", "c", "d"], (left, right) => left + right, "");
        Assert.Equal("bcd", textTree.Query(1, 4));
    }

    [Fact]
    public void RangeStructures_ShouldRejectInvalidRanges()
    {
        var fenwick = new FenwickTree(3);
        var segment = new SegmentTree<int>([1, 2, 3], Math.Max, int.MinValue);

        Assert.Throws<ArgumentOutOfRangeException>(() => fenwick.RangeSum(2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => segment.Query(-1, 2));
    }
}
