// 数组模式、Span 与二分查找：滑动窗口、Kadane、Two Sum 与二分都在固定的区间语义下给出答案。
// 改一改输入或目标值再跑，看区间 [Start, EndExclusive) 和下标怎么变。
using DataStructureAndAlgorithm.ArrayAlgorithms;
using DataStructureAndAlgorithm.Memory;
using DataStructureAndAlgorithm.Searching;

public static class Demo
{
    public static void Run()
    {
        int[] prices = [3, 1, 4, 1, 5, 9, 2, 6];
        Console.WriteLine($"输入：{string.Join(", ", prices)}");
        Console.WriteLine($"窗口为 3 的滑动最大值：{string.Join(", ", ArrayAlgorithms.SlidingWindowMaximum(prices, 3))}");

        long[] gains = [-2, 1, -3, 4, -1, 2, 1, -5, 4];
        var best = ArrayAlgorithms.MaximumSubarray(gains);
        Console.WriteLine($"Kadane 最大子数组：和 {best.Sum}，区间 [{best.Start}, {best.EndExclusive})");

        var pair = ArrayAlgorithms.TwoSum(prices, 10);
        Console.WriteLine(pair is { } found
            ? $"Two Sum = 10：下标 {found.First} 与 {found.Second}（{prices[found.First]} + {prices[found.Second]}）"
            : "Two Sum = 10：没有答案");

        int[] sorted = [1, 1, 2, 3, 4, 5, 6, 9];
        Console.WriteLine($"有序数组：{string.Join(", ", sorted)}");
        Console.WriteLine($"二分查找 5：下标 {SearchAlgorithms.BinarySearch(sorted, 5)}；查找 7：{SearchAlgorithms.BinarySearch(sorted, 7)}（不存在）");
        Console.WriteLine($"LowerBound(1) = {SearchAlgorithms.LowerBound(sorted, 1)}，UpperBound(1) = {SearchAlgorithms.UpperBound(sorted, 1)}：重复值的半开区间");
        Console.WriteLine($"Span 上的二分查找 9：下标 {SpanAlgorithms.BinarySearch<int>(sorted, 9)}");

        Span<int> buffer = [170, 45, 75, 90, 802, 24, 2, 66];
        SpanAlgorithms.PooledRadixSort(buffer);
        Console.WriteLine($"池化缓冲区的基数排序：{string.Join(", ", buffer.ToArray())}");
    }
}
