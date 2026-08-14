using DataStructureAndAlgorithm.Tree;

namespace DataStructureAndAlgorithm.Test.TreeTest;

public class TrieTest
{
    [Fact]
    public void Trie_ShouldSharePrefixesAndReturnSortedCompletions()
    {
        var trie = new Trie();
        trie.Add("car");
        trie.Add("card");
        trie.Add("care");
        trie.Add("cat");

        Assert.True(trie.Contains("car"));
        Assert.False(trie.Contains("ca"));
        Assert.True(trie.StartsWith("ca"));
        Assert.Equal(["car", "card", "care"], trie.GetWordsWithPrefix("car"));
    }

    [Fact]
    public void Remove_ShouldKeepNodesSharedWithOtherWords()
    {
        var trie = new Trie();
        trie.Add("");
        trie.Add("to");
        trie.Add("top");

        Assert.True(trie.Remove("to"));
        Assert.False(trie.Contains("to"));
        Assert.True(trie.Contains("top"));
        Assert.True(trie.Contains(""));
        Assert.Equal(2, trie.Count);
    }
}
