using DataStructureAndAlgorithm.Linear;

namespace DataStructureAndAlgorithm.Test.LinearTest;

public class AdvancedLinearStructuresTest
{
    [Fact]
    public void DoublyLinkedList_MaintainsHeadTailAndMiddleLinks()
    {
        var list = new DoublyLinkedList<int>();
        list.AddLast(2);
        list.AddFirst(1);
        list.AddLast(3);
        list.AddLast(4);

        Assert.Equal([1, 2, 3, 4], list);
        Assert.True(list.Contains(3));
        Assert.True(list.Remove(2));
        Assert.Equal([1, 3, 4], list);

        Assert.Equal(1, list.RemoveFirst());
        Assert.Equal(4, list.RemoveLast());
        Assert.Equal([3], list);
        Assert.Equal(1, list.Count);

        Assert.Equal(3, list.RemoveLast());
        Assert.True(list.IsEmpty);
        Assert.Throws<InvalidOperationException>(() => list.RemoveFirst());
        Assert.Throws<InvalidOperationException>(() => list.RemoveLast());
    }

    [Fact]
    public void DoublyLinkedList_ClearResetsTheListForReuse()
    {
        var list = new DoublyLinkedList<string>();
        list.AddFirst("b");
        list.AddFirst("a");
        list.Clear();
        list.AddLast("c");

        Assert.Equal(["c"], list);
        Assert.Equal(1, list.Count);
        Assert.False(list.Remove("missing"));
    }

    [Fact]
    public void ArrayDeque_WrapsAroundAndGrowsWithoutChangingLogicalOrder()
    {
        var deque = new ArrayDeque<int>(4);
        foreach (var value in new[] { 1, 2, 3, 4 }) deque.AddLast(value);

        Assert.Equal(1, deque.RemoveFirst());
        Assert.Equal(2, deque.RemoveFirst());
        deque.AddLast(5);
        deque.AddLast(6); // 物理数组在这里发生绕回。
        deque.AddFirst(2); // 已满后扩容，同时验证头部插入。

        Assert.Equal([2, 3, 4, 5, 6], deque);
        Assert.True(deque.Capacity >= 8);
        Assert.Equal(2, deque.PeekFirst());
        Assert.Equal(6, deque.PeekLast());
        Assert.Equal(6, deque.RemoveLast());
        Assert.Equal(2, deque.RemoveFirst());
        Assert.Equal([3, 4, 5], deque);
    }

    [Fact]
    public void ArrayDeque_EmptyOperationsThrowAndClearAllowsReuse()
    {
        var deque = new ArrayDeque<string>();
        Assert.Throws<InvalidOperationException>(() => deque.PeekFirst());
        Assert.Throws<InvalidOperationException>(() => deque.PeekLast());
        Assert.Throws<InvalidOperationException>(() => deque.RemoveFirst());
        Assert.Throws<InvalidOperationException>(() => deque.RemoveLast());

        deque.AddFirst("b");
        deque.AddFirst("a");
        deque.Clear();
        deque.AddLast("c");
        Assert.Equal(["c"], deque);
    }
}
