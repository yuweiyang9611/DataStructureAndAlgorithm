namespace DataStructureAndAlgorithm.Sorting;

/// <summary>
/// 经典排序算法集合。所有方法都会原地修改传入序列。
/// </summary>
/// <remarks>
/// 这些实现强调算法步骤和可读性。生产项目应优先使用经过高度优化的
/// <see cref="Array.Sort{T}(T[])"/> 或 <see cref="List{T}.Sort()"/>。
/// </remarks>
public static class SortAlgorithms
{
    /// <summary>
    /// 冒泡排序：稳定，时间 O(n²)，额外空间 O(1)。
    /// </summary>
    public static void BubbleSort<T>(IList<T> values, IComparer<T>? comparer = null)
    {
        Validate(values, ref comparer);

        for (var unsortedEnd = values.Count - 1; unsortedEnd > 0; unsortedEnd--)
        {
            var swapped = false;

            for (var index = 0; index < unsortedEnd; index++)
            {
                if (comparer!.Compare(values[index], values[index + 1]) <= 0)
                {
                    continue;
                }

                Swap(values, index, index + 1);
                swapped = true;
            }

            // 本轮没有交换说明整个序列已经有序，可提前结束。
            if (!swapped)
            {
                return;
            }
        }
    }

    /// <summary>
    /// 选择排序：不稳定，时间 O(n²)，额外空间 O(1)，交换次数最多 O(n)。
    /// </summary>
    public static void SelectionSort<T>(IList<T> values, IComparer<T>? comparer = null)
    {
        Validate(values, ref comparer);

        for (var sortedEnd = 0; sortedEnd < values.Count - 1; sortedEnd++)
        {
            var minimumIndex = sortedEnd;

            for (var index = sortedEnd + 1; index < values.Count; index++)
            {
                if (comparer!.Compare(values[index], values[minimumIndex]) < 0)
                {
                    minimumIndex = index;
                }
            }

            if (minimumIndex != sortedEnd)
            {
                Swap(values, sortedEnd, minimumIndex);
            }
        }
    }

    /// <summary>
    /// 插入排序：稳定，最坏 O(n²)，近乎有序时接近 O(n)，额外空间 O(1)。
    /// </summary>
    public static void InsertionSort<T>(IList<T> values, IComparer<T>? comparer = null)
    {
        Validate(values, ref comparer);

        for (var index = 1; index < values.Count; index++)
        {
            var valueToInsert = values[index];
            var position = index;

            // 把较大的元素整体右移。使用“>”而不是“>=”可保持相等元素的原顺序。
            while (position > 0 && comparer!.Compare(values[position - 1], valueToInsert) > 0)
            {
                values[position] = values[position - 1];
                position--;
            }

            values[position] = valueToInsert;
        }
    }

    /// <summary>
    /// 归并排序：稳定，时间 O(n log n)，额外空间 O(n)。
    /// </summary>
    public static void MergeSort<T>(IList<T> values, IComparer<T>? comparer = null)
    {
        Validate(values, ref comparer);

        if (values.Count < 2)
        {
            return;
        }

        var buffer = new T[values.Count];
        MergeSort(values, buffer, 0, values.Count, comparer!);
    }

    /// <summary>
    /// 快速排序：平均 O(n log n)，最坏 O(n²)，不稳定。
    /// </summary>
    public static void QuickSort<T>(IList<T> values, IComparer<T>? comparer = null)
    {
        Validate(values, ref comparer);
        QuickSort(values, 0, values.Count - 1, comparer!);
    }

    /// <summary>
    /// 堆排序：最坏 O(n log n)，额外空间 O(1)，不稳定。
    /// </summary>
    public static void HeapSort<T>(IList<T> values, IComparer<T>? comparer = null)
    {
        Validate(values, ref comparer);

        // 先构造最大堆，根节点是当前未排序区间的最大值。
        for (var index = values.Count / 2 - 1; index >= 0; index--)
        {
            SiftDown(values, index, values.Count, comparer!);
        }

        for (var heapSize = values.Count; heapSize > 1; heapSize--)
        {
            Swap(values, 0, heapSize - 1);
            SiftDown(values, 0, heapSize - 1, comparer!);
        }
    }

    private static void MergeSort<T>(
        IList<T> values,
        T[] buffer,
        int start,
        int end,
        IComparer<T> comparer)
    {
        if (end - start <= 1)
        {
            return;
        }

        var middle = start + (end - start) / 2;
        MergeSort(values, buffer, start, middle, comparer);
        MergeSort(values, buffer, middle, end, comparer);

        var left = start;
        var right = middle;
        var destination = start;

        while (left < middle && right < end)
        {
            // 相等时先取左半部分，归并排序因此保持稳定。
            buffer[destination++] = comparer.Compare(values[left], values[right]) <= 0
                ? values[left++]
                : values[right++];
        }

        while (left < middle)
        {
            buffer[destination++] = values[left++];
        }

        while (right < end)
        {
            buffer[destination++] = values[right++];
        }

        for (var index = start; index < end; index++)
        {
            values[index] = buffer[index];
        }
    }

    private static void QuickSort<T>(IList<T> values, int left, int right, IComparer<T> comparer)
    {
        // Hoare 分区把小于基准值的元素放左侧，大于基准值的元素放右侧。
        while (left < right)
        {
            var leftCursor = left;
            var rightCursor = right;
            var pivot = values[left + (right - left) / 2];

            while (leftCursor <= rightCursor)
            {
                while (comparer.Compare(values[leftCursor], pivot) < 0)
                {
                    leftCursor++;
                }

                while (comparer.Compare(values[rightCursor], pivot) > 0)
                {
                    rightCursor--;
                }

                if (leftCursor <= rightCursor)
                {
                    Swap(values, leftCursor, rightCursor);
                    leftCursor++;
                    rightCursor--;
                }
            }

            // 只递归较短分区，较长分区用循环处理，可把递归栈限制在 O(log n)。
            if (rightCursor - left < right - leftCursor)
            {
                QuickSort(values, left, rightCursor, comparer);
                left = leftCursor;
            }
            else
            {
                QuickSort(values, leftCursor, right, comparer);
                right = rightCursor;
            }
        }
    }

    private static void SiftDown<T>(IList<T> values, int root, int heapSize, IComparer<T> comparer)
    {
        while (true)
        {
            var leftChild = root * 2 + 1;
            if (leftChild >= heapSize)
            {
                return;
            }

            var largerChild = leftChild;
            var rightChild = leftChild + 1;
            if (rightChild < heapSize && comparer.Compare(values[rightChild], values[leftChild]) > 0)
            {
                largerChild = rightChild;
            }

            if (comparer.Compare(values[root], values[largerChild]) >= 0)
            {
                return;
            }

            Swap(values, root, largerChild);
            root = largerChild;
        }
    }

    private static void Validate<T>(IList<T> values, ref IComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(values);
        comparer ??= Comparer<T>.Default;
    }

    private static void Swap<T>(IList<T> values, int first, int second)
    {
        (values[first], values[second]) = (values[second], values[first]);
    }
}
