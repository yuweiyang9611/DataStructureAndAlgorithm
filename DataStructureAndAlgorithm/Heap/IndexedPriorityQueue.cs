namespace DataStructureAndAlgorithm.Heap;

/// <summary>
/// 支持按键定位并降低优先级（decrease-key）的最小索引堆。
/// </summary>
/// <remarks>
/// 普通 <see cref="PriorityQueue{TElement,TPriority}"/> 无法直接修改队列中的旧条目，Dijkstra 常用“重复入队、出队时丢弃旧值”规避。
/// 索引堆额外维护 key 到堆下标的字典，使降低优先级仍为 O(log n)，同时每个 key 在堆中最多出现一次。
/// 空间复杂度 O(n)。它适合 Prim、A* 和需要频繁更新任务优先级的调度器。
/// </remarks>
public sealed class IndexedPriorityQueue<TKey, TPriority> where TKey : notnull
{
    private sealed class Entry(TKey key, TPriority priority)
    {
        public TKey Key { get; } = key;
        public TPriority Priority { get; set; } = priority;
    }

    private readonly List<Entry> _heap = [];
    private readonly Dictionary<TKey, int> _indexes;
    private readonly IComparer<TPriority> _priorityComparer;

    public IndexedPriorityQueue(
        IEqualityComparer<TKey>? keyComparer = null,
        IComparer<TPriority>? priorityComparer = null)
    {
        _indexes = new Dictionary<TKey, int>(keyComparer);
        _priorityComparer = priorityComparer ?? Comparer<TPriority>.Default;
    }

    public int Count => _heap.Count;

    public bool ContainsKey(TKey key) => _indexes.ContainsKey(key);

    public bool TryGetPriority(TKey key, out TPriority priority)
    {
        if (_indexes.TryGetValue(key, out var index))
        {
            priority = _heap[index].Priority;
            return true;
        }

        priority = default!;
        return false;
    }

    /// <summary>
    /// 新 key 会入队；已存在 key 只在新优先级更小时更新。返回值表示队列是否发生改变。
    /// </summary>
    public bool EnqueueOrDecrease(TKey key, TPriority priority)
    {
        if (_indexes.TryGetValue(key, out var index))
        {
            if (_priorityComparer.Compare(priority, _heap[index].Priority) >= 0) return false;
            _heap[index].Priority = priority;
            SiftUp(index);
            return true;
        }

        _heap.Add(new Entry(key, priority));
        var addedIndex = _heap.Count - 1;
        _indexes.Add(key, addedIndex);
        SiftUp(addedIndex);
        return true;
    }

    public bool TryDequeue(out TKey key, out TPriority priority)
    {
        if (_heap.Count == 0)
        {
            key = default!;
            priority = default!;
            return false;
        }

        var minimum = _heap[0];
        key = minimum.Key;
        priority = minimum.Priority;
        _indexes.Remove(minimum.Key);

        var lastIndex = _heap.Count - 1;
        if (lastIndex == 0)
        {
            _heap.RemoveAt(lastIndex);
            return true;
        }

        _heap[0] = _heap[lastIndex];
        _heap.RemoveAt(lastIndex);
        _indexes[_heap[0].Key] = 0;
        SiftDown(0);
        return true;
    }

    public void Clear()
    {
        _heap.Clear();
        _indexes.Clear();
    }

    /// <summary>验证堆序与索引字典的一致性，供学习和性质测试使用。</summary>
    public bool HasValidInvariants()
    {
        if (_heap.Count != _indexes.Count) return false;
        for (var index = 0; index < _heap.Count; index++)
        {
            if (!_indexes.TryGetValue(_heap[index].Key, out var mapped) || mapped != index) return false;
            var left = index * 2 + 1;
            var right = left + 1;
            if (left < _heap.Count && Compare(left, index) < 0 ||
                right < _heap.Count && Compare(right, index) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            var parent = (index - 1) / 2;
            if (Compare(index, parent) >= 0) break;
            Swap(index, parent);
            index = parent;
        }
    }

    private void SiftDown(int index)
    {
        while (true)
        {
            var left = index * 2 + 1;
            if (left >= _heap.Count) return;
            var right = left + 1;
            var smaller = right < _heap.Count && Compare(right, left) < 0 ? right : left;
            if (Compare(smaller, index) >= 0) return;
            Swap(index, smaller);
            index = smaller;
        }
    }

    private int Compare(int first, int second) =>
        _priorityComparer.Compare(_heap[first].Priority, _heap[second].Priority);

    private void Swap(int first, int second)
    {
        (_heap[first], _heap[second]) = (_heap[second], _heap[first]);
        _indexes[_heap[first].Key] = first;
        _indexes[_heap[second].Key] = second;
    }
}
