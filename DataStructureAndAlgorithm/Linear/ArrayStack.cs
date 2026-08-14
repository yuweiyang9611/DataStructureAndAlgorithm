namespace DataStructureAndAlgorithm.Linear;

/// <summary>
/// 使用可扩容数组实现的后进先出（LIFO）栈。
/// </summary>
/// <remarks>
/// Push 的单次操作在扩容时是 O(n)，但连续插入的摊还复杂度是 O(1)。
/// “摊还”表示把少数昂贵的扩容成本分摊到之前多次廉价插入上。
/// </remarks>
public sealed class ArrayStack<T>
{
    private const int DefaultCapacity = 4;
    private T[] _items;

    public ArrayStack(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        _items = capacity == 0 ? [] : new T[capacity];
    }

    /// <summary>获取栈内元素数量。</summary>
    public int Count { get; private set; }

    /// <summary>获取当前底层数组容量。该属性用于观察扩容过程。</summary>
    public int Capacity => _items.Length;

    /// <summary>把元素压入栈顶。</summary>
    public void Push(T value)
    {
        EnsureCapacity(Count + 1);
        _items[Count] = value;
        Count++;
    }

    /// <summary>移除并返回栈顶元素。</summary>
    /// <exception cref="InvalidOperationException">栈为空时抛出。</exception>
    public T Pop()
    {
        EnsureNotEmpty();

        Count--;
        var value = _items[Count];

        // 清除槽位可及时释放引用类型对象；对值类型该操作也保持行为一致。
        _items[Count] = default!;
        return value;
    }

    /// <summary>返回但不移除栈顶元素。</summary>
    public T Peek()
    {
        EnsureNotEmpty();
        return _items[Count - 1];
    }

    /// <summary>清空栈，但保留已经申请的容量以便复用。</summary>
    public void Clear()
    {
        Array.Clear(_items, 0, Count);
        Count = 0;
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        if (_items.Length >= requiredCapacity)
        {
            return;
        }

        // 容量翻倍让 n 次 Push 的总复制次数保持 O(n)，从而得到 O(1) 摊还成本。
        var newCapacity = _items.Length == 0 ? DefaultCapacity : _items.Length * 2;
        Array.Resize(ref _items, newCapacity);
    }

    private void EnsureNotEmpty()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Stack is empty.");
        }
    }
}
