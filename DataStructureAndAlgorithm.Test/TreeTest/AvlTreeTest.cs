using DataStructureAndAlgorithm.Tree;
using ReflectionMagic;

namespace DataStructureAndAlgorithm.Test.TreeTest;

public class AvlTreeTest
{
    private readonly AvlTree<int> _unbalancedAvlTree = new(new AvlTreeNode<int>(10,
        new AvlTreeNode<int>(8, new AvlTreeNode<int>(4, null,
            new AvlTreeNode<int>(6, new AvlTreeNode<int>(5))), new AvlTreeNode<int>(9)),
        new AvlTreeNode<int>(12, new AvlTreeNode<int>(11), new AvlTreeNode<int>(13))));

    #region Tools

    private static IEnumerable<AvlTreeNode<int>> LevelOrder(AvlTreeNode<int>? currentNode)
    {
        var linkedList = new LinkedList<AvlTreeNode<int>>();
        var queue = new Queue<AvlTreeNode<int>>();
        while (currentNode != null)
        {
            linkedList.AddLast(currentNode);
            if (currentNode.LeftChild != null) queue.Enqueue(currentNode.LeftChild);
            if (currentNode.RightChild != null) queue.Enqueue(currentNode.RightChild);
            if (queue.Count == 0) break;
            currentNode = queue.Dequeue();
        }

        return linkedList;
    }

    private static string LeverOrderElementString(AvlTree<int> test)
    {
        var stringBuilder = new StringBuilder();
        var list = LevelOrder(test.RootNode);
        foreach (var item in list) stringBuilder.Append($"{item.Element},");
        return stringBuilder.ToString();
    }

    private static string LeverOrderTreeHeightString(AvlTree<int> test)
    {
        var stringBuilder = new StringBuilder();
        var list = LevelOrder(test.RootNode);
        foreach (var item in list) stringBuilder.Append($"{item.TreeHeight},");
        return stringBuilder.ToString();
    }

    #endregion

    [Fact]
    public void IsUnbalancedAvlTreeCreatedCorrect()
    {
        var leverOrderElementString = LeverOrderElementString(_unbalancedAvlTree);
        Assert.Equal("10,8,12,4,9,11,13,6,5,", leverOrderElementString);
    }

    [Fact]
    public void IsAvlTreeConvertsToNormalBinaryTreeWorked()
    {
        var normalBinaryTree = (NormalBinaryTree<int>?)_unbalancedAvlTree;
        Assert.NotNull(normalBinaryTree);
        var list = new List<int>();
        normalBinaryTree.LevelOrderTraverse(currentNode => { list.Add(currentNode.Element); });
        var stringBuilder = new StringBuilder();
        foreach (var element in list) stringBuilder.Append($"{element},");
        Assert.Equal("10,8,12,4,9,11,13,6,5,", stringBuilder.ToString());
    }

    [Fact]
    public void TreeHeightCalculateTest_Recursion()
    {
        var treeHeight = _unbalancedAvlTree.AsDynamic().UpdateTreeHeightRecursion(_unbalancedAvlTree.RootNode);
        Assert.Equal(5, treeHeight);
        var leverOrderTreeHeightString = LeverOrderTreeHeightString(_unbalancedAvlTree);
        Assert.Equal("5,4,2,3,1,1,1,2,1,", leverOrderTreeHeightString);
    }

    [Fact]
    public void TreeHeightCalculateTest_Iteration()
    {
        var treeHeight = _unbalancedAvlTree.AsDynamic().UpdateTreeHeightIteration(_unbalancedAvlTree.RootNode);
        Assert.Equal(5, treeHeight);
        var leverOrderTreeHeightString = LeverOrderTreeHeightString(_unbalancedAvlTree);
        Assert.Equal("5,4,2,3,1,1,1,2,1,", leverOrderTreeHeightString);
    }

