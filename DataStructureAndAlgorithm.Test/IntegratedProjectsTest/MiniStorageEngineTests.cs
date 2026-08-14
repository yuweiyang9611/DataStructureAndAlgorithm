using System.IO;
using System.Text.Json;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Scenarios.MiniStorage;
using FsCheck.Xunit;

namespace DataStructureAndAlgorithm.Test.IntegratedProjectsTest;

/// <summary>
/// 迷你存储引擎测试同时验证领域结果和内部结构不变量；参考模型只使用 SortedDictionary，
/// 不复制 B+ 树、Bloom Filter、LFU 或 WAL 的实现。
/// </summary>
public sealed class MiniStorageEngineTests
{
    [Fact]
    public void PutGetDeleteAndRangeScanRespectTombstonesAndCacheLayers()
    {
        using var engine = new MiniStorageEngine(new MiniStorageOptions(
            BPlusTreeOrder: 3,
            BloomBitCount: 256,
            BloomHashFunctionCount: 4,
            CacheCapacity: 2));

        engine.Put("item:03", "three");
        engine.Put("item:01", "one");
        engine.Put("item:02", "two");
        engine.Put("item:02", "two-updated");

        Assert.True(engine.TryGet("item:02", out var item));
        Assert.Equal("two-updated", item!.Value);
        Assert.True(engine.TryGet("item:02", out _));
        Assert.True(engine.Delete("item:02"));
        Assert.False(engine.Delete("item:02"));
        Assert.False(engine.TryGet("item:02", out _));
        Assert.False(engine.TryGet("missing", out _));

        var range = engine.RangeScan("item:00", "item:99");
        Assert.True(range.Select(entry => entry.Key).SequenceEqual(["item:01", "item:03"]));
        Assert.Empty(engine.RangeScan("item:00", "item:99", maximumCount: 0));
        Assert.Equal(2, engine.Count);
        Assert.True(engine.HasValidIndexInvariants());

        var statistics = engine.GetStatistics();
        Assert.Equal(3, statistics.IndexedKeyCount); // 墓碑仍是等待压缩的物理记录。
        Assert.True(statistics.CacheHits >= 2);
        Assert.True(statistics.BloomNegativeSkips >= 1);
    }

