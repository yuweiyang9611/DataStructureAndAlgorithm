using DataStructureAndAlgorithm.DynamicProgramming;

namespace DataStructureAndAlgorithm.Test.DynamicProgrammingTest;

public class DynamicProgrammingAlgorithmsTest
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(10, 55)]
    [InlineData(50, 12586269025)]
    public void Fibonacci_ShouldReturnExpectedValue(int n, long expected)
    {
        Assert.Equal(expected, DynamicProgrammingAlgorithms.Fibonacci(n));
    }

    [Fact]
    public void Fibonacci_WhenResultWouldOverflow_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DynamicProgrammingAlgorithms.Fibonacci(93));
    }

    [Fact]
    public void LongestCommonSubsequence_ShouldReturnAnActualSubsequence()
    {
        var result = DynamicProgrammingAlgorithms.LongestCommonSubsequence("ABCBDAB", "BDCABA");

        Assert.Equal(4, result.Length);
        Assert.True(IsSubsequence(result, "ABCBDAB"));
        Assert.True(IsSubsequence(result, "BDCABA"));
    }

    [Fact]
    public void ZeroOneKnapsack_ShouldReturnValueAndSelectedItems()
    {
        KnapsackItem[] items =
        [
            new(2, 3),
            new(3, 4),
            new(4, 5),
            new(5, 8)
        ];

        var result = DynamicProgrammingAlgorithms.ZeroOneKnapsack(items, 8);

        Assert.Equal(12, result.MaximumValue);
        Assert.Equal([1, 3], result.SelectedIndexes);
    }

    [Theory]
    [InlineData(11, 3)]
    [InlineData(3, 2)]
    [InlineData(0, 0)]
    public void MinimumCoins_ShouldFindOptimalCount(int amount, int expected)
    {
        Assert.Equal(expected, DynamicProgrammingAlgorithms.MinimumCoins([1, 2, 5], amount));
    }

    [Fact]
    public void MinimumCoins_WhenAmountIsUnreachable_ShouldReturnNull()
    {
        Assert.Null(DynamicProgrammingAlgorithms.MinimumCoins([2], 3));
    }

    private static bool IsSubsequence(string candidate, string source)
    {
        var candidateIndex = 0;

        foreach (var character in source)
        {
            if (candidateIndex < candidate.Length && candidate[candidateIndex] == character)
            {
                candidateIndex++;
            }
        }

        return candidateIndex == candidate.Length;
    }
}
