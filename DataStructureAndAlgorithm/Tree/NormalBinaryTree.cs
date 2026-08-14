namespace DataStructureAndAlgorithm.Tree;

public class NormalBinaryTree<TElementType>(NormalBinaryTreeNode<TElementType>? rootNode = null)
    where TElementType : notnull
{
    public NormalBinaryTreeNode<TElementType>? RootNode { get; } = rootNode;


    #region NormalBinaryTreeTraverseMethods

    public delegate void NormalBinaryNodeOperator(NormalBinaryTreeNode<TElementType> currentNode);

    // 根、左、右
    public void PreOrderTraverse(NormalBinaryNodeOperator visit)
    {
        ArgumentNullException.ThrowIfNull(visit);

        // 初始化栈
        var stack = new Stack<NormalBinaryTreeNode<TElementType>>();
        // 定义遍历指针
        var currentNode = RootNode;
        // 开始遍历
        while (currentNode != null || stack.Count != 0)
        {
            // 如果当前节点不为空
            if (currentNode != null)
            {
                // 访问当前节点
                visit(currentNode);
                // 当前节点入栈
                stack.Push(currentNode);
                // 继续访问当前节点的左子树
                currentNode = currentNode.LeftChild;
            }
            else
            {
                // 当前节点为空且栈中还有节点存在，这些节点还有右子树未处理
                // 栈顶节点出栈并访问
                currentNode = stack.Pop();
                // 遍历该节点的右子树
                currentNode = currentNode.RightChild;
            }
        }
    }

    // InOrder遍历操作与PreOrder相似，只是将访问根节点的操作推迟到左子树遍历完成之后
    public void InOrderTraverse(NormalBinaryNodeOperator visit)
    {
        ArgumentNullException.ThrowIfNull(visit);

        var stack = new Stack<NormalBinaryTreeNode<TElementType>>();
        var currentNode = RootNode;
        while (currentNode != null || stack.Count != 0)
        {
            if (currentNode != null)
            {
                stack.Push(currentNode);
                currentNode = currentNode.LeftChild;
            }
            else
            {
                // 栈顶元素出栈并访问
                currentNode = stack.Pop();
                visit(currentNode);
                currentNode = currentNode.RightChild;
            }
        }
    }

    /// <summary>
    /// <para>
    /// 相比于PreOrder和InOrder，PostOrder的遍历顺序为：左、右、根，这就涉及到回源的问题：
    /// 1. 对于一个节点，如果它的右子树存在且被访问过，则访问当前节点(左子树回源)
    /// 2. 如果它的右子树存在且未被访问过就访问当前节点的右子树(右子树回源)
    /// 3. 如果它的右子树不存在则访问当前节点(没有右子树)
    /// </para>
    /// <para>
    /// 综上，需要一个preNode指针，记录上一次访问的节点：
    /// 1. 如果上一次访问的节点不是当前节点的右子树，那么继续访问当前节点的右子树(左子树回源)
    /// 2. 如果上一次访问的节点是当前节点的右子树，那么访问当前节点(右子树回源)
    /// </para>
    /// </summary>
    /// <param name="visit">一个自定义委托，用于在遍历时对当前节点进行操作<see cref="NormalBinaryNodeOperator"/></param>
    public void PostOrderTraverse(NormalBinaryNodeOperator visit)
    {
        ArgumentNullException.ThrowIfNull(visit);

        NormalBinaryTreeNode<TElementType>? preNode = null;
        var stack = new Stack<NormalBinaryTreeNode<TElementType>>();
        var currentNode = RootNode;
        while (currentNode != null || stack.Count != 0)
        {
            if (currentNode != null)
            {
                stack.Push(currentNode);
                currentNode = currentNode.LeftChild;
            }
            else // 左子树遍历完成
            {
                // 判断栈顶元素的右子树是否被访问过[不出栈]
                currentNode = stack.Peek();
                // 栈顶元素的右子树存在且未被访问过
                if (currentNode.RightChild != null && currentNode.RightChild != preNode)
                {
                    // 继续遍历右子树
                    currentNode = currentNode.RightChild;
                }
                else // 当前节点的右子树不存在或已经被访问过
                {
                    // 出栈并访问当前节点
                    currentNode = stack.Pop();
                    visit(currentNode);
                    // 记录最近访问的节点
                    preNode = currentNode;
                    // 以当前结点为根的左右子树已经遍历完成，需要将遍历指针置空以便开始新一轮遍历
                    currentNode = null;
                }
            }
        }
    }

    /// <summary>
    /// levelOrder与preOrder、inOrder、postOrder的深度优先不同，levelOrder是广度优先的，需要先进先出的队列而不是先进后出的栈
    /// </summary>
    /// <param name="visit"></param>
    public void LevelOrderTraverse(NormalBinaryNodeOperator visit)
    {
        ArgumentNullException.ThrowIfNull(visit);

        var queue = new Queue<NormalBinaryTreeNode<TElementType>>();
        var currentNode = RootNode;
        while (currentNode != null)
        {
            // 访问当前节点
            visit(currentNode);
            // 将当前节点的左子树插入队列末尾(如果左子树存在)
            if (currentNode.LeftChild != null) queue.Enqueue(currentNode.LeftChild);
            // 将当前节点的右子树插入队列末尾(如果右子树存在)
            if (currentNode.RightChild != null) queue.Enqueue(currentNode.RightChild);
            // 如果队列为空则返回
            if (queue.Count == 0) return;
            // 队头节点出栈并访问
            currentNode = queue.Dequeue();
        }
    }

    #endregion

    #region CopyNormalBinaryTreeMethods

    // 二叉树复制的核心思想是同步遍历，即：
    // 1. 原二叉树访问节点，新二叉树也访问节点并复制原节点中的元素值Element(地址不能复制，要不然成浅复制了)
    // 2. 原二叉树找左子树，新二叉树也找左子树，没有就创建一个新节点作为左子树。右子树同理。
    // 3. 无论采用PreOrder、InOrder、PostOrder还是LevelOrder，均可完整复制出新的二叉树，下面仅以先序和中序为例：
    public NormalBinaryTree<TElementType> BinaryTreeCopyPreOrder() => BinaryTreeCopy(PreOrderCopy);
    public NormalBinaryTree<TElementType> BinaryTreeCopyInOrder() => BinaryTreeCopy(InOrderCopy);

    private delegate void TraverseMethodOfCopyingNode(NormalBinaryTreeNode<TElementType> newRootNode);

    private NormalBinaryTree<TElementType> BinaryTreeCopy(TraverseMethodOfCopyingNode traverseMethodOfCopyingNode)
    {
        if (RootNode == null) return new NormalBinaryTree<TElementType>();
        var newRootNode = new NormalBinaryTreeNode<TElementType>(RootNode.Element);
        traverseMethodOfCopyingNode(newRootNode);
        return new NormalBinaryTree<TElementType>(newRootNode);
    }

    private void PreOrderCopy(NormalBinaryTreeNode<TElementType> newRootNode)
    {
        var oldBinaryTreeCurrentNode = RootNode;
        var newBinaryTreeCurrentNode = newRootNode;
        var oldBinaryTreeTraverseStack = new Stack<NormalBinaryTreeNode<TElementType>>();
        var newBinaryTreeTraverseStack = new Stack<NormalBinaryTreeNode<TElementType>>();

        // 开始先序同步遍历
        while (oldBinaryTreeCurrentNode != null || oldBinaryTreeTraverseStack.Count != 0)
        {
            if (oldBinaryTreeCurrentNode != null)
            {
                // 当前节点入栈
                oldBinaryTreeTraverseStack.Push(oldBinaryTreeCurrentNode);
                newBinaryTreeTraverseStack.Push(newBinaryTreeCurrentNode);
                // 遍历该节点的左子树
                oldBinaryTreeCurrentNode = oldBinaryTreeCurrentNode.LeftChild;
                // 如果旧二叉树的左子树存在，就创建新的节点作为新二叉树根节点的左子树，然后同步遍历左子树
                // 如果不存在就说明左子树遍历完成，不需要再创建新节点了
                if (oldBinaryTreeCurrentNode == null) continue;
                newBinaryTreeCurrentNode.LeftChild =
                    new NormalBinaryTreeNode<TElementType>(oldBinaryTreeCurrentNode.Element);
                newBinaryTreeCurrentNode = newBinaryTreeCurrentNode.LeftChild;
            }
            else
            {
                // 当前节点为空且栈中还有节点存在，这些节点还有右子树未处理
                oldBinaryTreeCurrentNode = oldBinaryTreeTraverseStack.Pop();
                newBinaryTreeCurrentNode = newBinaryTreeTraverseStack.Pop();
                // 遍历该节点的右子树
                oldBinaryTreeCurrentNode = oldBinaryTreeCurrentNode.RightChild;
                // 如果旧二叉树的左子树存在，就创建新的节点作为新二叉树根节点的右子树，然后同步遍历右子树
                // 如果不存在就说明右子树遍历完成，不需要再创建新节点了
                if (oldBinaryTreeCurrentNode == null) continue;
                newBinaryTreeCurrentNode.RightChild =
                    new NormalBinaryTreeNode<TElementType>(oldBinaryTreeCurrentNode.Element);
                newBinaryTreeCurrentNode = newBinaryTreeCurrentNode.RightChild;
            }
        }
    }

    private void InOrderCopy(NormalBinaryTreeNode<TElementType> newRootNode)
    {
        var oldBinaryTreeCurrentNode = RootNode;
        var newBinaryTreeCurrentNode = newRootNode;
        var oldBinaryTreeTraverseStack = new Stack<NormalBinaryTreeNode<TElementType>>();
        var newBinaryTreeTraverseStack = new Stack<NormalBinaryTreeNode<TElementType>>();

        // 开始中序同步遍历
        while (oldBinaryTreeCurrentNode != null || oldBinaryTreeTraverseStack.Count != 0)
        {
            if (oldBinaryTreeCurrentNode != null)
            {
                // 当前节点入栈
                oldBinaryTreeTraverseStack.Push(oldBinaryTreeCurrentNode);
                newBinaryTreeTraverseStack.Push(newBinaryTreeCurrentNode);
                // 遍历该节点的左子树
                oldBinaryTreeCurrentNode = oldBinaryTreeCurrentNode.LeftChild;
                // 如果旧二叉树的左子树存在，就创建新的节点作为新二叉树根节点的左子树，然后同步遍历左子树
                // 如果不存在就说明左子树遍历完成，不需要再创建新节点了
                if (oldBinaryTreeCurrentNode == null) continue;
                newBinaryTreeCurrentNode.LeftChild =
                    new NormalBinaryTreeNode<TElementType>(oldBinaryTreeCurrentNode.Element);
                newBinaryTreeCurrentNode = newBinaryTreeCurrentNode.LeftChild;
            }
            else
            {
                // 当前节点为空且栈中还有节点存在，这些节点还有右子树未处理
                oldBinaryTreeCurrentNode = oldBinaryTreeTraverseStack.Pop();
                newBinaryTreeCurrentNode = newBinaryTreeTraverseStack.Pop();
                // 在处理这些节点的右子树之前先访问这些节点
                newBinaryTreeCurrentNode.Element = oldBinaryTreeCurrentNode.Element;
                // 遍历该节点的右子树
                oldBinaryTreeCurrentNode = oldBinaryTreeCurrentNode.RightChild;
                // 如果旧二叉树的左子树存在，就创建新的节点作为新二叉树根节点的右子树，然后同步遍历右子树
                // 如果不存在就说明右子树遍历完成，不需要再创建新节点了
                if (oldBinaryTreeCurrentNode == null) continue;
                newBinaryTreeCurrentNode.RightChild =
                    new NormalBinaryTreeNode<TElementType>(oldBinaryTreeCurrentNode.Element);
                newBinaryTreeCurrentNode = newBinaryTreeCurrentNode.RightChild;
            }
        }
    }

    #endregion

    #region Construct a binary tree by traverse sequences

    // 通过中序+任意其他的遍历序列可以唯一确定一颗二叉树
    public static NormalBinaryTree<TElementType>? BuildingABinaryTreeByPreInOrderSequences(
        IReadOnlyList<TElementType> preOrderSequence,
        IReadOnlyList<TElementType> inOrderSequence)
    {
        ArgumentNullException.ThrowIfNull(preOrderSequence);
        ArgumentNullException.ThrowIfNull(inOrderSequence);

        return BuildingABinaryTreeCore(preOrderSequence, inOrderSequence, RecursiveMethod_PreInOrder);
    }

    public static NormalBinaryTree<TElementType>? BuildingABinaryTreeByPostInOrderSequences(
        IReadOnlyList<TElementType> postOrderSequence,
        IReadOnlyList<TElementType> inOrderSequence)
    {
        ArgumentNullException.ThrowIfNull(postOrderSequence);
        ArgumentNullException.ThrowIfNull(inOrderSequence);

        return BuildingABinaryTreeCore(postOrderSequence, inOrderSequence, RecursiveMethod_PostInOrder);
    }

    /// <summary>
    /// 通过遍历序列求解二叉树的公共步骤
    /// </summary>
    /// <param name="anotherSequence">另一个遍历序列,可以是先序、后序或层序</param>
    /// <param name="inOrderSequence">中序遍历序列</param>
    /// <param name="recursiveFunc">具体的递归求解步骤</param>
    /// <returns>求解得到的二叉树</returns>
    private static NormalBinaryTree<TElementType>? BuildingABinaryTreeCore(
        IReadOnlyList<TElementType> anotherSequence, IReadOnlyList<TElementType> inOrderSequence,
        Func<IReadOnlyList<TElementType>, int, int,
            IReadOnlyDictionary<TElementType, int>, int, int,
            NormalBinaryTreeNode<TElementType>?> recursiveFunc)
    {
        // 空序列表示空树；其他非法输入不应伪装成空树，而应给调用者明确的异常。
        var inOrderSequenceCache = TraversalSequenceValidator.Validate(anotherSequence, inOrderSequence);
        if (anotherSequence.Count == 0) return null;

        // 开始递归
        var rootNode = recursiveFunc.Invoke(anotherSequence, 0, anotherSequence.Count - 1,
            inOrderSequenceCache, 0, inOrderSequence.Count - 1);
        return rootNode is null ? null : new NormalBinaryTree<TElementType>(rootNode);
    }

    private static NormalBinaryTreeNode<TElementType>? RecursiveMethod_PreInOrder(
        IReadOnlyList<TElementType> preOrderSequence, int preLeft, int preRight,
        IReadOnlyDictionary<TElementType, int> inorderSequenceDic, int inLeft, int inRight
    )
    {
        // 递归终止条件
        if (preLeft > preRight || inLeft > inRight) return null;
        // 对于前序序列，根节点就是序列的左端点
        var rootValue = preOrderSequence[preLeft];
        // 在中序序列中找到对于根节点的位置，用于确定划分左子树和右子树
        var pIndex = inorderSequenceDic[rootValue];
        if (pIndex < inLeft || pIndex > inRight)
        {
            throw new ArgumentException("前序序列与中序序列的左右子树分区不一致。", nameof(preOrderSequence));
        }
        // 构建这一根节点
        var rootNode = new NormalBinaryTreeNode<TElementType>(rootValue)
        {
            // 递归构造这一节点的左子树和右子树
            LeftChild = RecursiveMethod_PreInOrder(preOrderSequence, preLeft + 1, pIndex - inLeft + preLeft,
                inorderSequenceDic, inLeft, pIndex - 1),
            RightChild = RecursiveMethod_PreInOrder(preOrderSequence, pIndex - inLeft + preLeft + 1, preRight,
                inorderSequenceDic, pIndex + 1, inRight)
        };
        // 返回根节点
        return rootNode;
    }

    private static NormalBinaryTreeNode<TElementType>? RecursiveMethod_PostInOrder(
        IReadOnlyList<TElementType> postOrderSequence, int postLeft, int postRight,
        IReadOnlyDictionary<TElementType, int> inorderSequenceDic, int inLeft, int inRight
    )
    {
        // 递归终止条件
        if (postLeft > postRight || inLeft > inRight) return null;
        // 对于后序序列，根节点就是序列的右端点
        var rootValue = postOrderSequence[postRight];
        // 在中序序列中找到对于根节点的位置，用于确定划分左子树和右子树
        var pIndex = inorderSequenceDic[rootValue];
        if (pIndex < inLeft || pIndex > inRight)
        {
            throw new ArgumentException("后序序列与中序序列的左右子树分区不一致。", nameof(postOrderSequence));
        }
        // 构建这一根节点
        var rootNode = new NormalBinaryTreeNode<TElementType>(rootValue)
        {
            // 递归构造这一节点的左子树和右子树
            LeftChild = RecursiveMethod_PostInOrder(postOrderSequence, postLeft, postLeft + pIndex - inLeft - 1,
                inorderSequenceDic, inLeft, pIndex - 1),
            RightChild = RecursiveMethod_PostInOrder(postOrderSequence, postLeft + pIndex - inLeft, postRight - 1,
                inorderSequenceDic, pIndex + 1, inRight)
        };
        // 返回根节点
        return rootNode;
    }

    #endregion
}
