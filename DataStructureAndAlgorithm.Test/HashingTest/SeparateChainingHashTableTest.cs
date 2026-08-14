using DataStructureAndAlgorithm.Hashing;

namespace DataStructureAndAlgorithm.Test.HashingTest;

public class SeparateChainingHashTableTest
{
    [Fact]
    public void HashTable_ShouldHandleCollisionsUpdatesRemovalAndResize()
    {
        var table = new SeparateChainingHashTable<string, int>(2, new ConstantHashComparer());

        for (var index = 0; index < 20; index++)
        {
            Assert.True(table.TryAdd($"key-{index}", index));
        }

        Assert.True(table.Capacity > 2);
        Assert.Equal(20, table.Count);
        Assert.False(table.TryAdd("key-1", 100));

        table["key-1"] = 100;
        Assert.Equal(100, table["key-1"]);
        Assert.True(table.Remove("key-10"));
        Assert.False(table.ContainsKey("key-10"));
        Assert.Equal(19, table.Count);
    }

    [Fact]
    public void HashTable_ShouldHonorCustomEqualityComparer()
    {
        var table = new SeparateChainingHashTable<string, int>(comparer: StringComparer.OrdinalIgnoreCase);

        table["Algorithm"] = 1;
        table["ALGORITHM"] = 2;

        Assert.Equal(1, table.Count);
        Assert.Equal(2, table["algorithm"]);
    }

    private sealed class ConstantHashComparer : IEqualityComparer<string>
    {
        public bool Equals(string? first, string? second) => StringComparer.Ordinal.Equals(first, second);

        public int GetHashCode(string value) => 1;
    }
}
