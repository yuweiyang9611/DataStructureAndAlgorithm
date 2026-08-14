using DataStructureAndAlgorithm.Tree;

namespace DataStructureAndAlgorithm.Test.TreeTest;

public class CluesBinaryTreeTest
{
    private const string PreOrderSequence = "ABDECFG",
        InOrderSequence = "DBEAFCG",
        PostOrderSequence = "DEBFGCA",
        LevelOrderSequence = "ABCDEFG";

    #region NormalBinaryTreeToCluesBinaryTreeTest

    // 通过前序和中序序列构造一棵normalBinaryTree
    private static NormalBinaryTree<TElementType>? GetANormalBinaryTree<TElementType>(
        IReadOnlyList<TElementType> preOrderSequence,
        IReadOnlyList<TElementType> inOrderSequence)
        where TElementType : notnull
    {
        return NormalBinaryTree<TElementType>.BuildingABinaryTreeByPreInOrderSequences(preOrderSequence,
            inOrderSequence);
    }

    [Fact]
    public void WillGetANullCluesBinaryTreeIfNormalBinaryTreeIsNull()
    {
        NormalBinaryTree<char>? nullNormalBinaryTree = null;
        var nullCluesBinaryTree = (CluesBinaryTree<char>?)nullNormalBinaryTree;
        Assert.Null(nullNormalBinaryTree);
        Assert.Null(nullCluesBinaryTree);
        var newNullNormalBinaryTree = new NormalBinaryTree<object>();
        Assert.Null(newNullNormalBinaryTree.RootNode);
        var newNullCluesBinaryTree = (CluesBinaryTree<object>?)newNullNormalBinaryTree;
        Assert.NotNull(newNullCluesBinaryTree);
        Assert.Null(newNullCluesBinaryTree.RootNode);
    }

    [Fact]
    public void InvalidTraversalSequencesAreNotTreatedAsAnEmptyTree()
    {
        Assert.Throws<ArgumentException>(() => GetANormalBinaryTree("ab".ToList(), "a".ToList()));
    }

    private static CluesBinaryTree<char> GetCluesBinaryTreeFromANotNullNormalBinaryTree()
    {
        var notNullNormalBinaryTree = GetANormalBinaryTree(PreOrderSequence.ToList(), InOrderSequence.ToList());
        var notNullCluesBinaryTree = (CluesBinaryTree<char>?)notNullNormalBinaryTree;
        Assert.NotNull(notNullNormalBinaryTree);
        Assert.NotNull(notNullCluesBinaryTree);
        return notNullCluesBinaryTree;
    }

    #endregion

    private readonly CluesBinaryTree<char> _cluesBinaryTree = GetCluesBinaryTreeFromANotNullNormalBinaryTree();

    #region CluesBinaryTreeTraverseTest

    private static void CheckCluesGenericMethod(CluesBinaryTreeNode<char> currentNode, int count,
        IReadOnlyList<CluesBinaryTreeNode<char>> cluesBinaryTreeNodes)
    {
        Assert.True(currentNode.LeftTag == 1);
        Assert.True(currentNode.RightTag == 1);

        if (count - 1 < 0)
        {
            Assert.True(cluesBinaryTreeNodes[count].LeftChild == null);
            Assert.True(cluesBinaryTreeNodes[count].RightChild == cluesBinaryTreeNodes[count + 1]);
        }
        else if (count + 1 == cluesBinaryTreeNodes.Count)
        {
            Assert.True(cluesBinaryTreeNodes[count].LeftChild == cluesBinaryTreeNodes[count - 1]);
            Assert.True(cluesBinaryTreeNodes[count].RightChild == null);
        }
        else
        {
            Assert.True(cluesBinaryTreeNodes[count].LeftChild == cluesBinaryTreeNodes[count - 1]);
            Assert.True(cluesBinaryTreeNodes[count].RightChild == cluesBinaryTreeNodes[count + 1]);
        }
    }

