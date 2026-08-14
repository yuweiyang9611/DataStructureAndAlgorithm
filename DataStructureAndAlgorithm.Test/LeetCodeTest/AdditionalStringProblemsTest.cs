using DataStructureAndAlgorithm.LeetCode;

namespace DataStructureAndAlgorithm.Test.LeetCodeTest;

public class AdditionalStringProblemsTest
{
    [Fact]
    public void P5_LongestPalindromicSubstring()
    {
        var result = AdditionalStringProblems.LongestPalindromicSubstring("babad");
        Assert.Equal(3, result.Length);
        Assert.Equal(result, new string(result.Reverse().ToArray()));
    }

    [Fact]
    public void P8_StringToInteger()
    {
        Assert.Equal(-42, AdditionalStringProblems.StringToInteger("   -42 words"));
        Assert.Equal(int.MaxValue, AdditionalStringProblems.StringToInteger("99999999999"));
    }

    [Fact]
    public void P14_LongestCommonPrefix() =>
        Assert.Equal("fl", AdditionalStringProblems.LongestCommonPrefix(["flower", "flow", "flight"]));

    [Fact]
    public void P49_GroupAnagrams()
    {
        var groups = AdditionalStringProblems.GroupAnagrams(["eat", "tea", "tan", "ate", "nat", "bat"]);
        Assert.Equal(3, groups.Count);
        Assert.Contains(groups, group => group.Order().SequenceEqual(["ate", "eat", "tea"]));
    }

    [Fact]
    public void P125_IsPalindromeIgnoringSymbols() =>
        Assert.True(AdditionalStringProblems.IsPalindromeIgnoringSymbols("A man, a plan, a canal: Panama"));

    [Fact]
    public void P202_IsHappyNumber()
    {
        Assert.True(AdditionalStringProblems.IsHappyNumber(19));
        Assert.False(AdditionalStringProblems.IsHappyNumber(2));
    }

    [Fact]
    public void P242_IsAnagram() => Assert.True(AdditionalStringProblems.IsAnagram("anagram", "nagaram"));

    [Fact]
    public void P387_FirstUniqueCharacter() => Assert.Equal(2, AdditionalStringProblems.FirstUniqueCharacter("loveleetcode"));

    [Fact]
    public void P394_DecodeString() => Assert.Equal("accaccacc", AdditionalStringProblems.DecodeString("3[a2[c]]"));

    [Fact]
    public void P409_LongestPalindromeLength() => Assert.Equal(7, AdditionalStringProblems.LongestPalindromeLength("abccccdd"));

    [Fact]
    public void P438_FindAnagramStartIndexes() =>
        Assert.Equal([0, 6], AdditionalStringProblems.FindAnagramStartIndexes("cbaebabacd", "abc"));

    [Fact]
    public void P567_ContainsPermutation() => Assert.True(AdditionalStringProblems.ContainsPermutation("ab", "eidbaooo"));

    [Fact]
    public void P647_CountPalindromicSubstrings() => Assert.Equal(6, AdditionalStringProblems.CountPalindromicSubstrings("aaa"));

    [Fact]
    public void P763_PartitionLabels() =>
        Assert.Equal([9, 7, 8], AdditionalStringProblems.PartitionLabels("ababcbacadefegdehijhklij"));

    [Fact]
    public void P1768_MergeAlternately() => Assert.Equal("apbqcr", AdditionalStringProblems.MergeAlternately("abc", "pqr"));
}
