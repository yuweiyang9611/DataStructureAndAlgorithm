using DataStructureAndAlgorithm.Backtracking;

namespace DataStructureAndAlgorithm.Test.BacktrackingTest;

public class CombinatorialAlgorithmsTest
{
    [Fact]
    public void GenerateParentheses_ShouldReturnAllCatalanCombinations()
    {
        var result = CombinatorialAlgorithms.GenerateParentheses(3);

        Assert.Equal(5, result.Count);
        Assert.Contains("((()))", result);
        Assert.Contains("()()()", result);
    }

    [Fact]
    public void GenerateSubsets_ShouldReturnPowerSet()
    {
        var result = CombinatorialAlgorithms.GenerateSubsets(new[] { 1, 2, 3 });

        Assert.Equal(8, result.Count);
        Assert.Contains(result, subset => subset.SequenceEqual(Array.Empty<int>()));
        Assert.Contains(result, subset => subset.SequenceEqual(new[] { 1, 2, 3 }));
    }
}
