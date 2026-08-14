namespace DataStructureAndAlgorithm.Tree;

/// <summary>线索化采用的遍历顺序。</summary>
public enum ThreadingOrder
{
    PreOrder,
    InOrder,
    PostOrder,
    LevelOrder
}

/// <summary>
/// 将空孩子指针复用为遍历前驱/后继的线索二叉树。
/// </summary>
/// <remarks>
/// 普通遍历需要 O(h) 的栈；与当前线索顺序一致的前序、中序和后序遍历不再创建栈，
/// 只保留一个当前节点引用，因此辅助空间为 O(1)。
/// </remarks>
public class CluesBinaryTree<TElementType>(CluesBinaryTreeNode<TElementType>? rootNode = null)
    where TElementType : notnull
{
    public CluesBinaryTreeNode<TElementType>? RootNode { get; } = rootNode;

    /// <summary>当前保存的线索顺序；未线索化或使用自定义委托时为 null。</summary>
    public ThreadingOrder? CurrentThreadingOrder { get; private set; }

    /// <summary>把普通二叉树复制为尚未线索化的线索二叉树。</summary>
    public static explicit operator CluesBinaryTree<TElementType>?(NormalBinaryTree<TElementType>? normalBinaryTree)
    {
        if (normalBinaryTree is null) return null;
        if (normalBinaryTree.RootNode is null) return new CluesBinaryTree<TElementType>();

        var newRoot = new CluesBinaryTreeNode<TElementType>(normalBinaryTree.RootNode.Element);
        var stack = new Stack<(NormalBinaryTreeNode<TElementType> Source,
            CluesBinaryTreeNode<TElementType> Target)>();
        stack.Push((normalBinaryTree.RootNode, newRoot));

        while (stack.Count > 0)
        {
            var (source, target) = stack.Pop();
            if (source.RightChild is not null)
            {
                target.RightChild = new CluesBinaryTreeNode<TElementType>(source.RightChild.Element)
                {
                    Parent = target
                };
                stack.Push((source.RightChild, target.RightChild));
            }

            if (source.LeftChild is not null)
            {
                target.LeftChild = new CluesBinaryTreeNode<TElementType>(source.LeftChild.Element)
                {
                    Parent = target
                };
                stack.Push((source.LeftChild, target.LeftChild));
            }
        }

        return new CluesBinaryTree<TElementType>(newRoot);
    }

    /// <summary>
    /// 按指定顺序重新建立线索。重复调用会先安全移除旧线索，再建立新线索。
    /// </summary>
    public void Thread(ThreadingOrder order)
    {
        ClearThreads();
        var orderedNodes = order switch
        {
            ThreadingOrder.PreOrder => PreOrderWithStack().ToArray(),
            ThreadingOrder.InOrder => InOrderWithStack().ToArray(),
            ThreadingOrder.PostOrder => PostOrderWithStack().ToArray(),
            ThreadingOrder.LevelOrder => LevelOrderWithQueue().ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(order), order, "未知的线索化顺序。")
        };

        ApplyThreads(orderedNodes);
        CurrentThreadingOrder = order;
    }

    /// <summary>
    /// 兼容原项目的委托式 API。项目内置遍历会映射到强类型的 <see cref="Thread"/> 方法。
    /// </summary>
    public void CluesGeneric(Func<IEnumerable<CluesBinaryTreeNode<TElementType>>> traverseMethod)
    {
        ArgumentNullException.ThrowIfNull(traverseMethod);
        var order = traverseMethod.Method.Name switch
        {
            nameof(PreOrderTraverse) => ThreadingOrder.PreOrder,
            nameof(InOrderTraverse) => ThreadingOrder.InOrder,
            nameof(PostOrderTraverse) => ThreadingOrder.PostOrder,
            nameof(LevelOrderTraverse) => ThreadingOrder.LevelOrder,
            _ => (ThreadingOrder?)null
        };

        if (order is not null)
        {
            Thread(order.Value);
            return;
        }

        // 自定义顺序仍可线索化，但无法判断之后哪个内置遍历能消费这些线索。
        ClearThreads();
        var nodes = traverseMethod().ToArray();
        ApplyThreads(nodes);
        CurrentThreadingOrder = null;
    }

    /// <summary>前序遍历；前序线索化后使用 O(1) 辅助空间。</summary>
    public IEnumerable<CluesBinaryTreeNode<TElementType>> PreOrderTraverse() =>
        CurrentThreadingOrder == ThreadingOrder.PreOrder
            ? PreOrderUsingThreads()
            : PreOrderWithStack();

    /// <summary>中序遍历；中序线索化后使用 O(1) 辅助空间。</summary>
    public IEnumerable<CluesBinaryTreeNode<TElementType>> InOrderTraverse() =>
        CurrentThreadingOrder == ThreadingOrder.InOrder
            ? InOrderUsingThreads()
            : InOrderWithStack();

    /// <summary>后序遍历；后序线索化后借助父指针使用 O(1) 辅助空间。</summary>
    public IEnumerable<CluesBinaryTreeNode<TElementType>> PostOrderTraverse() =>
        CurrentThreadingOrder == ThreadingOrder.PostOrder
            ? PostOrderUsingThreads()
            : PostOrderWithStack();

    /// <summary>层序遍历天然需要保存同层节点，因此继续使用 O(w) 队列。</summary>
    public IEnumerable<CluesBinaryTreeNode<TElementType>> LevelOrderTraverse() => LevelOrderWithQueue();

    private void ClearThreads()
    {
        // 枚举时只沿 Tag=0 的真实孩子边移动，避免把旧线索误当成子树。
        var nodes = LevelOrderWithQueue().ToArray();
        foreach (var node in nodes)
        {
            if (node.LeftTag == 1)
            {
                node.LeftChild = null;
                node.LeftTag = 0;
            }

            if (node.RightTag == 1)
            {
                node.RightChild = null;
                node.RightTag = 0;
            }
        }

        CurrentThreadingOrder = null;
    }

    private static void ApplyThreads(IReadOnlyList<CluesBinaryTreeNode<TElementType>> nodes)
    {
        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            if (node.LeftChild is null)
            {
                node.LeftTag = 1;
                node.LeftChild = index == 0 ? null : nodes[index - 1];
            }

            if (node.RightChild is null)
            {
                node.RightTag = 1;
                node.RightChild = index == nodes.Count - 1 ? null : nodes[index + 1];
            }
        }
    }

    private IEnumerable<CluesBinaryTreeNode<TElementType>> PreOrderUsingThreads()
    {
        var current = RootNode;
        while (current is not null)
        {
            yield return current;
            // 有真实左孩子时前序的下一节点必为左孩子；否则右指针无论是孩子还是后继线索都可直接跟随。
            current = current.LeftTag == 0 && current.LeftChild is not null
                ? current.LeftChild
                : current.RightChild;
        }
    }

    private IEnumerable<CluesBinaryTreeNode<TElementType>> InOrderUsingThreads()
    {
        var current = Leftmost(RootNode);
        while (current is not null)
        {
            yield return current;
            current = current.RightTag == 1
                ? current.RightChild
                : Leftmost(current.RightChild);
        }
    }

    private IEnumerable<CluesBinaryTreeNode<TElementType>> PostOrderUsingThreads()
    {
        var current = FirstPostOrderNode(RootNode);
        while (current is not null)
        {
            yield return current;

            if (current.RightTag == 1)
            {
                current = current.RightChild;
                continue;
            }

            var parent = current.Parent;
            if (parent is null)
            {
                current = null;
            }
            else if (parent.LeftTag == 0 && ReferenceEquals(parent.LeftChild, current) &&
                     parent.RightTag == 0 && parent.RightChild is not null)
            {
                // 左子树刚结束时，后序的下一节点是右子树中第一个被访问的节点。
                current = FirstPostOrderNode(parent.RightChild);
            }
            else
            {
                current = parent;
            }
        }
    }

    private static CluesBinaryTreeNode<TElementType>? Leftmost(CluesBinaryTreeNode<TElementType>? node)
    {
        while (node is { LeftTag: 0, LeftChild: not null }) node = node.LeftChild;
        return node;
    }

    private static CluesBinaryTreeNode<TElementType>? FirstPostOrderNode(
        CluesBinaryTreeNode<TElementType>? node)
    {
        while (node is not null)
        {
            if (node.LeftTag == 0 && node.LeftChild is not null)
            {
                node = node.LeftChild;
            }
            else if (node.RightTag == 0 && node.RightChild is not null)
            {
                node = node.RightChild;
            }
            else
            {
                return node;
            }
        }

        return null;
    }

    private IEnumerable<CluesBinaryTreeNode<TElementType>> PreOrderWithStack()
    {
        if (RootNode is null) yield break;
        var stack = new Stack<CluesBinaryTreeNode<TElementType>>();
        stack.Push(RootNode);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            if (node.RightTag == 0 && node.RightChild is not null) stack.Push(node.RightChild);
            if (node.LeftTag == 0 && node.LeftChild is not null) stack.Push(node.LeftChild);
        }
    }

    private IEnumerable<CluesBinaryTreeNode<TElementType>> InOrderWithStack()
    {
        var stack = new Stack<CluesBinaryTreeNode<TElementType>>();
        var current = RootNode;
        while (current is not null || stack.Count > 0)
        {
            while (current is not null)
            {
                stack.Push(current);
                current = current.LeftTag == 0 ? current.LeftChild : null;
            }

            current = stack.Pop();
            yield return current;
            current = current.RightTag == 0 ? current.RightChild : null;
        }
    }

    private IEnumerable<CluesBinaryTreeNode<TElementType>> PostOrderWithStack()
    {
        var result = new Stack<CluesBinaryTreeNode<TElementType>>();
        var work = new Stack<CluesBinaryTreeNode<TElementType>>();
        if (RootNode is not null) work.Push(RootNode);

        while (work.Count > 0)
        {
            var node = work.Pop();
            result.Push(node);
            if (node.LeftTag == 0 && node.LeftChild is not null) work.Push(node.LeftChild);
            if (node.RightTag == 0 && node.RightChild is not null) work.Push(node.RightChild);
        }

        while (result.Count > 0) yield return result.Pop();
    }

    private IEnumerable<CluesBinaryTreeNode<TElementType>> LevelOrderWithQueue()
    {
        if (RootNode is null) yield break;
        var queue = new Queue<CluesBinaryTreeNode<TElementType>>();
        queue.Enqueue(RootNode);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            yield return node;
            if (node.LeftTag == 0 && node.LeftChild is not null) queue.Enqueue(node.LeftChild);
            if (node.RightTag == 0 && node.RightChild is not null) queue.Enqueue(node.RightChild);
        }
    }
}
