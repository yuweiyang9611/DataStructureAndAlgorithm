using DataStructureAndAlgorithm.Caching;
using DataStructureAndAlgorithm.Probabilistic;
using DataStructureAndAlgorithm.Tree;

namespace DataStructureAndAlgorithm.Test.TreeTest;

/// <summary>验证迷你存储引擎所依赖的三个可复用结构。</summary>
public sealed class BPlusTreeTest
{
    [Fact]
    public void BPlusTree_UpsertAndRangeMatchSortedDictionary()
    {
        var tree = new BPlusTree<int, string>(order: 4);
        var expected = new SortedDictionary<int, string>();

        // 逆序和交错更新会频繁触发左侧叶子分裂，比只测试递增插入更容易发现分隔键错误。
        for (var key = 99; key >= 0; key--)
        {
            Assert.True(tree.Upsert(key, $"value-{key}"));
            expected[key] = $"value-{key}";
            Assert.True(tree.HasValidInvariants());
        }

        Assert.False(tree.Upsert(50, "updated"));
        expected[50] = "updated";

        Assert.Equal(expected.Count, tree.Count);
        Assert.True(tree.SequenceEqual(expected));
        Assert.True(tree.Range(20, 31).SequenceEqual(expected.Where(pair => pair.Key is >= 20 and < 31)));
        Assert.True(tree.TryGetValue(50, out var updated));
        Assert.Equal("updated", updated);
        Assert.False(tree.TryGetValue(1000, out _));
        Assert.True(tree.HasValidInvariants());
    }

    [Fact]
    public void Range_WithOneResult_StopsTheLazyLeafWalkEarly()
    {
        var comparer = new CountingIntComparer();
        var tree = new BPlusTree<int, int>(order: 16, comparer);
        for (var key = 0; key < 10_000; key++)
        {
            tree.Upsert(key, key);
        }

        comparer.Reset();
        var result = tree.Range(0, 10_000, maximumCount: 1);

        Assert.Equal([new KeyValuePair<int, int>(0, 0)], result);
        // 若 Range 先物化整个区间，这里会发生约一万次上界比较；惰性叶链只需树高定位和首项比较。
        Assert.InRange(comparer.ComparisonCount, 1, 100);
    }

    private sealed class CountingIntComparer : IComparer<int>
    {
        public int ComparisonCount { get; private set; }

        public int Compare(int left, int right)
        {
            ComparisonCount++;
            return left.CompareTo(right);
        }

        public void Reset() => ComparisonCount = 0;
    }

    [Fact]
    public void BloomFilter_HasNoFalseNegativesForInsertedKeys()
    {
        var filter = new StringBloomFilter(bitCount: 256, hashFunctionCount: 4);
        var values = Enumerable.Range(0, 50).Select(index => $"key:{index:D2}").ToArray();

        foreach (var value in values)
        {
            filter.Add(value);
        }

        // Bloom Filter 允许未插入键偶尔返回 true，所以这里只断言其真正的硬契约：已插入键绝不能返回 false。
        Assert.All(values, value => Assert.True(filter.MightContain(value)));
    }

    [Fact]
    public void LfuCache_EvictsLeastFrequentAndUsesLruForTies()
    {
        var cache = new LfuCache<string, int>(capacity: 2, StringComparer.Ordinal);
        cache.Set("a", 1);
        cache.Set("b", 2);
        Assert.True(cache.TryGetValue("a", out _)); // a 的频率升为 2。

        cache.Set("c", 3); // b 仍为频率 1，因此先淘汰 b。

        Assert.True(cache.TryGetValue("a", out var a));
        Assert.Equal(1, a);
        Assert.False(cache.TryGetValue("b", out _));
        Assert.True(cache.TryGetValue("c", out var c));
        Assert.Equal(3, c);
    }

    [Fact]
    public void Structures_RejectInvalidConfigurationAndRanges()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BPlusTree<int, int>(order: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StringBloomFilter(bitCount: 7));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LfuCache<int, int>(capacity: 0));

        var tree = new BPlusTree<int, int>();
        Assert.Throws<ArgumentException>(() => tree.Range(2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.Range(1, 2, maximumCount: -1));
    }
}
