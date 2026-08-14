using DataStructureAndAlgorithm.Tree;

using ReflectionMagic;

namespace DataStructureAndAlgorithm.Test.TreeTest;

public class BTreeTest
{
    private const int Degree = 2;

    private readonly int[] _testKeyData = new int[] { 10, 20, 30, 50 };
    private readonly int[] _testPointerData = new int[] { 50, 60, 40, 20 };

    [Fact]
    public void CreateBTree()
    {
        var btree = new BTree<int, int>(Degree);
        var root = btree.Root;
        Assert.NotNull(root);
        Assert.NotNull(root.Entries);
        Assert.NotNull(root.Children);
        Assert.Empty(root.Entries);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void NodeViewsAndEntries_ShouldNotExposeMutableTreeState()
    {
        var tree = new BTree<int, int>(Degree);
        foreach (var key in new[] { 1, 2, 3, 4 }) tree.Insert(key, key);

        var exposedEntries = Assert.IsAssignableFrom<IList<BTreeNodeEntry<int, int>>>(tree.Root.Entries);
        var exposedChildren = Assert.IsAssignableFrom<IList<BTreeNode<int, int>>>(tree.Root.Children);

        Assert.Throws<NotSupportedException>(() => exposedEntries.Clear());
        Assert.Throws<NotSupportedException>(() => exposedChildren.Clear());
        Assert.False(typeof(BTreeNodeEntry<int, int>).GetProperty(nameof(BTreeNodeEntry<int, int>.Key))!.CanWrite);
        Assert.False(typeof(BTreeNodeEntry<int, int>).GetProperty(nameof(BTreeNodeEntry<int, int>.Value))!.CanWrite);
        Assert.All(new[] { 1, 2, 3, 4 }, key => Assert.NotNull(tree.Search(key)));
    }

    [Fact]
    public void InsertOneNode()
    {
        var btree = new BTree<int, int>(Degree);
        InsertTestDataAndValidateTree(btree, 0);
        Assert.Equal(1, btree.Height);
    }

    [Fact]
    public void InsertMultipleNodesToSplit()
    {
        var btree = new BTree<int, int>(Degree);

        for (int i = 0; i < this._testKeyData.Length; i++)
        {
            InsertTestDataAndValidateTree(btree, i);
        }

        Assert.Equal(2, btree.Height);
    }

    [Fact]
    public void DeleteNodes()
    {
        var btree = new BTree<int, int>(Degree);

        for (int i = 0; i < this._testKeyData.Length; i++)
        {
            InsertTestData(btree, i);
        }

        for (int i = 0; i < this._testKeyData.Length; i++)
        {
            btree.Delete(this._testKeyData[i]);
            TreeValidation.ValidateTree(btree.Root, Degree, this._testKeyData.Skip(i + 1).ToArray());
        }

        Assert.Equal(1, btree.Height);
    }

    [Fact]
    public void DeleteNonExistingNode()
    {
        var btree = new BTree<int, int>(Degree);

        for (int i = 0; i < _testKeyData.Length; i++)
        {
            InsertTestData(btree, i);
        }

        btree.Delete(99999);
        TreeValidation.ValidateTree(btree.Root, Degree, this._testKeyData.ToArray());
    }

    [Fact]
    public void DeleteInternalKey_ShouldRebalanceEntirePredecessorPath()
    {
        int[] keys = [8, 5, 9, 7, 6, 4, 3, 1, 0, 2];
        var tree = new BTree<int, int>(Degree);
        foreach (var key in keys) tree.Insert(key, key);

        Assert.Equal(new[] { 6 }, tree.Root.Entries.Select(entry => entry.Key));
        Assert.Equal(3, tree.Height);

        tree.Delete(6);

        TreeValidation.ValidateTree(tree.Root, Degree, keys.Where(key => key != 6).ToArray());
        Assert.Null(tree.Search(6));
    }

    [Fact]
    public void DeleteInternalKey_ShouldRebalanceEntireSuccessorPath()
    {
        int[] keys = [1, 4, 0, 2, 3, 5, 6, 8, 9, 7];
        var tree = new BTree<int, int>(Degree);
        foreach (var key in keys) tree.Insert(key, key);

        Assert.Equal(new[] { 3 }, tree.Root.Entries.Select(entry => entry.Key));
        Assert.Equal(3, tree.Height);

        tree.Delete(3);

        TreeValidation.ValidateTree(tree.Root, Degree, keys.Where(key => key != 3).ToArray());
        Assert.Null(tree.Search(3));
    }

    [Fact]
    public void SearchNodes()
    {
        var btree = new BTree<int, int>(Degree);

        for (int i = 0; i < _testKeyData.Length; i++)
        {
            InsertTestData(btree, i);
            SearchTestData(btree, i);
        }
    }

    [Fact]
    public void SearchNonExistingNode()
    {
        var btree = new BTree<int, int>(Degree);

        // search an empty tree
        var nonExisting = btree.Search(9999);
        Assert.Null(nonExisting);

        for (int i = 0; i < this._testKeyData.Length; i++)
        {
            InsertTestData(btree, i);
            SearchTestData(btree, i);
        }

        // search a populated tree
        nonExisting = btree.Search(9999);
        Assert.Null(nonExisting);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Constructor_ShouldRejectInvalidDegree(int degree)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BTree<int, int>(degree));
    }

