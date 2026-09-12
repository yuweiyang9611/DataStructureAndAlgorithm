using System.Globalization;
using DataStructureAndAlgorithm.Caching;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Probabilistic;
using DataStructureAndAlgorithm.Tree;

namespace DataStructureAndAlgorithm.Scenarios.MiniStorage;

/// <summary>
/// 把 B+ 树、Bloom Filter、LFU 缓存、墓碑与 WAL 组合起来的迷你键值存储引擎。
/// </summary>
/// <remarks>
/// <para>
/// 该类型刻意保持同步、小规模和单进程：目标是讲清每个数据结构在存储读写路径中的责任，
/// 而不是模拟数据库的并发控制、页缓存或复制协议。
/// </para>
/// <para>
/// 写路径为“WAL -> B+ 树 -> Bloom/缓存”，读路径为“LFU -> Bloom -> B+ 树”。
/// 删除写入墓碑，因此 Bloom Filter 中残留的旧位只会增加假阳性，不会让已存在键产生假阴性。
/// </para>
/// </remarks>
public sealed class MiniStorageEngine : IDisposable
{
    private const string TraceAlgorithm = "MiniStorage";
    private static readonly StringComparer KeyComparer = StringComparer.Ordinal;

    private BPlusTree<string, StoredValue> _index;
    private StringBloomFilter _bloomFilter;
    private LfuCache<string, CachedLookup> _cache;
    private readonly IAlgorithmTraceSink? _trace;
    private readonly WriteAheadLog? _writeAheadLog;
    private long _version;
    private int _liveKeyCount;
    private long _putCount;
    private long _deleteCount;
    private long _getCount;
    private long _cacheHits;
    private long _bloomNegativeSkips;
    private long _indexLookups;
    private long _walRecordsReplayed;
    private bool _disposed;
    private bool _faulted;
    private readonly MiniStorageOptions _options;
    private readonly IPersistenceFaults? _faults;
    private readonly FileStream? _ownership;
    private readonly string? _snapshotPath;
    private long _snapshotVersion;
    private int _snapshotEntriesLoaded;

    public MiniStorageEngine(MiniStorageOptions? options = null, IAlgorithmTraceSink? trace = null)
        : this(options, trace, null) { }

