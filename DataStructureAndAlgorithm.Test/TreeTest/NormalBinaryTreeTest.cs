using DataStructureAndAlgorithm.Tree;

namespace DataStructureAndAlgorithm.Test.TreeTest;

public class NormalBinaryTreeTest
{
    private static readonly NormalBinaryTreeNode<char> OldBinaryTreeRootNode =
        new NormalBinaryTreeNode<char>('A',
            new NormalBinaryTreeNode<char>('B',
                new NormalBinaryTreeNode<char>('D'),
                new NormalBinaryTreeNode<char>('E')),
            new NormalBinaryTreeNode<char>('C',
                new NormalBinaryTreeNode<char>('F'),
                new NormalBinaryTreeNode<char>('G')));

    private readonly NormalBinaryTree<char> _oldBinaryTree = new(OldBinaryTreeRootNode);

    private readonly List<char> _nodes = new();
    private readonly List<string> _hashCode = new();

    [Fact]
    public void IsEmptyNormalBinaryTreeInstanceCanBeCreated()
    {
        var temp = new NormalBinaryTree<char>();
        Assert.Null(temp.RootNode);
    }

    #region OutputToolsForTest

    private void ShowTraverseResult(NormalBinaryTreeNode<char> currentNode)
    {
        _nodes.Add(currentNode.Element);
    }

    private void GetHashCodeOfNodeInstance<TElementType>(NormalBinaryTreeNode<TElementType> currentNode)
        where TElementType : notnull
    {
        _hashCode.Add($"{currentNode.GetHashCode()},");
    }

    #endregion

    [Fact]
    public void IsElementOfRootNodeEqualsA_Test()
    {
        _nodes.Clear();
        Assert.NotNull(_oldBinaryTree.RootNode);
        ShowTraverseResult(_oldBinaryTree.RootNode);
        var stringBuilder = new StringBuilder();
        foreach (var node in _nodes) stringBuilder.Append(node);
        var actualString = stringBuilder.ToString();
        Assert.Equal("A", actualString);
        _nodes.Clear();
    }

    #region TraverseTest

    [Fact]
    public void TraverseTest_PreOrder()
    {
        _nodes.Clear();
        _oldBinaryTree.PreOrderTraverse(ShowTraverseResult);
        var stringBuilder = new StringBuilder();
        foreach (var node in _nodes) stringBuilder.Append(node);
        var actualString = stringBuilder.ToString();
        Assert.Equal("ABDECFG", actualString);
        _nodes.Clear();
    }

    [Fact]
    public void TraverseTest_InOrder()
    {
        _nodes.Clear();
        _oldBinaryTree.InOrderTraverse(ShowTraverseResult);
        var stringBuilder = new StringBuilder();
        foreach (var node in _nodes) stringBuilder.Append(node);
        var actualString = stringBuilder.ToString();
        Assert.Equal("DBEAFCG", actualString);
        _nodes.Clear();
    }

    [Fact]
    public void TraverseTest_PostOrder()
    {
        _nodes.Clear();
        _oldBinaryTree.PostOrderTraverse(ShowTraverseResult);
        var stringBuilder = new StringBuilder();
        foreach (var node in _nodes) stringBuilder.Append(node);
        var actualString = stringBuilder.ToString();
        Assert.Equal("DEBFGCA", actualString);
        _nodes.Clear();
    }

    [Fact]
    public void TraverseTest_LevelOrder()
    {
        _nodes.Clear();
        _oldBinaryTree.LevelOrderTraverse(ShowTraverseResult);
        var stringBuilder = new StringBuilder();
        foreach (var node in _nodes) stringBuilder.Append(node);
        var actualString = stringBuilder.ToString();
        Assert.Equal("ABCDEFG", actualString);
        _nodes.Clear();
    }

    #endregion

    #region BinaryTreeCopyTest

    private void BinaryTreeCopyTest_Core(Func<NormalBinaryTree<char>> copyANewBinaryTree)
    {
        var newBinaryTree = copyANewBinaryTree.Invoke();
        // 两个二叉树不是同一个二叉树
        Assert.NotSame(_oldBinaryTree, newBinaryTree);
        // 两个二叉树里面的对象都不是同一个对象
        AllTheNodesInTheTwoBinaryTreesAreNotSameObjects(newBinaryTree);
        // 判断两个二叉树结构一致：中序+其他任意序列可以唯一确定一颗二叉树，这里用中序+先序序列
        TheTwoBinaryTreesHaveSameStructures(newBinaryTree);
    }

    private void TheTwoBinaryTreesHaveSameStructures(NormalBinaryTree<char> newBinaryTree)
    {
        var preOrderSequenceOfOldBinaryTree = GetPreOrderSequence(_oldBinaryTree);
        var preOrderSequenceOfNewBinaryTree = GetPreOrderSequence(newBinaryTree);
        var inOrderSequenceOfOldBinaryTree = GetInOrderSequence(_oldBinaryTree);
        var inOrderSequenceOfNewBinaryTree = GetInOrderSequence(newBinaryTree);

        Assert.True(preOrderSequenceOfNewBinaryTree == preOrderSequenceOfOldBinaryTree);
        Assert.True(inOrderSequenceOfNewBinaryTree == inOrderSequenceOfOldBinaryTree);
    }

