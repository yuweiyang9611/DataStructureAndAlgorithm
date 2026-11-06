// 动态规划：线性、区间、树形与状态压缩各跑一个经典问题，答案都来自“子问题的最优解”。
// 把背包容量或硬币面额改一改，看选中的下标和最少硬币数怎么变。
using DataStructureAndAlgorithm.DynamicProgramming;

public static class Demo
{
    public static void Run()
    {
        Console.WriteLine($"Fibonacci(50) = {DynamicProgrammingAlgorithms.Fibonacci(50)}");
        Console.WriteLine($"LCS(ABCBDAB, BDCABA) = {DynamicProgrammingAlgorithms.LongestCommonSubsequence("ABCBDAB", "BDCABA")}");

        KnapsackItem[] items = [new(Weight: 1, Value: 1), new(Weight: 3, Value: 4), new(Weight: 4, Value: 5), new(Weight: 5, Value: 7)];
        var knapsack = DynamicProgrammingAlgorithms.ZeroOneKnapsack(items, capacity: 7);
        Console.WriteLine($"0/1 背包容量 7：最大价值 {knapsack.MaximumValue}，选中下标 {string.Join(", ", knapsack.SelectedIndexes)}");

        var coins = DynamicProgrammingAlgorithms.MinimumCoins(new[] { 1, 2, 5 }, 11);
        var impossible = DynamicProgrammingAlgorithms.MinimumCoins(new[] { 2 }, 3);
        Console.WriteLine($"凑 11 元最少硬币（面额 1,2,5）：{coins}；用 2 元凑 3 元：{(impossible is null ? "无解" : impossible.ToString())}");

        Console.WriteLine($"编辑距离 kitten -> sitting = {AdvancedDynamicProgrammingAlgorithms.EditDistance("kitten", "sitting")}");
        var lis = AdvancedDynamicProgrammingAlgorithms.LongestIncreasingSubsequence(new[] { 10, 9, 2, 5, 3, 7, 101, 18 });
        Console.WriteLine($"最长递增子序列：{string.Join(" ", lis)}（长度 {lis.Count}）");

        Console.WriteLine($"矩阵链 [40,20,30,10,30] 最少标量乘法次数：{P3DynamicProgrammingAlgorithms.MinimumMatrixChainMultiplications(new[] { 40, 20, 30, 10, 30 })}");

        var weights = new Dictionary<string, long> { ["root"] = 3, ["a"] = 2, ["b"] = 1, ["c"] = 4, ["d"] = 5 };
        var edges = new[] { ("root", "a"), ("root", "b"), ("a", "c"), ("a", "d") };
        Console.WriteLine($"树上最大权独立集：{P3DynamicProgrammingAlgorithms.MaximumWeightIndependentSetOnTree(weights, edges)}（选 root、c、d）");

        double[,] distances = { { 0, 2, 9, 10 }, { 1, 0, 6, 4 }, { 15, 7, 0, 8 }, { 6, 3, 12, 0 } };
        Console.WriteLine($"状态压缩 TSP（4 座城，从 0 出发回到 0）：{P3DynamicProgrammingAlgorithms.TravelingSalesperson(distances)}");
    }
}