    internal MiniStorageEngine(MiniStorageOptions? options, IAlgorithmTraceSink? trace, IPersistenceFaults? faults)
    {
        options ??= new MiniStorageOptions();
        _options = options;
        _faults = faults;
        ValidateOptions(options);

        _index = new BPlusTree<string, StoredValue>(options.BPlusTreeOrder, KeyComparer);
        _bloomFilter = new StringBloomFilter(options.BloomBitCount, options.BloomHashFunctionCount);
        _cache = new LfuCache<string, CachedLookup>(options.CacheCapacity, KeyComparer);
        _trace = BestEffortAlgorithmTraceSink.Wrap(trace);

        if (options.WriteAheadLogPath is not null)
        {
            var path = Path.GetFullPath(options.WriteAheadLogPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            _ownership = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            _snapshotPath = path + ".snapshot";
            try
            {
                Recover(path);
                WriteAheadLog.DiscardUncommittedTail(path);
                _writeAheadLog = new WriteAheadLog(path, faults);
            }
            catch
            {
                _ownership.Dispose();
                throw;
            }
        }
    }

    public int Count
    {
        get
        {
            ThrowIfDisposed();
            return _liveKeyCount;
        }
    }

    /// <summary>新增或覆盖一个键；WAL 成功落盘后才修改内存状态。</summary>
    public void Put(string key, string value)
    {
        ThrowIfDisposed();
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);

        var nextVersion = checked(_version + 1);
        Append(new WalRecord(nextVersion, WalOperation.Put, key, value));
        ApplyPut(key, value, nextVersion, isRecovery: false);
        _putCount++;

        if (_trace is not null)
        {
            _trace.Record(
                TraceAlgorithm,
                "Put",
                "WAL 已先于内存索引提交，随后更新 B+ 树、Bloom Filter 与 LFU 缓存。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["key"] = key,
                    ["liveKeys"] = Format(_liveKeyCount),
                    ["version"] = Format(_version)
                });
        }
    }

    /// <summary>
    /// 写入墓碑删除已有键。
    /// </summary>
    /// <returns>只有键在删除前确实存在时才返回 <see langword="true"/>。</returns>
    public bool Delete(string key)
    {
        ThrowIfDisposed();
        ValidateKey(key);
        if (!_index.TryGetValue(key, out var existing) || existing.IsDeleted)
        {
            return false;
        }

        var nextVersion = checked(_version + 1);
        Append(new WalRecord(nextVersion, WalOperation.Delete, key, Value: null));
        ApplyDelete(key, nextVersion, isRecovery: false);
        _deleteCount++;

        if (_trace is not null)
        {
            _trace.Record(
                TraceAlgorithm,
                "Delete",
                "删除被写成 B+ 树墓碑；Bloom 位无需清除，未来压缩阶段再回收物理记录。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["key"] = key,
                    ["liveKeys"] = Format(_liveKeyCount),
                    ["version"] = Format(_version)
                });
        }

        return true;
    }

    /// <summary>按照 LFU、Bloom Filter、B+ 树的顺序读取键。</summary>
    public bool TryGet(string key, out StorageEntry? entry)
    {
        ThrowIfDisposed();
        ValidateKey(key);
        _getCount++;

        if (_cache.TryGetValue(key, out var cached))
        {
            _cacheHits++;
            entry = cached.Found ? new StorageEntry(key, cached.Value!, cached.Version) : null;
            RecordReadTrace("CacheHit", key, entry is not null);
            return cached.Found;
        }

        if (!_bloomFilter.MightContain(key))
        {
            _bloomNegativeSkips++;
            entry = null;
            RecordReadTrace("BloomNegative", key, found: false);
            return false;
        }

        _indexLookups++;
        if (_index.TryGetValue(key, out var stored) && !stored.IsDeleted)
        {
            _cache.Set(key, CachedLookup.Hit(stored.Value!, stored.Version));
            entry = new StorageEntry(key, stored.Value!, stored.Version);
            RecordReadTrace("IndexHit", key, found: true);
            return true;
        }

        // Bloom 假阳性或墓碑都缓存为未命中，避免热点不存在键反复访问树。
        var version = stored?.Version ?? _version;
        _cache.Set(key, CachedLookup.Miss(version));
        entry = null;
        RecordReadTrace("IndexMiss", key, found: false);
        return false;
    }

    /// <summary>沿 B+ 树叶子链执行半开区间扫描，并过滤墓碑。</summary>
    public IReadOnlyList<StorageEntry> RangeScan(
        string fromInclusive,
        string toExclusive,
        int maximumCount = int.MaxValue)
    {
        ThrowIfDisposed();
        ValidateKey(fromInclusive);
        ValidateKey(toExclusive);
        if (maximumCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount), maximumCount, "最大结果数不能为负数。");
        }

        // 预分配也按实际小结果封顶，不能因为默认 maximumCount 是 int.MaxValue 就按整个索引规模申请数组。
        var initialCapacity = Math.Min(256, Math.Min(maximumCount, _liveKeyCount));
        var result = new List<StorageEntry>(initialCapacity);
        // 不能在 B+ 树层按“物理记录数”截断：前面的记录可能是墓碑，会让活跃结果被错误漏掉。
        // 惰性叶链让这里在收集到 maximumCount 个活跃值后真正早停，不必先物化整个物理区间。
        if (maximumCount > 0)
        {
            foreach (var pair in _index.EnumerateRange(fromInclusive, toExclusive))
            {
                if (!pair.Value.IsDeleted)
                {
                    result.Add(new StorageEntry(pair.Key, pair.Value.Value!, pair.Value.Version));
                    if (result.Count == maximumCount)
                    {
                        break;
                    }
                }
            }
        }

        if (_trace is not null)
        {
            _trace.Record(
                TraceAlgorithm,
                "RangeScan",
                "先定位首个叶子，再沿叶子链扫描半开区间并跳过墓碑。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["fromInclusive"] = fromInclusive,
                    ["results"] = Format(result.Count),
                    ["toExclusive"] = toExclusive
                });
        }

        return result;
    }

    public StorageStatistics GetStatistics()
    {
        ThrowIfDisposed();
        return new StorageStatistics(
            _liveKeyCount,
            _index.Count,
            _version,
            _putCount,
            _deleteCount,
            _getCount,
            _cacheHits,
            _bloomNegativeSkips,
            _indexLookups,
            _walRecordsReplayed)
        {
            SnapshotVersion = _snapshotVersion,
            SnapshotEntriesLoaded = _snapshotEntriesLoaded
        };
    }

    /// <summary>暴露结构校验而不暴露内部节点，供状态机测试验证每一步。</summary>
    public bool HasValidIndexInvariants()
    {
        ThrowIfDisposed();
        return _index.HasValidInvariants();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try { _writeAheadLog?.Dispose(); }
        finally { _ownership?.Dispose(); _disposed = true; }
    }

    private void Recover(string path)
    {
        if (_trace is not null)
        {
            _trace.Record(TraceAlgorithm, "RecoveryStarted", "开始按 WAL 顺序重建内存索引。");
        }

        var snapshot = StorageSnapshot.Read(path + ".snapshot");
        if (snapshot is not null)
        {
            foreach (var entry in snapshot.Entries) ApplyPut(entry.Key, entry.Value, entry.Version, isRecovery: true);
            _version = _snapshotVersion = snapshot.Version;
            _snapshotEntriesLoaded = snapshot.Entries.Length;
        }

        long? previousSequence = null;
        foreach (var record in WriteAheadLog.ReadAll(path))
        {
            if (previousSequence is { } previous && record.Sequence != checked(previous + 1))
                throw new InvalidDataException("WAL 序列号必须连续递增。");
            previousSequence = record.Sequence;
            if (record.Sequence <= _snapshotVersion) continue;
            if (record.Sequence != checked(_version + 1)) throw new InvalidDataException("WAL 缺少快照之后的记录。");
            if (record.Operation == WalOperation.Put) ApplyPut(record.Key, record.Value!, record.Sequence, isRecovery: true);
            else ApplyDelete(record.Key, record.Sequence, isRecovery: true);
            _walRecordsReplayed++;
        }

        if (_trace is not null)
        {
            _trace.Record(
                TraceAlgorithm,
                "RecoveryCompleted",
                "WAL 回放完成，B+ 树、Bloom Filter 和逻辑版本已恢复。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["liveKeys"] = Format(_liveKeyCount),
                    ["records"] = Format(_walRecordsReplayed),
                    ["version"] = Format(_version)
                });
        }
    }

    private void ApplyPut(string key, string value, long version, bool isRecovery)
    {
        var wasLive = _index.TryGetValue(key, out var oldValue) && !oldValue.IsDeleted;
        _index.Upsert(key, new StoredValue(value, version, IsDeleted: false));
        _bloomFilter.Add(key);
        if (!isRecovery)
        {
            _cache.Set(key, CachedLookup.Hit(value, version));
        }

        if (!wasLive)
        {
            _liveKeyCount++;
        }

        _version = version;
    }

    private void ApplyDelete(string key, long version, bool isRecovery)
    {
        var wasLive = _index.TryGetValue(key, out var oldValue) && !oldValue.IsDeleted;
        _index.Upsert(key, new StoredValue(Value: null, version, IsDeleted: true));
        _bloomFilter.Add(key);
        if (!isRecovery)
        {
            _cache.Set(key, CachedLookup.Miss(version));
        }

        if (wasLive)
        {
            _liveKeyCount--;
        }

        _version = version;
    }

    private void RecordReadTrace(string operation, string key, bool found)
    {
        if (_trace is null)
        {
            return;
        }

        _trace.Record(
            TraceAlgorithm,
            operation,
            "读路径只记录决定结果的层级，不暴露哈希位或 B+ 树页等噪声细节。",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["found"] = found.ToString(CultureInfo.InvariantCulture).ToLowerInvariant(),
                ["key"] = key
            });
    }

    private static void ValidateOptions(MiniStorageOptions options)
    {
        if (options.BPlusTreeOrder < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "B+ 树的阶必须至少为 3。");
        }

        if (options.BloomBitCount < 8 || options.BloomHashFunctionCount is < 1 or > 32 ||
            options.CacheCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Bloom Filter 与缓存参数超出有效范围。");
        }

        if (options.WriteAheadLogPath is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(options.WriteAheadLogPath);
        }
    }

    private static void ValidateKey(string key) => ArgumentException.ThrowIfNullOrWhiteSpace(key);

    private static string Format(long value) => value.ToString(CultureInfo.InvariantCulture);

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_faulted) throw new InvalidOperationException("持久化失败后必须关闭并重新打开引擎。");
    }

    private void Append(WalRecord record)
    {
        try { _writeAheadLog?.Append(record); }
        catch { _faulted = true; throw; }
    }

    /// <summary>先提交校验快照，再截断日志并回收内存墓碑。仅用于单写者进程恢复教学。</summary>
    public CheckpointResult Checkpoint()
    {
        ThrowIfDisposed();
        if (_writeAheadLog is null || _snapshotPath is null) throw new InvalidOperationException("检查点需要 WAL 持久化模式。");
        var entries = _index.Where(pair => !pair.Value.IsDeleted)
            .Select(pair => new StorageEntry(pair.Key, pair.Value.Value!, pair.Value.Version)).ToArray();
        var compacted = new BPlusTree<string, StoredValue>(_options.BPlusTreeOrder, KeyComparer);
        var bloom = new StringBloomFilter(_options.BloomBitCount, _options.BloomHashFunctionCount);
        var cache = new LfuCache<string, CachedLookup>(_options.CacheCapacity, KeyComparer);
        foreach (var entry in entries)
        {
            compacted.Upsert(entry.Key, new StoredValue(entry.Value, entry.Version, false));
            bloom.Add(entry.Key);
        }
        var before = _writeAheadLog.Length;
        var tombstones = _index.Count - entries.Length;
        try
        {
            new StorageSnapshot(_version, entries).Publish(_snapshotPath, _faults);
            _writeAheadLog.Truncate();
            _index = compacted; _bloomFilter = bloom; _cache = cache;
            _snapshotVersion = _version;
            _faults?.At(PersistenceStage.CheckpointCompleted);
        }
        catch { _faulted = true; throw; }
        _trace?.Record(TraceAlgorithm, "CheckpointCompleted", "快照已提交，旧 WAL 已截断，墓碑已回收。",
            new Dictionary<string, string>(StringComparer.Ordinal) { ["version"] = Format(_version), ["liveKeys"] = Format(entries.Length) });
        return new CheckpointResult(_version, entries.Length, tombstones, before, _writeAheadLog.Length);
    }

    private sealed record StoredValue(string? Value, long Version, bool IsDeleted);

    private sealed record CachedLookup(bool Found, string? Value, long Version)
    {
        public static CachedLookup Hit(string value, long version) => new(true, value, version);

        public static CachedLookup Miss(long version) => new(false, Value: null, version);
    }
}
