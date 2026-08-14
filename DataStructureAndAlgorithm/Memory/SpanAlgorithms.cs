using System.Buffers;

namespace DataStructureAndAlgorithm.Memory;

/// <summary>展示 Span 零拷贝视图和 ArrayPool 缓冲区复用的算法实现。</summary>
public static class SpanAlgorithms
{
    /// <summary>
    /// 在已排序只读 Span 上二分查找。Span 不拥有内存，可直接观察数组、栈内存或非托管内存的连续切片而不复制。
    /// </summary>
    public static int BinarySearch<T>(ReadOnlySpan<T> values, T target, IComparer<T>? comparer = null)
    {
        comparer ??= Comparer<T>.Default;
        var left = 0;
        var right = values.Length - 1;
        while (left <= right)
        {
            var middle = left + (right - left) / 2;
            var comparison = comparer.Compare(values[middle], target);
            if (comparison == 0) return middle;
            if (comparison < 0) left = middle + 1;
            else right = middle - 1;
        }

        return ~left; // 与 Array.BinarySearch 一致：按位取反后得到可以保持有序的插入位置。
    }

    /// <summary>
    /// 对 32 位有符号整数执行 LSD 基数排序，辅助数组从池中租用，避免每次调用都分配大数组。
    /// </summary>
    /// <remarks>
    /// 租出的数组可能大于请求长度且含旧数据，因此只使用 [0, values.Length)；finally 保证异常时也归还。
    /// 池化并不保证一定更快，尤其是小数组，所以项目保留普通教学版并通过 BenchmarkDotNet 比较后再选择。
    /// </remarks>
    public static void PooledRadixSort(Span<int> values, ArrayPool<int>? pool = null)
    {
        if (values.Length < 2) return;
        pool ??= ArrayPool<int>.Shared;
        var buffer = pool.Rent(values.Length);
        try
        {
            var rented = buffer.AsSpan(0, values.Length);
            Span<int> counts = stackalloc int[256];
            for (var shift = 0; shift < 32; shift += 8)
            {
                counts.Clear();
                var evenPass = (shift / 8 & 1) == 0;
                var source = evenPass ? values : rented;
                var destination = evenPass ? rented : values;
                foreach (var value in source) counts[Digit(value, shift)]++;

                var nextPosition = 0;
                for (var digit = 0; digit < counts.Length; digit++)
                {
                    var occurrences = counts[digit];
                    counts[digit] = nextPosition;
                    nextPosition += occurrences;
                }

                foreach (var value in source)
                {
                    var digit = Digit(value, shift);
                    destination[counts[digit]++] = value;
                }
            }
        }
        finally
        {
            // int 不含托管引用，不清零可减少归还成本；敏感数据场景应使用 clearArray: true。
            pool.Return(buffer, clearArray: false);
        }
    }

    private static byte Digit(int value, int shift) =>
        (byte)(((uint)(value ^ int.MinValue) >> shift) & byte.MaxValue);
}