    [Fact]
    public void HighDegreeTree_ShouldSupportRandomizedInsertSearchAndDelete()
    {
        const int degree = 16;
        var random = new Random(42);
        var keys = Enumerable.Range(0, 512).OrderBy(_ => random.Next()).ToArray();
        var tree = new BTree<int, int>(degree);

        foreach (var key in keys) tree.Insert(key, key * 10);

        TreeValidation.ValidateTree(tree.Root, degree, keys);
        foreach (var key in keys)
        {
            var entry = tree.Search(key);
            Assert.NotNull(entry);
            Assert.Equal(key * 10, entry.Value);
        }

        var deletedKeys = keys.Where((_, index) => index % 3 == 0).ToArray();
        foreach (var key in deletedKeys) tree.Delete(key);

        var remainingKeys = keys.Except(deletedKeys).ToArray();
        TreeValidation.ValidateTree(tree.Root, degree, remainingKeys);
        Assert.All(deletedKeys, key => Assert.Null(tree.Search(key)));
    }

    [Fact]
    public void DeleteKeyFromNode_ShouldUseSuccessorChildWhenPredecessorChildHasTooFewEntries()
    {
        var btree = new BTree<int, int>(Degree);
        var node = new BTreeNode<int, int>(Degree);
        node.MutableEntries.Add(new BTreeNodeEntry<int, int>(20, 20));
        node.MutableChildren.Add(new BTreeNode<int, int>(Degree));
        node.MutableChildren.Add(new BTreeNode<int, int>(Degree));

        node.MutableChildren[0].MutableEntries.Add(new BTreeNodeEntry<int, int>(10, 10));
        node.MutableChildren[1].MutableEntries.Add(new BTreeNodeEntry<int, int>(30, 30));
        node.MutableChildren[1].MutableEntries.Add(new BTreeNodeEntry<int, int>(40, 40));

        btree.AsDynamic().DeleteKeyFromNode(node, 20, 0);

        Assert.Equal(new[] { 30 }, node.Entries.Select(entry => entry.Key));
        Assert.Equal(new[] { 10 }, node.Children[0].Entries.Select(entry => entry.Key));
        Assert.Equal(new[] { 40 }, node.Children[1].Entries.Select(entry => entry.Key));
    }

    [Fact]
    public void DeleteKeyFromSubTree_ShouldUseLeftSeparatorWhenBorrowingFromLeftSibling()
    {
        var btree = new BTree<int, int>(Degree);
        var parent = new BTreeNode<int, int>(Degree);
        parent.MutableEntries.Add(new BTreeNodeEntry<int, int>(20, 20));
        parent.MutableEntries.Add(new BTreeNodeEntry<int, int>(40, 40));

        var left = new BTreeNode<int, int>(Degree);
        left.MutableEntries.Add(new BTreeNodeEntry<int, int>(10, 10));
        left.MutableEntries.Add(new BTreeNodeEntry<int, int>(15, 15));

        var middle = new BTreeNode<int, int>(Degree);
        middle.MutableEntries.Add(new BTreeNodeEntry<int, int>(30, 30));

        var right = new BTreeNode<int, int>(Degree);
        right.MutableEntries.Add(new BTreeNodeEntry<int, int>(50, 50));

        parent.MutableChildren.Add(left);
        parent.MutableChildren.Add(middle);
        parent.MutableChildren.Add(right);

        btree.AsDynamic().DeleteKeyFromSubTree(parent, 30, 1);

        Assert.Equal(new[] { 15, 40 }, parent.Entries.Select(entry => entry.Key));
        Assert.Equal(new[] { 20 }, middle.Entries.Select(entry => entry.Key));
        Assert.Equal(new[] { 10 }, left.Entries.Select(entry => entry.Key));
    }


