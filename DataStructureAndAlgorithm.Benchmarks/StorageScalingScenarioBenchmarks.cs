using BenchmarkDotNet.Attributes;
using DataStructureAndAlgorithm.Scenarios.MiniStorage;

namespace DataStructureAndAlgorithm.Benchmarks;

[MemoryDiagnoser]
public class StorageScalingScenarioBenchmarks
{
    private string _root = "";
    private string _iteration = "";
    private MiniStorageEngine? _checkpointEngine;
    [Params(100, 1_000)] public int LiveKeys { get; set; }
    [Params(1, 10)] public int HistoryPerKey { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _root = Path.Combine(Path.GetTempPath(), $"dsa-bench-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        var full = Path.Combine(_root, "full.wal");
        using (var engine = new MiniStorageEngine(new MiniStorageOptions(WriteAheadLogPath: full)))
        {
            for (var round = 0; round < HistoryPerKey; round++)
                for (var i = 0; i < LiveKeys; i++) engine.Put($"key:{i:D5}", $"value:{round}");
        }
        var compact = Path.Combine(_root, "snapshot.wal");
        File.Copy(full, compact);
        using (var engine = new MiniStorageEngine(new MiniStorageOptions(WriteAheadLogPath: compact))) engine.Checkpoint();
        // Both paths replay to identical values and versions, including one post-checkpoint update.
        foreach (var path in new[] { full, compact })
        {
            using var engine = new MiniStorageEngine(new MiniStorageOptions(WriteAheadLogPath: path));
            engine.Put("key:00000", "incremental");
        }
    }

    [IterationSetup]
    public void Prepare()
    {
        _iteration = Path.Combine(_root, $"iteration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_iteration);
        foreach (var name in new[] { "full.wal", "snapshot.wal", "snapshot.wal.snapshot" })
            File.Copy(Path.Combine(_root, name), Path.Combine(_iteration, name));
        File.Copy(Path.Combine(_root, "full.wal"), Path.Combine(_iteration, "checkpoint.wal"));
        _checkpointEngine = new MiniStorageEngine(new MiniStorageOptions(WriteAheadLogPath: Path.Combine(_iteration, "checkpoint.wal")));
    }
    [IterationCleanup]
    public void CleanupIteration()
    {
        _checkpointEngine?.Dispose(); _checkpointEngine = null;
        if (Directory.Exists(_iteration)) Directory.Delete(_iteration, recursive: true);
    }
    [GlobalCleanup]
    public void Cleanup()
    {
        CleanupIteration();
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
    [Benchmark(Baseline = true)] public long FullWalRecovery() => Recover("full.wal");
    [Benchmark] public long SnapshotRecovery() => Recover("snapshot.wal");
    [Benchmark] public CheckpointResult Checkpoint() => _checkpointEngine!.Checkpoint();
    private long Recover(string name)
    {
        using var engine = new MiniStorageEngine(new MiniStorageOptions(WriteAheadLogPath: Path.Combine(_iteration, name)));
        if (engine.Count != LiveKeys || !engine.TryGet("key:00000", out var first) || first!.Value != "incremental")
            throw new InvalidOperationException("Recovery benchmark inputs must be equivalent.");
        return engine.GetStatistics().WalRecordsReplayed;
    }
}

[MemoryDiagnoser]
public class StorageReadScalingScenarioBenchmarks
{
    private MiniStorageEngine _engine = null!;
    [Params(100, 1_000)] public int LiveKeys { get; set; }
    [Params(false, true)] public bool Hot { get; set; }
    [IterationSetup]
    public void Setup()
    {
        _engine = new MiniStorageEngine(new MiniStorageOptions(CacheCapacity: 16));
        for (var i = 0; i < LiveKeys; i++) _engine.Put($"key:{i:D5}", "value");
        if (Hot) for (var i = 0; i < 8; i++) _engine.TryGet($"key:{i:D5}", out _);
    }
    [Benchmark]
    public int Read()
    {
        var found = 0;
        for (var i = 0; i < 64; i++) if (_engine.TryGet($"key:{(Hot ? i % 8 : i):D5}", out _)) found++;
        return found;
    }
    [IterationCleanup] public void Cleanup() => _engine.Dispose();
}