    private string GetPreOrderSequence(NormalBinaryTree<char> binaryTree)
    {
        _nodes.Clear();
        binaryTree.PreOrderTraverse(ShowTraverseResult);
        var stringBuilder = new StringBuilder();
        foreach (var node in _nodes) stringBuilder.Append(node);
        var actualString = stringBuilder.ToString();
        return actualString;
    }

    private string GetInOrderSequence(NormalBinaryTree<char> binaryTree)
    {
        _nodes.Clear();
        binaryTree.InOrderTraverse(ShowTraverseResult);
        var stringBuilder = new StringBuilder();
        foreach (var node in _nodes) stringBuilder.Append(node);
        var actualString = stringBuilder.ToString();
        return actualString;
    }

    private void AllTheNodesInTheTwoBinaryTreesAreNotSameObjects<TElementType>(
        NormalBinaryTree<TElementType> newBinaryTree)
        where TElementType : notnull
    {
        _hashCode.Clear();
        _oldBinaryTree.PreOrderTraverse(GetHashCodeOfNodeInstance);
        var stringBuilder = new StringBuilder();
        foreach (var hash in _hashCode) stringBuilder.Append(hash);
        var actualString = stringBuilder.ToString();
        var oldBinaryTreesHashCodes = actualString.Split(',')[..^1]; // 过滤掉最后的空字符串

        _hashCode.Clear();
        stringBuilder.Clear();
        newBinaryTree.PreOrderTraverse(GetHashCodeOfNodeInstance);
        foreach (var hash in _hashCode) stringBuilder.Append(hash);
        actualString = stringBuilder.ToString();
        var newBinaryTreesHashCodes = actualString.Split(',')[..^1];
        Assert.True(newBinaryTreesHashCodes.Length == oldBinaryTreesHashCodes.Length);
        for (var i = 0; i < newBinaryTreesHashCodes.Length; i++)
        {
            Assert.True(oldBinaryTreesHashCodes[i] != newBinaryTreesHashCodes[i]);
        }

        _hashCode.Clear();
    }

    [Fact]
    public void BinaryTreeCopyTest_PreOrder()
    {
        BinaryTreeCopyTest_Core(() => _oldBinaryTree.BinaryTreeCopyPreOrder());
    }

    [Fact]
    public void BinaryTreeCopyTest_InOrder()
    {
        BinaryTreeCopyTest_Core(() => _oldBinaryTree.BinaryTreeCopyInOrder());
    }

    [Fact]
    public void BinaryTreeCopyTest_WithNullElement()
    {
        var nullNormalBinaryTree = new NormalBinaryTree<object>();
        Assert.Null(nullNormalBinaryTree.RootNode);
        var newNullNormalBinaryTree = nullNormalBinaryTree.BinaryTreeCopyPreOrder();
        Assert.NotNull(newNullNormalBinaryTree);
        Assert.Null(newNullNormalBinaryTree.RootNode);
        newNullNormalBinaryTree = nullNormalBinaryTree.BinaryTreeCopyInOrder();
        Assert.NotNull(newNullNormalBinaryTree);
        Assert.Null(newNullNormalBinaryTree.RootNode);
    }

    #endregion

    #region BuildingABinaryTreeByTraverseSequencesTest

    private const string PreOrderSequence = "DAEFBCHGI",
        InOrderSequence = "EAFDHCBGI",
        PostOrderSequence = "EFAHCIGBD",
        LevelOrderSequence = "DABEFCGHI";

    [Fact]
    public void BuildingABinaryTree_PreInOrderSequences()
    {
        var binaryTree =
            NormalBinaryTree<char>.BuildingABinaryTreeByPreInOrderSequences(
                PreOrderSequence.ToList(), InOrderSequence.ToList());
        Assert.NotNull(binaryTree);
        BinaryTreeStructureTest(binaryTree);
    }

    [Fact]
    public void BuildingABinaryTree_PostInOrderSequences()
    {
        var binaryTree =
            NormalBinaryTree<char>.BuildingABinaryTreeByPostInOrderSequences(
                PostOrderSequence.ToList(), InOrderSequence.ToList());
        Assert.NotNull(binaryTree);
        BinaryTreeStructureTest(binaryTree);
    }

    private void BinaryTreeStructureTest(NormalBinaryTree<char> binaryTree)
    {
        _nodes.Clear();
        binaryTree.PreOrderTraverse(ShowTraverseResult);
        binaryTree.InOrderTraverse(ShowTraverseResult);
        binaryTree.PostOrderTraverse(ShowTraverseResult);
        binaryTree.LevelOrderTraverse(ShowTraverseResult);
        const string expectString = PreOrderSequence + InOrderSequence + PostOrderSequence + LevelOrderSequence;
        var stringBuilder = new StringBuilder();
        foreach (var node in _nodes) stringBuilder.Append(node);
        var actualString = stringBuilder.ToString();
        Assert.Equal(expectString, actualString);
        _nodes.Clear();
    }

    #endregion
}
