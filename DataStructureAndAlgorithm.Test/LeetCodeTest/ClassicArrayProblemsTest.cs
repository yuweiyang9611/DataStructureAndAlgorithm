using DataStructureAndAlgorithm.LeetCode;

namespace DataStructureAndAlgorithm.Test.LeetCodeTest;

public class ClassicArrayProblemsTest
{
    [Fact]
    public void TwoSum_ShouldReturnIndexesWithoutOverflowFalsePositive()
    {
        Assert.True(ClassicArrayProblems.TwoSum([2, 7, 11, 15], 9).SequenceEqual([0, 1]));
        Assert.Empty(ClassicArrayProblems.TwoSum([int.MaxValue, 1], int.MinValue));
    }

    [Fact]
    public void ThreeSum_ShouldReturnUniqueTripletsWithoutChangingInput()
    {
        int[] input = [-1, 0, 1, 2, -1, -4];

        var result = ClassicArrayProblems.ThreeSum(input);

        Assert.True(input.SequenceEqual([-1, 0, 1, 2, -1, -4]));
        Assert.Equal(2, result.Count);
        Assert.Contains(result, values => values.SequenceEqual([-1, -1, 2]));
        Assert.Contains(result, values => values.SequenceEqual([-1, 0, 1]));
    }

    [Fact]
    public void TrapRainWater_ShouldUseBothBoundaries()
    {
        Assert.Equal(6, ClassicArrayProblems.TrapRainWater([0, 1, 0, 2, 1, 0, 1, 3, 2, 1, 2, 1]));
        Assert.Equal(0, ClassicArrayProblems.TrapRainWater([]));
    }

    [Fact]
    public void MaximumStockProfit_ShouldHandleProfitAndDecline()
    {
        Assert.Equal(5, ClassicArrayProblems.MaximumStockProfit([7, 1, 5, 3, 6, 4]));
        Assert.Equal(0, ClassicArrayProblems.MaximumStockProfit([7, 6, 4, 3, 1]));
    }

    [Theory]
    [InlineData(new[] { 3, 2, 1, 5, 6, 4 }, 2, 5)]
    [InlineData(new[] { 3, 2, 3, 1, 2, 4, 5, 5, 6 }, 4, 4)]
    public void FindKthLargest_ShouldHandleDuplicates(int[] values, int k, int expected)
    {
        Assert.Equal(expected, ClassicArrayProblems.FindKthLargest(values, k));
    }
}
