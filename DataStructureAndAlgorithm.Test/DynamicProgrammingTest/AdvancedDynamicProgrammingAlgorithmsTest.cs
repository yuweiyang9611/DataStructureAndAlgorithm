using DataStructureAndAlgorithm.DynamicProgramming;

namespace DataStructureAndAlgorithm.Test.DynamicProgrammingTest;

public class AdvancedDynamicProgrammingAlgorithmsTest
{
    [Theory]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("", "abc", 3)]
    [InlineData("algorithm", "algorithm", 0)]
    public void EditDistance_ShouldReturnMinimumOperations(string source, string target, int expected)
    {
        Assert.Equal(expected, AdvancedDynamicProgrammingAlgorithms.EditDistance(source, target));
    }

    [Fact]
    public void LongestIncreasingSubsequence_ShouldReturnStrictlyIncreasingOptimalSequence()
    {
        int[] values = [10, 9, 2, 5, 3, 7, 101, 18];

        var result = AdvancedDynamicProgrammingAlgorithms.LongestIncreasingSubsequence(values);

        Assert.Equal(4, result.Count);
        Assert.True(result.Zip(result.Skip(1)).All(pair => pair.First < pair.Second));
        Assert.True(IsSubsequence(result, values));
    }

    private static bool IsSubsequence<T>(IReadOnlyList<T> candidate, IReadOnlyList<T> source)
    {
        var index = 0;
        foreach (var value in source)
        {
            if (index < candidate.Count && EqualityComparer<T>.Default.Equals(candidate[index], value))
            {
                index++;
            }
        }

        return index == candidate.Count;
    }
}
