// 比较排序与非比较排序：同一组数据交给六种比较排序和三种非比较排序，顺序一样、代价不同。
// 换一个比较器，或者把输入改成已经有序的，看哪种排序受影响。
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Sorting;

public static class Demo
{
    static readonly int[] Sample = [38, 27, 43, 3, 9, 82, 10, 27];

    public static void Run()
    {
        Console.WriteLine($"输入：{string.Join(", ", Sample)}");
        Show("冒泡", SortAlgorithms.BubbleSort);
        Show("选择", SortAlgorithms.SelectionSort);
        Show("插入", SortAlgorithms.InsertionSort);
        Show("归并", SortAlgorithms.MergeSort);
        Show("快速", SortAlgorithms.QuickSort);
        Show("堆", SortAlgorithms.HeapSort);

        var descending = Sample.ToList();
        SortAlgorithms.MergeSort(descending, Comparer<int>.Create((left, right) => right.CompareTo(left)));
        Console.WriteLine($"归并排序 + 降序比较器：{string.Join(", ", descending)}");

        var counting = Sample.ToList();
        NonComparisonSortAlgorithms.CountingSort(counting);
        Console.WriteLine($"计数排序：{string.Join(", ", counting)}");

        var bucket = new List<double> { 0.42, 0.32, 0.23, 0.52, 0.25, 0.47 };
        NonComparisonSortAlgorithms.BucketSort(bucket);
        Console.WriteLine($"桶排序：{string.Join(", ", bucket)}");

        var trace = new CollectingAlgorithmTraceSink();
        int[] radix = [170, -45, 75, -90, 802, 24, 2, 66];
        NonComparisonSortAlgorithms.RadixSort(radix, trace);
        Console.WriteLine($"基数排序（含负数）：{string.Join(", ", radix)}，过程记录了 {trace.Events.Count} 个 Trace 事件");
    }

    static void Show(string name, Action<IList<int>, IComparer<int>?> sort)
    {
        var values = Sample.ToList();
        sort(values, null);
        Console.WriteLine($"{name}排序：{string.Join(", ", values)}");
    }
}
