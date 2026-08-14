using DataStructureAndAlgorithm.Tree;

namespace DataStructureAndAlgorithm.Test.TreeTest;

public class ConstructANormalBinaryTree
{
    private readonly List<char> _nodes = new();

    [Fact]
    public void BuildingABinaryTree_PreInOrderSequences_Test1()
    {
        _nodes.Clear();
        const string preOrderSequence = "DAEFBCHGI",
            inOrderSequence = "EAFDHCBGI",
            postOrderSequence = "EFAHCIGBD",
            levelOrderSequence = "DABEFCGHI";
        const string expectedString = preOrderSequence + inOrderSequence + postOrderSequence + levelOrderSequence;

        var binaryTreePreInOrder =
            ConstructANormalBinaryTreeIterativeSolution<char>.BuildingTreeIterativeSolutionPreInOrder(
                preOrderSequence.ToList(), inOrderSequence.ToList());
        Assert.NotNull(binaryTreePreInOrder);
        var binaryTreePostInOrder =
            ConstructANormalBinaryTreeIterativeSolution<char>.BuildingTreeIterativeSolutionPostInOrder(
                postOrderSequence.ToList(), inOrderSequence.ToList());
        Assert.NotNull(binaryTreePostInOrder);
        binaryTreePreInOrder.PreOrderTraverse(ShowTraverseResult);
        binaryTreePreInOrder.InOrderTraverse(ShowTraverseResult);
        binaryTreePreInOrder.PostOrderTraverse(ShowTraverseResult);
        binaryTreePreInOrder.LevelOrderTraverse(ShowTraverseResult);
        binaryTreePostInOrder.PreOrderTraverse(ShowTraverseResult);
        binaryTreePostInOrder.InOrderTraverse(ShowTraverseResult);
        binaryTreePostInOrder.PostOrderTraverse(ShowTraverseResult);
        binaryTreePostInOrder.LevelOrderTraverse(ShowTraverseResult);

        var stringBuilder = new StringBuilder();
        foreach (var node in _nodes)
        {
            stringBuilder.Append(node);
        }

        var actualString = stringBuilder.ToString();
        Assert.Equal(expectedString + expectedString, actualString);
        _nodes.Clear();
    }

    [Fact]
    public void BuildingABinaryTree_PreInOrderSequences_Test2()
    {
        _nodes.Clear();
        const string preOrderSequence = "ABDECFG",
            inOrderSequence = "DBEAFCG",
            postOrderSequence = "DEBFGCA",
            levelOrderSequence = "ABCDEFG";
        const string expectedString = preOrderSequence + inOrderSequence + postOrderSequence + levelOrderSequence;

        var binaryTreePreInOrder =
            ConstructANormalBinaryTreeIterativeSolution<char>.BuildingTreeIterativeSolutionPreInOrder(
                preOrderSequence.ToList(), inOrderSequence.ToList());
        Assert.NotNull(binaryTreePreInOrder);
        var binaryTreePostInOrder =
            ConstructANormalBinaryTreeIterativeSolution<char>.BuildingTreeIterativeSolutionPostInOrder(
                postOrderSequence.ToList(), inOrderSequence.ToList());
        Assert.NotNull(binaryTreePostInOrder);
        binaryTreePreInOrder.PreOrderTraverse(ShowTraverseResult);
        binaryTreePreInOrder.InOrderTraverse(ShowTraverseResult);
        binaryTreePreInOrder.PostOrderTraverse(ShowTraverseResult);
        binaryTreePreInOrder.LevelOrderTraverse(ShowTraverseResult);
        binaryTreePostInOrder.PreOrderTraverse(ShowTraverseResult);
        binaryTreePostInOrder.InOrderTraverse(ShowTraverseResult);
        binaryTreePostInOrder.PostOrderTraverse(ShowTraverseResult);
        binaryTreePostInOrder.LevelOrderTraverse(ShowTraverseResult);

        var stringBuilder = new StringBuilder();
        foreach (var node in _nodes) stringBuilder.Append(node);

        var actualString = stringBuilder.ToString();
        Assert.Equal(expectedString + expectedString, actualString);
        _nodes.Clear();
    }

    private void ShowTraverseResult(NormalBinaryTreeNode<char> currentNode) => _nodes.Add(currentNode.Element);

    [Fact]
    public void EmptySequences_ReturnAnEmptyTree()
    {
        Assert.Null(NormalBinaryTree<char>.BuildingABinaryTreeByPreInOrderSequences([], []));
        Assert.Null(ConstructANormalBinaryTreeIterativeSolution<char>
            .BuildingTreeIterativeSolutionPreInOrder([], []));
    }

    [Theory]
    [InlineData("AB", "A")]
    [InlineData("AAB", "ABA")]
    [InlineData("ABC", "ABD")]
    [InlineData("ACB", "BAC")]
    public void InvalidPreAndInOrderSequences_AreRejected(string preOrder, string inOrder)
    {
        Assert.Throws<ArgumentException>(() =>
            NormalBinaryTree<char>.BuildingABinaryTreeByPreInOrderSequences(
                preOrder.ToCharArray(), inOrder.ToCharArray()));
        Assert.Throws<ArgumentException>(() =>
            ConstructANormalBinaryTreeIterativeSolution<char>.BuildingTreeIterativeSolutionPreInOrder(
                preOrder.ToCharArray(), inOrder.ToCharArray()));
    }

    [Theory]
    [InlineData("AB", "A")]
    [InlineData("AAB", "ABA")]
    [InlineData("ABC", "ABD")]
    [InlineData("CBA", "BAC")]
    public void InvalidPostAndInOrderSequences_AreRejected(string postOrder, string inOrder)
    {
        Assert.Throws<ArgumentException>(() =>
            NormalBinaryTree<char>.BuildingABinaryTreeByPostInOrderSequences(
                postOrder.ToCharArray(), inOrder.ToCharArray()));
        Assert.Throws<ArgumentException>(() =>
            ConstructANormalBinaryTreeIterativeSolution<char>.BuildingTreeIterativeSolutionPostInOrder(
                postOrder.ToCharArray(), inOrder.ToCharArray()));
    }
}
