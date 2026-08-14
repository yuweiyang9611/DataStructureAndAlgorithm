using DataStructureAndAlgorithm.Caching;

namespace DataStructureAndAlgorithm.Test.CachingTest;

public class LruCacheTest
{
    [Fact]
    public void Set_ShouldEvictLeastRecentlyUsedEntry()
    {
        var cache = new LruCache<int, string>(2);
        cache.Set(1, "one");
        cache.Set(2, "two");

        Assert.True(cache.TryGetValue(1, out _)); // 1 变为最近使用，2 变为最旧。
        cache.Set(3, "three");

        Assert.False(cache.TryGetValue(2, out _));
        Assert.True(cache.TryGetValue(1, out var one));
        Assert.Equal("one", one);
        Assert.Equal([1, 3], cache.GetKeysMostRecentFirst());
    }

    [Fact]
    public void UpdatingExistingEntry_ShouldNotIncreaseCount()
    {
        var cache = new LruCache<string, int>(2, StringComparer.OrdinalIgnoreCase);
        cache.Set("A", 1);
        cache.Set("a", 2);

        Assert.Equal(1, cache.Count);
        Assert.True(cache.TryGetValue("A", out var value));
        Assert.Equal(2, value);
    }
}
