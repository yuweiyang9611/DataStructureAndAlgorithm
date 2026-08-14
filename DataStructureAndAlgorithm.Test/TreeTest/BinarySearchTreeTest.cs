using System.IO;
using DataStructureAndAlgorithm.Tree;

namespace DataStructureAndAlgorithm.Test.TreeTest;

public class BinarySearchTreeTest
{
    private readonly BinarySearchTree<int> _binarySearchTree = new();

    private readonly List<int> _nodes = new();
    private readonly StringBuilder _stringBuilder = new();

    private void ShowTraverseResult(NormalBinaryTreeNode<int> currentNode) => _nodes.Add(currentNode.Element);

    [Fact]
    public void InsertTest()
    {
        _binarySearchTree.Insert(18);
        Assert.NotNull(_binarySearchTree.RootNode);
        Assert.Equal(18, _binarySearchTree.RootNode.Element);

        using (var stringWriter = new StringWriter())
        {
            Console.SetOut(stringWriter);
            _binarySearchTree.Insert(18);
            var outputString = stringWriter.ToString();
            // 将Console的输出重定向回默认输出
            Console.SetOut(Console.Out);
            Assert.Equal("存在相同元素，已忽略" + Environment.NewLine, outputString);
        }

        var elementsWaitingForInsert = new int[] { 10, 20, 7, 15, 22, 21, 19, 9 };
        foreach (var element in elementsWaitingForInsert) _binarySearchTree.Insert(element);
        var normalBinaryTree = _binarySearchTree.ConvertToNormalBinaryTree();
        const string levelOrderSequence = "1810207151922921";
        const string preOrderSequence = "1810791520192221";
        const string inOrderSequence = "7910151819202122";
        const string postOrderSequence = "9715101921222018";
        const string expectedString = preOrderSequence + inOrderSequence + postOrderSequence + levelOrderSequence;
        Assert.True(levelOrderSequence.Length == preOrderSequence.Length);
        Assert.True(preOrderSequence.Length == inOrderSequence.Length);
        Assert.True(inOrderSequence.Length == postOrderSequence.Length);

        _nodes.Clear();
        _stringBuilder.Clear();
        normalBinaryTree.PreOrderTraverse(ShowTraverseResult);
        normalBinaryTree.InOrderTraverse(ShowTraverseResult);
        normalBinaryTree.PostOrderTraverse(ShowTraverseResult);
        normalBinaryTree.LevelOrderTraverse(ShowTraverseResult);
        foreach (var node in _nodes) _stringBuilder.Append(node);
        var result = _stringBuilder.ToString();
        Assert.Equal(expectedString, result);
        _nodes.Clear();
        _stringBuilder.Clear();
    }

    [Fact]
    public void FindTest()
    {
        _binarySearchTree.Clear();
        var elementsWaitingForInsert = new int[] { 18, 10, 20, 7, 15, 22, 21, 19, 9 };
        foreach (var element in elementsWaitingForInsert) _binarySearchTree.Insert(element);
        _nodes.Clear();
        var normalBinaryTree = _binarySearchTree.ConvertToNormalBinaryTree();
        normalBinaryTree.PreOrderTraverse(ShowTraverseResult);
        Assert.True(object.ReferenceEquals(normalBinaryTree.RootNode, _binarySearchTree.RootNode));
        Assert.Null(_binarySearchTree.Find(100, out _));

        // 每一个插入节点的值都应该在二叉搜索树中找到
        foreach (var element in elementsWaitingForInsert)
        {
            var returnedNode = _binarySearchTree.Find(element, out _);
            Assert.NotNull(returnedNode);
            Assert.Equal(element, returnedNode.Element);
        }
    }

    [Fact]
    public void DeleteAndFindMaxTest()
    {
        _binarySearchTree.Clear();
        _binarySearchTree.Insert(0);
        _binarySearchTree.Delete(0);
        Assert.Null(_binarySearchTree.RootNode);
        var elementsWaitingForInsert = new int[] { 18, 10, 20, 7, 15, 22, 21, 19, 9 };
        foreach (var element in elementsWaitingForInsert) _binarySearchTree.Insert(element);
        // 删除叶子节点
        DeleteLeafNodesTest();
        // 删除具有一个孩子的节点
        DeleteNodesWithOneChildTest();
        // 删除具有两个孩子的节点
        DeleteNodesWithTwoChildrenTest();
    }

    private void DeleteLeafNodesTest()
    {
        _nodes.Clear();
        _stringBuilder.Clear();
        _binarySearchTree.Delete(15);
        _binarySearchTree.Delete(21);
        var normalBinaryTree = _binarySearchTree.ConvertToNormalBinaryTree();
        normalBinaryTree.PreOrderTraverse(ShowTraverseResult);
        foreach (var node in _nodes) _stringBuilder.Append($"{node}, ");
        Assert.Equal("18, 10, 7, 9, 20, 19, 22, ", _stringBuilder.ToString());
    }

