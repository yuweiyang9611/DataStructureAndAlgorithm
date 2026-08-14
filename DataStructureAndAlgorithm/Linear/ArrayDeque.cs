using System.Collections;

namespace DataStructureAndAlgorithm.Linear;

/// <summary>
/// 使用循环数组实现的双端队列（deque）。
/// </summary>
/// <remarks>
/// <c>_head</c> 指向逻辑上的第一个元素；逻辑下标通过取模映射到物理数组。
/// 因而两端插入和删除的摊还复杂度均为 O(1)，只有扩容时需要 O(n) 搬移。
/// </remarks>
public sealed class ArrayDeque<T> : IEnumerable<T>
{
    private const int DefaultCapacity = 4;
    private T?[] _buffer;
    private int _head;

    public ArrayDeque(int capacity = DefaultCapacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "容量必须为正数。");
        }

        _buffer = new T?[capacity];
    }

    public int Count { get; private set; }

    public int Capacity => _buffer.Length;

    public bool IsEmpty => Count == 0;

    public void AddFirst(T value)
    {
        EnsureCapacityForOneMoreElement();
        _head = (_head - 1 + _buffer.Length) % _buffer.Length;
        _buffer[_head] = value;
        Count++;
    }

    public void AddLast(T value)
    {
        EnsureCapacityForOneMoreElement();
        _buffer[PhysicalIndex(Count)] = value;
        Count++;
    }

    public T PeekFirst()
    {
        EnsureNotEmpty();
        return _buffer[_head]!;
    }

    public T PeekLast()
    {
        EnsureNotEmpty();
        return _buffer[PhysicalIndex(Count - 1)]!;
    }

    public T RemoveFirst()
    {
        EnsureNotEmpty();
        var value = _buffer[_head]!;
        _buffer[_head] = default;
        _head = (_head + 1) % _buffer.Length;
        Count--;
        if (Count == 0) _head = 0;
        return value;
    }

    public T RemoveLast()
    {
        EnsureNotEmpty();
        var index = PhysicalIndex(Count - 1);
        var value = _buffer[index]!;
        _buffer[index] = default;
        Count--;
        if (Count == 0) _head = 0;
        return value;
    }

    public void Clear()
    {
        Array.Clear(_buffer);
        _head = 0;
        Count = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var logicalIndex = 0; logicalIndex < Count; logicalIndex++)
        {
            yield return _buffer[PhysicalIndex(logicalIndex)]!;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private int PhysicalIndex(int logicalIndex) => (_head + logicalIndex) % _buffer.Length;

    private void EnsureCapacityForOneMoreElement()
    {
        if (Count < _buffer.Length) return;

        var newBuffer = new T?[checked(_buffer.Length * 2)];
        for (var logicalIndex = 0; logicalIndex < Count; logicalIndex++)
        {
            // 扩容时按逻辑顺序复制，并把 head 归零，消除原数组中的绕回布局。
            newBuffer[logicalIndex] = _buffer[PhysicalIndex(logicalIndex)];
        }

        _buffer = newBuffer;
        _head = 0;
    }

    private void EnsureNotEmpty()
    {
        if (Count == 0) throw new InvalidOperationException("双端队列为空。");
    }
}
