using DataStructureAndAlgorithm.Linear;

namespace DataStructureAndAlgorithm.Test.LinearTest;

public class LinearStructuresTest
{
    [Fact]
    public void SinglyLinkedList_ShouldMaintainHeadTailAndCount()
    {
        var list = new SinglyLinkedList<int>();

        list.AddFirst(2);
        list.AddFirst(1);
        list.AddLast(3);

        Assert.Equal([1, 2, 3], list);
        Assert.Equal(3, list.Count);
        Assert.True(list.Remove(3));
        Assert.Equal(1, list.RemoveFirst());
        Assert.Equal([2], list);
        Assert.Equal(1, list.Count);
    }

    [Fact]
    public void SinglyLinkedList_RemoveFirstFromEmptyList_ShouldThrow()
    {
        var list = new SinglyLinkedList<int>();

        Assert.Throws<InvalidOperationException>(() => list.RemoveFirst());
    }

    [Fact]
    public void ArrayStack_ShouldGrowAndFollowLastInFirstOutOrder()
    {
        var stack = new ArrayStack<string>(1);

        stack.Push("first");
        stack.Push("second");
        stack.Push("third");

        Assert.True(stack.Capacity >= 3);
        Assert.Equal("third", stack.Peek());
        Assert.Equal("third", stack.Pop());
        Assert.Equal("second", stack.Pop());
        Assert.Equal("first", stack.Pop());
        Assert.Throws<InvalidOperationException>(() => stack.Pop());
    }

    [Fact]
    public void CircularQueue_ShouldPreserveOrderAcrossWrapAroundAndGrowth()
    {
        var queue = new CircularQueue<int>(3);

        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);
        Assert.Equal(1, queue.Dequeue());
        Assert.Equal(2, queue.Dequeue());

        // 此时尾指针会绕回数组开头，加入更多元素还会触发扩容。
        queue.Enqueue(4);
        queue.Enqueue(5);
        queue.Enqueue(6);

        Assert.Equal(3, queue.Dequeue());
        Assert.Equal(4, queue.Dequeue());
        Assert.Equal(5, queue.Dequeue());
        Assert.Equal(6, queue.Dequeue());
    }
}
