using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Scenarios.MiniStorage;

namespace DataStructureAndAlgorithm.Test.IntegratedProjectsTest;

public sealed class MiniStorageCheckpointTests
{
    [Fact]
    public void CheckpointCompactsAndPreservesVersionsAcrossIncrementalRecovery()
    {
        using var area = new StorageArea();
        var trace = new CollectingAlgorithmTraceSink();
        using (var engine = new MiniStorageEngine(area.Options, trace))
        {
            engine.Put("z", "first"); engine.Put("a", "second"); engine.Put("gone", "third"); engine.Delete("gone");
            var before = engine.GetStatistics();
            var result = engine.Checkpoint();
            Assert.Equal(4, result.Version);
            Assert.Equal(2, result.EntryCount);
            Assert.Equal(1, result.ReclaimedTombstones);
            Assert.True(result.WalBytesBefore > 0); Assert.Equal(0, result.WalBytesAfter);
            Assert.Equal(before.PutCount, engine.GetStatistics().PutCount);
            Assert.Equal(2, engine.GetStatistics().IndexedKeyCount);
            Assert.True(engine.TryGet("z", out var z)); Assert.Equal(1, z!.Version);
            engine.Put("b", "incremental");
            Assert.Contains(trace.Events, item => item.Operation == "CheckpointCompleted");
        }
        using (var recovered = new MiniStorageEngine(area.Options))
        {
            Assert.Equal(4, recovered.GetStatistics().SnapshotVersion);
            Assert.Equal(2, recovered.GetStatistics().SnapshotEntriesLoaded);
            Assert.Equal(1, recovered.GetStatistics().WalRecordsReplayed);
            Assert.Equal(5, recovered.GetStatistics().Version);
            Assert.Equal(new[] { "a", "b", "z" }, recovered.RangeScan("a", "zz").Select(x => x.Key));
            Assert.False(recovered.TryGet("gone", out _));
            Assert.Equal(0, recovered.Checkpoint().ReclaimedTombstones);
            Assert.Equal(0, recovered.Checkpoint().WalBytesBefore);
        }
        using var memory = new MiniStorageEngine();
        Assert.Throws<InvalidOperationException>(() => memory.Checkpoint());
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(6, true)]
    public void IoFailureRequiresReopenAndPreservesCommittedState(int stageValue, bool withSnapshot)
    {
        using var area = new StorageArea();
        Seed(area.Options, withSnapshot);
        var oldSnapshot = withSnapshot ? File.ReadAllBytes(area.Path + ".snapshot") : null;
        var oldWal = File.ReadAllBytes(area.Path);
        var stage = (PersistenceStage)stageValue;
        using (var engine = new MiniStorageEngine(area.Options, null, new ThrowAt(stage)))
        {
            Assert.Throws<IOException>(() => Execute(engine, stage));
            Assert.Throws<InvalidOperationException>(() => engine.Put("unexpected", "x"));
            Assert.Throws<InvalidOperationException>(() => engine.TryGet("a", out _));
        }
        AssertCheckpointFiles(area, (PersistenceStage)stageValue, oldSnapshot, oldWal);
        using var recovered = new MiniStorageEngine(area.Options);
        Assert.Equal(stageValue >= (int)PersistenceStage.SnapshotPublished ? 4 : withSnapshot ? 2 : 0, recovered.GetStatistics().SnapshotVersion);
        Assert.True(recovered.TryGet("a", out var value)); Assert.Equal("last", value!.Value);
        Assert.False(recovered.TryGet("gone", out _));
        Assert.Equal(stage == PersistenceStage.WalFlushed, recovered.TryGet("incremental", out _));
        recovered.Put("after", "restart");
        Assert.True(recovered.HasValidIndexInvariants());
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(6, true)]
    public async Task ProcessTerminationAtEveryPersistenceBoundaryRecovers(int stageValue, bool withSnapshot)
    {
        using var area = new StorageArea();
        Seed(area.Options, withSnapshot);
        var oldSnapshot = withSnapshot ? File.ReadAllBytes(area.Path + ".snapshot") : null;
        var oldWal = File.ReadAllBytes(area.Path);
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "DataStructureAndAlgorithm.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var dll = Path.Combine(root.FullName, "DataStructureAndAlgorithm.StorageCrashHost", "bin", configuration, "net10.0", "DataStructureAndAlgorithm.StorageCrashHost.dll");
        var info = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        info.ArgumentList.Add(dll); info.ArgumentList.Add(area.Path); info.ArgumentList.Add(((PersistenceStage)stageValue).ToString());
        using var process = Process.Start(info)!;
        try
        {
            var line = await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal("READY", line);
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
        await area.WaitForFileHandlesReleasedAsync();
        if (stageValue == (int)PersistenceStage.WalPartialWrite)
        {
            var bytes = File.ReadAllBytes(area.Path);
            Assert.NotEmpty(bytes);
            Assert.NotEqual((byte)'\n', bytes[^1]);
        }
        AssertCheckpointFiles(area, (PersistenceStage)stageValue, oldSnapshot, oldWal);
        using var recovered = new MiniStorageEngine(area.Options);
        Assert.Equal(stageValue >= (int)PersistenceStage.SnapshotPublished ? 4 : withSnapshot ? 2 : 0, recovered.GetStatistics().SnapshotVersion);
        Assert.True(recovered.TryGet("a", out var value)); Assert.Equal("last", value!.Value);
        Assert.False(recovered.TryGet("gone", out _));
        Assert.Equal(stageValue == (int)PersistenceStage.WalFlushed, recovered.TryGet("incremental", out _));
        recovered.Put("after", "restart");
    }

    [Fact]
    public void LegacyMixedWalAndExclusiveOwnership()
    {
        using var area = new StorageArea();
        File.WriteAllText(area.Path, JsonSerializer.Serialize(new WalRecord(1, WalOperation.Put, "old", "legacy")) + "\n");
        using (var engine = new MiniStorageEngine(area.Options))
        {
            Assert.Throws<IOException>(() => new MiniStorageEngine(area.Options));
            engine.Put("new", "v2");
        }
        using (var recovered = new MiniStorageEngine(area.Options))
        {
            Assert.Equal(2, recovered.Count);
            recovered.Checkpoint();
            Assert.Equal(0, new FileInfo(area.Path).Length);
        }
        using var again = new MiniStorageEngine(area.Options);
        Assert.Equal(2, again.Count);
    }

    [Theory]
    [InlineData("checksum")]
    [InlineData("version")]
    [InlineData("base64")]
    [InlineData("gap")]
    [InlineData("null")]
    [InlineData("missing-field")]
    [InlineData("delete-value")]
    public void CommittedWalCorruptionIsRejected(string kind)
    {
        using var area = new StorageArea();
        string text = kind switch
        {
            "checksum" => "{\"FormatVersion\":2,\"Payload\":\"e30=\",\"Sha256\":\"invalid\"}",
            "version" => "{\"FormatVersion\":99,\"Payload\":\"e30=\",\"Sha256\":\"invalid\"}",
            "base64" => "{\"FormatVersion\":2,\"Payload\":\"!\",\"Sha256\":\"invalid\"}",
            "gap" => PersistenceCodec.Encode(new WalRecord(2, WalOperation.Put, "a", "b")),
            "missing-field" => PersistenceCodec.Encode(new { Sequence = 1, Key = "a", Value = "b" }),
            "delete-value" => PersistenceCodec.Encode(new WalRecord(1, WalOperation.Delete, "a", "invalid")),
            _ => "null"
        };
        File.WriteAllText(area.Path, text + "\n");
        Assert.Throws<InvalidDataException>(() => new MiniStorageEngine(area.Options));
    }

    [Fact]
    public void SnapshotCorruptionAndInvalidEntriesNeverFallBackToEmpty()
    {
        using var area = new StorageArea();
        Seed(area.Options);
        using (var engine = new MiniStorageEngine(area.Options)) engine.Checkpoint();
        File.WriteAllText(area.Path + ".snapshot", "{}");
        Assert.Throws<InvalidDataException>(() => new MiniStorageEngine(area.Options));
        foreach (var snapshot in new[]
        {
            new StorageSnapshot(-1, []),
            new StorageSnapshot(2, [new("b", "x", 1), new("a", "y", 2)]),
            new StorageSnapshot(2, [new("a", "x", 3)]),
            new StorageSnapshot(2, [new("a", "x", 1), new("a", "y", 2)])
        })
        {
            File.WriteAllText(area.Path + ".snapshot", PersistenceCodec.Encode(snapshot));
            Assert.Throws<InvalidDataException>(() => new MiniStorageEngine(area.Options));
        }
    }

    [Fact]
    public void RandomOperationsWithCheckpointsMatchIndependentVersionedModel()
    {
        using var area = new StorageArea();
        var model = new SortedDictionary<string, StorageEntry>(StringComparer.Ordinal);
        var random = new Random(1729);
        long version = 0;
        var engine = new MiniStorageEngine(area.Options);
        try
        {
            for (var step = 0; step < 300; step++)
            {
                var key = $"key:{random.Next(12):D2}";
                switch (random.Next(5))
                {
                    case 0:
                        var value = $"value:{step}";
                        engine.Put(key, value); model[key] = new StorageEntry(key, value, ++version); break;
                    case 1:
                        var removed = model.Remove(key);
                        Assert.Equal(removed, engine.Delete(key)); if (removed) version++; break;
                    case 2: engine.Checkpoint(); break;
                    case 3: engine.Dispose(); engine = new MiniStorageEngine(area.Options); break;
                    default:
                        Assert.Equal(model.TryGetValue(key, out var expected), engine.TryGet(key, out var actual));
                        Assert.Equal(expected, actual); break;
                }
                Assert.Equal(version, engine.GetStatistics().Version);
                Assert.Equal(model.Values, engine.RangeScan("key:", "key;"));
                Assert.True(engine.HasValidIndexInvariants());
            }
        }
        finally { engine.Dispose(); }
    }

    private static void AssertCheckpointFiles(StorageArea area, PersistenceStage stage, byte[]? oldSnapshot, byte[] oldWal)
    {
        if (stage < PersistenceStage.SnapshotPublished)
        {
            if (oldSnapshot is null) Assert.False(File.Exists(area.Path + ".snapshot"));
            else Assert.Equal(oldSnapshot, File.ReadAllBytes(area.Path + ".snapshot"));
        }
        if (stage is PersistenceStage.SnapshotPartialWrite or PersistenceStage.SnapshotFlushed or PersistenceStage.SnapshotPublished)
            Assert.Equal(oldWal, File.ReadAllBytes(area.Path));
        if (stage >= PersistenceStage.WalTruncated)
            Assert.Empty(File.ReadAllBytes(area.Path));
    }

    private static void Seed(MiniStorageOptions options, bool checkpoint = true)
    {
        using var engine = new MiniStorageEngine(options);
        engine.Put("a", "first"); engine.Put("gone", "delete");
        if (checkpoint) engine.Checkpoint();
        engine.Put("a", "last"); engine.Delete("gone");
    }
    private static void Execute(MiniStorageEngine engine, PersistenceStage stage)
    {
        if (stage is PersistenceStage.WalPartialWrite or PersistenceStage.WalFlushed) engine.Put("incremental", "value");
        else engine.Checkpoint();
    }
    private sealed class ThrowAt(PersistenceStage target) : IPersistenceFaults
    {
        public void At(PersistenceStage stage) { if (stage == target) throw new IOException("Injected persistence failure"); }
    }
    private sealed class StorageArea : IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dsa-checkpoint-{Guid.NewGuid():N}");
        public StorageArea() { Directory.CreateDirectory(_directory); }
        public string Path => System.IO.Path.Combine(_directory, "data.wal");
        public MiniStorageOptions Options => new(WriteAheadLogPath: Path, CacheCapacity: 4);
        public async Task WaitForFileHandlesReleasedAsync()
        {
            var timer = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    foreach (var file in Directory.GetFiles(_directory))
                    {
                        using var probe = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
                    }
                    return;
                }
                catch (IOException exception) when (OperatingSystem.IsWindows() &&
                    (exception.HResult & 0xffff) is 32 or 33 && timer.Elapsed < TimeSpan.FromSeconds(2))
                {
                    // Process exit notifications can race Windows handle teardown and runner scans.
                    // Wait for exclusive file access before asserting bytes or reopening the database.
                    await Task.Delay(25);
                }
            }
        }

        public void Dispose()
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    Directory.Delete(_directory, recursive: true);
                    return;
                }
                catch (IOException exception) when (OperatingSystem.IsWindows() &&
                    (exception.HResult & 0xffff) is 32 or 33 && attempt < 20)
                {
                    // Terminated processes and runner file scanners can briefly retain a sharing lock.
                    // Retry only cleanup sharing violations; persistent locks still fail the test.
                    System.Threading.Thread.Sleep(50);
                }
            }
        }
    }
}
