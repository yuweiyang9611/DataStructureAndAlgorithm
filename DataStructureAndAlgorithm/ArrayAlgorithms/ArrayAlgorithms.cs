namespace DataStructureAndAlgorithm.ArrayAlgorithms;

/// <summary>数组、双指针、滑动窗口和前缀状态的常用算法。</summary>
public static class ArrayAlgorithms
{
    /// <summary>
    /// 返回每个固定长度窗口的最大值。
    /// </summary>
    /// <remarks>
    /// 双端队列保存“值单调递减”的候选下标，每个下标最多进出队一次，时间 O(n)。
    /// </remarks>
    public static IReadOnlyList<T> SlidingWindowMaximum<T>(
        IReadOnlyList<T> values,
        int windowSize,
        IComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (windowSize < 1 || windowSize > values.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSize));
        }

        comparer ??= Comparer<T>.Default;
        var deque = new int[values.Count];
        var head = 0;
        var tail = 0;
        var result = new List<T>(values.Count - windowSize + 1);

        for (var index = 0; index < values.Count; index++)
        {
            while (head < tail && deque[head] <= index - windowSize)
            {
                head++;
            }

            while (head < tail && comparer.Compare(values[deque[tail - 1]], values[index]) <= 0)
            {
                tail--;
            }

            deque[tail++] = index;

            if (index >= windowSize - 1)
            {
                result.Add(values[deque[head]]);
            }
        }

        return result;
    }

    /// <summary>返回和最大的连续非空子数组及其半开区间。</summary>
    public static MaximumSubarrayResult MaximumSubarray(IReadOnlyList<long> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            throw new ArgumentException("The input must contain at least one value.", nameof(values));
        }

        var bestSum = values[0];
        var currentSum = values[0];
        var bestStart = 0;
        var bestEnd = 1;
        var currentStart = 0;

        for (var index = 1; index < values.Count; index++)
        {
            if (currentSum < 0)
            {
                currentSum = values[index];
                currentStart = index;
            }
            else
            {
                currentSum += values[index];
            }

            if (currentSum > bestSum)
            {
                bestSum = currentSum;
                bestStart = currentStart;
                bestEnd = index + 1;
            }
        }

        return new MaximumSubarrayResult(bestSum, bestStart, bestEnd);
    }

    /// <summary>在整数数组中寻找和为 target 的两个不同下标，找不到时返回 null。</summary>
    public static (int First, int Second)? TwoSum(IReadOnlyList<int> values, int target)
    {
        ArgumentNullException.ThrowIfNull(values);
        var seen = new Dictionary<int, int>();

        for (var index = 0; index < values.Count; index++)
        {
            // ???? long??? target - value ? int ??????????
            var complement = (long)target - values[index];
            if (complement is >= int.MinValue and <= int.MaxValue &&
                seen.TryGetValue((int)complement, out var otherIndex))
            {
                return (otherIndex, index);
            }

            // 保留第一次出现的位置，使返回结果稳定且易于测试。
            seen.TryAdd(values[index], index);
        }

        return null;
    }
}

/// <summary>最大连续子数组的和及其半开区间 [Start, EndExclusive)。</summary>
public readonly record struct MaximumSubarrayResult(long Sum, int Start, int EndExclusive);
