namespace DataStructureAndAlgorithm.Range;

/// <summary>
/// 支持“区间统一增加”和“区间最大值查询”的懒标记线段树。
/// </summary>
/// <remarks>
/// <para>
/// 这棵树维护三个彼此配合的不变量：<c>_maximum[node]</c> 与 <c>_minimum[node]</c>
/// 分别是当前节点所覆盖区间的真实最大值、最小值；<c>_lazy[node]</c> 是已经作用到这两个极值、
/// 但还没有传播给孩子的统一增量。因此，当更新区间完全覆盖某个节点时，只需同时修改这三个字段，
/// 无须下降到每一个叶子。
/// </para>
/// <para>
/// 建树的时间复杂度为 O(n)，<see cref="RangeAdd"/> 与 <see cref="QueryMax"/> 的时间复杂度均为
/// O(log n)，空间复杂度为 O(n)。公开数据仍使用 <see cref="long"/>，每次更新同时检查最大值和最小值；
/// 内部懒增量使用 <see cref="Int128"/>，是因为两个合法的 <see cref="long"/> 值之差可能需要 65 位表示。
/// 这样，成功返回的更新就保证所有叶子仍在 <see cref="long"/> 范围内，之后的查询不会等到下推懒标记时才溢出。
/// </para>
/// <para>
/// 所有区间均采用半开形式 <c>[start, endExclusive)</c>。半开区间可让长度直接写成
/// <c>endExclusive - start</c>，也能自然表示空区间。空区间的最大值没有数学定义，因此
/// <see cref="QueryMax"/> 返回最大值运算的单位元 <see cref="long.MinValue"/>。
/// </para>
/// </remarks>
public sealed class LazyRangeAddMaxSegmentTree
{
    private readonly long[] _maximum;
    private readonly long[] _minimum;
    private readonly Int128[] _lazy;

    /// <summary>从给定值建立线段树。</summary>
    /// <param name="values">要复制到树中的初始序列；之后修改原序列不会影响本对象。</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="ArgumentOutOfRangeException">序列大到无法安全计算内部数组长度。</exception>
    public LazyRangeAddMaxSegmentTree(IReadOnlyList<long> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        Count = values.Count;
        if (Count > int.MaxValue / 4)
        {
            // 实际运行时会更早受到可用内存限制，但显式保护可避免 Count * 4 发生整数回绕，
            // 让失败原因保持为稳定的参数错误，而不是难以理解的负数组长度错误。
            throw new ArgumentOutOfRangeException(
                nameof(values),
                values.Count,
                "元素数量过大，无法建立线段树的内部数组。");
        }

        _maximum = new long[Math.Max(1, Count * 4)];
        _minimum = new long[_maximum.Length];
        _lazy = new Int128[_maximum.Length];
        if (Count > 0)
        {
            Build(node: 1, left: 0, right: Count, values);
        }
    }

    /// <summary>序列中的元素数量。</summary>
    public int Count { get; }

    /// <summary>
    /// 给半开区间 <c>[start, endExclusive)</c> 内的每个元素增加同一个值。
    /// </summary>
    /// <remarks>
    /// 空区间是合法的恒等操作。使用负增量可以撤销一次预订，这也是分支限界搜索回溯时的重要能力。
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">区间不满足 <c>0 &lt;= start &lt;= endExclusive &lt;= Count</c>。</exception>
    /// <exception cref="OverflowException">更新后至少一个元素会超出 <see cref="long"/> 范围。</exception>
    public void RangeAdd(int start, int endExclusive, long delta)
    {
        ValidateRange(start, endExclusive);
        if (start == endExclusive || delta == 0)
        {
            return;
        }

        // checked 溢出是公开契约，调用方捕获后理应仍能继续使用同一对象。区间更新可能先修改左分支，
        // 再在右分支溢出，因此只让单个 Apply 原子化还不够；记录本次触及节点的原值，失败时整体回滚。
        // 线段树一次区间更新只触及 O(log n) 个规范节点，回滚日志不会改变渐进复杂度。
        var snapshots = new Dictionary<int, NodeSnapshot>();
        try
        {
            Add(node: 1, left: 0, right: Count, start, endExclusive, delta, snapshots);
        }
        catch (OverflowException)
        {
            foreach (var (node, snapshot) in snapshots)
            {
                _maximum[node] = snapshot.Maximum;
                _minimum[node] = snapshot.Minimum;
                _lazy[node] = snapshot.Lazy;
            }

            throw;
        }
    }

    /// <summary>
    /// 查询半开区间 <c>[start, endExclusive)</c> 内的最大值。
    /// </summary>
    /// <returns>非空区间的最大值；空区间返回 <see cref="long.MinValue"/>。</returns>
    /// <exception cref="ArgumentOutOfRangeException">区间不满足 <c>0 &lt;= start &lt;= endExclusive &lt;= Count</c>。</exception>
    public long QueryMax(int start, int endExclusive)
    {
        ValidateRange(start, endExclusive);
        return start == endExclusive
            ? long.MinValue
            : QueryMax(node: 1, left: 0, right: Count, start, endExclusive);
    }

