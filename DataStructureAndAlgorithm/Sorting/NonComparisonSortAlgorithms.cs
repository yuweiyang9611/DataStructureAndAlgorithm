using DataStructureAndAlgorithm.Diagnostics;

namespace DataStructureAndAlgorithm.Sorting;

/// <summary>不依赖元素两两比较的整数排序。</summary>
public static class NonComparisonSortAlgorithms
{
    private const int MaximumRange = 10_000_000;
    private const int MaximumBucketCount = 1_000_000;

    /// <summary>
    /// 计数排序，支持负数。
    /// </summary>
    /// <remarks>时间 O(n + k)，空间 O(k)，k 为最大值与最小值之间的整数范围。</remarks>
    public static void CountingSort(IList<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count < 2)
        {
            return;
        }

        var minimum = values[0];
        var maximum = values[0];
        for (var index = 1; index < values.Count; index++)
        {
            minimum = Math.Min(minimum, values[index]);
            maximum = Math.Max(maximum, values[index]);
        }

        var range = (long)maximum - minimum + 1;
        if (range > MaximumRange)
        {
            throw new ArgumentException(
                $"The value range is too large for counting sort. Maximum supported range is {MaximumRange}.",
                nameof(values));
        }

        var counts = new int[(int)range];
        foreach (var value in values)
        {
            counts[value - minimum]++;
        }

        var destination = 0;
        for (var offset = 0; offset < counts.Length; offset++)
        {
            for (var occurrence = 0; occurrence < counts[offset]; occurrence++)
            {
                values[destination++] = minimum + offset;
            }
        }
    }

    /// <summary>
    /// 桶排序：把有限实数按值域映射到多个桶，桶内排序后依次写回。
    /// </summary>
    /// <param name="values">待原地排序的有限双精度数。</param>
    /// <param name="bucketCount">桶数量；省略时使用 sqrt(n)，在空间和桶内工作量之间折中。</param>
    /// <remarks>
    /// 当数据近似均匀分布且桶数合适时，平均时间接近 O(n + k)；
    /// 数据全部聚集到一个桶时会退化为桶内排序的 O(n log n)。空间复杂度 O(n + k)。
    /// </remarks>
    public static void BucketSort(IList<double> values, int? bucketCount = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count < 2) return;

        var count = bucketCount ?? Math.Max(1, (int)Math.Sqrt(values.Count));
        if (count is < 1 or > MaximumBucketCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bucketCount), count, $"桶数量必须在 1 到 {MaximumBucketCount} 之间。");
        }

        var minimum = values[0];
        var maximum = values[0];
        foreach (var value in values)
        {
            if (!double.IsFinite(value))
            {
                throw new ArgumentException("桶排序只接受有限实数。", nameof(values));
            }

            minimum = Math.Min(minimum, value);
            maximum = Math.Max(maximum, value);
        }

        if (minimum.Equals(maximum)) return;
        var range = maximum - minimum;
        if (!double.IsFinite(range))
        {
            throw new ArgumentException("数值跨度过大，无法稳定映射到桶。", nameof(values));
        }

        var buckets = new List<double>?[count];
        foreach (var value in values)
        {
            var normalized = (value - minimum) / range;
            // 最大值的 normalized 恰好为 1，需要压回最后一个有效桶下标。
            var bucketIndex = Math.Min((int)(normalized * count), count - 1);
            (buckets[bucketIndex] ??= []).Add(value);
        }

        var destination = 0;
        foreach (var bucket in buckets)
        {
            if (bucket is null) continue;
            // 桶内使用成熟的比较排序；桶排序的重点是“先按值域分治”，而不是重复实现比较排序。
            bucket.Sort();
            foreach (var value in bucket) values[destination++] = value;
        }
    }

    /// <summary>
    /// 对 32 位有符号整数执行稳定的 LSD 基数排序。
    /// </summary>
    /// <remarks>
    /// 每轮处理 8 位，共 4 轮，时间为 O(4n + 4×256)，可视为 O(n)，辅助空间 O(n + 256)。
    /// 将键与 <see cref="int.MinValue"/> 异或会翻转符号位，使有符号升序与 uint 的无符号升序完全一致，
    /// 从而自然支持全部负数以及 int.MinValue/int.MaxValue。
    /// </remarks>
    public static void RadixSort(IList<int> values, IAlgorithmTraceSink? trace = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count < 2) return;

        var source = values.ToArray();
        var destination = new int[source.Length];
        var positions = new int[256];
        trace?.Record("LSD Radix Sort", "Initialize", "复制输入并准备两个交替使用的缓冲区。",
            new Dictionary<string, string> { ["values"] = string.Join(",", source) });

        for (var shift = 0; shift < 32; shift += 8)
        {
            Array.Clear(positions);
            foreach (var value in source)
            {
                positions[Digit(value, shift)]++;
            }

            var nextPosition = 0;
            for (var digit = 0; digit < positions.Length; digit++)
            {
                var occurrences = positions[digit];
                positions[digit] = nextPosition;
                nextPosition += occurrences;
            }

            // 按 source 的原顺序放置相同 digit，保证每一轮稳定；LSD 基数排序必须依赖稳定性。
            foreach (var value in source)
            {
                var digit = Digit(value, shift);
                destination[positions[digit]++] = value;
            }

            (source, destination) = (destination, source);
            trace?.Record("LSD Radix Sort", "Pass", $"完成第 {shift / 8 + 1} 轮稳定分配。",
                new Dictionary<string, string>
                {
                    ["processedBits"] = $"{shift}-{shift + 7}",
                    ["values"] = string.Join(",", source)
                });
        }

        for (var index = 0; index < source.Length; index++) values[index] = source[index];
    }

    private static byte Digit(int value, int shift) =>
        (byte)(((uint)(value ^ int.MinValue) >> shift) & byte.MaxValue);
}
