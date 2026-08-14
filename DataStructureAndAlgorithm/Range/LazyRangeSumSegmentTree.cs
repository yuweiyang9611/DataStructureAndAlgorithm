namespace DataStructureAndAlgorithm.Range;

/// <summary>
/// 支持区间加法与区间求和的懒标记线段树。
/// </summary>
/// <remarks>
/// 完全覆盖节点时只更新该节点的 sum 和 lazy，而不立刻访问所有叶子；以后真正下降到子树时再 Push。
/// 因而建树 O(n)，RangeAdd 与 Query 都是 O(log n)，空间 O(n)。使用 long 降低累计和溢出的概率，但调用者仍需控制数值范围。
/// </remarks>
public sealed class LazyRangeSumSegmentTree
{
    private readonly long[] _sums;
    private readonly long[] _lazy;

    public LazyRangeSumSegmentTree(IReadOnlyList<long> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Count = values.Count;
        _sums = new long[Math.Max(1, Count * 4)];
        _lazy = new long[_sums.Length];
        if (Count > 0) Build(1, 0, Count, values);
    }

    public int Count { get; }

    public void RangeAdd(int start, int endExclusive, long delta)
    {
        ValidateRange(start, endExclusive);
        if (start == endExclusive || delta == 0)
        {
            return;
        }

        var snapshots = new Dictionary<int, NodeSnapshot>();
        try
        {
            Add(1, 0, Count, start, endExclusive, delta, snapshots);
        }
        catch (OverflowException)
        {
            Restore(snapshots);
            throw;
        }
    }

    public long Query(int start, int endExclusive)
    {
        ValidateRange(start, endExclusive);
        if (start == endExclusive)
        {
            return 0;
        }

        // 查询为了下降到子区间也会下推懒标记。若某个孩子的区间和溢出，必须撤销此前已经完成的下推，
        // 否则一次失败的只读调用会悄悄改变后续结果。
        var snapshots = new Dictionary<int, NodeSnapshot>();
        try
        {
            return Query(1, 0, Count, start, endExclusive, snapshots);
        }
        catch (OverflowException)
        {
            Restore(snapshots);
            throw;
        }
    }

    private void Build(int node, int left, int right, IReadOnlyList<long> values)
    {
        if (right - left == 1)
        {
            _sums[node] = values[left];
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
            Apply(node, right - left, delta, snapshots);
            return;
        }

        Push(node, left, right, snapshots);
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

    private long Query(
        int node,
        int left,
        int right,
        int queryLeft,
        int queryRight,
        Dictionary<int, NodeSnapshot> snapshots)
    {
        if (queryLeft <= left && right <= queryRight)
        {
            return _sums[node];
        }

        Push(node, left, right, snapshots);
        var middle = left + (right - left) / 2;
        var result = 0L;
        if (queryLeft < middle)
        {
            result = Query(node * 2, left, middle, queryLeft, queryRight, snapshots);
        }

        if (middle < queryRight)
        {
            result = checked(result + Query(node * 2 + 1, middle, right, queryLeft, queryRight, snapshots));
        }

        return result;
    }

    private void Apply(
        int node,
        int length,
        long delta,
        Dictionary<int, NodeSnapshot>? snapshots = null)
    {
        // 两个结果全部算出后才写入，避免同一节点出现 sum 已变、lazy 尚未变的半提交状态。
        var nextSum = checked(_sums[node] + delta * length);
        var nextLazy = checked(_lazy[node] + delta);
        Capture(node, snapshots);
        _sums[node] = nextSum;
        _lazy[node] = nextLazy;
    }

    private void Push(
        int node,
        int left,
        int right,
        Dictionary<int, NodeSnapshot>? snapshots = null)
    {
        if (_lazy[node] == 0 || right - left == 1)
        {
            return;
        }

        var middle = left + (right - left) / 2;
        var delta = _lazy[node];
        var leftChild = node * 2;
        var rightChild = leftChild + 1;

        // 先计算左右孩子的全部新值，再一次性提交，保证单次 Push 自身也是原子的。
        var leftSum = checked(_sums[leftChild] + delta * (middle - left));
        var leftLazy = checked(_lazy[leftChild] + delta);
        var rightSum = checked(_sums[rightChild] + delta * (right - middle));
        var rightLazy = checked(_lazy[rightChild] + delta);

        Capture(node, snapshots);
        Capture(leftChild, snapshots);
        Capture(rightChild, snapshots);
        _sums[leftChild] = leftSum;
        _lazy[leftChild] = leftLazy;
        _sums[rightChild] = rightSum;
        _lazy[rightChild] = rightLazy;
        _lazy[node] = 0;
    }

    private void Pull(int node, Dictionary<int, NodeSnapshot>? snapshots = null)
    {
        var sum = checked(_sums[node * 2] + _sums[node * 2 + 1]);
        Capture(node, snapshots);
        _sums[node] = sum;
    }

    private void Capture(int node, Dictionary<int, NodeSnapshot>? snapshots)
    {
        snapshots?.TryAdd(node, new NodeSnapshot(_sums[node], _lazy[node]));
    }

    private void Restore(Dictionary<int, NodeSnapshot> snapshots)
    {
        foreach (var (node, snapshot) in snapshots)
        {
            _sums[node] = snapshot.Sum;
            _lazy[node] = snapshot.Lazy;
        }
    }

    private void ValidateRange(int start, int endExclusive)
    {
        if (start < 0 || endExclusive < start || endExclusive > Count)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "Range must satisfy 0 <= start <= end <= Count.");
        }
    }

    private readonly record struct NodeSnapshot(long Sum, long Lazy);
}
