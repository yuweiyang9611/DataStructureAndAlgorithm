using System.Collections;
using DataStructureAndAlgorithm.Collections;
using DataStructureAndAlgorithm.Diagnostics;

namespace DataStructureAndAlgorithm.Tree;

/// <summary>
/// 左倾红黑树（Left-Leaning Red-Black Tree）实现的有序集合。
/// </summary>
/// <remarks>
/// 红链接可理解为把两个普通二叉节点临时合并成一个 2-3 树节点。左倾约定把红链接统一放在左侧，
/// 因而插入、删除只需少量局部旋转与颜色翻转。树高始终为 O(log n)，查找、插入和删除也都是 O(log n)。
/// 本实现不保存重复值；生产代码通常优先使用 <see cref="SortedSet{T}"/>。
/// </remarks>
public sealed class RedBlackTree<T> : IOrderedSet<T> where T : notnull
{
    private sealed class Node(T value, bool isRed)
    {
        public T Value { get; set; } = value;
        public Node? Left { get; set; }
        public Node? Right { get; set; }
        public bool IsRed { get; set; } = isRed;
    }

    private readonly IComparer<T> _comparer;
    private readonly IAlgorithmTraceSink? _trace;
    private Node? _root;

    public RedBlackTree(IComparer<T>? comparer = null, IAlgorithmTraceSink? trace = null)
    {
        _comparer = comparer ?? Comparer<T>.Default;
        _trace = trace;
    }

    public int Count { get; private set; }

    public IComparer<T> Comparer => _comparer;

    /// <summary>空树高度为 0，单节点树高度为 1。</summary>
    public int Height => GetHeight(_root);

    public bool Contains(T value)
    {
        var current = _root;
        while (current is not null)
        {
            var comparison = _comparer.Compare(value, current.Value);
            if (comparison == 0) return true;
            current = comparison < 0 ? current.Left : current.Right;
        }

        return false;
    }

    /// <summary>添加一个不重复的值；已存在时返回 false。</summary>
    public bool Add(T value)
    {
        var added = false;
        _root = Insert(_root, value, ref added);
        _root.IsRed = false; // 根节点对应 2-3 树的顶层，必须是黑色。
        if (added) Count++;
        _trace?.Record(
            "RedBlackTree",
            added ? "Add" : "SkipDuplicate",
            added ? "插入新值并把根节点恢复为黑色。" : "比较器判定该值已经存在，因此集合保持不变。",
            new Dictionary<string, string>
            {
                ["value"] = value.ToString() ?? "<null>",
                ["count"] = Count.ToString(),
                ["height"] = Height.ToString()
            });
        return added;
    }

    /// <summary>删除指定值；不存在时返回 false 且不改变树。</summary>
    public bool Remove(T value)
    {
        if (_root is null || !Contains(value)) return false;

        // 顶向下删除要求当前节点不是 2-节点；必要时临时把根染红，下降时再借红链接。
        if (!IsRed(_root.Left) && !IsRed(_root.Right)) _root.IsRed = true;
        _root = Delete(_root, value);
        if (_root is not null) _root.IsRed = false;
        Count--;
        _trace?.Record(
            "RedBlackTree",
            "Remove",
            "删除目标值并恢复左倾红黑树不变量。",
            new Dictionary<string, string>
            {
                ["value"] = value.ToString() ?? "<null>",
                ["count"] = Count.ToString(),
                ["height"] = Height.ToString()
            });
        return true;
    }

    public void Clear()
    {
        _root = null;
        Count = 0;
    }

    /// <summary>
    /// 验证左倾红黑树全部不变量，供学习和测试使用。
    /// </summary>
    public bool HasValidInvariants()
    {
        if (IsRed(_root)) return false;
        var nodeCount = 0;
        var valid = Validate(
            _root, hasMinimum: false, default!, hasMaximum: false, default!,
            parentIsRed: false, ref nodeCount, out _);
        return valid && nodeCount == Count;
    }

