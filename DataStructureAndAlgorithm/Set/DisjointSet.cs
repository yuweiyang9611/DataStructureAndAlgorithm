namespace DataStructureAndAlgorithm.Set;

/// <summary>
/// 并查集（Union-Find），用于维护元素所属的动态连通分量。
/// </summary>
/// <remarks>
/// 路径压缩让 Find 访问过的节点直接靠近根；按大小合并让小树挂到大树下。
/// 两种优化一起使用时，单次操作的摊还复杂度为 O(α(n))，实际中非常接近 O(1)。
/// </remarks>
public sealed class DisjointSet<T> where T : notnull
{
    private sealed class Node(T value)
    {
        public T Value { get; } = value;

        public Node Parent { get; set; } = null!;

        public int Size { get; set; } = 1;
    }

    private readonly Dictionary<T, Node> _nodes;

    public DisjointSet(IEqualityComparer<T>? comparer = null)
    {
        _nodes = new Dictionary<T, Node>(comparer);
    }

    public int Count => _nodes.Count;

    /// <summary>当前互不相交集合的数量。</summary>
    public int SetCount { get; private set; }

    public bool Add(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (_nodes.ContainsKey(value))
        {
            return false;
        }

        var node = new Node(value);
        node.Parent = node;
        _nodes.Add(value, node);
        SetCount++;
        return true;
    }

    /// <summary>返回元素所在集合的代表元素，并执行路径压缩。</summary>
    public T Find(T value)
    {
        var node = GetNode(value);
        return FindRoot(node).Value;
    }

    /// <summary>合并两个集合；原本已连通时返回 false。</summary>
    public bool Union(T first, T second)
    {
        var firstRoot = FindRoot(GetNode(first));
        var secondRoot = FindRoot(GetNode(second));

        if (ReferenceEquals(firstRoot, secondRoot))
        {
            return false;
        }

        // 小树挂到大树，限制树高；交换仅改变局部变量，不改变节点内容。
        if (firstRoot.Size < secondRoot.Size)
        {
            (firstRoot, secondRoot) = (secondRoot, firstRoot);
        }

        secondRoot.Parent = firstRoot;
        firstRoot.Size += secondRoot.Size;
        SetCount--;
        return true;
    }

    public bool AreConnected(T first, T second)
    {
        return ReferenceEquals(FindRoot(GetNode(first)), FindRoot(GetNode(second)));
    }

    public int GetSetSize(T value) => FindRoot(GetNode(value)).Size;

    private Node FindRoot(Node node)
    {
        var root = node;
        while (!ReferenceEquals(root, root.Parent))
        {
            root = root.Parent;
        }

        // 第二次沿路径走，把每个节点直接指向根。
        while (!ReferenceEquals(node, root))
        {
            var next = node.Parent;
            node.Parent = root;
            node = next;
        }

        return root;
    }

    private Node GetNode(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return _nodes.TryGetValue(value, out var node)
            ? node
            : throw new KeyNotFoundException("The specified element does not exist in the disjoint set.");
    }
}
