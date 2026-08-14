namespace DataStructureAndAlgorithm.Tree;

/* Avl树要求插入节点中的元素可比较（需要实时调整节点位置或节点中的元素值） */
public class AvlTree<TElementType>(AvlTreeNode<TElementType>? rootNode = null)
    // 这里的IComparable<TElementType>隐式实现了IComparable接口，
    // IComparable接口与IComparable<Object>接口等价，TElementType的基类一定是Object
    where TElementType : IComparable<TElementType>
{
    public AvlTreeNode<TElementType>? RootNode { get; private set; } = rootNode;

    #region Convert

    public static explicit operator NormalBinaryTree<TElementType>?(AvlTree<TElementType>? avlTree)
    {
        if (avlTree is null) return null;
        if (avlTree.RootNode is null) return new NormalBinaryTree<TElementType>();
        var normalBinaryTreeRootNode = new NormalBinaryTreeNode<TElementType>(avlTree.RootNode.Element);

        //  同步遍历
        var stackOfNormalBinaryTree = new Stack<NormalBinaryTreeNode<TElementType>>();
        var stackOfAvlTree = new Stack<AvlTreeNode<TElementType>>();
        var currentNodeOfNormalBinaryTree = normalBinaryTreeRootNode;
        var currentNodeOfAvlTree = avlTree.RootNode;

        // PreOrder
        while (currentNodeOfAvlTree != null || stackOfAvlTree.Count != 0)
        {
            // 如果当前节点不为空
            if (currentNodeOfAvlTree != null)
            {
                // 访问当前节点：不需要对节点进行任何操作

                // 当前节点入栈
                stackOfAvlTree.Push(currentNodeOfAvlTree);
                stackOfNormalBinaryTree.Push(currentNodeOfNormalBinaryTree);
                // 继续访问当前节点的左子树
                currentNodeOfAvlTree = currentNodeOfAvlTree.LeftChild;
                // 如果当前节点的左子树不存在，就不需要同步访问左子树了
                if (currentNodeOfAvlTree is null) continue;
                // 如果当前节点的左子树存在，需要同步访问左子树
                currentNodeOfNormalBinaryTree.LeftChild =
                    new NormalBinaryTreeNode<TElementType>(currentNodeOfAvlTree.Element);
                currentNodeOfNormalBinaryTree = currentNodeOfNormalBinaryTree.LeftChild;
            }
            else
            {
                // 当前节点为空且栈中还有节点存在，这些节点还有右子树未处理
                // 栈顶节点出栈并访问
                currentNodeOfAvlTree = stackOfAvlTree.Pop();
                currentNodeOfNormalBinaryTree = stackOfNormalBinaryTree.Pop();
                // 遍历该节点的右子树
                currentNodeOfAvlTree = currentNodeOfAvlTree.RightChild;
                // 如果当前节点的右子树不存在，就不需要同步访问右子树
                if (currentNodeOfAvlTree is null) continue;
                // 如果当前节点的右子树存在，需要同步访问右子树
                currentNodeOfNormalBinaryTree.RightChild =
                    new NormalBinaryTreeNode<TElementType>(currentNodeOfAvlTree.Element);
                currentNodeOfNormalBinaryTree = currentNodeOfNormalBinaryTree.RightChild;
            }
        }

        return new NormalBinaryTree<TElementType>(normalBinaryTreeRootNode);
    }

    #endregion

    # region Update The Height of Nodes

    /// <summary>
    /// 获取一个节点的树高，空节点的树高为0
    /// </summary>
    /// <param name="node">AVL树的一个节点</param>
    /// <returns>这个节点的树的高度</returns>
    private static int GetHeightOfCurrentNode(AvlTreeNode<TElementType>? node) => node?.TreeHeight ?? 0;

    /// <summary>
    /// 计算单一节点的树的高度，依赖于这一节点的左右子树的树高，如果其左右节点的树高不准确，则计算出错
    /// </summary>
    /// <param name="node">AVL树的一个节点</param>
    /// <exception cref="ArgumentNullException">如果接收到空节点，就抛出异常</exception>
    /// <returns>这个节点的树的高度</returns>
    private static int UpdateTreeHeightOfCurrentNode(AvlTreeNode<TElementType>? node)
    {
        if (node is null) throw new ArgumentNullException(nameof(node), $"{nameof(node)}不能为null");
        var treeHeight = Math.Max(GetHeightOfCurrentNode(node.LeftChild), GetHeightOfCurrentNode(node.RightChild)) + 1;
        return treeHeight;
    }

    /// <summary>
    /// 从一个节点开始，递归计算其所有子树中节点的高度，递归算法
    /// </summary>
    /// <param name="node">树的根节点</param>
    /// <returns>根节点的树高</returns>
    private static int UpdateTreeHeightRecursion(AvlTreeNode<TElementType>? node)
    {
        if (node is null) return 0;
        var leftHeight = UpdateTreeHeightRecursion(node.LeftChild);
        var rightHeight = UpdateTreeHeightRecursion(node.RightChild);
        var depth = (leftHeight > rightHeight ? leftHeight : rightHeight) + 1;
        node.TreeHeight = depth;
        return depth;
    }

    /// <summary>
    /// 从一个节点开始，递归计算并更新自身及其所有子节点所存储的树高，迭代算法
    /// </summary>
    /// <param name="node">树的根节点</param>
    /// <returns>根节点的树高</returns>
    private static int UpdateTreeHeightIteration(AvlTreeNode<TElementType>? node)
    {
        if (node is null) return 0;
        // 层序遍历获得整个树的结构
        var stack = LevelOrder(node);
        // 自底向上更新树高
        foreach (var item in stack) item.TreeHeight = UpdateTreeHeightOfCurrentNode(item);
        return node.TreeHeight;
    }

    /// <summary>
    /// 层序遍历，用于辅助<see cref="UpdateTreeHeightIteration"/>非递归的计算节点高度
    /// </summary>
    /// <param name="node">根节点的树高</param>
    /// <returns>层序遍历得到的栈，利用这个栈可以自底向上更新所有节点的树高</returns>
    private static Stack<AvlTreeNode<TElementType>> LevelOrder(AvlTreeNode<TElementType> node)
    {
        var queue = new Queue<AvlTreeNode<TElementType>>();
        var stack = new Stack<AvlTreeNode<TElementType>>();
        var currentNode = node;
        queue.Enqueue(currentNode);
        while (queue.Count != 0)
        {
            currentNode = queue.Dequeue();
            stack.Push(currentNode);
            if (currentNode.LeftChild != null) queue.Enqueue(currentNode.LeftChild);
            if (currentNode.RightChild != null) queue.Enqueue(currentNode.RightChild);
        }

        return stack;
    }

    # endregion

    #region Rotate Operations

    private readonly NullReferenceException _nullReferenceException = new("wrong rotation block");

    /// <summary>
    /// AVL树平衡调整——左旋
    /// </summary>
    /// <param name="rootNodeOfRotationBlock">平衡调整块的根节点</param>
    /// <returns>调整后的平衡调整块的根节点</returns>
    private AvlTreeNode<TElementType> LeftRotation(AvlTreeNode<TElementType> rootNodeOfRotationBlock)
    {
        var newRootNode = rootNodeOfRotationBlock.RightChild ?? throw _nullReferenceException;
        rootNodeOfRotationBlock.RightChild = newRootNode.LeftChild;
        newRootNode.LeftChild = rootNodeOfRotationBlock;

        // 树高调整，动了新旧根节点的左右子树，只需要调整这两个节点的树高
        // 旧的根节点成了新根节点的子树，所以先重新计算旧根节点的树高
        rootNodeOfRotationBlock.TreeHeight = UpdateTreeHeightOfCurrentNode(rootNodeOfRotationBlock);
        newRootNode.TreeHeight = UpdateTreeHeightOfCurrentNode(newRootNode);

        return newRootNode;
    }

    /// <summary>
    /// AVL树平衡调整——右旋
    /// </summary>
    /// <param name="rootNodeOfRotationBlock">平衡调整块的根节点</param>
    /// <returns>调整后的平衡调整块的根节点</returns>
    private AvlTreeNode<TElementType> RightRotation(AvlTreeNode<TElementType> rootNodeOfRotationBlock)
    {
        var newRootNode = rootNodeOfRotationBlock.LeftChild ?? throw _nullReferenceException;
        rootNodeOfRotationBlock.LeftChild = newRootNode.RightChild;
        newRootNode.RightChild = rootNodeOfRotationBlock;

        // 树高调整，动了新旧根节点的左右子树，只需要调整这两个节点的树高
        // 旧的根节点成了新根节点的子树，所以先重新计算旧根节点的树高
        rootNodeOfRotationBlock.TreeHeight = UpdateTreeHeightOfCurrentNode(rootNodeOfRotationBlock);
        newRootNode.TreeHeight = UpdateTreeHeightOfCurrentNode(newRootNode);

        return newRootNode;
    }

    // RR型不平衡调整
    private AvlTreeNode<TElementType> RR_Type(AvlTreeNode<TElementType>? rootNodeOfRotationBlock)
    {
        if (rootNodeOfRotationBlock is null) throw _nullReferenceException;
        return LeftRotation(rootNodeOfRotationBlock);
    }

    // LL型不平衡调整
    private AvlTreeNode<TElementType> LL_Type(AvlTreeNode<TElementType>? rootNodeOfRotationBlock)
    {
        if (rootNodeOfRotationBlock is null) throw _nullReferenceException;
        return RightRotation(rootNodeOfRotationBlock);
    }

    // LR型调整
    private AvlTreeNode<TElementType> LR_Type(AvlTreeNode<TElementType>? rootNodeOfRotationBlock)
    {
        if (rootNodeOfRotationBlock is null) throw _nullReferenceException;
        // 先让根结点左边的部分进行一次左旋
        rootNodeOfRotationBlock.LeftChild =
            LeftRotation(rootNodeOfRotationBlock.LeftChild ?? throw _nullReferenceException);
        // 然后根节点再右旋
        return RightRotation(rootNodeOfRotationBlock);
    }

    // RL型调整
    private AvlTreeNode<TElementType> RL_Type(AvlTreeNode<TElementType>? rootNodeOfRotationBlock)
    {
        if (rootNodeOfRotationBlock is null) throw _nullReferenceException;
        // 先让根结点右边的部分进行一次右旋
        rootNodeOfRotationBlock.RightChild =
            RightRotation(rootNodeOfRotationBlock.RightChild ?? throw _nullReferenceException);
        // 然后根节点再右旋
        return LeftRotation(rootNodeOfRotationBlock);
    }

    #endregion

    #region CURD

    public void Clear() => RootNode = null;

    /// <summary>
    /// 在AVL树中查找元素所在节点
    /// </summary>
    /// <param name="element">要查找的元素</param>
    /// <returns>如果找到该元素所在节点就返回该节点，否则返回null</returns>
    public AvlTreeNode<TElementType>? Find(TElementType element)
    {
        var currentNode = RootNode;
        while (currentNode != null)
        {
            switch (element.CompareTo(currentNode.Element))
            {
                case < 0:
                    currentNode = currentNode.LeftChild;
                    break;
                case > 0:
                    currentNode = currentNode.RightChild;
                    break;
                default:
                    return currentNode;
            }
        }

        return null;
    }

    /// <summary>
    /// 依据追踪栈修正树高、完成Avl树的再平衡
    /// </summary>
    /// <param name="traceStack">追踪栈</param>
    private void AvlTreeRebalanced(Stack<AvlTreeNode<TElementType>> traceStack)
    {
        // 自底向上逐级计算平衡因子，如果不平衡立即修正
        while (traceStack.Count > 0)
        {
            // 获取当前节点
            var currentNode = traceStack.Pop();
            // 更新当前节点的高度
            currentNode.TreeHeight = UpdateTreeHeightOfCurrentNode(currentNode);
            // 计算平衡因子
            var balanceFactor = GetHeightOfCurrentNode(currentNode.LeftChild) -
                                GetHeightOfCurrentNode(currentNode.RightChild);

            // 如果平衡因子小于2，当前节点不需要调整
            if (balanceFactor is < 2 and > -2) continue;

            // 如何调整当前节点?
            switch (balanceFactor)
            {
                // 当前节点左子树过高
                case 2:
                    {
                        // 判断类型 LL还是LR型
                        var leftChildOfCurrentNode = currentNode.LeftChild;
                        if (leftChildOfCurrentNode is null)
                            throw new ArgumentException($"{nameof(leftChildOfCurrentNode)}不能为空");

                        AvlTreeNode<TElementType> newRoot;
                        // LL型
                        if (GetHeightOfCurrentNode(leftChildOfCurrentNode.LeftChild) >
                            GetHeightOfCurrentNode(leftChildOfCurrentNode.RightChild))
                        {
                            newRoot = LL_Type(currentNode);
                        }
                        // LR型
                        else if (GetHeightOfCurrentNode(leftChildOfCurrentNode.LeftChild) <
                                 GetHeightOfCurrentNode(leftChildOfCurrentNode.RightChild))
                        {
                            newRoot = LR_Type(currentNode);
                        }
                        // 既是LL型又是LR型，当作LL型处理
                        else if (leftChildOfCurrentNode is { LeftChild: not null, RightChild: not null } &&
                                 GetHeightOfCurrentNode(leftChildOfCurrentNode.LeftChild) ==
                                 GetHeightOfCurrentNode(leftChildOfCurrentNode.RightChild))
                        {
                            newRoot = LL_Type(currentNode);
                        }
                        else
                        {
                            throw new Exception("追踪栈或树结构被破坏, 无法判断旋转类型。");
                        }

                        if (traceStack.Count == 0)
                        {
                            RootNode = newRoot;
                        }
                        else
                        {
                            var parentNode = traceStack.Peek();
                            if (parentNode.LeftChild == currentNode) parentNode.LeftChild = newRoot;
                            else parentNode.RightChild = newRoot;
                        }

                        break;
                    }
                // 当前节点右子树过高
                case -2:
                    {
                        // 判断类型 RR还是RL型
                        var rightChildOfCurrentNode = currentNode.RightChild;
                        if (rightChildOfCurrentNode is null)
                            throw new ArgumentException($"{nameof(rightChildOfCurrentNode)}不能为空");
                        AvlTreeNode<TElementType> newRoot;
                        // RR型
                        if (GetHeightOfCurrentNode(rightChildOfCurrentNode.RightChild) >
                            GetHeightOfCurrentNode(rightChildOfCurrentNode.LeftChild))
                        {
                            newRoot = RR_Type(currentNode);
                        }
                        // RL型
                        else if (GetHeightOfCurrentNode(rightChildOfCurrentNode.RightChild) <
                                 GetHeightOfCurrentNode(rightChildOfCurrentNode.LeftChild))
                        {
                            newRoot = RL_Type(currentNode);
                        }
                        // 既是RR型又是RL型，当作RR型处理
                        else if (rightChildOfCurrentNode is { LeftChild: not null, RightChild: not null } &&
                                 GetHeightOfCurrentNode(rightChildOfCurrentNode.RightChild) ==
                                 GetHeightOfCurrentNode(rightChildOfCurrentNode.LeftChild))
                        {
                            newRoot = RR_Type(currentNode);
                        }
                        else
                        {
                            throw new Exception("追踪栈或树结构被破坏, 无法判断旋转类型。");
                        }

                        if (traceStack.Count == 0)
                        {
                            RootNode = newRoot;
                        }
                        else
                        {
                            var parentNode = traceStack.Peek();
                            if (parentNode.LeftChild == currentNode) parentNode.LeftChild = newRoot;
                            else parentNode.RightChild = newRoot;
                        }

                        break;
                    }
                default:
                    {
                        /* 正常情况下不会进入这一分支，如果进入这一分支说明树结构被破坏，这里尝试修复 */
                        // 在删除时追踪栈中有平衡因子大于2的节点，要在这一节点的另一分支中旋转，将另一分支中的不平衡节点加入追踪栈
                        traceStack.Push(currentNode);
                        if (GetHeightOfCurrentNode(currentNode.LeftChild) > GetHeightOfCurrentNode(currentNode.RightChild))
                            currentNode = currentNode.LeftChild;
                        else if (GetHeightOfCurrentNode(currentNode.RightChild) >
                                 GetHeightOfCurrentNode(currentNode.LeftChild))
                            currentNode = currentNode.RightChild;
                        else throw new ArgumentException("追踪栈或树结构被破坏");
                        traceStack.Push(
                            currentNode ?? throw new ArgumentException($"追踪栈或树结构被破坏，{nameof(currentNode)} 不能为空"));
                        break;
                    }
            }
        }
    }

    #region InsertOperation

    /// <summary>
    /// 寻找插入位置，并记录搜寻插入位置的轨迹
    /// </summary>
    /// <param name="element">要插入的元素</param>
    /// <returns>追踪栈，记录了搜寻过程，栈顶元素就是插入位置，具体是插入左边还是右边还要再判断一次</returns>
    /// <exception cref="ArgumentException">AVL树不能存储重复元素，插入重复元素会抛出异常</exception>
    private Stack<AvlTreeNode<TElementType>> FindInsertPosition(TElementType element)
    {
        var traceStack = new Stack<AvlTreeNode<TElementType>>();
        var currentNode = RootNode;
        while (currentNode != null)
        {
            traceStack.Push(currentNode);
            currentNode = element.CompareTo(currentNode.Element) switch
            {
                // 如果要查找的元素比当前节点存储的元素小就向左走
                < 0 => currentNode.LeftChild,
                // 如果要查找的元素比当前节点存储的元素大就向右走
                > 0 => currentNode.RightChild,
                _ => throw new ArgumentException("duplicate element")
            };
        }

        return traceStack;
    }

    public void Insert(TElementType element)
    {
        if (RootNode == null)
        {
            RootNode = new AvlTreeNode<TElementType>(element);
            return;
        }

        var traceStack = FindInsertPosition(element);
        var currentNode = traceStack.Peek();
        switch (element.CompareTo(currentNode.Element))
        {
            case < 0:
                currentNode.LeftChild = new AvlTreeNode<TElementType>(element);
                break;
            case > 0:
                currentNode.RightChild = new AvlTreeNode<TElementType>(element);
                break;
            default:
                throw new ArgumentException("duplicate element");
        }

        // AVL树平衡调整
        AvlTreeRebalanced(traceStack);
    }

    #endregion

    #region DeleteOperation

    private AvlTreeNode<TElementType>? FindDeleteNode(TElementType element,
        out Stack<AvlTreeNode<TElementType>> traceStack)
    {
        traceStack = new Stack<AvlTreeNode<TElementType>>();
        var currentNode = RootNode;
        while (currentNode != null)
        {
            AvlTreeNode<TElementType>? preNode;
            switch (element.CompareTo(currentNode.Element))
            {
                // 如果要查找的元素比当前节点存储的元素小就向左走
                case < 0:
                    preNode = currentNode;
                    currentNode = currentNode.LeftChild;
                    traceStack.Push(preNode);
                    break;
                // 如果要查找的元素比当前节点存储的元素大就向右走
                case > 0:
                    preNode = currentNode;
                    currentNode = currentNode.RightChild;
                    traceStack.Push(preNode);
                    break;
                default:
                    // 如果要查找的元素 Equals 当前节点存储的元素，说明找到了这个节点，返回这个节点
                    return currentNode;
            }
        }

        // 遍历了整个二叉树都没找到这个节点，就返回空
        return null;
    }

    private void DeleteLeafNodes(Stack<AvlTreeNode<TElementType>> traceStack,
        AvlTreeNode<TElementType> currentNode)
    {
        // 如果追踪栈为空，且要删除的是叶子节点，说明整个AVL树只有一个根节点，直接清空这棵树即可
        if (traceStack.Count == 0)
        {
            Clear();
            return;
        }

        // 如果追踪栈不为空
        var preNode = traceStack.Peek();
        if (preNode.LeftChild == currentNode) preNode.LeftChild = null;
        if (preNode.RightChild == currentNode) preNode.RightChild = null;
    }

    private void DeleteNodesWithOneChild(Stack<AvlTreeNode<TElementType>> traceStack,
        AvlTreeNode<TElementType> currentNode)
    {
        // 追踪栈为空，要删除的是根节点
        if (traceStack.Count == 0)
        {
            // 把根节点的孩子变为新的根节点
            RootNode = currentNode.LeftChild ?? currentNode.RightChild;
            // 节点变动，重新计算树高
            if (RootNode == null) throw new NullReferenceException($"错误的分支，{nameof(RootNode)} 应该有1个孩子，现在为0个");
            RootNode.TreeHeight = UpdateTreeHeightOfCurrentNode(RootNode);
            return;
        }

        // 如果要删除的不是根节点
        var preNode = traceStack.Peek();
        var nextNode = currentNode.LeftChild ?? currentNode.RightChild;
        if (nextNode == null) throw new NullReferenceException($"错误的分支，{nameof(currentNode)} 应该有1个孩子，现在为0个");
        if (preNode.LeftChild == currentNode) preNode.LeftChild = nextNode;
        else preNode.RightChild = nextNode;
    }

    private static AvlTreeNode<TElementType> FindMaxNodeBeginWithLeftChildOfCurrentNode(
        AvlTreeNode<TElementType> currentNode, out Stack<AvlTreeNode<TElementType>> traceStackRecordedFromCurrentNode)
    {
        if (currentNode.LeftChild == null || currentNode.RightChild == null)
            throw new NullReferenceException($"错误的分支: {nameof(currentNode)} 的左子树或右子树不存在.");

        traceStackRecordedFromCurrentNode = new Stack<AvlTreeNode<TElementType>>();
        traceStackRecordedFromCurrentNode.Push(currentNode);
        var maxNodeInLeft = currentNode.LeftChild;
        while (maxNodeInLeft.RightChild != null)
        {
            traceStackRecordedFromCurrentNode.Push(maxNodeInLeft);
            maxNodeInLeft = maxNodeInLeft.RightChild;
        }

        return maxNodeInLeft;
    }

    private Stack<AvlTreeNode<TElementType>> DeleteNodesWithTwoChildren(
        Stack<AvlTreeNode<TElementType>> traceStackRecordedFromRoot,
        AvlTreeNode<TElementType> currentNode)
    {
        // 查看currentNode左孩子存储的元素，找出最大的那个节点maxNodeInLeft
        var maxNodeInLeft = FindMaxNodeBeginWithLeftChildOfCurrentNode(currentNode,
            // 追踪栈：记录了从currentNode到maxNodeInLeft的所有途经节点 (不包括maxNodeInLeft)
            out var traceStackRecordedFromCurrentNode);

        // 用节点maxNodeInLeft的值覆盖掉要删除节点的元素值(树结构没变)
        currentNode.Element = maxNodeInLeft.Element;

        // 删除maxNodeInLeft，问题被转化为删除一个叶子节点或只有一个子节点的节点。
        // 合并追踪栈: [RootNode,currentNode) ∪ [currentNode,maxNodeInLeft)
        // 栈转换为列表是从栈顶到栈底的顺序，例如：1，2，3依次入栈(用stack表示)，转换成列表(用list表示)后为3，2，1，
        // 所以这里用倒序索引将traceStackRecordedFromCurrentNode合并到traceStackRecordedFromRoot
        var traceStackRecordedFromCurrentNodeToList = traceStackRecordedFromCurrentNode.ToList();
        for (var i = traceStackRecordedFromCurrentNodeToList.Count - 1; i >= 0; i--)
        {
            traceStackRecordedFromRoot.Push(traceStackRecordedFromCurrentNodeToList[i]);
        }

        if (maxNodeInLeft.LeftChild != null)
            DeleteNodesWithOneChild(traceStackRecordedFromRoot, maxNodeInLeft);
        else
            DeleteLeafNodes(traceStackRecordedFromRoot, maxNodeInLeft);

        return traceStackRecordedFromRoot;
    }

    public void Delete(TElementType element)
    {
        if (RootNode == null) return;
        // 获取要删除的节点和从根节点出发的追踪栈(记录在traceStackRecordedFromRoot中)，traceStackRecordedFromRoot记录了从根节点到currentNode的所有途经节点 (不包括currentNode节点)
        var currentNode = FindDeleteNode(element, out var traceStackRecordedFromRoot)
                          ?? throw new ArgumentException("未找到要删除的节点");

        // 节点删除操作，沿用BinarySearchTree的Delete方法，在Delete后更新树高和再平衡，需要用到追踪栈，所以加了追踪栈
        // 如果要删除的节点有两个子树，还要再建立从currentNode到maxNodeInLeft的次级追踪栈，然后合并两个追踪栈到traceStackRecordedFromRoot
        var aggregateTraceStack = traceStackRecordedFromRoot;
        if (currentNode.LeftChild == null && currentNode.RightChild == null)
        {
            // 等待删除的节点是叶子节点
            DeleteLeafNodes(traceStackRecordedFromRoot, currentNode);
        }
        else if (currentNode.LeftChild == null || currentNode.RightChild == null)
        {
            // 等待删除的节点有一个子节点
            DeleteNodesWithOneChild(traceStackRecordedFromRoot, currentNode);
        }
        else
        {
            // 等待删除的节点有两个子节点
            aggregateTraceStack = DeleteNodesWithTwoChildren(traceStackRecordedFromRoot, currentNode);
        }

        // 重新平衡二叉树
        AvlTreeRebalanced(aggregateTraceStack);
    }

    #endregion

    /// <summary>
    /// <para>更改AVL树中的值</para>
    /// <para>将旧值<paramref name="oldValue"/>修改为新值<paramref name="newValue"/>，实际上是先删除后插入。</para>
    /// <para>注意：不能简单将oldValue中的元素替换成新的，这样替换后不符合AVL树的定义</para>
    /// </summary>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    public void Change(TElementType oldValue, TElementType newValue)
    {
        // Change 必须具备“全成或全不成”的原子语义。旧实现先删除再插入；
        // 如果 newValue 已存在，Insert 会抛异常，但 oldValue 已经永久丢失。
        if (Find(oldValue) is null)
        {
            throw new ArgumentException("未找到要修改的旧值。", nameof(oldValue));
        }

        // 比较结果相等表示排序意义上的同一个键，无需破坏并重建树结构。
        if (oldValue.CompareTo(newValue) == 0) return;

        // 必须在 Delete 之前检查冲突，保证失败时树完全不变。
        if (Find(newValue) is not null)
        {
            throw new ArgumentException("新值已存在，AVL 树不允许重复元素。", nameof(newValue));
        }

        Delete(oldValue);
        Insert(newValue);
    }

    #endregion
}
