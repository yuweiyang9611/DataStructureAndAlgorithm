using System.Collections;

namespace DataStructureAndAlgorithm.Linear;

/// <summary>
/// 同时保存前驱和后继引用的教学版双向链表。
/// </summary>
/// <remarks>
/// 与单向链表相比，每个节点多保存一个 <c>Previous</c> 引用，换来 O(1) 的尾部删除能力。
/// 实现的核心不变量是：头节点的 Previous 永远为空、尾节点的 Next 永远为空；
/// 空表时头尾同时为空，非空表时沿 Next 和 Previous 得到的顺序互为逆序。
/// </remarks>
public sealed class DoublyLinkedList<T> : IEnumerable<T>
{
    private sealed class Node(T value)
    {
        public T Value { get; } = value;
        public Node? Previous { get; set; }
        public Node? Next { get; set; }
    }

    private Node? _head;
    private Node? _tail;

    public int Count { get; private set; }

    public bool IsEmpty => Count == 0;

    /// <summary>在头部插入，时间复杂度 O(1)。</summary>
    public void AddFirst(T value)
    {
        var node = new Node(value) { Next = _head };
        if (_head is null)
        {
            // 第一个节点既是头也是尾。
            _tail = node;
        }
        else
        {
            _head.Previous = node;
        }

        _head = node;
        Count++;
    }

    /// <summary>在尾部插入，时间复杂度 O(1)。</summary>
    public void AddLast(T value)
    {
        var node = new Node(value) { Previous = _tail };
        if (_tail is null)
        {
            _head = node;
        }
        else
        {
            _tail.Next = node;
        }

        _tail = node;
        Count++;
    }

    /// <summary>删除并返回头元素，时间复杂度 O(1)。</summary>
    public T RemoveFirst()
    {
        var node = _head ?? throw new InvalidOperationException("不能从空双向链表删除元素。");
        _head = node.Next;
        if (_head is null)
        {
            _tail = null;
        }
        else
        {
            _head.Previous = null;
        }

        // 主动断开引用有助于调试时确认节点已经离开链表。
        node.Next = null;
        Count--;
        return node.Value;
    }

    /// <summary>删除并返回尾元素，时间复杂度 O(1)。</summary>
    public T RemoveLast()
    {
        var node = _tail ?? throw new InvalidOperationException("不能从空双向链表删除元素。");
        _tail = node.Previous;
        if (_tail is null)
        {
            _head = null;
        }
        else
        {
            _tail.Next = null;
        }

        node.Previous = null;
        Count--;
        return node.Value;
    }

    /// <summary>删除从头部开始遇到的第一个相等元素，查找时间 O(n)。</summary>
    public bool Remove(T value)
    {
        var comparer = EqualityComparer<T>.Default;
        var current = _head;
        while (current is not null && !comparer.Equals(current.Value, value))
        {
            current = current.Next;
        }

        if (current is null) return false;
        if (ReferenceEquals(current, _head))
        {
            RemoveFirst();
        }
        else if (ReferenceEquals(current, _tail))
        {
            RemoveLast();
        }
        else
        {
            // 中间节点一定同时具有前驱与后继，把两边直接跨过当前节点连接起来。
            current.Previous!.Next = current.Next;
            current.Next!.Previous = current.Previous;
            current.Previous = null;
            current.Next = null;
            Count--;
        }

        return true;
    }

    public bool Contains(T value) => this.Contains(value, EqualityComparer<T>.Default);

    private bool Contains(T value, IEqualityComparer<T> comparer)
    {
        for (var current = _head; current is not null; current = current.Next)
        {
            if (comparer.Equals(current.Value, value)) return true;
        }

        return false;
    }

    public void Clear()
    {
        // 只清空头尾即可让 GC 回收整条链；不必逐节点删除，因而 Clear 为 O(1)。
        _head = null;
        _tail = null;
        Count = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var current = _head; current is not null; current = current.Next)
        {
            yield return current.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
