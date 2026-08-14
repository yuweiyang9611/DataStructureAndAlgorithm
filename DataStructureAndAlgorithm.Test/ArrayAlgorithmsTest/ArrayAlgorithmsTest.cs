using Algorithms = DataStructureAndAlgorithm.ArrayAlgorithms.ArrayAlgorithms;

namespace DataStructureAndAlgorithm.Test.ArrayAlgorithmsTest;

public class ArrayAlgorithmsTest
{
    [Fact]
    public void SlidingWindowMaximum_ShouldReturnMaximumForEveryWindow()
    {
        int[] values = [1, 3, -1, -3, 5, 3, 6, 7];

        Assert.Equal([3, 3, 5, 5, 6, 7], Algorithms.SlidingWindowMaximum(values, 3));
    }

    [Fact]
    public void MaximumSubarray_ShouldHandleMixedAndAllNegativeInputs()
    {
        var mixed = Algorithms.MaximumSubarray([-2, 1, -3, 4, -1, 2, 1, -5, 4]);
        var negative = Algorithms.MaximumSubarray([-5, -2, -8]);

        Assert.Equal(6, mixed.Sum);
        Assert.Equal((3, 7), (mixed.Start, mixed.EndExclusive));
        Assert.Equal(-2, negative.Sum);
        Assert.Equal((1, 2), (negative.Start, negative.EndExclusive));
    }

    [Fact]
    public void TwoSum_ShouldReturnTwoDistinctIndexes()
    {
        var result = Algorithms.TwoSum([2, 7, 11, 15], 9);

        Assert.NotNull(result);
        Assert.Equal((0, 1), result.Value);
        Assert.Null(Algorithms.TwoSum([1, 2, 3], 100));
    }
}
