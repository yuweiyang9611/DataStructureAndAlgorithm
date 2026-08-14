namespace DataStructureAndAlgorithm.Range;

/// <summary>
/// Fenwick 树（树状数组），支持单点增量更新和前缀/区间求和。
/// </summary>
/// <remarks>
/// 对外使用 0 基下标，内部转成 1 基下标。表达式 i &amp; -i 得到最低有效位，
/// 它决定每个内部节点负责的区间长度。更新和查询均为 O(log n)，空间 O(n)。
/// </remarks>
public sealed class FenwickTree
{
    private readonly long[] _tree;

    public FenwickTree(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        Count = count;
        _tree = new long[count + 1];
    }

    public FenwickTree(IReadOnlyList<long> values)
        : this(values?.Count ?? throw new ArgumentNullException(nameof(values)))
    {
        for (var index = 0; index < values.Count; index++)
        {
            Add(index, values[index]);
        }
    }

    public int Count { get; }

    /// <summary>给指定位置增加 delta。</summary>
    public void Add(int index, long delta)
    {
        ValidateIndex(index);

        for (var internalIndex = index + 1;
             internalIndex < _tree.Length;
             internalIndex += internalIndex & -internalIndex)
        {
            _tree[internalIndex] += delta;
        }
    }

    /// <summary>返回区间 [0, endExclusive) 的元素和。</summary>
    public long PrefixSum(int endExclusive)
    {
        if (endExclusive < 0 || endExclusive > Count)
        {
            throw new ArgumentOutOfRangeException(nameof(endExclusive));
        }

        long sum = 0;
        for (var internalIndex = endExclusive;
             internalIndex > 0;
             internalIndex -= internalIndex & -internalIndex)
        {
            sum += _tree[internalIndex];
        }

        return sum;
    }

    /// <summary>返回半开区间 [start, endExclusive) 的元素和。</summary>
    public long RangeSum(int start, int endExclusive)
    {
        ValidateRange(start, endExclusive);
        return PrefixSum(endExclusive) - PrefixSum(start);
    }

    private void ValidateIndex(int index)
    {
        if (index < 0 || index >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    private void ValidateRange(int start, int endExclusive)
    {
        if (start < 0 || endExclusive < start || endExclusive > Count)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "Range must satisfy 0 <= start <= end <= Count.");
        }
    }
}
