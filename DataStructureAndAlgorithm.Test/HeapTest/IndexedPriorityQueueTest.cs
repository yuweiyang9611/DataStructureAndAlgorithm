using DataStructureAndAlgorithm.Heap;

namespace DataStructureAndAlgorithm.Test.HeapTest;

public class IndexedPriorityQueueTest
{
    [Fact]
    public void DecreaseKey_ChangesOrderWithoutCreatingDuplicateEntries()
    {
        var queue = new IndexedPriorityQueue<string, int>(StringComparer.OrdinalIgnoreCase);
        Assert.True(queue.EnqueueOrDecrease("A", 10));
        Assert.True(queue.EnqueueOrDecrease("B", 5));
        Assert.True(queue.EnqueueOrDecrease("a", 1));
        Assert.False(queue.EnqueueOrDecrease("A", 20));

        Assert.Equal(2, queue.Count);
        Assert.True(queue.HasValidInvariants());
        Assert.True(queue.TryDequeue(out var first, out var priority));
        Assert.Equal("A", first, ignoreCase: true);
        Assert.Equal(1, priority);
        Assert.True(queue.TryDequeue(out var second, out _));
        Assert.Equal("B", second);
        Assert.False(queue.TryDequeue(out _, out _));
    }

    [Fact]
    public void RandomOperations_AlwaysMatchAReferencePriorityMap()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        var expected = new Dictionary<int, int>();
        var random = new Random(42);

        for (var operation = 0; operation < 1_000; operation++)
        {
            var key = random.Next(100);
            var priority = random.Next(10_000);
            var changed = !expected.TryGetValue(key, out var old) || priority < old;
            if (changed) expected[key] = priority;
            Assert.Equal(changed, queue.EnqueueOrDecrease(key, priority));
            Assert.True(queue.HasValidInvariants());
        }

        var dequeued = new List<int>();
        while (queue.TryDequeue(out var key, out var priority))
        {
            Assert.Equal(expected[key], priority);
            dequeued.Add(priority);
            Assert.True(queue.HasValidInvariants());
        }

        Assert.True(dequeued.SequenceEqual(dequeued.Order()));
    }
}