    [Fact]
    public void RotationTest_LLType()
    {
        var test = new AvlTree<int>(new AvlTreeNode<int>(5, new AvlTreeNode<int>(3, new AvlTreeNode<int>(1))));
        var rootNode = (AvlTreeNode<int>)test.AsDynamic().LL_Type(test.RootNode);
        Assert.Equal(3, rootNode.Element);
        Assert.Equal(2, rootNode.TreeHeight);
        Assert.NotNull(rootNode.LeftChild);
        Assert.Equal(1, rootNode.LeftChild.Element);
        Assert.Equal(1, rootNode.LeftChild.TreeHeight);
        Assert.NotNull(rootNode.RightChild);
        Assert.Equal(5, rootNode.RightChild.Element);
        Assert.Equal(1, rootNode.RightChild.TreeHeight);
    }

    [Fact]
    public void RotationTest_RRType()
    {
        var test = new AvlTree<int>(new AvlTreeNode<int>(5, null,
            new AvlTreeNode<int>(6, null, new AvlTreeNode<int>(7))));
        var rootNode = test.AsDynamic().RR_Type(test.RootNode);
        Assert.Equal(6, rootNode.Element);
        Assert.Equal(2, rootNode.TreeHeight);
        Assert.NotNull(rootNode.LeftChild);
        Assert.Equal(5, rootNode.LeftChild.Element);
        Assert.Equal(1, rootNode.LeftChild.TreeHeight);
        Assert.NotNull(rootNode.RightChild);
        Assert.Equal(7, rootNode.RightChild.Element);
        Assert.Equal(1, rootNode.RightChild.TreeHeight);
    }

    [Fact]
    public void RotationTest_LRType()
    {
        var test = new AvlTree<int>(new AvlTreeNode<int>(15,
            new AvlTreeNode<int>(12,
                new AvlTreeNode<int>(11), new AvlTreeNode<int>(13)),
            new AvlTreeNode<int>(20)));

        var treeHeight = test.AsDynamic().UpdateTreeHeightIteration(test.RootNode);
        Assert.Equal(3, treeHeight);
        var node = test.Find(13);
        Assert.NotNull(node);
        node.RightChild = new AvlTreeNode<int>(14);
        var rootNode = test.AsDynamic().LR_Type(test.RootNode);
        Assert.Equal(13, rootNode.Element);
        Assert.Equal(3, rootNode.TreeHeight);
        Assert.NotNull(rootNode.LeftChild);
        Assert.Equal(12, rootNode.LeftChild.Element);
        Assert.Equal(2, rootNode.LeftChild.TreeHeight);
        Assert.NotNull(rootNode.RightChild);
        Assert.Equal(15, rootNode.RightChild.Element);
        Assert.Equal(2, rootNode.RightChild.TreeHeight);
        Assert.Null(rootNode.LeftChild.RightChild);
        Assert.NotNull(rootNode.LeftChild.LeftChild);
        Assert.Equal(11, rootNode.LeftChild.LeftChild.Element);
        Assert.Equal(1, rootNode.LeftChild.LeftChild.TreeHeight);
        Assert.NotNull(rootNode.RightChild.LeftChild);
        Assert.Equal(14, rootNode.RightChild.LeftChild.Element);
        Assert.Equal(1, rootNode.RightChild.LeftChild.TreeHeight);
        Assert.NotNull(rootNode.RightChild.RightChild);
        Assert.Equal(20, rootNode.RightChild.RightChild.Element);
        Assert.Equal(1, rootNode.RightChild.RightChild.TreeHeight);
    }

