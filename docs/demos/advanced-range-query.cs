// Fenwick、线段树、Sparse Table 与懒标记：同一个数组，四种结构各答一种区间问题。
// 所有区间都是半开的 [start, endExclusive)；改一改边界，看答案是否跟着变。
using DataStructureAndAlgorithm.Range;

public static class Demo
{
    public static void Run()
    {
        long[] values = [5, 3, 7, 9, 6, 4, 1, 2];
        Console.WriteLine($"数组：{string.Join(", ", values)}");

        var fenwick = new FenwickTree(values);
        Console.WriteLine($"Fenwick 前缀和 [0,4) = {fenwick.PrefixSum(4)}，区间和 [2,6) = {fenwick.RangeSum(2, 6)}");
        fenwick.Add(3, 10);
        Console.WriteLine($"给下标 3 加 10 后，区间和 [2,6) = {fenwick.RangeSum(2, 6)}");

        var ints = values.Select(value => (int)value).ToArray();
        var segment = new SegmentTree<int>(ints, Math.Min, int.MaxValue);
        Console.WriteLine($"线段树区间最小 [1,5) = {segment.Query(1, 5)}");
        segment.Update(1, 8);
        Console.WriteLine($"把下标 1 改成 8 后 [1,5) = {segment.Query(1, 5)}");

        var sparse = new SparseTable<int>(ints, Math.Max);
        Console.WriteLine($"Sparse Table 区间最大 [0,8) = {sparse.Query(0, 8)}，[4,7) = {sparse.Query(4, 7)}（不可修改，每次查询 O(1)）");

        var lazy = new LazyRangeAddMaxSegmentTree(values);
        lazy.RangeAdd(0, 4, 100);
        Console.WriteLine($"懒标记树给 [0,4) 整体加 100：[0,8) 的最大值 {lazy.QueryMax(0, 8)}，[4,8) 的最大值 {lazy.QueryMax(4, 8)}");
    }
}
