using DataStructureAndAlgorithm.PatternMatching;

namespace DataStructureAndAlgorithm.Test.PatternMatchingTest;

public class P3StringAlgorithmsTest
{
    [Theory]
    [InlineData("babad", 3)]
    [InlineData("cbbd", 2)]
    [InlineData("a", 1)]
    [InlineData("", 0)]
    public void Manacher_ReturnsALongestPalindrome(string text, int expectedLength)
    {
        var range = P3StringAlgorithms.LongestPalindromeManacher(text);
        var palindrome = text[range.Start..range.EndExclusive];
        Assert.Equal(expectedLength, range.Length);
        Assert.Equal(palindrome, new string(palindrome.Reverse().ToArray()));
    }

    [Fact]
    public void RuneSlidingWindow_DoesNotSplitSurrogatePairs()
    {
        const string text = "😀A😀BC";
        var range = P3StringAlgorithms.LongestSubstringWithoutRepeatedRunes(text);

        Assert.Equal(4, range.RuneLength);
        Assert.Equal("A😀BC", text.Substring(range.Utf16Start, range.Utf16Length));
    }

    [Fact]
    public void SuffixAutomaton_RecognizesSubstringsAndCountsDistinctOnes()
    {
        var automaton = new SuffixAutomaton("ababa");

        Assert.True(automaton.Contains("bab"));
        Assert.False(automaton.Contains("baa"));
        Assert.Equal(9, automaton.CountDistinctSubstrings());
        Assert.Equal(4, automaton.LongestCommonSubstringLength("zzbabaqq"));
    }
}
