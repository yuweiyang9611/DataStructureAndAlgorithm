namespace DataStructureAndAlgorithm.Tree;

public sealed class HuffmanTreeNode<TElementType>(
    TElementType value,
    double weight = 1,
    HuffmanTreeNode<TElementType>? leftChild = null,
    HuffmanTreeNode<TElementType>? rightChild = null,
    HuffmanTreeNode<TElementType>? parent = null)
    where TElementType : notnull
{
    public double Weight { get; private set; } = weight;
    public HuffmanTreeNode<TElementType>? LeftChild { get; private set; } = leftChild;
    public HuffmanTreeNode<TElementType>? RightChild { get; private set; } = rightChild;
    public HuffmanTreeNode<TElementType>? Parent { get; set; } = parent;
    public TElementType Value { get; private set; } = value;
}
