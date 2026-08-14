using DataStructureAndAlgorithm.Tree;

namespace DataStructureAndAlgorithm.Test.TreeTest;

public class AdvancedOrderedStructuresTest
{
    [Fact]
    public void RedBlackTree_RandomInsertionsAndDeletionsPreserveEveryInvariant()
    {
        var tree = new RedBlackTree<int>();
        var random = new Random(20260713);
        var insertionOrder = Enumerable.Range(0, 300).OrderBy(_ => random.Next()).ToArray();

        foreach (var value in insertionOrder)
        {
            Assert.True(tree.Add(value));
            Assert.True(tree.HasValidInvariants());
        }

        Assert.False(tree.Add(42));
        Assert.Equal(Enumerable.Range(0, 300), tree);
        // 红黑树高度不超过 2*log2(n+1)，这里只验证一个略宽松的整数上界。
        Assert.True(tree.Height <= 2 * Math.Ceiling(Math.Log2(tree.Count + 1)));

        var deletionOrder = insertionOrder.Where(value => value % 2 == 0).ToArray();
        foreach (var value in deletionOrder)
        {
            Assert.True(tree.Remove(value));
            Assert.True(tree.HasValidInvariants());
        }

        Assert.Equal(Enumerable.Range(0, 300).Where(value => value % 2 == 1), tree);
        Assert.False(tree.Remove(1000));
        Assert.Equal(150, tree.Count);
    }

    [Fact]
    public void RedBlackTree_CanDeleteEveryNodeAndBeReused()
    {
        var tree = new RedBlackTree<int>();
        foreach (var value in Enumerable.Range(1, 50)) tree.Add(value);
        foreach (var value in Enumerable.Range(1, 50).Reverse()) Assert.True(tree.Remove(value));

        Assert.Empty(tree);
        Assert.Equal(0, tree.Height);
        Assert.True(tree.HasValidInvariants());

        tree.Add(7);
        tree.Clear();
        Assert.Empty(tree);
        Assert.True(tree.HasValidInvariants());
    }

    [Fact]
    public void SkipList_EnumeratesInOrderAndSupportsSetOperations()
    {
        var list = new SkipList<int>(maximumLevel: 12, randomSeed: 42);
        foreach (var value in new[] { 7, 1, 9, 3, 5, 2, 8, 6, 4 })
        {
            Assert.True(list.Add(value));
        }

        Assert.False(list.Add(5));
        Assert.Equal(Enumerable.Range(1, 9), list);
        Assert.True(list.Contains(1));
        Assert.True(list.Contains(9));
        Assert.False(list.Contains(10));

        Assert.True(list.Remove(1));
        Assert.True(list.Remove(5));
        Assert.True(list.Remove(9));
        Assert.False(list.Remove(99));
        Assert.Equal([2, 3, 4, 6, 7, 8], list);
        Assert.Equal(6, list.Count);
        Assert.InRange(list.CurrentLevel, 1, 12);
    }

    [Fact]
    public void SkipList_AgreesWithSortedSetUnderMixedOperations()
    {
        var list = new SkipList<int>(randomSeed: 7);
        var expected = new SortedSet<int>();
        var random = new Random(7);

        for (var operation = 0; operation < 500; operation++)
        {
            var value = random.Next(0, 100);
            if (random.Next(2) == 0)
            {
                Assert.Equal(expected.Add(value), list.Add(value));
            }
            else
            {
                Assert.Equal(expected.Remove(value), list.Remove(value));
            }

            Assert.Equal(expected, list);
        }

        list.Clear();
        Assert.Empty(list);
        Assert.Equal(1, list.CurrentLevel);
    }
}
