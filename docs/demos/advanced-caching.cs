// LRU 与 LFU 缓存：同样容量为 2，同样的访问序列，两种策略淘汰的键不一样。
// 改一改读取次数，看 LFU 的频次快照和淘汰结果。
using DataStructureAndAlgorithm.Caching;

public static class Demo
{
    public static void Run()
    {
        var lru = new LruCache<string, int>(capacity: 2);
        lru.Set("a", 1);
        lru.Set("b", 2);
        lru.TryGetValue("a", out _);
        lru.Set("c", 3);
        Console.WriteLine($"LRU 容量 2：写 a、写 b、读 a、写 c，b 最久没用被淘汰（b 还在？{lru.TryGetValue("b", out _)}），最近优先 {string.Join(" ", lru.GetKeysMostRecentFirst())}");

        var lfu = new LfuCache<string, int>(capacity: 2);
        lfu.Set("a", 1);
        lfu.Set("b", 2);
        lfu.TryGetValue("a", out _);
        lfu.TryGetValue("a", out _);
        lfu.TryGetValue("b", out _);
        Console.WriteLine($"LFU 容量 2：写入各算一次，再读 a 两次、读 b 一次，频次快照 {Snapshot(lfu)}");
        lfu.Set("c", 3);
        Console.WriteLine($"写 c 后频次最低的 b 被淘汰（b 还在？{lfu.TryGetValue("b", out _)}），快照 {Snapshot(lfu)}");

        lfu.Set("c", 30);
        lfu.TryGetValue("c", out var value);
        Console.WriteLine($"覆写 c 后读到 {value}，Count = {lfu.Count}，Capacity = {lfu.Capacity}");
    }

    static string Snapshot(LfuCache<string, int> cache) =>
        string.Join(", ", cache.GetFrequencySnapshot().Select(entry => $"{entry.Key}×{entry.Frequency}"));
}
