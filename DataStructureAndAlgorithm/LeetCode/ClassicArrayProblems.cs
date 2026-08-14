namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>LeetCode 中经典的数组、哈希和双指针题目。</summary>
public static class ClassicArrayProblems
{
    /// <summary>
    /// LeetCode 1 - Two Sum：返回和为 target 的两个不同下标。
    /// </summary>
    /// <remarks>哈希表保存已访问值，时间 O(n)，空间 O(n)。</remarks>
    public static int[] TwoSum(IReadOnlyList<int> numbers, int target)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        var seen = new Dictionary<int, int>();

        for (var index = 0; index < numbers.Count; index++)
        {
            // 使用 long 计算补数，避免 int 回绕制造并不存在的答案。
            var complement = (long)target - numbers[index];
            if (complement is >= int.MinValue and <= int.MaxValue &&
                seen.TryGetValue((int)complement, out var otherIndex))
            {
                return [otherIndex, index];
            }

            seen.TryAdd(numbers[index], index);
        }

        return [];
    }

    /// <summary>
    /// LeetCode 15 - 3Sum：返回所有和为 0 的不重复三元组。
    /// </summary>
    /// <remarks>排序后固定一个数，另外两个数使用双指针。时间 O(n²)，额外空间 O(n)。</remarks>
    public static IReadOnlyList<IReadOnlyList<int>> ThreeSum(IReadOnlyList<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        var sorted = numbers.ToArray();
        Array.Sort(sorted);
        var result = new List<IReadOnlyList<int>>();

        for (var first = 0; first < sorted.Length - 2; first++)
        {
            if (first > 0 && sorted[first] == sorted[first - 1])
            {
                continue;
            }

            if (sorted[first] > 0)
            {
                break;
            }

            var left = first + 1;
            var right = sorted.Length - 1;

            while (left < right)
            {
                var sum = (long)sorted[first] + sorted[left] + sorted[right];
                if (sum < 0)
                {
                    left++;
                    continue;
                }

                if (sum > 0)
                {
                    right--;
                    continue;
                }

                result.Add([sorted[first], sorted[left], sorted[right]]);
                var leftValue = sorted[left];
                var rightValue = sorted[right];

                // 找到答案后同时跳过两侧重复值，避免重复三元组。
                while (left < right && sorted[left] == leftValue)
                {
                    left++;
                }

                while (left < right && sorted[right] == rightValue)
                {
                    right--;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// LeetCode 42 - Trapping Rain Water：计算柱状图能够接住的雨水量。
    /// </summary>
    /// <remarks>双指针维护左右最高柱，时间 O(n)，空间 O(1)。</remarks>
    public static long TrapRainWater(IReadOnlyList<int> heights)
    {
        ArgumentNullException.ThrowIfNull(heights);
        if (heights.Any(height => height < 0))
        {
            throw new ArgumentException("Heights must be non-negative.", nameof(heights));
        }

        var left = 0;
        var right = heights.Count - 1;
        var leftMaximum = 0;
        var rightMaximum = 0;
        long water = 0;

        while (left <= right)
        {
            // 较低一侧的最高边界已经足以确定该位置水量，不必等待另一侧最终最大值。
            if (leftMaximum <= rightMaximum)
            {
                leftMaximum = Math.Max(leftMaximum, heights[left]);
                water += leftMaximum - heights[left];
                left++;
            }
            else
            {
                rightMaximum = Math.Max(rightMaximum, heights[right]);
                water += rightMaximum - heights[right];
                right--;
            }
        }

        return water;
    }

    /// <summary>
    /// LeetCode 121 - Best Time to Buy and Sell Stock：一次买入卖出的最大利润。
    /// </summary>
    /// <remarks>扫描时维护此前最低价格，时间 O(n)，空间 O(1)。</remarks>
    public static int MaximumStockProfit(IReadOnlyList<int> prices)
    {
        ArgumentNullException.ThrowIfNull(prices);
        if (prices.Any(price => price < 0))
        {
            throw new ArgumentException("Prices must be non-negative.", nameof(prices));
        }

        var minimumPrice = int.MaxValue;
        var maximumProfit = 0;

        foreach (var price in prices)
        {
            minimumPrice = Math.Min(minimumPrice, price);
            maximumProfit = Math.Max(maximumProfit, price - minimumPrice);
        }

        return maximumProfit;
    }

    /// <summary>
    /// LeetCode 215 - Kth Largest Element：返回第 k 大元素。
    /// </summary>
    /// <remarks>Quickselect 平均 O(n)、最坏 O(n²)，复制输入以避免修改调用方数据。</remarks>
    public static int FindKthLargest(IReadOnlyList<int> numbers, int k)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        if (k < 1 || k > numbers.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(k));
        }

        var values = numbers.ToArray();
        var targetIndex = values.Length - k;
        var left = 0;
        var right = values.Length - 1;

        while (true)
        {
            var pivotIndex = Partition(values, left, right);
            if (pivotIndex == targetIndex)
            {
                return values[pivotIndex];
            }

            if (pivotIndex < targetIndex)
            {
                left = pivotIndex + 1;
            }
            else
            {
                right = pivotIndex - 1;
            }
        }
    }

    private static int Partition(int[] values, int left, int right)
    {
        var pivotIndex = left + (right - left) / 2;
        (values[pivotIndex], values[right]) = (values[right], values[pivotIndex]);
        var pivot = values[right];
        var storeIndex = left;

        for (var index = left; index < right; index++)
        {
            if (values[index] <= pivot)
            {
                (values[index], values[storeIndex]) = (values[storeIndex], values[index]);
                storeIndex++;
            }
        }

        (values[storeIndex], values[right]) = (values[right], values[storeIndex]);
        return storeIndex;
    }
}