    [Fact]
    public void RotationTest_RLType()
    {
        var test = new AvlTree<int>(new AvlTreeNode<int>(15,
            new AvlTreeNode<int>(13),
            new AvlTreeNode<int>(20,
                new AvlTreeNode<int>(17), new AvlTreeNode<int>(21))));
        var treeHeight = test.AsDynamic().UpdateTreeHeightIteration(test.RootNode);
        Assert.Equal(3, treeHeight);
        var node = test.Find(17);
        Assert.NotNull(node);
        node.LeftChild = new AvlTreeNode<int>(16);
        var rootNode = test.AsDynamic().RL_Type(test.RootNode);
        Assert.Equal(17, rootNode.Element);
        Assert.Equal(3, rootNode.TreeHeight);
        Assert.NotNull(rootNode.LeftChild);
        Assert.Equal(15, rootNode.LeftChild.Element);
        Assert.Equal(2, rootNode.LeftChild.TreeHeight);
        Assert.NotNull(rootNode.RightChild);
        Assert.Equal(20, rootNode.RightChild.Element);
        Assert.Equal(2, rootNode.RightChild.TreeHeight);
        Assert.Null(rootNode.RightChild.LeftChild);
        Assert.NotNull(rootNode.RightChild.RightChild);
        Assert.Equal(21, rootNode.RightChild.RightChild.Element);
        Assert.Equal(1, rootNode.RightChild.RightChild.TreeHeight);
        Assert.NotNull(rootNode.LeftChild.LeftChild);
        Assert.Equal(13, rootNode.LeftChild.LeftChild.Element);
        Assert.Equal(1, rootNode.LeftChild.LeftChild.TreeHeight);
        Assert.NotNull(rootNode.LeftChild.RightChild);
        Assert.Equal(16, rootNode.LeftChild.RightChild.Element);
        Assert.Equal(1, rootNode.LeftChild.RightChild.TreeHeight);
    }

    [Fact]
    public void InsertTest_DuplicateElement()
    {
        var test = new AvlTree<int>();
        test.Insert(15);
        test.Insert(16);
        test.Insert(14);
        test.Insert(4);
        test.Insert(3);
        test.Insert(5);
        Assert.Throws<ArgumentException>(() => test.Insert(4));
    }

    [Fact]
    public void Change_IsAtomicWhenNewValueAlreadyExists()
    {
        var tree = new AvlTree<int>();
        foreach (var value in new[] { 10, 5, 15 }) tree.Insert(value);

        Assert.Throws<ArgumentException>(() => tree.Change(5, 15));

        // 失败后旧值和新值都仍在，证明操作没有只完成一半。
        Assert.NotNull(tree.Find(5));
        Assert.NotNull(tree.Find(15));
        Assert.Equal("10,5,15,", LeverOrderElementString(tree));
    }

    [Fact]
    public void Change_ValidatesOldValueAndSupportsSuccessfulReplacement()
    {
        var tree = new AvlTree<int>();
        foreach (var value in new[] { 10, 5, 15 }) tree.Insert(value);

        Assert.Throws<ArgumentException>(() => tree.Change(99, 7));
        Assert.NotNull(tree.Find(5));

        tree.Change(5, 7);
        Assert.Null(tree.Find(5));
        Assert.NotNull(tree.Find(7));

        // 排序意义上的同一值是安全的空操作。
        tree.Change(7, 7);
        Assert.NotNull(tree.Find(7));
    }

    [Fact]
    public void InsertAndDeleteTest_NoNeedToRotate_Test1()
    {
        var test = new AvlTree<int>();
        test.Insert(15);
        test.Insert(16);
        test.Insert(14);

        var leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("15,14,16,", leverOrderElementString);
        var leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("2,1,1,", leverOrderTreeHeightString);

        test.Delete(15);
        leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("14,16,", leverOrderElementString);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("2,1,", leverOrderTreeHeightString);
    }

    [Fact]
    public void InsertAndDeleteTest_NoNeedToRotate_Test2()
    {
        var test = new AvlTree<int>();
        test.Insert(15);
        test.Insert(10);
        test.Insert(20);
        test.Insert(8);
        test.Insert(12);
        test.Insert(16);
        test.Insert(21);
        var leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("15,10,20,8,12,16,21,", leverOrderElementString);
        var leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("3,2,2,1,1,1,1,", leverOrderTreeHeightString);
    }