    private void Build(int node, int left, int right, IReadOnlyList<long> values)
    {
        if (right - left == 1)
        {
            _maximum[node] = values[left];
            _minimum[node] = values[left];
            return;
        }

        var middle = left + (right - left) / 2;
        Build(node * 2, left, middle, values);
        Build(node * 2 + 1, middle, right, values);
        Pull(node);
    }

    private void Add(
        int node,
        int left,
        int right,
        int queryLeft,
        int queryRight,
        long delta,
        Dictionary<int, NodeSnapshot> snapshots)
    {
        if (queryLeft <= left && right <= queryRight)
        {
            Apply(node, delta, snapshots);
            return;
        }

        // 只有部分覆盖才需要下降。先把父节点欠下的增量传给孩子，才能保证孩子的 maximum
        // 仍代表真实值；递归结束后再由两个孩子重新计算父节点。
        Push(node, right - left, snapshots);
        var middle = left + (right - left) / 2;
        if (queryLeft < middle)
        {
            Add(node * 2, left, middle, queryLeft, queryRight, delta, snapshots);
        }

        if (middle < queryRight)
        {
            Add(node * 2 + 1, middle, right, queryLeft, queryRight, delta, snapshots);
        }

        Pull(node, snapshots);
    }

    private long QueryMax(
        int node,
        int left,
        int right,
        int queryLeft,
        int queryRight)
    {
        if (queryLeft <= left && right <= queryRight)
        {
            return _maximum[node];
        }

        Push(node, right - left);
        var middle = left + (right - left) / 2;
        var result = long.MinValue;
        if (queryLeft < middle)
        {
            result = QueryMax(node * 2, left, middle, queryLeft, queryRight);
        }

        if (middle < queryRight)
        {
            result = Math.Max(
                result,
                QueryMax(node * 2 + 1, middle, right, queryLeft, queryRight));
        }

        return result;
    }

    private void Apply(int node, Int128 delta, Dictionary<int, NodeSnapshot>? snapshots = null)
    {
        // maximum 只能证明正增量安全；负增量必须检查 minimum，否则“成功”的更新可能在以后下推时才溢出。
        // 三个结果全部预计算成功后才提交，保证当前节点不会留下半次修改。
        var nextMaximum = checked((long)((Int128)_maximum[node] + delta));
        var nextMinimum = checked((long)((Int128)_minimum[node] + delta));
        var nextLazy = checked(_lazy[node] + delta);
        Capture(node, snapshots);
        _maximum[node] = nextMaximum;
        _minimum[node] = nextMinimum;
        _lazy[node] = nextLazy;
    }

    private void Push(int node, int length, Dictionary<int, NodeSnapshot>? snapshots = null)
    {
        if (_lazy[node] == 0 || length == 1)
        {
            return;
        }

        // Int128 懒增量可容纳任意两个 long 值之差，避免“当前值合法，但历史增量之和超出 long”的假溢出。
        // 左右孩子的六个结果全部成功后才一起提交，避免“左孩子已改变、右孩子溢出”的半下推状态。
        var delta = _lazy[node];
        var left = node * 2;
        var right = left + 1;
        var leftMaximum = checked((long)((Int128)_maximum[left] + delta));
        var leftMinimum = checked((long)((Int128)_minimum[left] + delta));
        var leftLazy = checked(_lazy[left] + delta);
        var rightMaximum = checked((long)((Int128)_maximum[right] + delta));
        var rightMinimum = checked((long)((Int128)_minimum[right] + delta));
        var rightLazy = checked(_lazy[right] + delta);

        Capture(node, snapshots);
        Capture(left, snapshots);
        Capture(right, snapshots);
        _maximum[left] = leftMaximum;
        _minimum[left] = leftMinimum;
        _lazy[left] = leftLazy;
        _maximum[right] = rightMaximum;
        _minimum[right] = rightMinimum;
        _lazy[right] = rightLazy;
        _lazy[node] = 0;
    }

    private void Pull(int node, Dictionary<int, NodeSnapshot>? snapshots = null)
    {
        Capture(node, snapshots);
        _maximum[node] = Math.Max(_maximum[node * 2], _maximum[node * 2 + 1]);
        _minimum[node] = Math.Min(_minimum[node * 2], _minimum[node * 2 + 1]);
    }

    private void Capture(int node, Dictionary<int, NodeSnapshot>? snapshots)
    {
        snapshots?.TryAdd(node, new NodeSnapshot(_maximum[node], _minimum[node], _lazy[node]));
    }

    private void ValidateRange(int start, int endExclusive)
    {
        if (start < 0 || endExclusive < start || endExclusive > Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(start),
                $"区间必须满足 0 <= start <= endExclusive <= {Count}。");
        }
    }

    private readonly record struct NodeSnapshot(long Maximum, long Minimum, Int128 Lazy);
}
