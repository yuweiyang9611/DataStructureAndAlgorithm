namespace DataStructureAndAlgorithm.Linear;

/// <summary>
/// 使用循环数组实现的先进先出（FIFO）队列。
/// </summary>
/// <remarks>
/// 头尾索引到达数组末端后会绕回起点，已经出队的空间因此可以被再次利用。
/// Enqueue 和 Dequeue 的摊还复杂度均为 O(1)。
/// </remarks>
public sealed class CircularQueue<T>
{
    private const int DefaultCapacity = 4;
    private T[] _items;
    private int _head;
    private int _tail;

    public CircularQueue(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        _items = capacity == 0 ? [] : new T[capacity];
    }

    public int Count { get; private set; }

    public int Capacity => _items.Length;

    /// <summary>在队尾加入元素。</summary>
    public void Enqueue(T value)
    {
        EnsureCapacity(Count + 1);

        _items[_tail] = value;
        _tail = (_tail + 1) % _items.Length;
        Count++;
    }

    /// <summary>移除并返回队头元素。</summary>
    public T Dequeue()
    {
        EnsureNotEmpty();

        var value = _items[_head];
        _items[_head] = default!;
        _head = (_head + 1) % _items.Length;
        Count--;
        return value;
    }

    /// <summary>返回但不移除队头元素。</summary>
    public T Peek()
    {
        EnsureNotEmpty();
        return _items[_head];
    }

    public void Clear()
    {
        Array.Clear(_items, 0, _items.Length);
        _head = 0;
        _tail = 0;
        Count = 0;
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        if (_items.Length >= requiredCapacity)
        {
            return;
        }

        var newCapacity = _items.Length == 0 ? DefaultCapacity : _items.Length * 2;
        var newItems = new T[newCapacity];

        // 逻辑队列可能被数组末尾切成两段。逐个按逻辑顺序复制最容易证明正确，
        // 扩容结束后把头部归零，也简化了后续索引计算。
        for (var index = 0; index < Count; index++)
        {
            newItems[index] = _items[(_head + index) % _items.Length];
        }

        _items = newItems;
        _head = 0;
        _tail = Count;
    }

    private void EnsureNotEmpty()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Queue is empty.");
        }
    }
}
