namespace DataStructureAndAlgorithm.Range;

/// <summary>
/// 支持单点更新和区间聚合的迭代式线段树。
/// </summary>
/// <remarks>
/// <paramref name="combine"/> 必须满足结合律，并提供单位元 <paramref name="identity"/>。
/// 例如求和使用加法与 0，区间最小值使用 Min 与正无穷。
/// 查询和更新均为 O(log n)，建树 O(n)，空间 O(n)。
/// </remarks>
public sealed class SegmentTree<T>
{
    private readonly T[] _tree;
    private readonly Func<T, T, T> _combine;
    private readonly T _identity;
    private readonly int _leafOffset;

    public SegmentTree(IReadOnlyList<T> values, Func<T, T, T> combine, T identity)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(combine);

        Count = values.Count;
        _combine = combine;
        _identity = identity;

        _leafOffset = 1;
        while (_leafOffset < Count)
        {
            _leafOffset *= 2;
        }

        _tree = Enumerable.Repeat(identity, _leafOffset * 2).ToArray();

        for (var index = 0; index < Count; index++)
        {
            _tree[_leafOffset + index] = values[index];
        }

        for (var node = _leafOffset - 1; node > 0; node--)
        {
            _tree[node] = _combine(_tree[node * 2], _tree[node * 2 + 1]);
        }
    }

    public int Count { get; }

    /// <summary>把指定位置替换为新值，并沿祖先链重新计算聚合值。</summary>
    public void Update(int index, T value)
    {
        ValidateIndex(index);
        var node = _leafOffset + index;
        _tree[node] = value;

        for (node /= 2; node > 0; node /= 2)
        {
            _tree[node] = _combine(_tree[node * 2], _tree[node * 2 + 1]);
        }
    }

    /// <summary>聚合半开区间 [start, endExclusive)。</summary>
    public T Query(int start, int endExclusive)
    {
        ValidateRange(start, endExclusive);
        var left = start + _leafOffset;
        var right = endExclusive + _leafOffset;
        var leftResult = _identity;
        var rightResult = _identity;

        while (left < right)
        {
            if ((left & 1) == 1)
            {
                leftResult = _combine(leftResult, _tree[left++]);
            }

            if ((right & 1) == 1)
            {
                rightResult = _combine(_tree[--right], rightResult);
            }

            left /= 2;
            right /= 2;
        }

        // 左右累积方向不能交换，使实现也支持字符串连接等非交换但满足结合律的操作。
        return _combine(leftResult, rightResult);
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
