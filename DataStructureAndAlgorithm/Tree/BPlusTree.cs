using System.Collections;

namespace DataStructureAndAlgorithm.Tree;

/// <summary>
/// 面向范围扫描的教学版 B+ 树。
/// </summary>
/// <remarks>
/// <para>
/// B+ 树和普通 B 树最重要的区别是：真实值只保存在叶子节点，内部节点只保存“导航分隔键”。
/// 这样所有叶子都位于同一深度，并且可以通过叶子链表连续扫描，非常适合数据库索引。
/// </para>
/// <para>
/// 本实现聚焦插入、更新、单点查询和范围查询。存储引擎中的删除使用墓碑记录，而不是立即合并页面；
/// 这与日志结构存储系统的常见做法一致，也把“逻辑删除”和“物理压缩”两个概念清晰分开。
/// </para>
/// </remarks>
public sealed class BPlusTree<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : notnull
{
    private readonly IComparer<TKey> _comparer;
    private readonly int _maximumKeys;
    private Node _root;

    /// <summary>
    /// 创建一棵 B+ 树。
    /// </summary>
    /// <param name="order">内部节点最多拥有的子节点数，至少为 3。</param>
    /// <param name="comparer">键的全序比较器；相等键表示更新，而不是重复插入。</param>
    public BPlusTree(int order = 4, IComparer<TKey>? comparer = null)
    {
        if (order < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(order), order, "B+ 树的阶必须至少为 3。");
        }

