using DataStructureAndAlgorithm.LeetCode;
using DataStructureAndAlgorithm.LeetCode.Models;

namespace DataStructureAndAlgorithm.Test.LeetCodeTest;

public class ClassicLinkedListProblemsTest
{
    [Fact]
    public void MergeTwoSortedLists_ShouldReturnSortedChain()
    {
        var first = ListNode.FromValues(1, 2, 4);
        var second = ListNode.FromValues(1, 3, 4);

        var result = ClassicLinkedListProblems.MergeTwoSortedLists(first, second);

        Assert.True(ListNode.ToArray(result).SequenceEqual([1, 1, 2, 3, 4, 4]));
    }

    [Fact]
    public void HasCycle_ShouldUseNodeIdentityRatherThanValue()
    {
        var first = new ListNode(1);
        var second = new ListNode(1);
        var third = new ListNode(1);
        first.Next = second;
        second.Next = third;

        Assert.False(ClassicLinkedListProblems.HasCycle(first));

        third.Next = second;
        Assert.True(ClassicLinkedListProblems.HasCycle(first));
    }

    [Fact]
    public void ReverseList_ShouldReverseLinksInPlace()
    {
        var head = ListNode.FromValues(1, 2, 3, 4, 5);

        var result = ClassicLinkedListProblems.ReverseList(head);

        Assert.True(ListNode.ToArray(result).SequenceEqual([5, 4, 3, 2, 1]));
    }
}
