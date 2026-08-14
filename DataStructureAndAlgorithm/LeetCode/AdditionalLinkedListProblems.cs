using DataStructureAndAlgorithm.LeetCode.Models;

namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>LeetCode 100 题专题中的进阶链表题。</summary>
public static class AdditionalLinkedListProblems
{
    /// <summary>2 - Add Two Numbers。逐位相加并传播进位，时间 O(max(n,m))。</summary>
    public static ListNode? AddTwoNumbers(ListNode? first, ListNode? second)
    {
        var dummy = new ListNode();
        var tail = dummy;
        var carry = 0;

        while (first is not null || second is not null || carry != 0)
        {
            var sum = (first?.Value ?? 0) + (second?.Value ?? 0) + carry;
            carry = sum / 10;
            tail.Next = new ListNode(sum % 10);
            tail = tail.Next;
            first = first?.Next;
            second = second?.Next;
        }

        return dummy.Next;
    }

    /// <summary>19 - Remove Nth Node From End。快指针先走 n 步，再同步移动，O(n)。</summary>
    public static ListNode? RemoveNthFromEnd(ListNode? head, int n)
    {
        if (n < 1) throw new ArgumentOutOfRangeException(nameof(n));
        var dummy = new ListNode(0, head);
        var fast = dummy;

        for (var step = 0; step < n; step++)
        {
            fast = fast.Next ?? throw new ArgumentOutOfRangeException(nameof(n), "n exceeds list length.");
        }

        var slow = dummy;
        while (fast.Next is not null)
        {
            fast = fast.Next;
            slow = slow.Next!;
        }

        slow.Next = slow.Next?.Next;
        return dummy.Next;
    }

    /// <summary>23 - Merge k Sorted Lists。最小堆只保存每条链表当前头，O(N log k)。</summary>
    public static ListNode? MergeKSortedLists(IEnumerable<ListNode?> lists)
    {
        ArgumentNullException.ThrowIfNull(lists);
        var queue = new PriorityQueue<ListNode, int>();
        foreach (var node in lists)
        {
            if (node is not null) queue.Enqueue(node, node.Value);
        }

        var dummy = new ListNode();
        var tail = dummy;
        while (queue.TryDequeue(out var node, out _))
        {
            var next = node.Next;
            tail.Next = node;
            tail = node;
            if (next is not null) queue.Enqueue(next, next.Value);
        }

        tail.Next = null;
        return dummy.Next;
    }

    /// <summary>24 - Swap Nodes in Pairs。dummy 统一处理头节点交换，O(n)。</summary>
    public static ListNode? SwapPairs(ListNode? head)
    {
        var dummy = new ListNode(0, head);
        var beforePair = dummy;

        while (beforePair.Next?.Next is not null)
        {
            var first = beforePair.Next;
            var second = first.Next!;
            first.Next = second.Next;
            second.Next = first;
            beforePair.Next = second;
            beforePair = first;
        }

        return dummy.Next;
    }

    /// <summary>25 - Reverse Nodes in k-Group。先确认完整 k 组，再原地反转，O(n)。</summary>
    public static ListNode? ReverseNodesInKGroup(ListNode? head, int k)
    {
        if (k < 1) throw new ArgumentOutOfRangeException(nameof(k));
        var dummy = new ListNode(0, head);
        var groupBefore = dummy;

        while (true)
        {
            var groupEnd = groupBefore;
            for (var step = 0; step < k && groupEnd is not null; step++) groupEnd = groupEnd.Next;
            if (groupEnd is null) break;

            var groupAfter = groupEnd.Next;
            var previous = groupAfter;
            var current = groupBefore.Next;

            // previous 从 groupAfter 开始，使原组头反转后自动指向下一组。
            while (!ReferenceEquals(current, groupAfter))
            {
                var next = current!.Next;
                current.Next = previous;
                previous = current;
                current = next;
            }

            var oldGroupStart = groupBefore.Next!;
            groupBefore.Next = groupEnd;
            groupBefore = oldGroupStart;
        }

        return dummy.Next;
    }

