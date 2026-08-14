namespace DataStructureAndAlgorithm.Tree;

/* 二叉搜索树要求插入节点中的元素可比较（在插入和删除时需要确定插入和删除的位置） */
public class BinarySearchTree<TElementType>(NormalBinaryTreeNode<TElementType>? rootNode = null)
    where TElementType : IComparable<TElementType>
{
    // 可空类型的默认值为null
    public NormalBinaryTreeNode<TElementType>? RootNode { get; private set; } = rootNode;


    #region User Defined Type Conversion: BianrySearchTree<T> -> NormalBinaryTree<T>

    // 类型转换：无法使用自定义显式类型转换(需要用到类成员，没办法将方法设置为static)
    //public static explicit operator NormalBinaryTree<TElementType>(BinarySearchTree<TElementType> obj)
    public NormalBinaryTree<TElementType> ConvertToNormalBinaryTree()
    {
        return new NormalBinaryTree<TElementType>(RootNode);
    }

    #endregion

    // RootNode置为空，二叉树中其他节点无法访问，GC会回收剩余节点。
    // 注：在非托管语言中，要手动释放为其他节点分配的空间，否则会造成内存泄漏
    public void Clear() => RootNode = null;

    public void Insert(TElementType element)
    {
        if (RootNode is null)
        {
            // 如果根节点为空，说明二叉搜索树中没有任何节点，构建根节点即可
            RootNode = new NormalBinaryTreeNode<TElementType>(element);
            return;
        }

        // 如果根节点不为空，那么开始遍历二叉搜索树，寻找新节点的插入位置
        var currentNode = RootNode;
        while (true)
        {
            // 如果插入的节点小于当前节点，走左子树
            if (element.CompareTo(currentNode.Element) < 0)
            {
                // 如果当前节点的左子树为空，说明这就是插入的位置
                if (currentNode.LeftChild is null)
                {
                    currentNode.LeftChild = new NormalBinaryTreeNode<TElementType>(element);
                    // 插入完成，停止循环
                    break;
                }

                // 如果当前节点的左子树不为空，需要考察这个左子树
                currentNode = currentNode.LeftChild;
            }
            // 如果插入的节点大于当前节点，走右子树
            else if (element.CompareTo(currentNode.Element) > 0)
            {
                // 如果当前节点的右子树为空，说明这就是插入的位置
                if (currentNode.RightChild is null)
                {
                    currentNode.RightChild = new NormalBinaryTreeNode<TElementType>(element);
                    // 插入完成，停止循环
                    break;
                }

                // 如果当前节点的右子树不为空，需要考察这个右子树
                currentNode = currentNode.RightChild;
            }
            else
            {
                Console.WriteLine("存在相同元素，已忽略");
                break;
            }
        }
    }

    public NormalBinaryTreeNode<TElementType>? Find(TElementType element,
        out NormalBinaryTreeNode<TElementType>? preNode)
    {
        preNode = null;
        var currentNode = RootNode;
        while (currentNode != null)
        {
            switch (element.CompareTo(currentNode.Element))
            {
                // 如果要查找的元素比当前节点存储的元素小就向左走
                case < 0:
                    preNode = currentNode;
                    currentNode = currentNode.LeftChild;
                    break;
                // 如果要查找的元素比当前节点存储的元素大就向右走
                case > 0:
                    preNode = currentNode;
                    currentNode = currentNode.RightChild;
                    break;
                default:
                    // 如果要查找的元素 Equals 当前节点存储的元素，说明找到了这个节点，返回这个节点
                    return currentNode;
            }
        }

        // 遍历了整个二叉树都没找到这个节点，就返回空
        return null;
    }

    private static NormalBinaryTreeNode<TElementType>? FindMax(NormalBinaryTreeNode<TElementType>? currentNode,
        out NormalBinaryTreeNode<TElementType>? preNode)
    {
        preNode = null;
        if (currentNode == null) return null;
        while (currentNode.RightChild != null)
        {
            preNode = currentNode;
            currentNode = currentNode.RightChild;
        }

        return currentNode;
    }

    /* 二叉树删除操作，可能有一下三种情况:
     * 1. 要删除的节点是叶子节点(直接删除即可)
     * 2. 要删除的节点只有一个孩子: 这个孩子直接替代要删除的节点
     * 3. 要删除的节点有两个孩子(两种选择):
     * *  ① 用左子树中最大节点的值替代要删除的节点的值，然后删除左子树中最大的节点(删除操作被转换为情况2或情况1)
     * *  ② 用右子树中最小节点的值替代要删除的节点的值，然后删除右子树中最小的节点(删除操作被转换为情况2或情况1)
     * */
    public void Delete(TElementType element)
    {
        var currentNode = Find(element, out var preNode);
        // 没有找到要删除的节点
        if (currentNode is null) return;
        // 要删除的节点是叶子节点
        if (currentNode.LeftChild == null && currentNode.RightChild == null)
        {
            DeleteLeafNodes(preNode, currentNode);
        }
        // 要删除的节点有一个孩子
        else if (currentNode.LeftChild == null || currentNode.RightChild == null)
        {
            DeleteNodesWithOneChild(preNode, currentNode);
        }
        // 要删除的节点有两个孩子
        else
        {
            DeleteNodesWithTwoChildren(currentNode);
        }
    }

    private void DeleteLeafNodes(NormalBinaryTreeNode<TElementType>? preNode,
        NormalBinaryTreeNode<TElementType> currentNode)
    {
        // 如果要删除的叶子节点是根节点，说明整个二叉搜索树只有一个元素，将二叉搜索树清空即可
        if (preNode == null)
        {
            Clear();
            return;
        }

        if (currentNode.Element == null || preNode.Element == null) throw new NullReferenceException();
        if (currentNode.Element.CompareTo(preNode.Element) < 0) preNode.LeftChild = null;
        else
        {
            preNode.RightChild = null;
        }
    }

    private void DeleteNodesWithOneChild(NormalBinaryTreeNode<TElementType>? preNode,
        NormalBinaryTreeNode<TElementType> currentNode)
    {
        // 如果要删除的是根节点，把根节点的孩子变为新的根节点
        if (preNode == null)
        {
            RootNode = currentNode.LeftChild ?? currentNode.RightChild;
            return;
        }

        var nextNode = currentNode.LeftChild ?? currentNode.RightChild;

        if (currentNode.Element == null || preNode.Element == null)
            throw new NullReferenceException("节点中存在空元素");

        if (nextNode == null) throw new NullReferenceException("执行了错误的分支，当前节点应该有1个孩子，现在为0个");

        if (preNode.LeftChild != null)
        {
            if (preNode.LeftChild.Element == null)
                throw new NullReferenceException($"{nameof(preNode.LeftChild.Element)}为空");
            if (preNode.LeftChild.Element.Equals(currentNode.Element))
            {
                preNode.LeftChild = nextNode;
                return;
            }
        }

        if (preNode.RightChild == null) throw new NullReferenceException("PreNode指向的节点错误(preNode至少有一个子树，现在只有0个子树)");
        if (preNode.RightChild.Element == null)
            throw new NullReferenceException($"{nameof(preNode.RightChild.Element)}为空");
        if (!preNode.RightChild.Element.Equals(currentNode.Element)) throw new Exception("PreNode指向的节点错误(节点中存储的值不匹配)");
        preNode.RightChild = nextNode;
    }

    private void DeleteNodesWithTwoChildren(NormalBinaryTreeNode<TElementType> currentNode)
    {
        if (currentNode.LeftChild == null || currentNode.RightChild == null)
            throw new NullReferenceException($"The left and right subtree of {nameof(currentNode)} should exist.");
        // Search the largest node in the left subtree of the current node
        var maxNodeInLeft = FindMax(currentNode.LeftChild, out var preNodeOfMaxNode);
        if (maxNodeInLeft is null)
            throw new NullReferenceException(
                $"The process entered the wrong branch and the left subtree of {nameof(currentNode)} should exist.");
        if (maxNodeInLeft.RightChild != null)
            throw new ArgumentException(
                $" {nameof(maxNodeInLeft)} is not the actual max node in the left subtree of {nameof(currentNode)} ");
        // Move the largest node in the left subtree up to the current node(Replace currentNode.Element with maxNodeInLeft.Element)
        currentNode.Element = maxNodeInLeft.Element;
        // 当前节点的直接左孩子可能就是左子树中的最大节点，此时它的父节点是 currentNode 而不是 null。
        preNodeOfMaxNode ??= currentNode;
        // Delete max node in the subtree of currentNode (using recursive method if the maxNode has two children).
        if (maxNodeInLeft.LeftChild != null)
        {
            DeleteNodesWithOneChild(preNodeOfMaxNode, maxNodeInLeft);
        }
        else
        {
            DeleteLeafNodes(preNodeOfMaxNode, maxNodeInLeft);
        }
    }
}
