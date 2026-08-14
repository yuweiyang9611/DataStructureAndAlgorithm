namespace DataStructureAndAlgorithm.Heap;

/// <summary>
/// 基于完全二叉树的最小堆：父节点总是不大于它的子节点。
/// </summary>
/// <remarks>
/// 完全二叉树可以紧凑地放入数组。下标为 i 的节点，其父节点为 (i - 1) / 2，
/// 左右子节点分别为 2i + 1 和 2i + 2，因此不需要额外的节点对象和指针。
/// </remarks>
public sealed class BinaryMinHeap<T>
{
    private readonly List<T> _items = [];
    private readonly IComparer<T> _comparer;

    public BinaryMinHeap(IComparer<T>? comparer = null)
    {
        _comparer = comparer ?? Comparer<T>.Default;
    }

    /// <summary>从已有元素在线性时间内建堆。</summary>
    public BinaryMinHeap(IEnumerable<T> values, IComparer<T>? comparer = null)
        : this(comparer)
    {
        ArgumentNullException.ThrowIfNull(values);
        _items.AddRange(values);

        // 叶节点天然满足堆性质，从最后一个非叶节点开始向下调整即可。
        for (var index = _items.Count / 2 - 1; index >= 0; index--)
        {
            SiftDown(index);
        }
    }

    public int Count => _items.Count;

    /// <summary>插入元素，并通过向上调整恢复堆性质，复杂度 O(log n)。</summary>
    public void Enqueue(T value)
    {
        _items.Add(value);
        SiftUp(_items.Count - 1);
    }

    /// <summary>返回但不删除最小元素，复杂度 O(1)。</summary>
    public T Peek()
    {
        EnsureNotEmpty();
        return _items[0];
    }

    /// <summary>删除并返回最小元素，复杂度 O(log n)。</summary>
    public T Dequeue()
    {
        EnsureNotEmpty();

        var minimum = _items[0];
        var lastIndex = _items.Count - 1;
        _items[0] = _items[lastIndex];
        _items.RemoveAt(lastIndex);

        if (_items.Count > 0)
        {
            SiftDown(0);
        }

        return minimum;
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            var parentIndex = (index - 1) / 2;
            if (_comparer.Compare(_items[parentIndex], _items[index]) <= 0)
            {
                return;
            }

            (_items[parentIndex], _items[index]) = (_items[index], _items[parentIndex]);
            index = parentIndex;
        }
    }

    private void SiftDown(int index)
    {
        while (true)
        {
            var leftChildIndex = index * 2 + 1;
            if (leftChildIndex >= _items.Count)
            {
                return;
            }

            var smallerChildIndex = leftChildIndex;
            var rightChildIndex = leftChildIndex + 1;

            if (rightChildIndex < _items.Count &&
                _comparer.Compare(_items[rightChildIndex], _items[leftChildIndex]) < 0)
            {
                smallerChildIndex = rightChildIndex;
            }

            if (_comparer.Compare(_items[index], _items[smallerChildIndex]) <= 0)
            {
                return;
            }

            (_items[index], _items[smallerChildIndex]) = (_items[smallerChildIndex], _items[index]);
            index = smallerChildIndex;
        }
    }

    private void EnsureNotEmpty()
    {
        if (_items.Count == 0)
        {
            throw new InvalidOperationException("Heap is empty.");
        }
    }
}
