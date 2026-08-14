using DataStructureAndAlgorithm.Range;

namespace DataStructureAndAlgorithm.Test.RangeTest;

/// <summary>
/// 使用 <see cref="long"/> 边界值证明：成功更新不会把溢出风险推迟到以后的查询。
/// </summary>
public sealed class LazyRangeAddMaxSegmentTreeBoundaryTest
{
    [Fact]
    public void SuccessfulUpdates_ShouldNeverDeferOverflowUntilQuery()
    {
        var tree = new LazyRangeAddMaxSegmentTree(
        [
            long.MinValue,
            long.MinValue,
            long.MinValue,
            long.MinValue
        ]);

        // 左子树先积累 long.MaxValue，随后根节点再积累 1：两个增量之和无法放进 long，
        // 但所有真实元素仍然合法。内部用 Int128 组合懒增量，因此后续任意范围查询都应安全。
        tree.RangeAdd(0, 2, long.MaxValue);
        tree.RangeAdd(0, 4, 1);
        var expected = new[] { 0L, 0L, long.MinValue + 1, long.MinValue + 1 };

        AssertEveryRangeMatches(tree, expected);
    }

    [Fact]
    public void BoundaryHeavyRandomOperations_ShouldMatchCheckedArrayOracle()
    {
        var random = new Random(2026071801);
        var deltas = new[]
        {
            long.MinValue,
            long.MinValue + 1,
            -10L,
            -1L,
            0L,
            1L,
            10L,
            long.MaxValue - 1,
            long.MaxValue
        };
        var expected = new[]
        {
            long.MinValue,
            long.MinValue + 1,
            -1L,
            0L,
            1L,
            long.MaxValue - 1,
            long.MaxValue,
            42L
        };
        var tree = new LazyRangeAddMaxSegmentTree(expected);

        for (var operation = 0; operation < 500; operation++)
        {
            var start = random.Next(expected.Length + 1);
            var end = random.Next(start, expected.Length + 1);
            var delta = deltas[random.Next(deltas.Length)];
            var canApply = Enumerable.Range(start, end - start)
                .All(index => IsLong((Int128)expected[index] + delta));

            if (canApply)
            {
                tree.RangeAdd(start, end, delta);
                for (var index = start; index < end; index++)
                {
                    expected[index] = (long)((Int128)expected[index] + delta);
                }
            }
            else
            {
                Assert.Throws<OverflowException>(() => tree.RangeAdd(start, end, delta));
            }

            // 每一步（包括失败更新）都比较所有 O(n^2) 个范围，同时验证数值正确性和强异常安全。
            AssertEveryRangeMatches(tree, expected);
        }
    }

    private static void AssertEveryRangeMatches(
        LazyRangeAddMaxSegmentTree tree,
        IReadOnlyList<long> expected)
    {
        for (var start = 0; start <= expected.Count; start++)
        {
            for (var end = start; end <= expected.Count; end++)
            {
                var expectedMaximum = start == end
                    ? long.MinValue
                    : Enumerable.Range(start, end - start).Max(index => expected[index]);
                Assert.Equal(expectedMaximum, tree.QueryMax(start, end));
            }
        }
    }

    private static bool IsLong(Int128 value) =>
        value >= long.MinValue && value <= long.MaxValue;
}
