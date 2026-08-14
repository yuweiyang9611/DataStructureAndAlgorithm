using DataStructureAndAlgorithm.Hashing;

namespace DataStructureAndAlgorithm.Test.HashingTest;

public class OpenAddressingHashTableTest
{
    private sealed class ConstantHashComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y) => StringComparer.OrdinalIgnoreCase.Equals(x, y);

        public int GetHashCode(string obj) => 1;
    }

    [Fact]
    public void LinearProbingHandlesCollisionsUpdatesAndTombstones()
    {
        var table = new OpenAddressingHashTable<string, int>(4, new ConstantHashComparer());
        Assert.True(table.TryAdd("alpha", 1));
        Assert.True(table.TryAdd("beta", 2));
        Assert.True(table.TryAdd("gamma", 3));

        Assert.Equal(2, table["BETA"]);
        Assert.False(table.TryAdd("ALPHA", 99));
        table["alpha"] = 10;
        Assert.Equal(10, table["ALPHA"]);

        Assert.True(table.Remove("beta"));
        // gamma 位于被删槽位之后；如果删除时错误地改成 Empty，这次查找会提前停止。
        Assert.Equal(3, table["gamma"]);
        Assert.True(table.TryAdd("delta", 4));
        Assert.Equal(3, table.Count);
        Assert.False(table.Remove("missing"));
    }

    [Fact]
    public void ResizeAndClearPreserveThePublicContract()
    {
        var table = new OpenAddressingHashTable<int, string>(2);
        for (var value = 0; value < 100; value++)
        {
            Assert.True(table.TryAdd(value, value.ToString()));
        }

        Assert.True(table.Capacity > 2);
        Assert.Equal(100, table.Count);
        Assert.All(Enumerable.Range(0, 100), value => Assert.Equal(value.ToString(), table[value]));
        Assert.Equal(100, table.Select(pair => pair.Key).Distinct().Count());

        table.Clear();
        Assert.Empty(table);
        Assert.Equal(0, table.Count);
        Assert.False(table.TryGetValue(1, out _));
        Assert.Throws<KeyNotFoundException>(() => table[1]);
    }
}
