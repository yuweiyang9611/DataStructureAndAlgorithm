namespace DataStructureAndAlgorithm.Tree;

/// <summary>
/// 使用显式栈、根据两条遍历序列重建普通二叉树。
/// </summary>
/// <typeparam name="TElementType">唯一节点值的类型，只要求可进行相等性比较。</typeparam>
/// <remarks>
/// 旧实现使用了 <see cref="IComparable{T}"/> 约束，但算法从未比较大小，只比较相等性。
/// <c>notnull</c> 与 <see cref="EqualityComparer{T}"/> 更准确地表达了真实需求。
/// </remarks>
public static class ConstructANormalBinaryTreeIterativeSolution<TElementType>
    where TElementType : notnull
{
    /// <summary>
    /// 根据前序和中序序列重建二叉树，时间复杂度 O(n)，辅助空间 O(h)。
    /// </summary>
    public static NormalBinaryTree<TElementType>? BuildingTreeIterativeSolutionPreInOrder(
        IReadOnlyList<TElementType> preOrderSequence,
        IReadOnlyList<TElementType> inOrderSequence)
    {
        TraversalSequenceValidator.Validate(preOrderSequence, inOrderSequence);
        if (preOrderSequence.Count == 0) return null;

        var comparer = EqualityComparer<TElementType>.Default;
        var root = new NormalBinaryTreeNode<TElementType>(preOrderSequence[0]);
        var stack = new Stack<NormalBinaryTreeNode<TElementType>>();
        stack.Push(root);
        var inIndex = 0;

        for (var preIndex = 1; preIndex < preOrderSequence.Count; preIndex++)
        {
            var value = preOrderSequence[preIndex];
            var parent = stack.Peek();

            if (inIndex < inOrderSequence.Count &&
                !comparer.Equals(parent.Element, inOrderSequence[inIndex]))
            {
                // 前序的下一个节点仍在当前根的中序位置左侧，所以它是左孩子。
                parent.LeftChild = new NormalBinaryTreeNode<TElementType>(value);
                stack.Push(parent.LeftChild);
                continue;
            }

            // 当前中序节点已经匹配，沿栈回退越过完成的左子树和祖先，再接右孩子。
            while (stack.Count > 0 && inIndex < inOrderSequence.Count &&
                   comparer.Equals(stack.Peek().Element, inOrderSequence[inIndex]))
            {
                parent = stack.Pop();
                inIndex++;
            }

            parent.RightChild = new NormalBinaryTreeNode<TElementType>(value);
            stack.Push(parent.RightChild);
        }

        ConsumeCompletedNodes(stack, inOrderSequence, ref inIndex, comparer, fromRight: false);
        if (stack.Count != 0 || inIndex != inOrderSequence.Count)
        {
            throw new ArgumentException("前序序列与中序序列无法构成同一棵二叉树。", nameof(preOrderSequence));
        }

        return new NormalBinaryTree<TElementType>(root);
    }

    /// <summary>
    /// 根据后序和中序序列重建二叉树，时间复杂度 O(n)，辅助空间 O(h)。
    /// </summary>
    public static NormalBinaryTree<TElementType>? BuildingTreeIterativeSolutionPostInOrder(
        IReadOnlyList<TElementType> postOrderSequence,
        IReadOnlyList<TElementType> inOrderSequence)
    {
        TraversalSequenceValidator.Validate(postOrderSequence, inOrderSequence);
        if (postOrderSequence.Count == 0) return null;

        var comparer = EqualityComparer<TElementType>.Default;
        var lastIndex = postOrderSequence.Count - 1;
        var root = new NormalBinaryTreeNode<TElementType>(postOrderSequence[lastIndex]);
        var stack = new Stack<NormalBinaryTreeNode<TElementType>>();
        stack.Push(root);
        var inIndex = inOrderSequence.Count - 1;

        for (var postIndex = lastIndex - 1; postIndex >= 0; postIndex--)
        {
            var value = postOrderSequence[postIndex];
            var parent = stack.Peek();

            if (inIndex >= 0 && !comparer.Equals(parent.Element, inOrderSequence[inIndex]))
            {
                // 反向扫描后序时先遇到右子树，因此这里与前序算法完全镜像。
                parent.RightChild = new NormalBinaryTreeNode<TElementType>(value);
                stack.Push(parent.RightChild);
                continue;
            }

            while (stack.Count > 0 && inIndex >= 0 &&
                   comparer.Equals(stack.Peek().Element, inOrderSequence[inIndex]))
            {
                parent = stack.Pop();
                inIndex--;
            }

            parent.LeftChild = new NormalBinaryTreeNode<TElementType>(value);
            stack.Push(parent.LeftChild);
        }

        ConsumeCompletedNodes(stack, inOrderSequence, ref inIndex, comparer, fromRight: true);
        if (stack.Count != 0 || inIndex != -1)
        {
            throw new ArgumentException("后序序列与中序序列无法构成同一棵二叉树。", nameof(postOrderSequence));
        }

        return new NormalBinaryTree<TElementType>(root);
    }

    /// <summary>
    /// 构造结束后继续消费栈，确保整条中序序列都被精确匹配。
    /// 这一步能识别“长度与集合相同，但左右子树分区矛盾”的输入。
    /// </summary>
    private static void ConsumeCompletedNodes(
        Stack<NormalBinaryTreeNode<TElementType>> stack,
        IReadOnlyList<TElementType> inOrderSequence,
        ref int inIndex,
        IEqualityComparer<TElementType> comparer,
        bool fromRight)
    {
        while (stack.Count > 0 && inIndex >= 0 && inIndex < inOrderSequence.Count &&
               comparer.Equals(stack.Peek().Element, inOrderSequence[inIndex]))
        {
            stack.Pop();
            inIndex += fromRight ? -1 : 1;
        }
    }
}
