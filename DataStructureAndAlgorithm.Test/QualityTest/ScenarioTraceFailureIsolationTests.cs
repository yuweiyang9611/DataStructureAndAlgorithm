using System.IO;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Scenarios.CityDelivery;
using DataStructureAndAlgorithm.Scenarios.MiniSearch;
using DataStructureAndAlgorithm.Scenarios.MiniStorage;
using DataStructureAndAlgorithm.Scenarios.ProjectScheduling;

namespace DataStructureAndAlgorithm.Test.QualityTest;

/// <summary>
/// 证明可选观察器即使自身故障，也不能把已经成功的领域操作变成“向调用方报错但状态已修改”。
/// </summary>
public sealed class ScenarioTraceFailureIsolationTests
{
    [Fact]
    public void ThrowingTraceSink_DoesNotChangeAnyScenarioResultOrDurability()
    {
        var throwingTrace = new ThrowingTraceSink();

        var search = new MiniSearchEngineV2(trace: throwingTrace);
        search.AddDocument(new SearchDocument(1, "alpha", "reliable search"));
        Assert.Single(search.SearchBm25("alpha").Hits);
        Assert.Equal(1, search.DocumentCount);

        var city = new CityDeliveryPlanner(trace: throwingTrace).Plan(new CityDeliveryScenario(
            MapVersion: 1,
            PlanningTime: DateTimeOffset.UnixEpoch,
            Locations: [new DeliveryLocation("hub")],
            Roads: [],
            Vehicles: [],
            Orders: []));
        Assert.Empty(city.Assignments);
        Assert.Equal(1, city.MaintenanceForest.ComponentCount);

        var schedule = new ProjectScheduler(capacity: 1, throwingTrace).CreateSchedule(
            [new WorkItem("A", Duration: 1, ResourceDemand: 1, Dependencies: [])]);
        Assert.Equal(1, schedule.Makespan);

        var walPath = Path.Combine(Path.GetTempPath(), $"trace-isolation-{Guid.NewGuid():N}.wal");
        try
        {
            var options = new MiniStorageOptions(WriteAheadLogPath: walPath);
            using (var storage = new MiniStorageEngine(options, throwingTrace))
            {
                storage.Put("alpha", "one");
                Assert.True(storage.TryGet("alpha", out var entry));
                Assert.Equal("one", entry!.Value);
            }

            // 最关键的检查不是“没有抛异常”，而是重新启动后 WAL 中仍存在同一个已提交结果。
            using var recovered = new MiniStorageEngine(options);
            Assert.True(recovered.TryGet("alpha", out var recoveredEntry));
            Assert.Equal("one", recoveredEntry!.Value);
        }
        finally
        {
            File.Delete(walPath);
        }

        Assert.True(throwingTrace.AttemptCount > 0);
    }

    private sealed class ThrowingTraceSink : IAlgorithmTraceSink
    {
        public int AttemptCount { get; private set; }

        public void Record(
            string algorithm,
            string operation,
            string description,
            IReadOnlyDictionary<string, string>? state = null)
        {
            AttemptCount++;
            throw new InvalidOperationException("模拟观察器故障。");
        }
    }
}
