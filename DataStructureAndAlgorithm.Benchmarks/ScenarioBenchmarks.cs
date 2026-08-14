using BenchmarkDotNet.Attributes;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Scenarios.CityDelivery;
using DataStructureAndAlgorithm.Scenarios.MiniSearch;
using DataStructureAndAlgorithm.Scenarios.MiniStorage;
using DataStructureAndAlgorithm.Scenarios.ProjectScheduling;

namespace DataStructureAndAlgorithm.Benchmarks;

/// <summary>
/// 四个综合项目的端到端基准烟雾集。
/// </summary>
/// <remarks>
/// 不同方法代表不同业务，不应横向比较谁“更快”；它们的用途是长期观察同一个方法的趋势。
/// 输入全部固定，避免随机数和系统时钟把业务变化伪装成性能变化。MiniStorage 额外保留追踪开关对照，
/// 用真实工作负载回答“可解释性需要付出多少时间和分配”这一工程问题。
/// </remarks>
[MemoryDiagnoser]
public class ScenarioBenchmarks
{
    private CityDeliveryScenario _deliveryScenario = null!;
    private WorkItem[] _workItems = [];

    [GlobalSetup]
    public void Setup()
    {
        var planningTime = new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);
        _deliveryScenario = new CityDeliveryScenario(
            MapVersion: 1,
            PlanningTime: planningTime,
            Locations: [new("Depot"), new("A"), new("B"), new("C")],
            Roads:
            [
                new("Depot", "A", TravelMinutes: 4, MaintenanceCost: 6),
                new("Depot", "B", TravelMinutes: 2, MaintenanceCost: 5),
                new("B", "A", TravelMinutes: 1, MaintenanceCost: 2),
                new("A", "C", TravelMinutes: 3, MaintenanceCost: 4),
                new("B", "C", TravelMinutes: 7, MaintenanceCost: 3)
            ],
            Vehicles:
            [
                new("V-01", "Depot", Capacity: 5, Skills: ["ColdChain"]),
                new("V-02", "B", Capacity: 3, Skills: [])
            ],
            Orders:
            [
                new("O-001", "C", Volume: 2, planningTime.AddMinutes(12), Urgency: 5, "ColdChain"),
                new("O-002", "A", Volume: 1, planningTime.AddMinutes(6), Urgency: 3)
            ]);

        _workItems =
        [
            new("准备源码", Duration: 2, ResourceDemand: 2, Dependencies: []),
            new("编译", Duration: 4, ResourceDemand: 1, Dependencies: ["准备源码"]),
            new("静态分析", Duration: 3, ResourceDemand: 2, Dependencies: ["准备源码"]),
            new("单元测试", Duration: 2, ResourceDemand: 2, Dependencies: ["编译"]),
            new("打包", Duration: 1, ResourceDemand: 1, Dependencies: ["编译", "静态分析"]),
            new("发布", Duration: 2, ResourceDemand: 3, Dependencies: ["单元测试", "打包"])
        ];
    }

    [Benchmark]
    public CityDeliveryPlan CityDeliveryPlan()
    {
        // 每轮创建规划器，避免上一轮的路线缓存把“完整规划”悄悄变成“缓存命中规划”。
        var planner = new CityDeliveryPlanner(routeCacheCapacity: 16);
        return planner.Plan(_deliveryScenario);
    }

    [Benchmark]
    public Bm25SearchResponse MiniSearchV2Bm25IndexAndQuery()
    {
        // 索引和 BM25 查询一起测，保证基准真正覆盖 V2 位置倒排表，而不是只测 V1 的缓存路径。
        var engine = new MiniSearchEngineV2(queryCacheCapacity: 4);
        engine.AddDocument(new SearchDocument(
            101,
            "Trie 自动补全",
            "A trie shares prefixes and supports search autocomplete.",
            Popularity: 12,
            Keywords: ["trie", "search", "autocomplete"]));
        engine.AddDocument(new SearchDocument(
            102,
            "倒排索引与缓存",
            "An inverted search index maps terms while a cache keeps hot results.",
            Popularity: 18,
            Keywords: ["index", "search", "cache"]));
        engine.AddDocument(new SearchDocument(
            103,
            "综合检索",
            "The search scenario combines a trie, index, heap and cache.",
            Popularity: 25,
            Keywords: ["search", "trie", "index"]));
        return engine.SearchBm25("search trie", maxResults: 3);
    }

    [Benchmark]
    public ProjectSchedule ProjectScheduling()
    {
        var scheduler = new ProjectScheduler(capacity: 3);
        return scheduler.CreateSchedule(_workItems);
    }

    [Benchmark]
    public ProjectScheduleOptimalityComparison ProjectSchedulingExactComparison()
    {
        // 六个工作项处于精确求解器的受控边界内；该实验持续记录“正确性证明”本身的成本。
        var scheduler = new ProjectScheduler(capacity: 3);
        return scheduler.CompareWithOptimalSchedule(_workItems);
    }

    [Benchmark]
    public long MiniStorageWithoutTrace() => RunStorageWorkload(traceEnabled: false);

    [Benchmark]
    public long MiniStorageWithTrace() => RunStorageWorkload(traceEnabled: true);

    private static long RunStorageWorkload(bool traceEnabled)
    {
        // 每次创建新引擎，使有无追踪的两组实验拥有完全相同的空索引、空 Bloom 和空 LFU 起点。
        var trace = traceEnabled ? new CollectingAlgorithmTraceSink() : null;
        using var engine = new MiniStorageEngine(
            new MiniStorageOptions(
                BPlusTreeOrder: 8,
                BloomBitCount: 2_048,
                BloomHashFunctionCount: 4,
                CacheCapacity: 16),
            trace);

        for (var index = 0; index < 64; index++)
        {
            engine.Put($"key:{index:000}", $"value:{index:000}");
        }

        var found = 0L;
        // key:060..063 是刚写入、仍留在容量 16 的 LFU 缓存中的确定性热点。
        // 反复访问这四个键会提高其频率并产生可观察的 CacheHits；不能按 0..63 顺序扫描，
        // 因为冷键会不断写回缓存，恰好在访问热点前把初始缓存全部冲掉。
        for (var round = 0; round < 8; round++)
        {
            for (var index = 60; index < 64; index++)
            {
                if (engine.TryGet($"key:{index:000}", out _))
                {
                    found++;
                }
            }
        }

        // 从未插入的命名空间读取，稳定触发 Bloom negative，从而证明过滤器确实跳过了 B+ 树。
        for (var index = 0; index < 16; index++)
        {
            engine.TryGet($"missing:{index:000}", out _);
        }

        var statistics = engine.GetStatistics();
        if (statistics.CacheHits == 0 || statistics.BloomNegativeSkips == 0)
        {
            throw new InvalidOperationException("基准工作负载必须同时覆盖 LFU 热点命中与 Bloom negative。");
        }

        // 业务输入和所有读写在 trace 开关两边完全相同。把关键统计和事件数量折叠为可消费的校验值，
        // 既阻止 JIT 把工作当作死代码，也让工作负载退化为“零缓存命中”时立即反映到结果中。
        return found
               + statistics.CacheHits * 1_000L
               + statistics.BloomNegativeSkips * 1_000_000L
               + statistics.IndexLookups * 1_000_000_000L
               + (trace?.Events.Count ?? 0) * 1_000_000_000_000L;
    }
}
