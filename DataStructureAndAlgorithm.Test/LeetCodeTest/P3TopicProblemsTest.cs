using DataStructureAndAlgorithm.LeetCode;
using DataStructureAndAlgorithm.LeetCode.Models;

namespace DataStructureAndAlgorithm.Test.LeetCodeTest;

public class P3TopicProblemsTest
{
    [Fact]
    public void NumericAndBitProblems_CoverP50P69P191P204P231P338()
    {
        Assert.Equal(1024, NumericAndBitProblems.Power(2, 10));
        Assert.Equal(0.25, NumericAndBitProblems.Power(2, -2));
        Assert.Equal(46_340, NumericAndBitProblems.IntegerSquareRoot(int.MaxValue));
        Assert.Equal(3, NumericAndBitProblems.HammingWeight(0b1011));
        Assert.Equal(4, NumericAndBitProblems.CountPrimes(10));
        Assert.True(NumericAndBitProblems.IsPowerOfTwo(1024));
        Assert.True(NumericAndBitProblems.CountBits(5).SequenceEqual([0, 1, 1, 2, 1, 2]));
    }

    [Fact]
    public void P312_MaximumCoins_UsesIntervalBoundaries() =>
        Assert.Equal(167, AdvancedTopicProblems.MaximumCoins([3, 1, 5, 8]));

    [Fact]
    public void P332_ReconstructItinerary_UsesEveryTicketLexically()
    {
        var route = AdvancedTopicProblems.ReconstructItinerary([
            ("MUC", "LHR"), ("JFK", "MUC"), ("SFO", "SJC"), ("LHR", "SFO")]);
        Assert.True(route.SequenceEqual(["JFK", "MUC", "LHR", "SFO", "SJC"]));
    }

    [Fact]
    public void P337_RobTree_CombinesTakeAndSkipStates()
    {
        var root = new TreeNode(3,
            new TreeNode(2, right: new TreeNode(3)),
            new TreeNode(3, right: new TreeNode(1)));
        Assert.Equal(7, AdvancedTopicProblems.RobTree(root));
    }

    [Theory]
    [InlineData("bbbab", 4)]
    [InlineData("cbbd", 2)]
    [InlineData("", 0)]
    public void P516_LongestPalindromicSubsequence(string text, int expected) =>
        Assert.Equal(expected, AdvancedTopicProblems.LongestPalindromicSubsequence(text));

    [Fact]
    public void P698_CanPartitionKEqualSumSubsets_PrunesEquivalentBuckets()
    {
        Assert.True(AdvancedTopicProblems.CanPartitionKEqualSumSubsets([4, 3, 2, 3, 5, 2, 1], 4));
        Assert.False(AdvancedTopicProblems.CanPartitionKEqualSumSubsets([1, 2, 3, 4], 3));
    }

    [Fact]
    public void P847_ShortestPathVisitingAllNodes_UsesVertexAndMaskState() =>
        Assert.Equal(4, AdvancedTopicProblems.ShortestPathVisitingAllNodes([[1, 2, 3], [0], [0], [0]]));

    [Fact]
    public void P1044_LongestDuplicateSubstring_UsesMaximumLcp() =>
        Assert.Equal("ana", AdvancedTopicProblems.LongestDuplicateSubstring("banana"));

    [Fact]
    public void P1192_CriticalConnections_UsesLowLinkValues()
    {
        var bridges = AdvancedTopicProblems.CriticalConnections(4, [(0, 1), (1, 2), (2, 0), (1, 3)]);
        Assert.True(bridges.SequenceEqual([(1, 3)]));
    }
}
