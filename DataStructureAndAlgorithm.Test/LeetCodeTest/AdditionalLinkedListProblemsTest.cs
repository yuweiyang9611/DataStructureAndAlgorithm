using DataStructureAndAlgorithm.LeetCode;
using DataStructureAndAlgorithm.LeetCode.Models;

namespace DataStructureAndAlgorithm.Test.LeetCodeTest;

public class AdditionalLinkedListProblemsTest
{
    [Fact]
    public void P2_AddTwoNumbers() => AssertList([7, 0, 8],
        AdditionalLinkedListProblems.AddTwoNumbers(ListNode.FromValues(2, 4, 3), ListNode.FromValues(5, 6, 4)));

    [Fact]
    public void P19_RemoveNthFromEnd() => AssertList([1, 2, 3, 5],
        AdditionalLinkedListProblems.RemoveNthFromEnd(ListNode.FromValues(1, 2, 3, 4, 5), 2));

    [Fact]
    public void P23_MergeKSortedLists() => AssertList([1, 1, 2, 3, 4, 4, 5, 6],
        AdditionalLinkedListProblems.MergeKSortedLists([
            ListNode.FromValues(1,4,5), ListNode.FromValues(1,3,4), ListNode.FromValues(2,6)]));

    [Fact]
    public void P24_SwapPairs() => AssertList([2, 1, 4, 3],
        AdditionalLinkedListProblems.SwapPairs(ListNode.FromValues(1, 2, 3, 4)));

    [Fact]
    public void P25_ReverseNodesInKGroup() => AssertList([2, 1, 4, 3, 5],
        AdditionalLinkedListProblems.ReverseNodesInKGroup(ListNode.FromValues(1, 2, 3, 4, 5), 2));

    [Fact]
    public void P138_CopyRandomList()
    {
        var first = new RandomListNode(7);
        var second = new RandomListNode(13);
        first.Next = second;
        second.Random = first;
        var copy = AdditionalLinkedListProblems.CopyRandomList(first)!;
        Assert.NotSame(first, copy);
        Assert.NotSame(second, copy.Next);
        Assert.Same(copy, copy.Next!.Random);
    }

    [Fact]
    public void P142_DetectCycleEntry()
    {
        var head = ListNode.FromValues(3, 2, 0, -4)!;
        var entry = head.Next;
        head.Next!.Next!.Next = entry;
        Assert.Same(entry, AdditionalLinkedListProblems.DetectCycleEntry(head));
    }

    [Fact]
    public void P143_ReorderList()
    {
        var head = ListNode.FromValues(1, 2, 3, 4);
        AdditionalLinkedListProblems.ReorderList(head);
        AssertList([1, 4, 2, 3], head);
    }

    [Fact]
    public void P148_SortList() => AssertList([-1, 0, 1, 2, 3, 4, 5],
        AdditionalLinkedListProblems.SortList(ListNode.FromValues(4, 2, 1, 3, 0, -1, 5)));

    [Fact]
    public void P160_GetIntersectionNode()
    {
        var shared = ListNode.FromValues(8, 4, 5);
        var first = new ListNode(4, new ListNode(1, shared));
        var second = new ListNode(5, new ListNode(6, new ListNode(1, shared)));
        Assert.Same(shared, AdditionalLinkedListProblems.GetIntersectionNode(first, second));
    }

    [Fact]
    public void P234_IsPalindromeListAndRestoreInput()
    {
        var head = ListNode.FromValues(1, 2, 2, 1);
        Assert.True(AdditionalLinkedListProblems.IsPalindromeList(head));
        AssertList([1, 2, 2, 1], head);
    }

    [Fact]
    public void P876_MiddleNode() => Assert.Equal(4,
        AdditionalLinkedListProblems.MiddleNode(ListNode.FromValues(1, 2, 3, 4, 5, 6))!.Value);

    [Fact]
    public void P155_MinStack()
    {
        var stack = new MinStack();
        stack.Push(-2); stack.Push(0); stack.Push(-3);
        Assert.Equal(-3, stack.GetMinimum());
        Assert.Equal(-3, stack.Pop());
        Assert.Equal(0, stack.Top());
        Assert.Equal(-2, stack.GetMinimum());
    }

    [Fact]
    public void P703_KthLargestStream()
    {
        var stream = new KthLargestStream(3, [4, 5, 8, 2]);
        Assert.Equal(4, stream.Add(3));
        Assert.Equal(5, stream.Add(5));
        Assert.Equal(5, stream.Add(10));
        Assert.Equal(8, stream.Add(9));
        Assert.Equal(8, stream.Add(4));
    }

    private static void AssertList(int[] expected, ListNode? actual) =>
        Assert.True(ListNode.ToArray(actual).SequenceEqual(expected));
}
