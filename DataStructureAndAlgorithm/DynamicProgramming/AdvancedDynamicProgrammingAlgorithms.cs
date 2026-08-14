namespace DataStructureAndAlgorithm.DynamicProgramming;

/// <summary>更多经典动态规划示例。</summary>
public static class AdvancedDynamicProgrammingAlgorithms
{
    /// <summary>
    /// 计算把 <paramref name="source"/> 转换成 <paramref name="target"/> 所需的最少单字符编辑次数。
    /// </summary>
    /// <remarks>允许插入、删除和替换。时间 O(nm)，空间 O(min(n,m))。</remarks>
    public static int EditDistance(string source, string target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        // 让 target 成为较短字符串，二维表压缩后只需两行较短数组。
        if (source.Length < target.Length)
        {
            (source, target) = (target, source);
        }

        var previous = Enumerable.Range(0, target.Length + 1).ToArray();
        var current = new int[target.Length + 1];

        for (var sourceLength = 1; sourceLength <= source.Length; sourceLength++)
        {
            current[0] = sourceLength;

            for (var targetLength = 1; targetLength <= target.Length; targetLength++)
            {
                var replacementCost = source[sourceLength - 1] == target[targetLength - 1] ? 0 : 1;
                current[targetLength] = Math.Min(
                    Math.Min(
                        previous[targetLength] + 1,       // 删除 source 当前字符
                        current[targetLength - 1] + 1),  // 插入 target 当前字符
                    previous[targetLength - 1] + replacementCost);
            }

            (previous, current) = (current, previous);
        }

        return previous[target.Length];
    }

    /// <summary>
    /// 返回一个最长严格递增子序列，而不仅是其长度。
    /// </summary>
    /// <remarks>使用“牌堆顶 + 前驱下标”方法，时间 O(n log n)，空间 O(n)。</remarks>
    public static IReadOnlyList<T> LongestIncreasingSubsequence<T>(
        IReadOnlyList<T> values,
        IComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            return [];
        }

        comparer ??= Comparer<T>.Default;
        var tailIndexes = new int[values.Count];
        var previousIndexes = Enumerable.Repeat(-1, values.Count).ToArray();
        var length = 0;

        for (var index = 0; index < values.Count; index++)
        {
            // 找第一个 >= 当前值的牌堆顶，替换它可为后续元素留下更小的结尾。
            var left = 0;
            var right = length;
            while (left < right)
            {
                var middle = left + (right - left) / 2;
                if (comparer.Compare(values[tailIndexes[middle]], values[index]) < 0)
                {
                    left = middle + 1;
                }
                else
                {
                    right = middle;
                }
            }

            if (left > 0)
            {
                previousIndexes[index] = tailIndexes[left - 1];
            }

            tailIndexes[left] = index;
            if (left == length)
            {
                length++;
            }
        }

        var result = new T[length];
        var currentIndex = tailIndexes[length - 1];
        for (var resultIndex = length - 1; resultIndex >= 0; resultIndex--)
        {
            result[resultIndex] = values[currentIndex];
            currentIndex = previousIndexes[currentIndex];
        }

        return result;
    }
}
