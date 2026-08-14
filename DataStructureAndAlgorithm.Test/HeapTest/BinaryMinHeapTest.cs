using DataStructureAndAlgorithm.Heap;

namespace DataStructureAndAlgorithm.Test.HeapTest;

public class BinaryMinHeapTest
{
    [Fact]
    public void Constructor_ShouldBuildHeapAndDequeueInAscendingOrder()
    {
        var heap = new BinaryMinHeap<int>([7, 2, 9, 1, 5, 2]);
        var result = new List<int>();

        while (heap.Count > 0)
        {
            result.Add(heap.Dequeue());
        }

        Assert.Equal([1, 2, 2, 5, 7, 9], result);
    }

    [Fact]
    public void CustomComparer_ShouldAllowMaxHeapBehavior()
    {
        var heap = new BinaryMinHeap<int>(Comparer<int>.Create((left, right) => right.CompareTo(left)));

        heap.Enqueue(1);
        heap.Enqueue(3);
        heap.Enqueue(2);

        Assert.Equal(3, heap.Peek());
        Assert.Equal(3, heap.Dequeue());
    }
}
