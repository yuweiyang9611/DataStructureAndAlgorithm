namespace DataStructureAndAlgorithm.Tree;

/// <summary>
/// BK-tree 中一次近似查询命中的值与它到查询值的度量距离。
/// </summary>
/// <param name="Value">树中保存的原始值。</param>
/// <param name="Distance">该值到查询值的距离。</param>
public readonly record struct BkTreeMatch(string Value, int Distance);

/// <summary>
/// 使用度量空间三角不等式加速近似字符串查询的 BK-tree（Burkhard-Keller tree）。
/// </summary>
/// <remarks>
/// <para>
/// 每个节点保存一个字符串；从父节点到子节点的边权，等于两个字符串之间的距离。
/// 同一个父节点下，每一种距离最多只有一条边。插入新词时不断计算它与当前节点的距离，
/// 沿对应边向下；若该距离的边尚不存在，就把新词挂到这里。
/// </para>
/// <para>
/// 查询半径为 <c>r</c>、查询值到当前节点的距离为 <c>d</c> 时，三角不等式保证：
/// 只有边权位于 <c>[d-r, d+r]</c> 的子树才可能包含答案。其余子树可以安全剪枝，
/// 这正是 BK-tree 相比“扫描整个词典并逐词计算编辑距离”的价值。
/// </para>
/// <para>
/// 调用方传入的距离函数必须是非负度量，尤其要满足对称性和三角不等式；
/// Levenshtein 编辑距离是典型选择。类本身不写死某一种距离，因此也可用于其他字符串度量。
/// 返回结果会按“距离升序、字符串序号字典序”统一排序，避免输出依赖插入顺序。
/// </para>
/// </remarks>
public sealed class BkTree
{
    private readonly Func<string, string, int> _distance;
    private readonly StringComparer _valueComparer;
    private Node? _root;

    /// <summary>
    /// 创建一棵空 BK-tree。
    /// </summary>
    /// <param name="distance">
    /// 非负的字符串度量函数。函数返回负数时会抛出异常，因为负边权会破坏剪枝区间。
    /// </param>
    /// <param name="valueComparer">
    /// 用于识别完全相同的值并稳定排序；默认使用不受区域性影响的序号比较。
    /// </param>
    public BkTree(
        Func<string, string, int> distance,
        StringComparer? valueComparer = null)
    {
        ArgumentNullException.ThrowIfNull(distance);
        _distance = distance;
        _valueComparer = valueComparer ?? StringComparer.Ordinal;
    }

    /// <summary>树中不同字符串的数量。</summary>
    public int Count { get; private set; }

    /// <summary>
    /// 插入一个字符串。
    /// </summary>
    /// <returns>首次插入返回 <see langword="true"/>；比较器认定的重复值返回 <see langword="false"/>。</returns>
    public bool Add(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (_root is null)
        {
            _root = new Node(value);
            Count = 1;
            return true;
        }

        var current = _root;
        while (true)
        {
            // 先用比较器处理重复值，既避免无意义的距离计算，也让“是否重复”的语义
            // 与最终结果排序使用同一套字符串规则。
            if (_valueComparer.Equals(current.Value, value))
            {
                return false;
            }

            var edgeDistance = Measure(value, current.Value);
            Node? next = null;
            foreach (var edge in current.Children)
            {
                if (edge.Distance == edgeDistance)
                {
                    next = edge.Child;
                    break;
                }
            }

            if (next is not null)
            {
                current = next;
                continue;
            }

            // 同一节点的距离边是唯一的。用紧凑 List 显式保存边，避免 Dictionary
            // 隐藏 BK-tree 的“距离就是边标签”这一核心结构，也便于学习者逐步调试。
            current.Children.Add(new Edge(edgeDistance, new Node(value)));
            Count = checked(Count + 1);
            return true;
        }
    }

    /// <summary>
    /// 返回与查询值距离不超过指定阈值的全部字符串。
    /// </summary>
    /// <param name="query">待匹配字符串。</param>
    /// <param name="maximumDistance">允许的最大距离，必须非负。</param>
    public IReadOnlyList<BkTreeMatch> Search(string query, int maximumDistance)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumDistance);

        if (_root is null)
        {
            return [];
        }

        var matches = new List<BkTreeMatch>();
        var pending = new Stack<Node>();
        pending.Push(_root);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            var distanceToQuery = Measure(query, current.Value);
            if (distanceToQuery <= maximumDistance)
            {
                matches.Add(new BkTreeMatch(current.Value, distanceToQuery));
            }

            // 若 child 到 current 的边权为 e，则 |d-e| <= r 才可能让 child
            // 或其后代进入查询半径。使用 long 计算上界，避免 d+r 在极端输入下溢出。
            var minimumEdge = Math.Max(0, distanceToQuery - maximumDistance);
            var maximumEdge = (long)distanceToQuery + maximumDistance;
            foreach (var edge in current.Children)
            {
                if (edge.Distance >= minimumEdge && edge.Distance <= maximumEdge)
                {
                    pending.Push(edge.Child);
                }
            }
        }

        matches.Sort(new MatchComparer(_valueComparer));
        return matches;
    }

    private int Measure(string left, string right)
    {
        var result = _distance(left, right);
        return result >= 0
            ? result
            : throw new InvalidOperationException("BK-tree 的距离函数不能返回负数。");
    }

    private sealed class Node(string value)
    {
        public string Value { get; } = value;

        public List<Edge> Children { get; } = [];
    }

    private readonly record struct Edge(int Distance, Node Child);

    private sealed class MatchComparer(StringComparer valueComparer) : IComparer<BkTreeMatch>
    {
        public int Compare(BkTreeMatch left, BkTreeMatch right)
        {
            var byDistance = left.Distance.CompareTo(right.Distance);
            return byDistance != 0
                ? byDistance
                : valueComparer.Compare(left.Value, right.Value);
        }
    }
}