    [Fact]
    public void CheckAndCluesIt_PreOrder()
    {
        var preOrderSequenceNode = _cluesBinaryTree.PreOrderTraverse();
        var stringBuilder = new StringBuilder();
        foreach (var currentNode in preOrderSequenceNode)
        {
            stringBuilder.Append(currentNode.Element);
        }

        var traverseResult = stringBuilder.ToString();
        Assert.True(PreOrderSequence == traverseResult);

        // 线索化应当成功
        _cluesBinaryTree.CluesGeneric(_cluesBinaryTree.PreOrderTraverse);
        Assert.Equal(ThreadingOrder.PreOrder, _cluesBinaryTree.CurrentThreadingOrder);
        // 前序遍历序列：ABDECFG，能够线索化的节点：DEFG，索引为：2，3，5，6
        preOrderSequenceNode = _cluesBinaryTree.PreOrderTraverse();
        PreOrderCheck(preOrderSequenceNode);
    }

    private static void PreOrderCheck(IEnumerable<CluesBinaryTreeNode<char>> preOrderSequenceNode)
    {
        var count = 0;
        var cluesBinaryTreeNodes =
            preOrderSequenceNode as CluesBinaryTreeNode<char>[] ?? preOrderSequenceNode.ToArray();
        foreach (var currentNode in cluesBinaryTreeNodes)
        {
            if (count is 2 or 3 or 5 or 6)
            {
                CheckCluesGenericMethod(currentNode, count, cluesBinaryTreeNodes);
            }
            else
            {
                Assert.True(currentNode.LeftTag == 0);
                Assert.True(currentNode.RightTag == 0);
            }

            count++;
        }
    }

    [Fact]
    public void CheckAndCluesIt_InOrder()
    {
        var inOrderSequenceNode = _cluesBinaryTree.InOrderTraverse();
        var stringBuilder = new StringBuilder();
        foreach (var currentNode in inOrderSequenceNode)
        {
            stringBuilder.Append(currentNode.Element);
        }

        var traverseResult = stringBuilder.ToString();
        Assert.True(InOrderSequence == traverseResult);

        // 线索化应当成功
        _cluesBinaryTree.CluesGeneric(_cluesBinaryTree.InOrderTraverse);
        Assert.Equal(ThreadingOrder.InOrder, _cluesBinaryTree.CurrentThreadingOrder);
        // 中序遍历序列：DBEAFCG，能够线索化的节点：DEFG，索引为：0，2，4，6
        inOrderSequenceNode = _cluesBinaryTree.InOrderTraverse();
        InOrderCheck(inOrderSequenceNode);
    }

    private static void InOrderCheck(IEnumerable<CluesBinaryTreeNode<char>> inOrderSequenceNode)
    {
        var count = 0;
        var cluesBinaryTreeNodes = inOrderSequenceNode as CluesBinaryTreeNode<char>[] ?? inOrderSequenceNode.ToArray();
        foreach (var currentNode in cluesBinaryTreeNodes)
        {
            if (count % 2 == 0)
            {
                CheckCluesGenericMethod(currentNode, count, cluesBinaryTreeNodes);
            }
            else
            {
                Assert.True(currentNode.LeftTag == 0);
                Assert.True(currentNode.RightTag == 0);
            }

            count++;
        }
    }

    [Fact]
    public void CheckAndCluesIt_PostOrder()
    {
        var postOrderSequenceNode = _cluesBinaryTree.PostOrderTraverse();
        var stringBuilder = new StringBuilder();
        foreach (var currentNode in postOrderSequenceNode)
        {
            stringBuilder.Append(currentNode.Element);
        }

        var traverseResult = stringBuilder.ToString();
        Assert.True(PostOrderSequence == traverseResult);

        // 线索化应当成功
        _cluesBinaryTree.CluesGeneric(_cluesBinaryTree.PostOrderTraverse);
        Assert.Equal(ThreadingOrder.PostOrder, _cluesBinaryTree.CurrentThreadingOrder);
        // 后序遍历序列：DEBFGCA，能够线索化的节点：DEFG，索引为：0，1，3，4
        postOrderSequenceNode = _cluesBinaryTree.PostOrderTraverse();
        PostOrderCheck(postOrderSequenceNode);
    }

