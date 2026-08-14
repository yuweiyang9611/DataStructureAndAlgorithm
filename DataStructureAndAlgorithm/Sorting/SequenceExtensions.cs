namespace DataStructureAndAlgorithm.Sorting;

/// <summary>
/// 使用 C# 14 扩展块（extension block）声明的序列算法。
/// </summary>
/// <remarks>
/// 扩展块把同一接收者类型的扩展成员组织在一起。调用方式仍然像实例方法：
/// <c>values.IsSorted()</c>。
/// </remarks>
public static class SequenceExtensions
{
    extension<T>(IEnumerable<T> source)
    {
        /// <summary>
        /// 判断序列是否按照指定比较器非递减排列。
        /// </summary>
        /// <remarks>
        /// 方法只枚举一次输入，时间复杂度 O(n)、额外空间 O(1)。
        /// 空序列和单元素序列按定义是有序的。
        /// </remarks>
        public bool IsSorted(IComparer<T>? comparer = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            comparer ??= Comparer<T>.Default;

            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return true;
            }

            var previous = enumerator.Current;

            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (comparer.Compare(previous, current) > 0)
                {
                    return false;
                }

                previous = current;
            }

            return true;
        }
    }
}