        Order = order;
        _maximumKeys = order - 1;
        _comparer = comparer ?? Comparer<TKey>.Default;
        _root = Node.CreateLeaf();
    }

    /// <summary>内部节点最多拥有的子节点数。</summary>
    public int Order { get; }

    /// <summary>当前不同键的数量；更新已有键不会增加该值。</summary>
    public int Count { get; private set; }

    /// <summary>
    /// 插入新键或更新已有键。
    /// </summary>
    /// <returns><see langword="true"/> 表示插入新键；<see langword="false"/> 表示覆盖旧值。</returns>
    public bool Upsert(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        var split = Insert(_root, key, value, out var added);
        if (split is not null)
        {
            // 根节点分裂时树高增加一层。旧根和新右节点成为新根的两个孩子。
            var newRoot = Node.CreateInternal();
            newRoot.Keys.Add(split.Value.Separator);
            newRoot.Children.Add(_root);
            newRoot.Children.Add(split.Value.Right);
            _root = newRoot;
        }

        if (added)
        {
            Count++;
        }

        return added;
    }

    /// <summary>以 O(log n) 的树高代价查询一个键。</summary>
    public bool TryGetValue(TKey key, out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        var leaf = FindLeaf(key);
        var index = LowerBound(leaf.Keys, key);
        if (index < leaf.Keys.Count && _comparer.Compare(leaf.Keys[index], key) == 0)
        {
            value = leaf.Values[index];
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// 返回半开区间 <c>[fromInclusive, toExclusive)</c> 内按键升序排列的条目。
    /// </summary>
    /// <remarks>
    /// 第一次定位叶子需要 O(log n)，之后沿叶子链顺序读取 k 个结果，总复杂度为 O(log n + k)。
    /// </remarks>
    public IReadOnlyList<KeyValuePair<TKey, TValue>> Range(
        TKey fromInclusive,
        TKey toExclusive,
        int maximumCount = int.MaxValue)
    {
        ValidateRangeBounds(fromInclusive, toExclusive);

        if (maximumCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount), maximumCount, "最大结果数不能为负数。");
        }

        if (maximumCount == 0 || Count == 0)
        {
            return [];
        }

        // 不能按整棵树的 Count 预分配：窄区间或 maximumCount 很小时，这会把 O(k) 结果空间
        // 错误放大为 O(n)。256 只是小结果的扩容折中；更大结果让 List 按实际消费量自然增长。
        var result = new List<KeyValuePair<TKey, TValue>>(Math.Min(maximumCount, 256));
        foreach (var pair in EnumerateRangeCore(fromInclusive, toExclusive))
        {
            result.Add(pair);
            if (result.Count == maximumCount)
            {
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// 惰性枚举半开区间 <c>[fromInclusive, toExclusive)</c> 内的条目。
    /// </summary>
    /// <remarks>
    /// 调用方法时立即校验边界；真正的树定位和叶链读取在枚举时发生。消费者取得 k 个条目后停止，
    /// 树也会立刻停止扫描，因此分页、墓碑过滤等上层逻辑无需先物化整个物理区间。
    /// </remarks>
    public IEnumerable<KeyValuePair<TKey, TValue>> EnumerateRange(TKey fromInclusive, TKey toExclusive)
    {
        ValidateRangeBounds(fromInclusive, toExclusive);
        return EnumerateRangeCore(fromInclusive, toExclusive);
    }

    private IEnumerable<KeyValuePair<TKey, TValue>> EnumerateRangeCore(TKey fromInclusive, TKey toExclusive)
    {
        var leaf = FindLeaf(fromInclusive);
        var index = LowerBound(leaf.Keys, fromInclusive);

        while (leaf is not null)
        {
            for (; index < leaf.Keys.Count; index++)
            {
                var key = leaf.Keys[index];
                if (_comparer.Compare(key, toExclusive) >= 0)
                {
                    yield break;
                }

                yield return new KeyValuePair<TKey, TValue>(key, leaf.Values[index]);
            }

            leaf = leaf.NextLeaf;
            index = 0;
        }
    }

    private void ValidateRangeBounds(TKey fromInclusive, TKey toExclusive)
    {
        ArgumentNullException.ThrowIfNull(fromInclusive);
        ArgumentNullException.ThrowIfNull(toExclusive);
        if (_comparer.Compare(fromInclusive, toExclusive) > 0)
        {
            throw new ArgumentException("范围起点不能大于终点。", nameof(fromInclusive));
        }
    }

    /// <summary>
    /// 检查教学实现最关键的结构不变量。
    /// </summary>
    /// <remarks>
    /// 该方法不是业务查询的一部分，而是为随机状态机测试提供“白盒安全带”：所有叶子必须等深、
    /// 节点键必须严格有序、内部节点的分隔键必须等于右子树最小键、叶子链必须与中序遍历一致。
    /// </remarks>
    public bool HasValidInvariants()
    {
        var leaves = new List<Node>();
        int? leafDepth = null;
        var valid = ValidateNode(_root, depth: 0, isRoot: true, leaves, ref leafDepth, out _, out _);
        if (!valid)
        {
            return false;
        }

        var linked = GetLeftmostLeaf();
        var itemCount = 0;
        for (var index = 0; index < leaves.Count; index++)
        {
            if (!ReferenceEquals(linked, leaves[index]))
            {
                return false;
            }

            itemCount += linked.Keys.Count;
            linked = linked.NextLeaf;
        }

        return linked is null && itemCount == Count;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        var leaf = GetLeftmostLeaf();
        while (leaf is not null)
        {
            for (var index = 0; index < leaf.Keys.Count; index++)
            {
                yield return new KeyValuePair<TKey, TValue>(leaf.Keys[index], leaf.Values[index]);
            }

            leaf = leaf.NextLeaf;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private SplitResult? Insert(Node node, TKey key, TValue value, out bool added)
    {
        if (node.IsLeaf)
        {
            var index = LowerBound(node.Keys, key);
            if (index < node.Keys.Count && _comparer.Compare(node.Keys[index], key) == 0)
            {
                node.Values[index] = value;
                added = false;
                return null;
            }

            node.Keys.Insert(index, key);
            node.Values.Insert(index, value);
            added = true;
            return node.Keys.Count > _maximumKeys ? SplitLeaf(node) : null;
        }

        // 内部节点使用“右偏上界”：等于分隔键时必须进入右孩子，因为真实数据只存在于叶子。
        var childIndex = UpperBound(node.Keys, key);
        var childSplit = Insert(node.Children[childIndex], key, value, out added);
        if (childSplit is null)
        {
            return null;
        }

        node.Keys.Insert(childIndex, childSplit.Value.Separator);
        node.Children.Insert(childIndex + 1, childSplit.Value.Right);
        return node.Keys.Count > _maximumKeys ? SplitInternal(node) : null;
    }

    private SplitResult SplitLeaf(Node leaf)
    {
        var splitIndex = leaf.Keys.Count / 2;
        var right = Node.CreateLeaf();
        right.Keys.AddRange(leaf.Keys.GetRange(splitIndex, leaf.Keys.Count - splitIndex));
        right.Values.AddRange(leaf.Values.GetRange(splitIndex, leaf.Values.Count - splitIndex));

        leaf.Keys.RemoveRange(splitIndex, leaf.Keys.Count - splitIndex);
        leaf.Values.RemoveRange(splitIndex, leaf.Values.Count - splitIndex);

        right.NextLeaf = leaf.NextLeaf;
        leaf.NextLeaf = right;

        // B+ 树把右叶子的第一项“复制”到父节点作为导航键，真实键值仍保留在叶子中。
        return new SplitResult(right.Keys[0], right);
    }

    private SplitResult SplitInternal(Node node)
    {
        var middle = node.Keys.Count / 2;
        var separator = node.Keys[middle];
        var right = Node.CreateInternal();

        right.Keys.AddRange(node.Keys.GetRange(middle + 1, node.Keys.Count - middle - 1));
        right.Children.AddRange(node.Children.GetRange(middle + 1, node.Children.Count - middle - 1));

        node.Keys.RemoveRange(middle, node.Keys.Count - middle);
        node.Children.RemoveRange(middle + 1, node.Children.Count - middle - 1);

        // 内部节点分裂时分隔键“上移”，它不再留在左右内部节点中。
        return new SplitResult(separator, right);
    }

    private Node FindLeaf(TKey key)
    {
        var node = _root;
        while (!node.IsLeaf)
        {
            node = node.Children[UpperBound(node.Keys, key)];
        }

        return node;
    }

    private Node GetLeftmostLeaf()
    {
        var node = _root;
        while (!node.IsLeaf)
        {
            node = node.Children[0];
        }

        return node;
    }

    private int LowerBound(IReadOnlyList<TKey> keys, TKey key)
    {
        var low = 0;
        var high = keys.Count;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (_comparer.Compare(keys[middle], key) < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private int UpperBound(IReadOnlyList<TKey> keys, TKey key)
    {
        var low = 0;
        var high = keys.Count;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (_comparer.Compare(keys[middle], key) <= 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private bool ValidateNode(
        Node node,
        int depth,
        bool isRoot,
        List<Node> leaves,
        ref int? leafDepth,
        out TKey? firstKey,
        out TKey? lastKey)
    {
        firstKey = default;
        lastKey = default;

        if (node.Keys.Count > _maximumKeys || !AreStrictlyIncreasing(node.Keys))
        {
            return false;
        }

        if (!isRoot && node.Keys.Count == 0)
        {
            return false;
        }

        if (node.IsLeaf)
        {
            if (node.Values.Count != node.Keys.Count || node.Children.Count != 0)
            {
                return false;
            }

            leafDepth ??= depth;
            if (leafDepth != depth)
            {
                return false;
            }

            leaves.Add(node);
            if (node.Keys.Count > 0)
            {
                firstKey = node.Keys[0];
                lastKey = node.Keys[^1];
            }

            return true;
        }

        if (node.Values.Count != 0 || node.Children.Count != node.Keys.Count + 1)
        {
            return false;
        }

        TKey? previousLast = default;
        var hasPrevious = false;
        for (var index = 0; index < node.Children.Count; index++)
        {
            if (!ValidateNode(node.Children[index], depth + 1, isRoot: false, leaves, ref leafDepth,
                    out var childFirst, out var childLast) || childFirst is null || childLast is null)
            {
                return false;
            }

            if (hasPrevious && _comparer.Compare(previousLast!, childFirst) >= 0)
            {
                return false;
            }

            if (index > 0 && _comparer.Compare(node.Keys[index - 1], childFirst) != 0)
            {
                return false;
            }

            // 不能用 firstKey ??= childFirst 充当“尚未赋值”哨兵：当 TKey 是 int 等值类型时，
            // default(TKey) 是 0 而不是 null，第二层内部节点就会把子树最小键错误报告成 0。
            // index == 0 才是与键类型无关、也与 comparer 语义无关的第一个孩子判定。
            if (index == 0)
            {
                firstKey = childFirst;
            }

            lastKey = childLast;
            previousLast = childLast;
            hasPrevious = true;
        }

        return true;
    }

    private bool AreStrictlyIncreasing(IReadOnlyList<TKey> keys)
    {
        for (var index = 1; index < keys.Count; index++)
        {
            if (_comparer.Compare(keys[index - 1], keys[index]) >= 0)
            {
                return false;
            }
        }

        return true;
    }

    private sealed class Node(bool isLeaf)
    {
        public List<TKey> Keys { get; } = [];

        public List<TValue> Values { get; } = [];

        public List<Node> Children { get; } = [];

        public Node? NextLeaf { get; set; }

        public bool IsLeaf { get; } = isLeaf;

        public static Node CreateLeaf() => new(isLeaf: true);

        public static Node CreateInternal() => new(isLeaf: false);
    }

    private readonly record struct SplitResult(TKey Separator, Node Right);
}
