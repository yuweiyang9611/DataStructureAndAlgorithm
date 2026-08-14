using DataStructureAndAlgorithm.Caching;

namespace DataStructureAndAlgorithm.Test.CachingTest;

/// <summary>
/// 验证 LFU 缓存的两级淘汰顺序：先比较频率，同频时再比较最近使用时间。
/// </summary>
/// <remarks>
/// 只验证“低频先淘汰”还不够，因为真实热点经常拥有相同频率；如果同频规则依赖字典枚举顺序，
/// 相同输入可能在不同运行时得到不同结果。这里把同频 LRU、删除后的最小频率修复和稳定快照分别锁定。
/// </remarks>
public sealed class LfuCacheTests
{
    [Fact]
    public void Set_WhenAllEntriesHaveSameFrequency_EvictsLeastRecentlyUsedEntry()
    {
        var cache = new LfuCache<string, int>(capacity: 3, StringComparer.Ordinal);
        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);

        // 按 a、b、c 的顺序命中后，三者都处于频率 2；c 最新，a 最旧。
        // 这比“某个键频率更低”的测试更准确地验证 LFU 的 LRU 次级排序规则。
        Assert.True(cache.TryGetValue("a", out _));
        Assert.True(cache.TryGetValue("b", out _));
        Assert.True(cache.TryGetValue("c", out _));

        var beforeEviction = cache.GetFrequencySnapshot();
        Assert.True(beforeEviction.SequenceEqual(
        [
            ("c", 2),
            ("b", 2),
            ("a", 2)
        ]));

        cache.Set("d", 4);

        Assert.False(cache.TryGetValue("a", out _));
        Assert.True(cache.TryGetValue("b", out var b));
        Assert.Equal(2, b);
        Assert.True(cache.TryGetValue("c", out var c));
        Assert.Equal(3, c);
        Assert.True(cache.TryGetValue("d", out var d));
        Assert.Equal(4, d);
    }

    [Fact]
    public void Remove_RepairsMinimumFrequencyAndDoesNotRemoveOtherEntries()
    {
        var cache = new LfuCache<string, int>(capacity: 3, StringComparer.Ordinal);
        cache.Set("cold", 1);
        cache.Set("warm", 2);
        cache.Set("hot", 3);
        Assert.True(cache.TryGetValue("warm", out _));
        Assert.True(cache.TryGetValue("hot", out _));
        Assert.True(cache.TryGetValue("hot", out _));

        // 删除唯一的最低频条目后，内部 minimumFrequency 必须前进到 2。
        // 随后的插入会重新建立频率 1；若最小频率修复错误，下一次满容量插入就会找不到桶。
        Assert.True(cache.Remove("cold"));
        Assert.False(cache.Remove("cold"));
        Assert.Equal(2, cache.Count);

        cache.Set("new", 4);
        cache.Set("newer", 5); // 满容量时应淘汰刚插入、频率仍为 1 的 new。

        Assert.False(cache.TryGetValue("cold", out _));
        Assert.False(cache.TryGetValue("new", out _));
        Assert.True(cache.TryGetValue("warm", out var warm));
        Assert.Equal(2, warm);
        Assert.True(cache.TryGetValue("hot", out var hot));
        Assert.Equal(3, hot);
        Assert.True(cache.TryGetValue("newer", out var newer));
        Assert.Equal(5, newer);
        Assert.Equal(3, cache.Count);
    }

    [Fact]
    public void GetFrequencySnapshot_IsStableAndDoesNotPromoteEntries()
    {
        var cache = new LfuCache<string, int>(capacity: 4, StringComparer.Ordinal);
        cache.Set("alpha", 1);
        cache.Set("beta", 2);
        cache.Set("gamma", 3);
        cache.Set("delta", 4);
        Assert.True(cache.TryGetValue("beta", out _));
        Assert.True(cache.TryGetValue("alpha", out _));
        Assert.True(cache.TryGetValue("alpha", out _));

        // 快照按“频率升序、同频 MRU -> LRU”排序，并且读取快照本身不能算作缓存访问。
        var expected = new (string Key, int Frequency)[]
        {
            ("delta", 1),
            ("gamma", 1),
            ("beta", 2),
            ("alpha", 3)
        };
        var first = cache.GetFrequencySnapshot();
        var second = cache.GetFrequencySnapshot();

        Assert.True(first.SequenceEqual(expected));
        Assert.True(second.SequenceEqual(expected));
        Assert.Equal(4, cache.Count);
    }
}
