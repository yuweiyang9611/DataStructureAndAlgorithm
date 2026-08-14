using DataStructureAndAlgorithm.LeetCode;

namespace DataStructureAndAlgorithm.Test.LeetCodeTest;

public class AdditionalDynamicGreedyProblemsTest
{
    [Fact]
    public void P55_CanJump()
    {
        Assert.True(AdditionalDynamicGreedyProblems.CanJump([2, 3, 1, 1, 4]));
        Assert.False(AdditionalDynamicGreedyProblems.CanJump([3, 2, 1, 0, 4]));
        Assert.True(AdditionalDynamicGreedyProblems.CanJump([1, int.MaxValue, 0]));
    }

    [Fact]
    public void P62_UniquePaths() => Assert.Equal(28, AdditionalDynamicGreedyProblems.UniquePaths(3, 7));

    [Fact]
    public void P64_MinimumPathSum() => Assert.Equal(7,
        AdditionalDynamicGreedyProblems.MinimumPathSum([[1, 3, 1], [1, 5, 1], [4, 2, 1]]));

    [Fact]
    public void P84_LargestRectangleArea() => Assert.Equal(10,
        AdditionalDynamicGreedyProblems.LargestRectangleArea([2, 1, 5, 6, 2, 3]));

    [Fact]
    public void P152_MaximumProductSubarray() => Assert.Equal(6,
        AdditionalDynamicGreedyProblems.MaximumProductSubarray([2, 3, -2, 4]));

    [Fact]
    public void P209_MinimumSubarrayLength() => Assert.Equal(2,
        AdditionalDynamicGreedyProblems.MinimumSubarrayLength(7, [2, 3, 1, 2, 4, 3]));

    [Fact]
    public void P221_MaximalSquareArea() => Assert.Equal(4,
        AdditionalDynamicGreedyProblems.MaximalSquareArea([
            ['1','0','1','0','0'], ['1','0','1','1','1'], ['1','1','1','1','1'], ['1','0','0','1','0']]));
}