    [Fact]
    public void InsertAndDeleteTest_LLType_Test1()
    {
        var test = new AvlTree<int>();
        test.Insert(15);
        test.Insert(10);
        test.Insert(5);
        var leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("10,5,15,", leverOrderElementString);
        var leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("2,1,1,", leverOrderTreeHeightString);

        test.Insert(3);
        test.Insert(1);
        leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("10,3,15,1,5,", leverOrderElementString);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("3,2,1,1,1,", leverOrderTreeHeightString);

        test.Insert(4);
        test.Insert(9);
        leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("5,3,10,1,4,9,15,", leverOrderElementString);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("3,2,2,1,1,1,1,", leverOrderTreeHeightString);

        test.Insert(-5);
        leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("5,3,10,1,4,9,15,-5,", leverOrderElementString);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("4,3,2,2,1,1,1,1,", leverOrderTreeHeightString);

        test.Insert(-10);
        leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("5,3,10,-5,4,9,15,-10,1,", leverOrderElementString);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("4,3,2,2,1,1,1,1,1,", leverOrderTreeHeightString);

        test.Delete(9);
        leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("5,3,10,-5,4,15,-10,1,", leverOrderElementString);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("4,3,2,2,1,1,1,1,", leverOrderTreeHeightString);

        test.Delete(1);
        leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("5,3,10,-5,4,15,-10,", leverOrderElementString);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("4,3,2,2,1,1,1,", leverOrderTreeHeightString);

        test.Delete(-5);
        leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("5,3,10,-10,4,15,", leverOrderElementString);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("3,2,2,1,1,1,", leverOrderTreeHeightString);

        test.Delete(5);
        leverOrderElementString = LeverOrderElementString(test);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("4,3,10,-10,15,", leverOrderElementString);
        Assert.Equal("3,2,2,1,1,", leverOrderTreeHeightString);

        test.Delete(15);
        test.Delete(10);
        leverOrderElementString = LeverOrderElementString(test);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("3,-10,4,", leverOrderElementString);
        Assert.Equal("2,1,1,", leverOrderTreeHeightString);

        Assert.Throws<ArgumentException>(() => test.Delete(100));
    }

    [Fact]
    public void InsertAndDeleteTest_RRType_Test1()
    {
        var test = new AvlTree<int>();
        test.Insert(15);
        test.Insert(20);
        test.Insert(30);
        var leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("20,15,30,", leverOrderElementString);
        var leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("2,1,1,", leverOrderTreeHeightString);

        test.Insert(40);
        test.Insert(50);
        leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("20,15,40,30,50,", leverOrderElementString);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("3,1,2,1,1,", leverOrderTreeHeightString);

        test.Insert(60);
        leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("40,20,50,15,30,60,", leverOrderElementString);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("3,2,2,1,1,1,", leverOrderTreeHeightString);

        test.Delete(15);
        test.Delete(20);
        leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("40,30,50,60,", leverOrderElementString);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("3,1,2,1,", leverOrderTreeHeightString);
    }

    [Fact]
    public void InsertAndDeleteTest_RLType_Test1()
    {
        var test = new AvlTree<int>();
        test.Insert(15);
        test.Insert(20);
        test.Insert(17);
        test.Insert(16);
        var leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("17,15,20,16,", leverOrderElementString);
        var leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("3,2,1,1,", leverOrderTreeHeightString);

        test.Insert(18);
        test.Delete(15);
        test.Delete(16);
        leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("18,17,20,", leverOrderElementString);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("2,1,1,", leverOrderTreeHeightString);
    }

    [Fact]
    public void InsertAndDeleteTest_LRType_Test1()
    {
        var test = new AvlTree<int>();
        test.Insert(20);
        test.Insert(15);
        test.Insert(17);
        var leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("17,15,20,", leverOrderElementString);
        var leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("2,1,1,", leverOrderTreeHeightString);

        test.Insert(25);
        test.Insert(23);
        leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("17,15,23,20,25,", leverOrderElementString);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("3,1,2,1,1,", leverOrderTreeHeightString);


        test.Delete(15);
        leverOrderElementString = LeverOrderElementString(test);
        Assert.Equal("23,17,25,20,", leverOrderElementString);
        leverOrderTreeHeightString = LeverOrderTreeHeightString(test);
        Assert.Equal("3,2,1,1,", leverOrderTreeHeightString);

        var tempNode = test.Find(17);
        Assert.NotNull(tempNode);
        Assert.NotNull(tempNode.RightChild);
        Assert.Equal(20, tempNode.RightChild.Element);
    }
}
