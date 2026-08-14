using DataStructureAndAlgorithm.LeetCode;

namespace DataStructureAndAlgorithm.Test.LeetCodeTest;

public class ClassicStringProblemsTest
{
    [Theory]
    [InlineData("abcabcbb", 3)]
    [InlineData("bbbbb", 1)]
    [InlineData("pwwkew", 3)]
    [InlineData("", 0)]
    public void LongestSubstring_ShouldMaintainNonRepeatingWindow(string text, int expected)
    {
        Assert.Equal(expected, ClassicStringProblems.LongestSubstringWithoutRepeatingCharacters(text));
    }

    [Theory]
    [InlineData("()[]{}", true)]
    [InlineData("([{}])", true)]
    [InlineData("(]", false)]
    [InlineData("([)]", false)]
    [InlineData("(", false)]
    public void ValidParentheses_ShouldRequireCorrectOrderAndClosure(string text, bool expected)
    {
        Assert.Equal(expected, ClassicStringProblems.HasValidParentheses(text));
    }

    [Theory]
    [InlineData("ADOBECODEBANC", "ABC", "BANC")]
    [InlineData("a", "a", "a")]
    [InlineData("a", "aa", "")]
    public void MinimumWindow_ShouldReturnShortestCoveringSubstring(string text, string target, string expected)
    {
        Assert.Equal(expected, ClassicStringProblems.MinimumWindowSubstring(text, target));
    }
}
