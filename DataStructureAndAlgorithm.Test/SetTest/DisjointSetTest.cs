using DataStructureAndAlgorithm.Set;

namespace DataStructureAndAlgorithm.Test.SetTest;

public class DisjointSetTest
{
    [Fact]
    public void Union_ShouldMergeBySetAndMaintainComponentSizes()
    {
        var sets = new DisjointSet<int>();
        foreach (var value in Enumerable.Range(1, 6))
        {
            sets.Add(value);
        }

        Assert.True(sets.Union(1, 2));
        Assert.True(sets.Union(2, 3));
        Assert.True(sets.Union(4, 5));
        Assert.False(sets.Union(1, 3));

        Assert.True(sets.AreConnected(1, 3));
        Assert.False(sets.AreConnected(1, 4));
        Assert.Equal(3, sets.GetSetSize(2));
        Assert.Equal(3, sets.SetCount);
    }

    [Fact]
    public void MissingElement_ShouldThrowClearException()
    {
        var sets = new DisjointSet<string>();
        sets.Add("known");

        Assert.Throws<KeyNotFoundException>(() => sets.Find("missing"));
    }
}
