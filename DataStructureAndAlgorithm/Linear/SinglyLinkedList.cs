using System.Collections;

namespace DataStructureAndAlgorithm.Linear;

/// <summary>
/// 一个教学用途的单向链表。
/// </summary>
/// <remarks>
/// 与 <see cref="List{T}"/> 不同，链表节点不要求位于连续内存中。
/// 因此在已知节点位置时插入和删除可以达到 O(1)，但按下标查找需要 O(n)。
/// 生产代码通常应优先使用 BCL 集合；这里手写实现是为了观察引用如何把节点串起来。
/// </remarks>
public sealed class SinglyLinkedList<T> : IEnumerable<T>
{
    // Node 是实现细节。将它设为私有可防止调用方绕过链表规则直接修改 Next。
    private sealed class Node(T value)
    {
        public T Value { get; } = value;

        public Node? Next { get; set; }
    }

    private Node? _head;
    private Node? _tail;

    /// <summary>获取链表中的元素数量。</summary>
    public int Count { get; private set; }

    /// <summary>获取链表是否为空。</summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// 在链表头部插入元素，时间复杂度为 O(1)。
    /// </summary>
    public void AddFirst(T value)
    {
        var newNode = new Node(value)
        {
            Next = _head
        };

        _head = newNode;

        // 空链表插入第一个节点后，头尾必须指向同一个节点。
        _tail ??= newNode;
        Count++;
    }

    /// <summary>
    /// 在链表尾部插入元素。由于维护了尾指针，所以时间复杂度为 O(1)。
    /// </summary>
    public void AddLast(T value)
    {
        var newNode = new Node(value);

        if (_tail is null)
        {
            _head = newNode;
            _tail = newNode;
        }
        else
        {
            _tail.Next = newNode;
            _tail = newNode;
        }

        Count++;
    }

    /// <summary>
    /// 删除并返回头部元素，时间复杂度为 O(1)。
    /// </summary>
    /// <exception cref="InvalidOperationException">链表为空时抛出。</exception>
    public T RemoveFirst()
    {
        if (_head is null)
        {
            throw new InvalidOperationException("Cannot remove an element from an empty linked list.");
        }

        var value = _head.Value;
        _head = _head.Next;
        Count--;

        // 删除最后一个节点时还要清空尾指针，否则它会继续引用已移除节点。
        if (_head is null)
        {
            _tail = null;
        }

        return value;
    }

    /// <summary>
    /// 删除第一个与 <paramref name="value"/> 相等的元素。
    /// </summary>
    /// <returns>找到并删除时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    public bool Remove(T value)
    {
        var comparer = EqualityComparer<T>.Default;
        Node? previous = null;
        var current = _head;

        while (current is not null)
        {
            if (comparer.Equals(current.Value, value))
            {
                if (previous is null)
                {
                    // current 是头节点，复用 RemoveFirst 可集中维护头尾和 Count 不变量。
                    RemoveFirst();
                }
                else
                {
                    previous.Next = current.Next;

                    if (ReferenceEquals(current, _tail))
                    {
                        _tail = previous;
                    }

                    Count--;
                }

                return true;
            }

            previous = current;
            current = current.Next;
        }

        return false;
    }

    /// <summary>删除所有元素，使链表恢复为空状态。</summary>
    public void Clear()
    {
        _head = null;
        _tail = null;
        Count = 0;
    }

    /// <summary>
    /// 按从头到尾的顺序惰性枚举元素。
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        var current = _head;

        while (current is not null)
        {
            yield return current.Value;
            current = current.Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
