namespace DataStructureAndAlgorithm.Scenarios.MiniStorage;

/// <summary>迷你存储引擎的可配置边界。</summary>
/// <param name="BPlusTreeOrder">B+ 树内部节点最多拥有的子节点数。</param>
/// <param name="BloomBitCount">Bloom Filter 位图长度。</param>
/// <param name="BloomHashFunctionCount">每个键设置的 Bloom 位数。</param>
/// <param name="CacheCapacity">LFU 热点缓存容量。</param>
/// <param name="WriteAheadLogPath">可选 WAL 文件；为 <see langword="null"/> 时运行纯内存模式。</param>
public sealed record MiniStorageOptions(
    int BPlusTreeOrder = 4,
    int BloomBitCount = 8_192,
    int BloomHashFunctionCount = 5,
    int CacheCapacity = 128,
    string? WriteAheadLogPath = null);

/// <summary>范围查询或诊断接口返回的只读键值版本。</summary>
public sealed record StorageEntry(string Key, string Value, long Version);

/// <summary>
/// 存储引擎的可观察统计快照。
/// </summary>
/// <remarks>
/// 统计值只解释性能路径，不参与业务结果；因此打开或关闭 Trace 都不会改变这些计数的语义。
/// </remarks>
public sealed record StorageStatistics(
    int LiveKeyCount,
    int IndexedKeyCount,
    long Version,
    long PutCount,
    long DeleteCount,
    long GetCount,
    long CacheHits,
    long BloomNegativeSkips,
    long IndexLookups,
    long WalRecordsReplayed)
{
    public long SnapshotVersion { get; init; }
    public int SnapshotEntriesLoaded { get; init; }
}

/// <summary>一次手动检查点回收的空间与提交版本。</summary>
public sealed record CheckpointResult(long Version, int EntryCount, int ReclaimedTombstones, long WalBytesBefore, long WalBytesAfter);
