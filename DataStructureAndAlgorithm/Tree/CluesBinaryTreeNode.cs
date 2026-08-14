namespace DataStructureAndAlgorithm.Tree;

/// <summary>
/// 线索二叉树节点。
/// </summary>
/// <remarks>
/// <para><see cref="LeftTag"/> 为 0 时 <see cref="LeftChild"/> 是真实左孩子，为 1 时是遍历前驱。</para>
/// <para><see cref="RightTag"/> 为 0 时 <see cref="RightChild"/> 是真实右孩子，为 1 时是遍历后继。</para>
/// <para><see cref="Parent"/> 让后序线索遍历能在 O(1) 额外空间内从子节点回到父节点。</para>
/// </remarks>
public sealed class CluesBinaryTreeNode<TElementType> where TElementType : notnull
{
    public CluesBinaryTreeNode<TElementType>? LeftChild;
    public int LeftTag;
    public TElementType? Element;
    public int RightTag;
    public CluesBinaryTreeNode<TElementType>? RightChild;

    public CluesBinaryTreeNode<TElementType>? Parent { get; internal set; }

    public CluesBinaryTreeNode(
        TElementType? element,
        CluesBinaryTreeNode<TElementType>? leftChild = null,
        int leftTag = 0,
        CluesBinaryTreeNode<TElementType>? rightChild = null,
        int rightTag = 0)
    {
        Element = element;
        LeftChild = leftChild;
        LeftTag = leftTag;
        RightChild = rightChild;
        RightTag = rightTag;

        if (leftTag == 0 && leftChild is not null) leftChild.Parent = this;
        if (rightTag == 0 && rightChild is not null) rightChild.Parent = this;
    }

    public CluesBinaryTreeNode()
    {
    }
}
