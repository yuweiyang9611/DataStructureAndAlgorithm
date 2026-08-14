using DataStructureAndAlgorithm.Caching;

namespace DataStructureAndAlgorithm.Test.CachingTest;

/// <summary>
/// 验证 LFU 频率饱和策略。测试使用较小上限快速抵达生产环境中 <see cref="int.MaxValue"/> 的同一分支。
/// </summary>
public sealed class LfuCacheFrequencyBoundaryTests
{
    [Fact]
    public void SaturatedHit_ShouldRefreshLruOrderWithoutChangingFrequency()
    {
        var cache = new LfuCache<string, int>(
            capacity: 3,
            maximumFrequency: 2,
            StringComparer.Ordinal);
        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);

        Assert.True(cache.TryGetValue("a", out _));
        Assert.True(cache.TryGetValue("b", out _));
        Assert.True(cache.TryGetValue("c", out _));
        Assert.True(cache.TryGetValue("a", out _));

        // 三项都已饱和为 2；最后一次命中的 a 仍应成为 MRU，而不是因不能 +1 就完全忽略访问。
        Assert.True(cache.GetFrequencySnapshot().SequenceEqual(
        [
            ("a", 2),
            ("c", 2),
            ("b", 2)
        ]));
    }

    [Fact]
    public void SetExisting_AtSaturation_ShouldAtomicallyUpdateValueAndRecency()
    {
        var cache = new LfuCache<string, int>(
            capacity: 3,
            maximumFrequency: 2,
            StringComparer.Ordinal);
        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);
        Assert.True(cache.TryGetValue("a", out _));
        Assert.True(cache.TryGetValue("b", out _));
        Assert.True(cache.TryGetValue("c", out _));

        cache.Set("b", 20);

        // Set(existing) 在饱和边界仍是一次完整操作：b 的值更新、频率保持 2，并成为同频 MRU。
        Assert.True(cache.GetFrequencySnapshot().SequenceEqual(
        [
            ("b", 2),
            ("c", 2),
            ("a", 2)
        ]));
        Assert.True(cache.TryGetValue("b", out var value));
        Assert.Equal(20, value);

        // b 的再次读取把它保持在 MRU；插入 d 时应淘汰同频桶尾部的 a，证明桶没有在边界处损坏。
        cache.Set("d", 4);
        Assert.False(cache.TryGetValue("a", out _));
        Assert.True(cache.TryGetValue("c", out _));
        Assert.True(cache.TryGetValue("d", out _));
    }

    [Fact]
    public void Constructor_ShouldRejectNonPositiveTestFrequencyCeiling()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LfuCache<string, int>(capacity: 1, maximumFrequency: 0));
    }
}