    private void InsertTestData(BTree<int, int> btree, int testDataIndex)
    {
        btree.Insert(_testKeyData[testDataIndex], _testPointerData[testDataIndex]);
    }

    private void InsertTestDataAndValidateTree(BTree<int, int> btree, int testDataIndex)
    {
        btree.Insert(_testKeyData[testDataIndex], _testPointerData[testDataIndex]);
        TreeValidation.ValidateTree(btree.Root, Degree, _testKeyData.Take(testDataIndex + 1).ToArray());
    }

    private void SearchTestData(BTree<int, int> btree, int testKeyDataIndex)
    {
        for (int i = 0; i <= testKeyDataIndex; i++)
        {
            var entry = btree.Search(_testKeyData[i]);
            Assert.NotNull(entry);
            Assert.Equal(_testKeyData[i], entry.Key);
            Assert.Equal(_testPointerData[i], entry.Value);
        }
    }
}

public static class TreeValidation
{
    public static void ValidateTree(BTreeNode<int, int> tree, int degree, params int[] expectedKeys)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var foundKeys = new Dictionary<int, List<BTreeNodeEntry<int, int>>>();
        int? leafDepth = null;
        ValidateSubtree(tree, tree, degree, int.MinValue, int.MaxValue, foundKeys, 0, ref leafDepth);

        Assert.Equal(expectedKeys.OrderBy(key => key), foundKeys.Keys.OrderBy(key => key));
        foreach (var keyValuePair in foundKeys)
        {
            Assert.Single(keyValuePair.Value);
        }
    }

    private static void UpdateFoundKeys(IDictionary<int, List<BTreeNodeEntry<int, int>>> foundKeys,
        BTreeNodeEntry<int, int> entry)
    {
        if (!foundKeys.TryGetValue(entry.Key, out var foundEntries))
        {
            foundEntries = new List<BTreeNodeEntry<int, int>>();
            foundKeys.Add(entry.Key, foundEntries);
        }

        foundEntries.Add(entry);
    }

    private static void ValidateSubtree(BTreeNode<int, int> root, BTreeNode<int, int> node, int degree, int nodeMin,
        int nodeMax,
        Dictionary<int, List<BTreeNodeEntry<int, int>>> foundKeys, int depth, ref int? leafDepth)
    {
        var maximumEntries = (2 * degree) - 1;
        if (ReferenceEquals(root, node))
        {
            Assert.InRange(node.Entries.Count, node.IsLeaf ? 0 : 1, maximumEntries);
        }
        else
        {
            Assert.InRange(node.Entries.Count, degree - 1, maximumEntries);
        }

        Assert.All(node.Entries.Zip(node.Entries.Skip(1)), pair => Assert.True(pair.First.Key < pair.Second.Key));

        if (node.IsLeaf)
        {
            Assert.Empty(node.Children);
            leafDepth ??= depth;
            Assert.Equal(leafDepth.Value, depth);
        }
        else
        {
            Assert.Equal(node.Entries.Count + 1, node.Children.Count);
        }

        for (int i = 0; i <= node.Entries.Count; i++)
        {
            int subtreeMin = nodeMin;
            int subtreeMax = nodeMax;

            if (i < node.Entries.Count)
            {
                var entry = node.Entries[i];
                UpdateFoundKeys(foundKeys, entry);
                Assert.True(entry.Key >= nodeMin && entry.Key <= nodeMax);

                subtreeMax = entry.Key;
            }

            if (i > 0)
            {
                subtreeMin = node.Entries[i - 1].Key;
            }

            if (node.IsLeaf) continue;
            ValidateSubtree(root, node.Children[i], degree, subtreeMin, subtreeMax, foundKeys, depth + 1,
                ref leafDepth);
        }
    }
}
