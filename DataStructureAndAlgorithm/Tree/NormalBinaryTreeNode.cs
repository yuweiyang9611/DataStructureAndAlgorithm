namespace DataStructureAndAlgorithm.Tree;

public sealed class NormalBinaryTreeNode<TElementType>(
    TElementType element,
    NormalBinaryTreeNode<TElementType>? leftChild = null,
    NormalBinaryTreeNode<TElementType>? rightChild = null)
    where TElementType : notnull
{
    public TElementType Element = element;
    public NormalBinaryTreeNode<TElementType>? LeftChild = leftChild;
    public NormalBinaryTreeNode<TElementType>? RightChild = rightChild;
}
