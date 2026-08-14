using DataStructureAndAlgorithm.LeetCode.Models;

namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>LeetCode 中经典的单向链表题目。</summary>
public static class ClassicLinkedListProblems
{
    /// <summary>
    /// LeetCode 21 - Merge Two Sorted Lists：合并两个升序链表。
    /// </summary>
    /// <remarks>复用并重新连接原节点，时间 O(n + m)，额外空间 O(1)。</remarks>
    public static ListNode? MergeTwoSortedLists(ListNode? first, ListNode? second)
    {
        var dummy = new ListNode();
        var tail = dummy;

        while (first is not null && second is not null)
        {
            if (first.Value <= second.Value)
            {
                tail.Next = first;
                first = first.Next;
            }
            else
            {
                tail.Next = second;
                second = second.Next;
            }

            tail = tail.Next;
        }

        tail.Next = first ?? second;
        return dummy.Next;
    }

    /// <summary>
    /// LeetCode 141 - Linked List Cycle：使用快慢指针检测环。
    /// </summary>
    /// <remarks>时间 O(n)，空间 O(1)。有环时快指针最终会追上慢指针。</remarks>
    public static bool HasCycle(ListNode? head)
    {
        var slow = head;
        var fast = head;

        while (fast?.Next is not null)
        {
            slow = slow!.Next;
            fast = fast.Next.Next;

            if (ReferenceEquals(slow, fast))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// LeetCode 206 - Reverse Linked List：原地反转单向链表。
    /// </summary>
    /// <remarks>时间 O(n)，空间 O(1)。每轮把 current.Next 从后继改为前驱。</remarks>
    public static ListNode? ReverseList(ListNode? head)
    {
        ListNode? previous = null;
        var current = head;

        while (current is not null)
        {
            // 必须先保存后继，否则改写 Next 后会丢失未处理部分。
            var next = current.Next;
            current.Next = previous;
            previous = current;
            current = next;
        }

        return previous;
    }
}