    /// <summary>138 - Copy List with Random Pointer。映射原节点到副本，再连接两种指针，O(n)。</summary>
    public static RandomListNode? CopyRandomList(RandomListNode? head)
    {
        if (head is null) return null;
        var copies = new Dictionary<RandomListNode, RandomListNode>();

        for (var node = head; node is not null; node = node.Next) copies[node] = new RandomListNode(node.Value);
        for (var node = head; node is not null; node = node.Next)
        {
            var copy = copies[node];
            copy.Next = node.Next is null ? null : copies[node.Next];
            copy.Random = node.Random is null ? null : copies[node.Random];
        }

        return copies[head];
    }

    /// <summary>142 - Linked List Cycle II。相遇后一个指针回到头部，再次相遇即环入口。</summary>
    public static ListNode? DetectCycleEntry(ListNode? head)
    {
        var slow = head;
        var fast = head;

        do
        {
            if (fast?.Next is null) return null;
            slow = slow!.Next;
            fast = fast.Next.Next;
        } while (!ReferenceEquals(slow, fast));

        slow = head;
        while (!ReferenceEquals(slow, fast))
        {
            slow = slow!.Next;
            fast = fast!.Next;
        }

        return slow;
    }

    /// <summary>143 - Reorder List。找中点、反转后半段、交替合并，O(n)。</summary>
    public static void ReorderList(ListNode? head)
    {
        if (head?.Next is null) return;
        var slow = head;
        var fast = head;
        while (fast.Next?.Next is not null)
        {
            slow = slow.Next!;
            fast = fast.Next.Next;
        }

        var second = Reverse(slow.Next);
        slow.Next = null;
        var first = head;

        while (second is not null)
        {
            var firstNext = first!.Next;
            var secondNext = second.Next;
            first.Next = second;
            second.Next = firstNext;
            first = firstNext;
            second = secondNext;
        }
    }

    /// <summary>148 - Sort List。链表归并排序，时间 O(n log n)，递归栈 O(log n)。</summary>
    public static ListNode? SortList(ListNode? head)
    {
        if (head?.Next is null) return head;

        var slow = head;
        var fast = head.Next;
        while (fast?.Next is not null)
        {
            slow = slow.Next!;
            fast = fast.Next.Next;
        }

        var second = slow.Next;
        slow.Next = null;
        return Merge(SortList(head), SortList(second));
    }

    /// <summary>160 - Intersection of Two Linked Lists。切换链表头使两指针走过相同总长度。</summary>
    public static ListNode? GetIntersectionNode(ListNode? first, ListNode? second)
    {
        var firstCursor = first;
        var secondCursor = second;

        while (!ReferenceEquals(firstCursor, secondCursor))
        {
            firstCursor = firstCursor is null ? second : firstCursor.Next;
            secondCursor = secondCursor is null ? first : secondCursor.Next;
        }

        return firstCursor;
    }

    /// <summary>234 - Palindrome Linked List。反转后半段比较，并恢复原链表，O(n)。</summary>
    public static bool IsPalindromeList(ListNode? head)
    {
        if (head?.Next is null) return true;
        var slow = head;
        var fast = head;
        while (fast.Next?.Next is not null)
        {
            slow = slow.Next!;
            fast = fast.Next.Next;
        }

        var reversedSecond = Reverse(slow.Next);
        slow.Next = reversedSecond;
        var left = head;
        var right = reversedSecond;
        var isPalindrome = true;

        while (right is not null)
        {
            if (left!.Value != right.Value) isPalindrome = false;
            left = left.Next;
            right = right.Next;
        }

        slow.Next = Reverse(reversedSecond); // API 不应因查询而改变链表。
        return isPalindrome;
    }

    /// <summary>876 - Middle of the Linked List。快指针走两步，慢指针走一步。</summary>
    public static ListNode? MiddleNode(ListNode? head)
    {
        var slow = head;
        var fast = head;
        while (fast?.Next is not null)
        {
            slow = slow!.Next;
            fast = fast.Next.Next;
        }

        return slow;
    }

    private static ListNode? Reverse(ListNode? head)
    {
        ListNode? previous = null;
        while (head is not null)
        {
            var next = head.Next;
            head.Next = previous;
            previous = head;
            head = next;
        }

        return previous;
    }

    private static ListNode? Merge(ListNode? first, ListNode? second)
    {
        var dummy = new ListNode();
        var tail = dummy;
        while (first is not null && second is not null)
        {
            ref var selected = ref (first.Value <= second.Value ? ref first : ref second);
            tail.Next = selected;
            selected = selected.Next;
            tail = tail.Next;
        }

        tail.Next = first ?? second;
        return dummy.Next;
    }
}
