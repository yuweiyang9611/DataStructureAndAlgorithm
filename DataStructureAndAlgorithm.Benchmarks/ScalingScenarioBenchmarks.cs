using BenchmarkDotNet.Attributes;
using DataStructureAndAlgorithm.Scenarios.CityDelivery;
using DataStructureAndAlgorithm.Scenarios.MiniSearch;
using DataStructureAndAlgorithm.Scenarios.ProjectScheduling;

namespace DataStructureAndAlgorithm.Benchmarks;

[MemoryDiagnoser]
public class SearchScalingScenarioBenchmarks
{
    private SearchDocument[] _documents = [];
    private MiniSearchEngineV2 _warm = null!;
    private MiniSearchEngineV2 _cold = null!;
    [Params(100, 1_000, 10_000)] public int DocumentCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        _documents = Enumerable.Range(0, DocumentCount).Select(index =>
            new SearchDocument(index, $"search group{index % 16}", "search trie index cache graph algorithm", random.Next(100))).ToArray();
        _warm = Build();
        _warm.Search("search trie", 10);
    }

    private MiniSearchEngineV2 Build()
    {
        var engine = new MiniSearchEngineV2();
        foreach (var document in _documents) engine.AddDocument(document);
        return engine;
    }
    [IterationSetup(Target = nameof(ColdQuery))]
    public void PrepareCold() => _cold = Build();
    [Benchmark] public int BuildIndex() => Build().DocumentCount;
    [Benchmark] public SearchResponse ColdQuery() => _cold.Search("search trie", 10);
    [Benchmark] public SearchResponse CachedQuery() => _warm.Search("search trie", 10);
    // BM25 currently has no query cache; measure its ranking work explicitly.
    [Benchmark] public Bm25SearchResponse Bm25Query() => _warm.SearchBm25("search trie", 10);
}

[MemoryDiagnoser]
public class DeliveryScalingScenarioBenchmarks
{
    private CityDeliveryScenario _scenario = null!;
    private CityDeliveryPlanner _warm = null!;
    [Params(20, 100)] public int LocationCount { get; set; }
    [Params(2, 8)] public int OutDegree { get; set; }
    [GlobalSetup]
    public void Setup()
    {
        var locations = Enumerable.Range(0, LocationCount).Select(i => new DeliveryLocation($"L{i:D3}")).ToArray();
        var roads = new List<DeliveryRoad>();
        var random = new Random(42);
        for (var i = 0; i < LocationCount; i++)
            for (var offset = 1; offset <= OutDegree; offset++)
                roads.Add(new DeliveryRoad(locations[i].Id, locations[(i + offset) % LocationCount].Id,
                    random.Next(1, 10), random.Next(1, 10), IsBidirectional: false));
        var time = DateTimeOffset.UnixEpoch;
        _scenario = new CityDeliveryScenario(1, time, locations, roads,
            Enumerable.Range(0, 8).Select(i => new DeliveryVehicle($"V{i}", locations[i].Id, 2, [])).ToArray(),
            Enumerable.Range(0, 8).Select(i => new DeliveryOrder($"O{i}", locations[LocationCount - i - 1].Id, 1, time.AddDays(1))).ToArray());
        _warm = new CityDeliveryPlanner(routeCacheCapacity: 128);
        _warm.Plan(_scenario);
    }
    [Benchmark] public CityDeliveryPlan FirstPlan() => new CityDeliveryPlanner(routeCacheCapacity: 128).Plan(_scenario);
    [Benchmark] public CityDeliveryPlan CachedRoutes() => _warm.Plan(_scenario);
}

[MemoryDiagnoser]
public class SchedulingScalingScenarioBenchmarks
{
    private WorkItem[] _items = [];
    [Params(20, 100)] public int TaskCount { get; set; }
    [GlobalSetup]
    public void Setup() => _items = Enumerable.Range(0, TaskCount).Select(i =>
        new WorkItem($"T{i:D3}", 1 + i % 3, 1 + i % 2, i < 4 ? [] : [$"T{i - 4:D3}"])).ToArray();
    [Benchmark] public ProjectSchedule Greedy() => new ProjectScheduler(4).CreateSchedule(_items);
}

[MemoryDiagnoser]
public class ExactScalingScenarioBenchmarks
{
    private WorkItem[] _items = [];
    [Params(4, 6, 8)] public int TaskCount { get; set; }
    [GlobalSetup]
    public void Setup() => _items = Enumerable.Range(0, TaskCount).Select(i =>
        new WorkItem($"T{i}", 1 + i % 2, 1, i < 2 ? [] : [$"T{i - 2}"])).ToArray();
    [Benchmark] public ProjectScheduleOptimalityComparison ExactComparison() => new ProjectScheduler(2).CompareWithOptimalSchedule(_items);
}
