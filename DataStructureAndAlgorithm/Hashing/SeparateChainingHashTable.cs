using System.Collections;

namespace DataStructureAndAlgorithm.Hashing;

/// <summary>
/// 使用链地址法解决哈希冲突的教学版哈希表。
/// </summary>
/// <remarks>
/// 哈希函数只能决定“可能在哪个桶”，不同键可能落入同一个桶，这就是哈希冲突。
/// 链地址法让每个桶保存一组条目，再用相等比较器在桶内确认真正的键。
/// 平均查找、插入和删除为 O(1)，最坏情况下所有键落入同一桶，会退化为 O(n)。
/// 生产代码应优先使用 <see cref="Dictionary{TKey,TValue}"/>。
/// </remarks>
public sealed class SeparateChainingHashTable<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : notnull
{
    private const int DefaultCapacity = 8;
    private const double MaximumLoadFactor = 0.75;

    private sealed class Entry(TKey key, TValue value)
    {
        public TKey Key { get; } = key;

        public TValue Value { get; set; } = value;
    }

    private List<Entry>?[] _buckets;
    private readonly IEqualityComparer<TKey> _comparer;

    public SeparateChainingHashTable(
        int capacity = DefaultCapacity,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
        }

        _buckets = new List<Entry>?[capacity];
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
    }

    public int Count { get; private set; }

    /// <summary>桶数量。它与实际元素数量不同。</summary>
    public int Capacity => _buckets.Length;

    public TValue this[TKey key]
    {
        get
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException("The specified key does not exist in the hash table.");
        }
        set
        {
            ValidateKey(key);
            var bucket = GetOrCreateBucket(key);
            var existing = FindEntry(bucket, key);

            if (existing is not null)
            {
                existing.Value = value;
                return;
            }

            EnsureCapacityForOneMoreElement();
            GetOrCreateBucket(key).Add(new Entry(key, value));
            Count++;
        }
    }

    /// <summary>仅当键不存在时添加条目。</summary>
    public bool TryAdd(TKey key, TValue value)
    {
        ValidateKey(key);

        if (TryGetValue(key, out _))
        {
            return false;
        }

        EnsureCapacityForOneMoreElement();
        GetOrCreateBucket(key).Add(new Entry(key, value));
        Count++;
        return true;
    }

    public bool ContainsKey(TKey key) => TryGetValue(key, out _);

    public bool TryGetValue(TKey key, out TValue value)
    {
        ValidateKey(key);
        var bucket = _buckets[GetBucketIndex(key, _buckets.Length)];
        var entry = bucket is null ? null : FindEntry(bucket, key);

        if (entry is null)
        {
            value = default!;
            return false;
        }

        value = entry.Value;
        return true;
    }

    public bool Remove(TKey key)
    {
        ValidateKey(key);
        var bucket = _buckets[GetBucketIndex(key, _buckets.Length)];
        if (bucket is null)
        {
            return false;
        }

        for (var index = 0; index < bucket.Count; index++)
        {
            if (!_comparer.Equals(bucket[index].Key, key))
            {
                continue;
            }

            bucket.RemoveAt(index);
            Count--;
            return true;
        }

        return false;
    }

    public void Clear()
    {
        _buckets = new List<Entry>?[_buckets.Length];
        Count = 0;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var bucket in _buckets)
        {
            if (bucket is null)
            {
                continue;
            }

            foreach (var entry in bucket)
            {
                yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void EnsureCapacityForOneMoreElement()
    {
        if ((Count + 1d) / _buckets.Length <= MaximumLoadFactor)
        {
            return;
        }

        var newBuckets = new List<Entry>?[checked(_buckets.Length * 2)];

        // 扩容后取模基数改变，旧桶下标不再有效，所有条目都必须重新散列。
        foreach (var bucket in _buckets)
        {
            if (bucket is null)
            {
                continue;
            }

            foreach (var entry in bucket)
            {
                var newIndex = GetBucketIndex(entry.Key, newBuckets.Length);
                (newBuckets[newIndex] ??= []).Add(entry);
            }
        }

        _buckets = newBuckets;
    }

    private List<Entry> GetOrCreateBucket(TKey key)
    {
        var bucketIndex = GetBucketIndex(key, _buckets.Length);
        return _buckets[bucketIndex] ??= [];
    }

    private Entry? FindEntry(IEnumerable<Entry> bucket, TKey key)
    {
        return bucket.FirstOrDefault(entry => _comparer.Equals(entry.Key, key));
    }

    private int GetBucketIndex(TKey key, int bucketCount)
    {
        // 与 int.MaxValue 可清除符号位，避免负哈希码产生负数组下标。
        return (_comparer.GetHashCode(key) & int.MaxValue) % bucketCount;
    }

    private static void ValidateKey(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
    }
}
