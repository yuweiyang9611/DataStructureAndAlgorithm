using DataStructureAndAlgorithm.Range;

namespace DataStructureAndAlgorithm.Test.RangeTest;

public class AdvancedRangeQueryTest
{
    [Fact]
    public void SparseTable_AnswersStaticMinimumQueriesInConstantTime()
    {
        int[] values = [7, 2, 3, 0, 5, 10, 3, 12, 18];
        var table = new SparseTable<int>(values, Math.Min);

        Assert.Equal(0, table.Query(0, 5));
        Assert.Equal(3, table.Query(4, 7));
        Assert.Equal(18, table.Query(8, 9));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.Query(3, 3));
    }

    [Fact]
    public void SparseTable_RandomQueriesMatchBruteForce()
    {
        var random = new Random(42);
        var values = Enumerable.Range(0, 200).Select(_ => random.Next(-1000, 1000)).ToArray();
        var table = new SparseTable<int>(values, Math.Min);

        for (var query = 0; query < 1_000; query++)
        {
            var start = random.Next(values.Length);
            var end = random.Next(start + 1, values.Length + 1);
            Assert.Equal(values[start..end].Min(), table.Query(start, end));
        }
    }

    [Fact]
    public void LazySegmentTree_RangeUpdatesAndQueriesMatchAnArray()
    {
        var random = new Random(42);
        var expected = Enumerable.Range(0, 100).Select(index => (long)index).ToArray();
        var tree = new LazyRangeSumSegmentTree(expected);

        for (var operation = 0; operation < 1_000; operation++)
        {
            var start = random.Next(expected.Length + 1);
            var end = random.Next(start, expected.Length + 1);
            if (random.Next(2) == 0)
            {
                var delta = random.Next(-20, 21);
                tree.RangeAdd(start, end, delta);
                for (var index = start; index < end; index++) expected[index] += delta;
            }
            else
            {
                Assert.Equal(expected[start..end].Sum(), tree.Query(start, end));
            }
        }

        Assert.Equal(expected.Sum(), tree.Query(0, expected.Length));
    }

    [Fact]
    public void LazySegmentTree_HandlesEmptyInputAndChecksOverflow()
    {
        var empty = new LazyRangeSumSegmentTree([]);
        Assert.Equal(0, empty.Query(0, 0));
        empty.RangeAdd(0, 0, 10);

        var tree = new LazyRangeSumSegmentTree([long.MaxValue]);
        Assert.Throws<OverflowException>(() => tree.RangeAdd(0, 1, 1));
    }

    [Fact]
    public void LazySegmentTree_FailedRangeAddAcrossBranchesShouldRollBackEarlierChanges()
    {
        var tree = new LazyRangeSumSegmentTree([0, 0, long.MaxValue, 0]);

        Assert.Throws<OverflowException>(() => tree.RangeAdd(1, 3, 1));

        Assert.Equal(0, tree.Query(1, 2));
        Assert.Equal(long.MaxValue, tree.Query(2, 3));
    }

    [Fact]
    public void LazySegmentTree_FailedRangeAddDuringPushShouldRestorePendingState()
    {
        var tree = CreateTreeWithOverflowingPendingPush();

        Assert.Throws<OverflowException>(() => tree.RangeAdd(0, 1, -1));

        AssertPendingUpdateWasNotPartiallyPushed(tree);
    }

    [Fact]
    public void LazySegmentTree_FailedQueryDuringPushShouldRestorePendingState()
    {
        var tree = CreateTreeWithOverflowingPendingPush();

        Assert.Throws<OverflowException>(() => tree.Query(0, 1));

        AssertPendingUpdateWasNotPartiallyPushed(tree);
    }

    private static LazyRangeSumSegmentTree CreateTreeWithOverflowingPendingPush()
    {
        var tree = new LazyRangeSumSegmentTree([long.MinValue, long.MaxValue]);
        tree.RangeAdd(0, 2, 1);
        return tree;
    }

    private static void AssertPendingUpdateWasNotPartiallyPushed(LazyRangeSumSegmentTree tree)
    {
        Assert.Equal(1, tree.Query(0, 2));

        // 撤销根节点尚未下推的增量后，两个叶子必须精确恢复初值。若失败调用曾只修改左孩子，
        // 根 lazy 已经归零，下一次叶子查询就会暴露那个半提交状态。
        tree.RangeAdd(0, 2, -1);
        Assert.Equal(long.MinValue, tree.Query(0, 1));
        Assert.Equal(long.MaxValue, tree.Query(1, 2));
    }
}
