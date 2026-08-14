namespace DataStructureAndAlgorithm.LeetCode.Models;

/// <summary>与 LeetCode 单向链表题目相同形态的节点模型。</summary>
public sealed class ListNode(int value = 0, ListNode? next = null)
{
    public int Value { get; set; } = value;

    public ListNode? Next { get; set; } = next;

    /// <summary>使用 C# 13+ params Span 语法从一组值构建链表。</summary>
    public static ListNode? FromValues(params ReadOnlySpan<int> values)
    {
        var dummy = new ListNode();
        var tail = dummy;

        foreach (var value in values)
        {
            tail.Next = new ListNode(value);
            tail = tail.Next;
        }

        return dummy.Next;
    }

    public static int[] ToArray(ListNode? head)
    {
        var values = new List<int>();

        while (head is not null)
        {
            values.Add(head.Value);
            head = head.Next;
        }

        return [.. values];
    }
}