    public IEnumerator<T> GetEnumerator()
    {
        // 中序遍历天然产生 comparer 定义的升序序列。
        var stack = new Stack<Node>();
        var current = _root;
        while (current is not null || stack.Count > 0)
        {
            while (current is not null)
            {
                stack.Push(current);
                current = current.Left;
            }

            current = stack.Pop();
            yield return current.Value;
            current = current.Right;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private Node Insert(Node? node, T value, ref bool added)
    {
        if (node is null)
        {
            added = true;
            return new Node(value, isRed: true);
        }

        var comparison = _comparer.Compare(value, node.Value);
        if (comparison < 0)
        {
            node.Left = Insert(node.Left, value, ref added);
        }
        else if (comparison > 0)
        {
            node.Right = Insert(node.Right, value, ref added);
        }

        // 三个局部规则分别修复右倾红链接、连续左红链接和临时 4-节点。
        if (IsRed(node.Right) && !IsRed(node.Left)) node = RotateLeft(node);
        if (IsRed(node.Left) && IsRed(node.Left!.Left)) node = RotateRight(node);
        if (IsRed(node.Left) && IsRed(node.Right)) FlipColors(node);
        return node;
    }

    private Node? Delete(Node node, T value)
    {
        if (_comparer.Compare(value, node.Value) < 0)
        {
            if (node.Left is not null)
            {
                if (!IsRed(node.Left) && !IsRed(node.Left.Left)) node = MoveRedLeft(node);
                node.Left = Delete(node.Left!, value);
            }
        }
        else
        {
            if (IsRed(node.Left)) node = RotateRight(node);

            var comparison = _comparer.Compare(value, node.Value);
            if (comparison == 0 && node.Right is null) return null;

            if (node.Right is not null)
            {
                if (!IsRed(node.Right) && !IsRed(node.Right.Left)) node = MoveRedRight(node);
                comparison = _comparer.Compare(value, node.Value);

                if (comparison == 0)
                {
                    var successor = Minimum(node.Right!);
                    node.Value = successor.Value;
                    node.Right = DeleteMinimum(node.Right!);
                }
                else
                {
                    node.Right = Delete(node.Right!, value);
                }
            }
        }

        return Balance(node);
    }

    private Node? DeleteMinimum(Node node)
    {
        if (node.Left is null) return null;
        if (!IsRed(node.Left) && !IsRed(node.Left.Left)) node = MoveRedLeft(node);
        node.Left = DeleteMinimum(node.Left!);
        return Balance(node);
    }

    private Node MoveRedLeft(Node node)
    {
        // 把当前 2-节点与孩子临时合并；若右孩子内部有红链接，再旋转借到左侧。
        FlipColors(node);
        if (IsRed(node.Right?.Left))
        {
            node.Right = RotateRight(node.Right!);
            node = RotateLeft(node);
            FlipColors(node);
        }

        return node;
    }

    private Node MoveRedRight(Node node)
    {
        FlipColors(node);
        if (IsRed(node.Left?.Left))
        {
            node = RotateRight(node);
            FlipColors(node);
        }

        return node;
    }

    private Node Balance(Node node)
    {
        if (IsRed(node.Right)) node = RotateLeft(node);
        if (IsRed(node.Left) && IsRed(node.Left!.Left)) node = RotateRight(node);
        if (IsRed(node.Left) && IsRed(node.Right)) FlipColors(node);
        return node;
    }

    private Node RotateLeft(Node node)
    {
        var promoted = node.Right!;
        node.Right = promoted.Left;
        promoted.Left = node;
        promoted.IsRed = node.IsRed;
        node.IsRed = true;
        _trace?.Record(
            "RedBlackTree",
            "RotateLeft",
            "右侧红链接违反左倾约定，执行左旋并继承原根颜色。",
            new Dictionary<string, string>
            {
                ["oldRoot"] = node.Value.ToString() ?? "<null>",
                ["newRoot"] = promoted.Value.ToString() ?? "<null>"
            });
        return promoted;
    }

    private Node RotateRight(Node node)
    {
        var promoted = node.Left!;
        node.Left = promoted.Right;
        promoted.Right = node;
        promoted.IsRed = node.IsRed;
        node.IsRed = true;
        _trace?.Record(
            "RedBlackTree",
            "RotateRight",
            "出现连续左红链接，执行右旋以恢复平衡。",
            new Dictionary<string, string>
            {
                ["oldRoot"] = node.Value.ToString() ?? "<null>",
                ["newRoot"] = promoted.Value.ToString() ?? "<null>"
            });
        return promoted;
    }

    private void FlipColors(Node node)
    {
        node.IsRed = !node.IsRed;
        if (node.Left is not null) node.Left.IsRed = !node.Left.IsRed;
        if (node.Right is not null) node.Right.IsRed = !node.Right.IsRed;
        _trace?.Record(
            "RedBlackTree",
            "FlipColors",
            "翻转父子三点颜色，用等价的 2-3 树分裂或合并消除临时 4-节点。",
            new Dictionary<string, string>
            {
                ["node"] = node.Value.ToString() ?? "<null>",
                ["nodeColor"] = node.IsRed ? "Red" : "Black"
            });
    }

    private static bool IsRed(Node? node) => node?.IsRed == true;

    private static Node Minimum(Node node)
    {
        while (node.Left is not null) node = node.Left;
        return node;
    }

    private static int GetHeight(Node? node) =>
        node is null ? 0 : 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));

    private bool Validate(
        Node? node,
        bool hasMinimum,
        T minimum,
        bool hasMaximum,
        T maximum,
        bool parentIsRed,
        ref int nodeCount,
        out int blackHeight)
    {
        if (node is null)
        {
            // null 叶子在红黑树定义中视作黑色哨兵。
            blackHeight = 1;
            return true;
        }

        if (hasMinimum && _comparer.Compare(node.Value, minimum) <= 0 ||
            hasMaximum && _comparer.Compare(node.Value, maximum) >= 0 ||
            parentIsRed && node.IsRed ||
            IsRed(node.Right))
        {
            blackHeight = 0;
            return false;
        }

        nodeCount++;
        if (!Validate(node.Left, hasMinimum, minimum, true, node.Value, node.IsRed,
                ref nodeCount, out var leftBlackHeight) ||
            !Validate(node.Right, true, node.Value, hasMaximum, maximum, node.IsRed,
                ref nodeCount, out var rightBlackHeight) ||
            leftBlackHeight != rightBlackHeight)
        {
            blackHeight = 0;
            return false;
        }

        blackHeight = leftBlackHeight + (node.IsRed ? 0 : 1);
        return true;
    }
}
