namespace DataStructureAndAlgorithm.Caching;

/// <summary>
/// 固定容量的最近最少使用（LRU）缓存。
/// </summary>
/// <remarks>
/// 字典提供 O(1) 定位，双向链表提供 O(1) 移动和淘汰。
/// 链表头表示最近使用，尾部表示最久未使用。
/// </remarks>
public sealed class LruCache<TKey, TValue> where TKey : notnull
{
    private sealed class Entry(TKey key, TValue value)
    {
        public TKey Key { get; } = key;

        public TValue Value { get; set; } = value;
    }

    private readonly Dictionary<TKey, LinkedListNode<Entry>> _nodes;
    private readonly LinkedList<Entry> _usageOrder = [];

    public LruCache(int capacity, IEqualityComparer<TKey>? comparer = null)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
        }

        Capacity = capacity;
        _nodes = new Dictionary<TKey, LinkedListNode<Entry>>(capacity, comparer);
    }

    public int Capacity { get; }

    public int Count => _nodes.Count;

    /// <summary>读取成功时把条目提升为最近使用。</summary>
    public bool TryGetValue(TKey key, out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!_nodes.TryGetValue(key, out var node))
        {
            value = default!;
            return false;
        }

        MoveToMostRecent(node);
        value = node.Value.Value;
        return true;
    }

    /// <summary>新增或更新条目；超出容量时淘汰最久未使用项。</summary>
    public void Set(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_nodes.TryGetValue(key, out var existingNode))
        {
            existingNode.Value.Value = value;
            MoveToMostRecent(existingNode);
            return;
        }

        var node = _usageOrder.AddFirst(new Entry(key, value));
        _nodes.Add(key, node);

        if (_nodes.Count <= Capacity)
        {
            return;
        }

        var leastRecent = _usageOrder.Last!;
        _usageOrder.RemoveLast();
        _nodes.Remove(leastRecent.Value.Key);
    }

    public bool Remove(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!_nodes.Remove(key, out var node))
        {
            return false;
        }

        _usageOrder.Remove(node);
        return true;
    }

    /// <summary>返回从最近使用到最久未使用的键快照。</summary>
    public IReadOnlyList<TKey> GetKeysMostRecentFirst()
    {
        return [.. _usageOrder.Select(entry => entry.Key)];
    }

    private void MoveToMostRecent(LinkedListNode<Entry> node)
    {
        if (ReferenceEquals(node, _usageOrder.First))
        {
            return;
        }

        _usageOrder.Remove(node);
        _usageOrder.AddFirst(node);
    }
}
