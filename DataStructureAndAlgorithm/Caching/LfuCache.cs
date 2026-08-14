namespace DataStructureAndAlgorithm.Caching;

/// <summary>
/// 固定容量、O(1) 平均读写的最不经常使用（LFU）缓存。
/// </summary>
/// <remarks>
/// <para>
/// 一个字典负责从键直接定位条目；另一个字典把访问频率映射到双向链表。
/// 每次未饱和命中只需把节点从频率 f 的链表移动到 f+1 的链表，因此不需要扫描全部缓存。
/// </para>
/// <para>
/// 当多个条目频率相同时，本实现使用 LRU 作为第二排序规则：同频率链表头部最新，尾部最旧。
/// 这个确定性规则既符合常见 LFU 设计，也使教学测试不依赖字典枚举顺序。
/// </para>
/// <para>
/// 频率到达 <see cref="int.MaxValue"/> 后采用饱和计数，但命中仍会刷新同频桶内的 LRU 顺序。
/// 饱和避免 f+1 溢出，也让运行时间很长的缓存继续保持结构完整和确定的淘汰行为。
/// </para>
/// </remarks>
public sealed class LfuCache<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, Entry> _entries;
    private readonly Dictionary<int, LinkedList<Entry>> _frequencyBuckets = [];
    private int _minimumFrequency;
    private readonly int _maximumFrequency;

    public LfuCache(int capacity, IEqualityComparer<TKey>? comparer = null)
        : this(capacity, int.MaxValue, comparer)
    {
    }

    /// <summary>
    /// 使用可配置频率上限建立缓存；生产代码使用 <see cref="int.MaxValue"/>，较小上限只用于快速验证饱和边界。
    /// </summary>
    internal LfuCache(
        int capacity,
        int maximumFrequency,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "缓存容量必须为正数。");
        }

        if (maximumFrequency < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumFrequency), maximumFrequency, "频率上限必须为正数。");
        }

        Capacity = capacity;
        _maximumFrequency = maximumFrequency;
        _entries = new Dictionary<TKey, Entry>(capacity, comparer);
    }

    public int Capacity { get; }

    public int Count => _entries.Count;

    public bool TryGetValue(TKey key, out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!_entries.TryGetValue(key, out var entry))
        {
            value = default!;
            return false;
        }

        Promote(entry);
        value = entry.Value;
        return true;
    }

    /// <summary>新增或更新条目；超出容量时淘汰最低频率中最久未使用的一项。</summary>
    public void Set(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_entries.TryGetValue(key, out var existing))
        {
            // 先完成可能分配新桶/节点的结构操作，再提交值；若晋升失败，旧值和原有链表关系都保持不变。
            // 到达频率上限时 Promote 只在现有链表中移动节点，不再执行可能溢出的 f+1。
            Promote(existing);
            existing.Value = value;
            return;
        }

        if (_entries.Count == Capacity)
        {
            EvictOne();
        }

        var entry = new Entry(key, value);
        var bucket = GetOrCreateBucket(frequency: 1);
        entry.Node = bucket.AddFirst(entry);
        _entries.Add(key, entry);
        _minimumFrequency = 1;
    }

    public bool Remove(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!_entries.Remove(key, out var entry))
        {
            return false;
        }

        RemoveFromBucket(entry);
        if (_entries.Count == 0)
        {
            _minimumFrequency = 0;
        }
        else if (!_frequencyBuckets.ContainsKey(_minimumFrequency))
        {
            _minimumFrequency = _frequencyBuckets.Keys.Min();
        }

        return true;
    }

    /// <summary>返回用于教学与测试的稳定快照，按频率升序、同频率从新到旧排列。</summary>
    public IReadOnlyList<(TKey Key, int Frequency)> GetFrequencySnapshot()
    {
        var result = new List<(TKey Key, int Frequency)>(_entries.Count);
        foreach (var pair in _frequencyBuckets.OrderBy(pair => pair.Key))
        {
            result.AddRange(pair.Value.Select(entry => (entry.Key, pair.Key)));
        }

        return result;
    }

    private void Promote(Entry entry)
    {
        var oldFrequency = entry.Frequency;
        var oldBucket = _frequencyBuckets[oldFrequency];
        if (oldFrequency == _maximumFrequency)
        {
            // 计数已经饱和，但“这次刚被访问”仍是有意义的信息：把节点移到桶头，保留同频 LRU 语义。
            // 复用原 LinkedListNode 不需要分配内存，也就不会在边界路径中留下半次移动。
            var node = entry.Node!;
            if (node.Previous is not null)
            {
                oldBucket.Remove(node);
                oldBucket.AddFirst(node);
            }

            return;
        }

        // 先计算并准备目标节点、目标桶，再修改旧结构。由于 oldFrequency 严格小于上限，+1 必然不会溢出。
        // 若准备阶段因资源不足抛出异常，entry 仍位于原桶，Set(existing) 也尚未提交新值。
        var newFrequency = oldFrequency + 1;
        var newNode = new LinkedListNode<Entry>(entry);
        var newBucket = GetOrCreateBucket(newFrequency);

        RemoveFromBucket(entry);
        entry.Frequency = newFrequency;
        entry.Node = newNode;
        newBucket.AddFirst(newNode);

        if (_minimumFrequency == oldFrequency && !_frequencyBuckets.ContainsKey(oldFrequency))
        {
            _minimumFrequency = newFrequency;
        }
    }

    private void EvictOne()
    {
        var bucket = _frequencyBuckets[_minimumFrequency];
        var victim = bucket.Last!;
        bucket.RemoveLast();
        _entries.Remove(victim.Value.Key);
        if (bucket.Count == 0)
        {
            _frequencyBuckets.Remove(_minimumFrequency);
        }
    }

    private void RemoveFromBucket(Entry entry)
    {
        var bucket = _frequencyBuckets[entry.Frequency];
        bucket.Remove(entry.Node!);
        entry.Node = null;
        if (bucket.Count == 0)
        {
            _frequencyBuckets.Remove(entry.Frequency);
        }
    }

    private LinkedList<Entry> GetOrCreateBucket(int frequency)
    {
        if (!_frequencyBuckets.TryGetValue(frequency, out var bucket))
        {
            bucket = [];
            _frequencyBuckets.Add(frequency, bucket);
        }

        return bucket;
    }

    private sealed class Entry(TKey key, TValue value)
    {
        public TKey Key { get; } = key;

        public TValue Value { get; set; } = value;

        public int Frequency { get; set; } = 1;

        public LinkedListNode<Entry>? Node { get; set; }
    }
}