    private void DeleteNodesWithOneChildTest()
    {
        _nodes.Clear();
        _stringBuilder.Clear();
        _binarySearchTree.Delete(10);
        Assert.NotNull(_binarySearchTree.RootNode);
        Assert.NotNull(_binarySearchTree.RootNode.LeftChild);
        Assert.True(_binarySearchTree.RootNode.LeftChild.Element == 7);
        var normalBinaryTree = _binarySearchTree.ConvertToNormalBinaryTree();
        normalBinaryTree.PreOrderTraverse(ShowTraverseResult);
        foreach (var node in _nodes) _stringBuilder.Append($"{node}, ");
        Assert.Equal("18, 7, 9, 20, 19, 22, ", _stringBuilder.ToString());
    }

    private void DeleteNodesWithTwoChildrenTest()
    {
        _nodes.Clear();
        _stringBuilder.Clear();
        var elementsWaitingForInsert = new int[] { 4, 2, 0, 5, 3, 6 };
        foreach (var element in elementsWaitingForInsert) _binarySearchTree.Insert(element);
        var normalBinaryTree = _binarySearchTree.ConvertToNormalBinaryTree();
        normalBinaryTree.PreOrderTraverse(ShowTraverseResult);
        foreach (var node in _nodes) _stringBuilder.Append($"{node}, ");
        Assert.Equal("18, 7, 4, 2, 0, 3, 5, 6, 9, 20, 19, 22, ", _stringBuilder.ToString());

        _nodes.Clear();
        _stringBuilder.Clear();
        _binarySearchTree.Delete(7);
        _binarySearchTree.Delete(18);
        normalBinaryTree = _binarySearchTree.ConvertToNormalBinaryTree();
        normalBinaryTree.PreOrderTraverse(ShowTraverseResult);
        foreach (var node in _nodes) _stringBuilder.Append($"{node}, ");
        Assert.Equal("9, 6, 4, 2, 0, 3, 5, 20, 19, 22, ", _stringBuilder.ToString());

        _nodes.Clear();
        _stringBuilder.Clear();
        _binarySearchTree.Clear();
        elementsWaitingForInsert = new int[] { 100, 80, 120, 70, 90, 60, 75, 85, 95, 87, 94, 92, 110, 130 };
        foreach (var element in elementsWaitingForInsert) _binarySearchTree.Insert(element);
        normalBinaryTree = _binarySearchTree.ConvertToNormalBinaryTree();
        normalBinaryTree.LevelOrderTraverse(ShowTraverseResult);
        foreach (var node in _nodes) _stringBuilder.Append($"{node}, ");
        Assert.Equal("100, 80, 120, 70, 90, 110, 130, 60, 75, 85, 95, 87, 94, 92, ", _stringBuilder.ToString());

        _nodes.Clear();
        _stringBuilder.Clear();
        _binarySearchTree.Delete(80);
        _binarySearchTree.Delete(100);
        normalBinaryTree = _binarySearchTree.ConvertToNormalBinaryTree();
        normalBinaryTree.LevelOrderTraverse(ShowTraverseResult);
        foreach (var node in _nodes) _stringBuilder.Append($"{node}, ");
        Assert.Equal("95, 75, 120, 70, 90, 110, 130, 60, 85, 94, 87, 92, ", _stringBuilder.ToString());
    }

    [Fact]
    public void DeleteNodesWithTwoChildren_WhenDirectLeftChildIsPredecessor()
    {
        _binarySearchTree.Clear();
        foreach (var element in new[] { 10, 5, 4, 20 }) _binarySearchTree.Insert(element);

        _binarySearchTree.Delete(10);

        Assert.NotNull(_binarySearchTree.RootNode);
        Assert.Equal(5, _binarySearchTree.RootNode.Element);
        Assert.NotNull(_binarySearchTree.RootNode.LeftChild);
        Assert.Equal(4, _binarySearchTree.RootNode.LeftChild.Element);
        Assert.NotNull(_binarySearchTree.RootNode.RightChild);
        Assert.Equal(20, _binarySearchTree.RootNode.RightChild.Element);

        _nodes.Clear();
        _stringBuilder.Clear();
        var normalBinaryTree = _binarySearchTree.ConvertToNormalBinaryTree();
        normalBinaryTree.LevelOrderTraverse(ShowTraverseResult);
        foreach (var node in _nodes) _stringBuilder.Append($"{node}, ");
        Assert.Equal("5, 4, 20, ", _stringBuilder.ToString());
    }
}
