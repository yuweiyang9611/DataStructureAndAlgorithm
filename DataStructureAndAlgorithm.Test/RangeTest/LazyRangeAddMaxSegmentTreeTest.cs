using DataStructureAndAlgorithm.Range;

namespace DataStructureAndAlgorithm.Test.RangeTest;

/// <summary>
/// “区间加 + 区间最大值”线段树的差分与边界测试。
/// </summary>
public class LazyRangeAddMaxSegmentTreeTest
{
    [Fact]
    public void RandomRangeUpdatesAndQueries_ShouldMatchPlainArrayOracle()
    {
        // why：懒标记错误通常只在“整段更新后查询子段”或“子段更新后查询跨区间范围”时出现。
        // 固定种子的随机差分能组合出大量交错区间，同时让 CI 失败仍可完全重放。
        var random = new Random(20260718);
        var expected = Enumerable.Range(0, 64)
            .Select(_ => (long)random.Next(-100, 101))
            .ToArray();
        var tree = new LazyRangeAddMaxSegmentTree(expected);

        for (var operation = 0; operation < 2_000; operation++)
        {
            var start = random.Next(expected.Length + 1);
            var end = random.Next(start, expected.Length + 1);
            if (random.Next(3) != 0)
            {
                var delta = random.Next(-25, 26);
                tree.RangeAdd(start, end, delta);
                for (var index = start; index < end; index++)
                {
                    expected[index] += delta;
                }
            }
            else
            {
                var expectedMaximum = start == end
                    ? long.MinValue
                    : expected[start..end].Max();
                Assert.Equal(expectedMaximum, tree.QueryMax(start, end));
            }
        }

        Assert.Equal(expected.Max(), tree.QueryMax(0, expected.Length));
    }

    [Fact]
    public void NegativeUpdate_ShouldExactlyUndoPreviousReservation()
    {
        // why：精确排程的回溯依靠负增量撤销资源预订。如果撤销后内部 lazy 没有复原，
        // 后续兄弟分支就会看到幽灵占用，最终可能把可行的最优解错误剪掉。
        var tree = new LazyRangeAddMaxSegmentTree([0, 0, 0, 0, 0, 0]);

        tree.RangeAdd(1, 5, 7);
        tree.RangeAdd(2, 4, 3);
        Assert.Equal(10, tree.QueryMax(0, 6));

        tree.RangeAdd(2, 4, -3);
        tree.RangeAdd(1, 5, -7);

        Assert.Equal(0, tree.QueryMax(0, 6));
        Assert.Equal(0, tree.QueryMax(2, 4));
    }

    [Fact]
    public void EmptyTreeAndEmptyRange_ShouldBehaveAsDocumentedIdentityOperations()
    {
        var empty = new LazyRangeAddMaxSegmentTree([]);

        empty.RangeAdd(0, 0, 123);

        Assert.Equal(0, empty.Count);
        Assert.Equal(long.MinValue, empty.QueryMax(0, 0));

        var nonEmpty = new LazyRangeAddMaxSegmentTree([4, -2, 9]);
        nonEmpty.RangeAdd(2, 2, long.MaxValue);
        Assert.Equal(long.MinValue, nonEmpty.QueryMax(1, 1));
        Assert.Equal(9, nonEmpty.QueryMax(0, 3));
    }

    [Fact]
    public void PublicMethods_ShouldRejectEveryMalformedRangeAndDetectOverflow()
    {
        Assert.Throws<ArgumentNullException>(() => new LazyRangeAddMaxSegmentTree(null!));

        var tree = new LazyRangeAddMaxSegmentTree([1, 2, 3]);
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.RangeAdd(-1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.RangeAdd(2, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.RangeAdd(0, 4, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.QueryMax(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.QueryMax(2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.QueryMax(0, 4));

        var overflowing = new LazyRangeAddMaxSegmentTree([long.MaxValue]);
        Assert.Throws<OverflowException>(() => overflowing.RangeAdd(0, 1, 1));
        // why：失败更新不能悄悄改变可观察结果；否则调用方即使捕获异常，也无法安全继续使用对象。
        Assert.Equal(long.MaxValue, overflowing.QueryMax(0, 1));
    }

    [Fact]
    public void Overflow_ShouldRejectHiddenMinimumAndRollBackEarlierSiblingUpdates()
    {
        // 根节点的 maximum 为 0，单看 maximum + (-1) 完全合法；但第一个叶子的 long.MinValue 会下溢。
        // RangeAdd 必须在返回前通过 minimum 发现它，并保持所有可观察值不变。
        var hiddenMinimum = new LazyRangeAddMaxSegmentTree([long.MinValue, 0]);
        Assert.Throws<OverflowException>(() => hiddenMinimum.RangeAdd(0, 2, -1));
        Assert.Equal(long.MinValue, hiddenMinimum.QueryMax(0, 1));
        Assert.Equal(0, hiddenMinimum.QueryMax(1, 2));

        // [0, 3) 会先成功更新完整左子树，再在值为 long.MaxValue 的第三个叶子溢出。
        // 公共操作失败后必须回滚已经修改的兄弟分支，并允许后续合法更新继续执行。
        var branchAtomicity = new LazyRangeAddMaxSegmentTree([0, 0, long.MaxValue, 0]);
        Assert.Throws<OverflowException>(() => branchAtomicity.RangeAdd(0, 3, 1));
        Assert.Equal(0, branchAtomicity.QueryMax(0, 2));
        Assert.Equal(long.MaxValue, branchAtomicity.QueryMax(2, 3));
        Assert.Equal(0, branchAtomicity.QueryMax(3, 4));

        branchAtomicity.RangeAdd(3, 4, 2);
        Assert.Equal(2, branchAtomicity.QueryMax(3, 4));
    }
}
