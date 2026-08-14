using DataStructureAndAlgorithm.LeetCode;

namespace DataStructureAndAlgorithm.Test.LeetCodeTest;

public class ClassicDynamicProgrammingProblemsTest
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 2)]
    [InlineData(5, 8)]
    public void ClimbStairs_ShouldReturnNumberOfWays(int stairs, int expected)
    {
        Assert.Equal(expected, ClassicDynamicProgrammingProblems.ClimbStairs(stairs));
    }

    [Theory]
    [InlineData(new[] { 1, 2, 3, 1 }, 4)]
    [InlineData(new[] { 2, 7, 9, 3, 1 }, 12)]
    public void HouseRobber_ShouldAvoidAdjacentSelections(int[] values, int expected)
    {
        Assert.Equal(expected, ClassicDynamicProgrammingProblems.HouseRobber(values));
    }

    [Fact]
    public void WordBreak_ShouldUseReachablePrefixes()
    {
        Assert.True(ClassicDynamicProgrammingProblems.WordBreak("leetcode", ["leet", "code"]));
        Assert.False(ClassicDynamicProgrammingProblems.WordBreak("catsandog", ["cats", "dog", "sand", "and", "cat"]));
    }

    [Fact]
    public void CoinChange_ShouldAdaptNullResultToMinusOne()
    {
        Assert.Equal(3, ClassicDynamicProgrammingProblems.CoinChange([1, 2, 5], 11));
        Assert.Equal(-1, ClassicDynamicProgrammingProblems.CoinChange([2], 3));
    }
}
