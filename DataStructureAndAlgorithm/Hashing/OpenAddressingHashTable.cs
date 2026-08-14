using System.Collections;

namespace DataStructureAndAlgorithm.Hashing;

/// <summary>
/// 使用开放寻址和线性探测解决冲突的教学版哈希表。
/// </summary>
/// <remarks>
/// 所有条目直接保存在一个数组中。发生冲突时依次检查后续槽位；删除时必须留下 Tombstone（墓碑），
/// 否则查找会在空洞处提前停止，导致墓碑后面的键“失踪”。平均操作为 O(1)，严重聚集时最坏为 O(n)。
/// </remarks>
public sealed class OpenAddressingHashTable<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : notnull
{
    private const int DefaultCapacity = 8;
    private const double MaximumUsedLoadFactor = 0.70;

    private enum SlotState : byte
    {
        Empty,
        Occupied,
        Deleted
    }

    private struct Slot
    {
        public TKey Key;
        public TValue Value;
        public SlotState State;
    }

    private Slot[] _slots;
    private readonly IEqualityComparer<TKey> _comparer;
    private int _usedSlots;

    public OpenAddressingHashTable(
        int capacity = DefaultCapacity,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "容量必须为正数。");
        }

        _slots = new Slot[capacity];
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
    }

    public int Count { get; private set; }

    public int Capacity => _slots.Length;

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out var value)
            ? value
            : throw new KeyNotFoundException("哈希表中不存在指定键。");
        set
        {
            ValidateKey(key);
            var index = FindSlot(key, out var found);
            if (found)
            {
                _slots[index].Value = value;
                return;
            }

            EnsureCapacityForOneMoreElement();
            InsertNew(key, value);
        }
    }

    public bool TryAdd(TKey key, TValue value)
    {
        ValidateKey(key);
        FindSlot(key, out var found);
        if (found) return false;

        EnsureCapacityForOneMoreElement();
        InsertNew(key, value);
        return true;
    }

    public bool ContainsKey(TKey key) => TryGetValue(key, out _);

    public bool TryGetValue(TKey key, out TValue value)
    {
        ValidateKey(key);
        var index = FindSlot(key, out var found);
        if (!found)
        {
            value = default!;
            return false;
        }

        value = _slots[index].Value;
        return true;
    }

    public bool Remove(TKey key)
    {
        ValidateKey(key);
        var index = FindSlot(key, out var found);
        if (!found) return false;

        _slots[index].Key = default!;
        _slots[index].Value = default!;
        _slots[index].State = SlotState.Deleted;
        Count--;

        if (Count == 0) Clear();
        return true;
    }

    public void Clear()
    {
        _slots = new Slot[_slots.Length];
        Count = 0;
        _usedSlots = 0;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var slot in _slots)
        {
            if (slot.State == SlotState.Occupied)
            {
                yield return new KeyValuePair<TKey, TValue>(slot.Key, slot.Value);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private int FindSlot(TKey key, out bool found)
    {
        var start = GetStartIndex(key, _slots.Length);
        var firstDeleted = -1;

        for (var step = 0; step < _slots.Length; step++)
        {
            var index = (start + step) % _slots.Length;
            ref var slot = ref _slots[index];
            switch (slot.State)
            {
                case SlotState.Empty:
                    found = false;
                    return firstDeleted >= 0 ? firstDeleted : index;
                case SlotState.Deleted:
                    firstDeleted = firstDeleted < 0 ? index : firstDeleted;
                    break;
                case SlotState.Occupied when _comparer.Equals(slot.Key, key):
                    found = true;
                    return index;
            }
        }

        found = false;
        return firstDeleted;
    }

    private void InsertNew(TKey key, TValue value)
    {
        var index = FindSlot(key, out var found);
        if (found) throw new InvalidOperationException("内部错误：尝试重复插入键。");
        if (index < 0) throw new InvalidOperationException("哈希表没有可用槽位。");

        if (_slots[index].State == SlotState.Empty) _usedSlots++;
        _slots[index] = new Slot { Key = key, Value = value, State = SlotState.Occupied };
        Count++;
    }

    private void EnsureCapacityForOneMoreElement()
    {
        if ((_usedSlots + 1d) / _slots.Length <= MaximumUsedLoadFactor) return;

        // 若真正元素也接近阈值就扩容；若只是墓碑太多，则同容量重散列即可清理墓碑。
        var newCapacity = (Count + 1d) / _slots.Length > MaximumUsedLoadFactor
            ? checked(_slots.Length * 2)
            : _slots.Length;
        Rehash(newCapacity);
    }

    private void Rehash(int capacity)
    {
        var oldSlots = _slots;
        _slots = new Slot[capacity];
        Count = 0;
        _usedSlots = 0;

        foreach (var slot in oldSlots)
        {
            if (slot.State == SlotState.Occupied) InsertNew(slot.Key, slot.Value);
        }
    }

    private int GetStartIndex(TKey key, int capacity) =>
        (int)((uint)_comparer.GetHashCode(key) % (uint)capacity);

    private static void ValidateKey(TKey key) => ArgumentNullException.ThrowIfNull(key);
}
