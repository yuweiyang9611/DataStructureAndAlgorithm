namespace DataStructureAndAlgorithm.Tree;

public sealed class AvlTreeNode<TElementType>(
    TElementType element,
    AvlTreeNode<TElementType>? leftChild = null,
    AvlTreeNode<TElementType>? rightChild = null)
    where TElementType : IComparable<TElementType>
{
    public TElementType Element = element;
    public AvlTreeNode<TElementType>? LeftChild = leftChild;

    public AvlTreeNode<TElementType>? RightChild = rightChild;

    // In order to determine whether the AVL tree is in balance and to correct the ALV tree in case of imbalance,
    // the height of the tree should be recorded in each node.

    private int _treeHeight = 1;

    public int TreeHeight
    {
        get => _treeHeight;
        set
        {
            if (value < 1) throw new ArgumentException("treeHeight must bigger than 0");
            _treeHeight = value;
        }
    }
}