    [Fact]
    public void WriteAheadLog_RestoresLatestValuesAndDeletesAfterRestart()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mini-storage-test-{Guid.NewGuid():N}.wal");
        try
        {
            var options = new MiniStorageOptions(WriteAheadLogPath: path, CacheCapacity: 2);
            using (var first = new MiniStorageEngine(options))
            {
                first.Put("alpha", "v1");
                first.Put("beta", "v2");
                first.Put("alpha", "v3");
                Assert.True(first.Delete("beta"));
            }

            using var recovered = new MiniStorageEngine(options);
            Assert.True(recovered.TryGet("alpha", out var alpha));
            Assert.Equal("v3", alpha!.Value);
            Assert.Equal(3, alpha.Version);
            Assert.False(recovered.TryGet("beta", out _));
            Assert.Equal(1, recovered.Count);
            Assert.Equal(4, recovered.GetStatistics().WalRecordsReplayed);
            Assert.True(recovered.HasValidIndexInvariants());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void WriteAheadLog_IgnoresAndRepairsOnlyAnUncommittedTail()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mini-storage-torn-tail-{Guid.NewGuid():N}.wal");
        var options = new MiniStorageOptions(WriteAheadLogPath: path, CacheCapacity: 2);
        try
        {
            using (var first = new MiniStorageEngine(options))
            {
                first.Put("alpha", "one");
                first.Put("beta", "two");
            }

            // 模拟崩溃发生在 JSON 尚未写完、LF 提交标记也尚未落盘的时刻。
            File.AppendAllText(path, "{\"Sequence\":3,\"Operation\":0,\"Key\":\"torn", new System.Text.UTF8Encoding(false));

            using (var recovered = new MiniStorageEngine(options))
            {
                Assert.True(recovered.TryGet("alpha", out var alpha));
                Assert.Equal("one", alpha!.Value);
                Assert.True(recovered.TryGet("beta", out _));
                Assert.Equal(2, recovered.GetStatistics().WalRecordsReplayed);

                // 若尾部只被“忽略”而未截断，这次追加会和半条 JSON 粘在一起，并让下一次恢复失败。
                recovered.Put("gamma", "three");
            }

            using var secondRecovery = new MiniStorageEngine(options);
            Assert.True(secondRecovery.TryGet("gamma", out var gamma));
            Assert.Equal("three", gamma!.Value);
            Assert.Equal(3, secondRecovery.GetStatistics().WalRecordsReplayed);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("")]
    public void WriteAheadLog_RejectsCorruptionThatHasACommitMarker(string corruptedLine)
    {
        var path = Path.Combine(Path.GetTempPath(), $"mini-storage-corrupt-line-{Guid.NewGuid():N}.wal");
        var options = new MiniStorageOptions(WriteAheadLogPath: path);
        try
        {
            using (var first = new MiniStorageEngine(options))
            {
                first.Put("alpha", "one");
            }

            // 空白行也不能跳过：它可能是某条已提交记录被损坏后的残骸，静默继续会恢复出错误状态。
            File.AppendAllText(
                path, $"{corruptedLine}{Environment.NewLine}", new System.Text.UTF8Encoding(false));

            Assert.Throws<InvalidDataException>(() => new MiniStorageEngine(options));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RangeScan_WithSmallLimit_SkipsTombstonesAndStopsAfterEnoughLiveValues()
    {
        using var engine = new MiniStorageEngine(new MiniStorageOptions(BPlusTreeOrder: 16));
        for (var index = 0; index < 1_000; index++)
        {
            engine.Put($"key:{index:D4}", $"value:{index:D4}");
        }

        for (var index = 0; index < 40; index++)
        {
            Assert.True(engine.Delete($"key:{index:D4}"));
        }

        var result = engine.RangeScan("key:0000", "key:9999", maximumCount: 1);

        Assert.Equal([new StorageEntry("key:0040", "value:0040", Version: 41)], result);
        Assert.True(engine.HasValidIndexInvariants());
    }

    [Property(MaxTest = 80)]
    public bool RandomOperationSequence_MatchesSortedDictionaryAfterEveryStep(int[]? source)
    {
        var operations = source?.Take(80).ToArray() ?? [];
        using var engine = new MiniStorageEngine(new MiniStorageOptions(
            BPlusTreeOrder: 4,
            BloomBitCount: 512,
            BloomHashFunctionCount: 4,
            CacheCapacity: 4));
        var model = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var raw in operations)
        {
            var bits = unchecked((uint)raw);
            var key = $"key:{bits % 12:D2}";
            switch ((bits >> 8) % 3)
            {
                case 0:
                    var value = $"value:{bits % 97:D2}";
                    engine.Put(key, value);
                    model[key] = value;
                    break;
                case 1:
                    if (engine.Delete(key) != model.Remove(key))
                    {
                        return false;
                    }

                    break;
                default:
                    var actualFound = engine.TryGet(key, out var actual);
                    var expectedFound = model.TryGetValue(key, out var expected);
                    if (actualFound != expectedFound || actualFound && actual!.Value != expected)
                    {
                        return false;
                    }

                    break;
            }

            var actualSnapshot = engine.RangeScan("key:00", "key:99")
                .Select(entry => new KeyValuePair<string, string>(entry.Key, entry.Value));
            if (engine.Count != model.Count || !actualSnapshot.SequenceEqual(model) ||
                !engine.HasValidIndexInvariants())
            {
                return false;
            }
        }

        return true;
    }

    [Fact]
    public void OptionalTraceExplainsLayersWithoutChangingStorageResults()
    {
        using var plain = new MiniStorageEngine();
        var trace = new CollectingAlgorithmTraceSink();
        using var traced = new MiniStorageEngine(trace: trace);

        ApplyScenario(plain);
        ApplyScenario(traced);

        var plainResult = new
        {
            entries = plain.RangeScan("a", "z"),
            plain.Count,
            statistics = plain.GetStatistics()
        };
        var tracedResult = new
        {
            entries = traced.RangeScan("a", "z"),
            traced.Count,
            statistics = traced.GetStatistics()
        };

        Assert.Equal(JsonSerializer.Serialize(plainResult), JsonSerializer.Serialize(tracedResult));
        Assert.Contains(trace.Events, item => item.Operation == "Put");
        Assert.Contains(trace.Events, item => item.Operation == "Delete");
        Assert.Contains(trace.Events, item => item.Operation == "CacheHit");
        Assert.Contains(trace.Events, item => item.Operation == "BloomNegative");
        Assert.Contains(trace.Events, item => item.Operation == "RangeScan");
        Assert.All(trace.Events, item => Assert.Equal("MiniStorage", item.Algorithm));
        Assert.True(trace.Events.Select(item => item.Step).SequenceEqual(Enumerable.Range(1, trace.Events.Count)));
    }

    private static void ApplyScenario(MiniStorageEngine engine)
    {
        engine.Put("alpha", "one");
        engine.Put("beta", "two");
        engine.TryGet("alpha", out _);
        engine.TryGet("unknown", out _);
        engine.Delete("beta");
    }
}
