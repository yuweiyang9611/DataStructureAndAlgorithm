// 迷你存储引擎：B+ 树做有序索引，Bloom 过滤器挡掉不存在的键，LFU 缓存记住热点读。
// 浏览器里的引擎只在内存中工作；写前日志与崩溃恢复要在本机运行 Scenarios.MiniStorage 项目。
using DataStructureAndAlgorithm.Probabilistic;
using DataStructureAndAlgorithm.Scenarios.MiniStorage;
using DataStructureAndAlgorithm.Tree;

public static class Demo
{
    public static void Run()
    {
        var options = new MiniStorageOptions(BPlusTreeOrder: 4, BloomBitCount: 512, BloomHashFunctionCount: 4, CacheCapacity: 2);
        using var engine = new MiniStorageEngine(options);
        engine.Put("user:001", "Ada");
        engine.Put("user:002", "Edsger");
        engine.Put("user:003", "Grace");
        engine.TryGet("user:001", out _);
        engine.TryGet("user:001", out _);
        engine.Delete("user:002");
        engine.TryGet("user:999", out _);

        Console.WriteLine($"活跃键 {engine.Count} 个，范围扫描 user:000..user:999：{string.Join(", ", engine.RangeScan("user:000", "user:999").Select(entry => $"{entry.Key}={entry.Value}@v{entry.Version}"))}");
        Console.WriteLine($"再读 user:002：{(engine.TryGet("user:002", out var deleted) ? deleted!.Value : "已删除")}");
        var statistics = engine.GetStatistics();
        Console.WriteLine($"统计：写 {statistics.PutCount} 删 {statistics.DeleteCount} 读 {statistics.GetCount}，缓存命中 {statistics.CacheHits}，Bloom 直接否定 {statistics.BloomNegativeSkips} 次，索引查找 {statistics.IndexLookups} 次");
        Console.WriteLine($"索引不变量成立：{engine.HasValidIndexInvariants()}");

        var index = new BPlusTree<int, string>(order: 4);
        foreach (var key in new[] { 50, 20, 70, 10, 30, 60, 80, 40 }) index.Upsert(key, $"v{key}");
        Console.WriteLine($"B+ 树（阶 4）装 {index.Count} 个键，范围 [25, 65)：{string.Join(" ", index.Range(25, 65).Select(pair => pair.Key))}，不变量成立 {index.HasValidInvariants()}");

        var bloom = new StringBloomFilter(bitCount: 256, hashFunctionCount: 3);
        foreach (var word in new[] { "alpha", "beta", "gamma" }) bloom.Add(word);
        Console.WriteLine($"Bloom 过滤器：alpha {bloom.MightContain("alpha")}，delta {bloom.MightContain("delta")}（否定一定正确，肯定可能误报）");
    }
}
