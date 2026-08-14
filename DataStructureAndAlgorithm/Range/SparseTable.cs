using System.Numerics;

namespace DataStructureAndAlgorithm.Range;

/// <summary>
/// 面向静态数组的稀疏表，可在 O(1) 时间完成幂等区间查询。
/// </summary>
/// <remarks>
/// combine 必须满足结合律和幂等律 f(x,x)=x，例如 Min、Max、GCD。查询把区间覆盖为两个可能重叠的 2^k 块；
/// 幂等性保证重叠元素被计算两次也不改变答案。预处理 O(n log n)，空间 O(n log n)，不支持更新。
/// </remarks>
public sealed class SparseTable<T>
{
    private readonly T[][] _levels;
    private readonly Func<T, T, T> _combine;

    public SparseTable(IReadOnlyList<T> values, Func<T, T, T> combine)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(combine);
        Count = values.Count;
        _combine = combine;
        if (Count == 0)
        {
            _levels = [];
            return;
        }

        var levelCount = BitOperations.Log2((uint)Count) + 1;
        _levels = new T[levelCount][];
        _levels[0] = values.ToArray();
        for (var level = 1; level < levelCount; level++)
        {
            var blockLength = 1 << level;
            var half = blockLength >> 1;
            var itemCount = Count - blockLength + 1;
            _levels[level] = new T[itemCount];
            for (var start = 0; start < itemCount; start++)
            {
                _levels[level][start] = _combine(_levels[level - 1][start], _levels[level - 1][start + half]);
            }
        }
    }

    public int Count { get; }

    /// <summary>查询非空半开区间 [start, endExclusive)。</summary>
    public T Query(int start, int endExclusive)
    {
        if (start < 0 || endExclusive > Count || start >= endExclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "Range must satisfy 0 <= start < end <= Count.");
        }

        var length = endExclusive - start;
        var level = BitOperations.Log2((uint)length);
        var blockLength = 1 << level;
        return _combine(_levels[level][start], _levels[level][endExclusive - blockLength]);
    }
}