    private static void PostOrderCheck(IEnumerable<CluesBinaryTreeNode<char>> postOrderSequenceNode)
    {
        var count = 0;
        var cluesBinaryTreeNodes =
            postOrderSequenceNode as CluesBinaryTreeNode<char>[] ?? postOrderSequenceNode.ToArray();
        foreach (var currentNode in cluesBinaryTreeNodes)
        {
            if (count is 0 or 1 or 3 or 4)
            {
                CheckCluesGenericMethod(currentNode, count, cluesBinaryTreeNodes);
            }
            else
            {
                Assert.True(currentNode.LeftTag == 0);
                Assert.True(currentNode.RightTag == 0);
            }

            count++;
        }
    }

    [Fact]
    public void CheckAndCluesIt_LevelOrder()
    {
        var levelOrderSequenceNode = _cluesBinaryTree.LevelOrderTraverse();
        var stringBuilder = new StringBuilder();
        foreach (var currentNode in levelOrderSequenceNode)
        {
            stringBuilder.Append(currentNode.Element);
        }

        var traverseResult = stringBuilder.ToString();
        Assert.True(LevelOrderSequence == traverseResult);

        // 线索化应当成功
        _cluesBinaryTree.CluesGeneric(_cluesBinaryTree.LevelOrderTraverse);
        Assert.Equal(ThreadingOrder.LevelOrder, _cluesBinaryTree.CurrentThreadingOrder);
        // 层序遍历序列：ABCDEFG，能够线索化的节点：DEFG，索引为：3，4，5，6
        levelOrderSequenceNode = _cluesBinaryTree.LevelOrderTraverse();
        LevelOrderCheck(levelOrderSequenceNode);
    }

    private static void LevelOrderCheck(IEnumerable<CluesBinaryTreeNode<char>> postOrderSequenceNode)
    {
        var count = 0;
        var cluesBinaryTreeNodes =
            postOrderSequenceNode as CluesBinaryTreeNode<char>[] ?? postOrderSequenceNode.ToArray();
        foreach (var currentNode in cluesBinaryTreeNodes)
        {
            if (count is >= 3 and <= 6)
            {
                CheckCluesGenericMethod(currentNode, count, cluesBinaryTreeNodes);
            }
            else
            {
                Assert.True(currentNode.LeftTag == 0);
                Assert.True(currentNode.RightTag == 0);
            }

            count++;
        }
    }

    #endregion

    [Fact]
    public void RethreadingInDifferentOrdersPreservesEveryTraversal()
    {
        var expected = new Dictionary<ThreadingOrder, string>
        {
            [ThreadingOrder.PreOrder] = PreOrderSequence,
            [ThreadingOrder.InOrder] = InOrderSequence,
            [ThreadingOrder.PostOrder] = PostOrderSequence
        };

        foreach (var order in expected.Keys)
        {
            _cluesBinaryTree.Thread(order);
            var actual = order switch
            {
                ThreadingOrder.PreOrder => _cluesBinaryTree.PreOrderTraverse(),
                ThreadingOrder.InOrder => _cluesBinaryTree.InOrderTraverse(),
                ThreadingOrder.PostOrder => _cluesBinaryTree.PostOrderTraverse(),
                _ => throw new InvalidOperationException()
            };

            Assert.Equal(expected[order], string.Concat(actual.Select(node => node.Element)));
            Assert.Equal(order, _cluesBinaryTree.CurrentThreadingOrder);
        }
    }
}
