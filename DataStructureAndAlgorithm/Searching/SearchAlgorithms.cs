namespace DataStructureAndAlgorithm.Searching;

/// <summary>有序序列上的经典查找算法。</summary>
public static class SearchAlgorithms
{
    /// <summary>
    /// 在升序序列中执行二分查找，找到目标时返回其下标，否则返回 -1。
    /// </summary>
    /// <remarks>
    /// 前置条件是 <paramref name="values"/> 已按照同一个比较器升序排列。
    /// 算法不会主动检查这一条件，因为检查本身需要 O(n)，会抵消二分查找的 O(log n) 优势。
    /// </remarks>
    public static int BinarySearch<T>(
        IReadOnlyList<T> values,
        T target,
        IComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        comparer ??= Comparer<T>.Default;

        var left = 0;
        var right = values.Count - 1;

        while (left <= right)
        {
            // 这种写法避免 left + right 在极大数组上发生整数溢出。
            var middle = left + (right - left) / 2;
            var comparison = comparer.Compare(values[middle], target);

            if (comparison == 0)
            {
                return middle;
            }

            if (comparison < 0)
            {
                left = middle + 1;
            }
            else
            {
                right = middle - 1;
            }
        }

        return -1;
    }

    /// <summary>
    /// 返回第一个“大于等于目标值”的位置；若所有元素都更小，则返回 Count。
    /// </summary>
    public static int LowerBound<T>(
        IReadOnlyList<T> values,
        T target,
        IComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        comparer ??= Comparer<T>.Default;

        // 使用 [left, right) 半开区间。循环结束时区间为空，left 就是答案。
        var left = 0;
        var right = values.Count;

        while (left < right)
        {
            var middle = left + (right - left) / 2;
            if (comparer.Compare(values[middle], target) < 0)
            {
                left = middle + 1;
            }
            else
            {
                right = middle;
            }
        }

        return left;
    }

    /// <summary>
    /// 返回第一个“大于目标值”的位置；若不存在则返回 Count。
    /// </summary>
    public static int UpperBound<T>(
        IReadOnlyList<T> values,
        T target,
        IComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        comparer ??= Comparer<T>.Default;

        var left = 0;
        var right = values.Count;

        while (left < right)
        {
            var middle = left + (right - left) / 2;
            if (comparer.Compare(values[middle], target) <= 0)
            {
                left = middle + 1;
            }
            else
            {
                right = middle;
            }
        }

        return left;
    }
}
