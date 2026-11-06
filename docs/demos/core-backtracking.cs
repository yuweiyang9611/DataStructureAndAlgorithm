// 回溯与组合搜索：每放一个皇后就检查一次约束，不满足就退回上一行，所以解的数量才算得出来。
// 把 6 改成 8 或 10，看解的数量和耗时。
using DataStructureAndAlgorithm.Backtracking;

public static class Demo
{
    public static void Run()
    {
        var solutions = BacktrackingAlgorithms.SolveNQueens(6);
        Console.WriteLine($"6 皇后共有 {solutions.Count} 个解，第一个解按行给出皇后所在列：{string.Join(" ", solutions[0])}");
        foreach (var column in solutions[0])
        {
            Console.WriteLine("  " + string.Concat(Enumerable.Range(0, 6).Select(cell => cell == column ? "Q " : ". ")));
        }
        Console.WriteLine($"8 皇后：{BacktrackingAlgorithms.SolveNQueens(8).Count} 个解");

        Console.WriteLine($"3 对括号的全部合法组合：{string.Join(" ", CombinatorialAlgorithms.GenerateParentheses(3))}");

        var subsets = CombinatorialAlgorithms.GenerateSubsets(new[] { 1, 2, 3 });
        var listed = string.Join(" ", subsets.Select(subset => "{" + string.Join(",", subset) + "}"));
        Console.WriteLine($"{{1,2,3}} 的 {subsets.Count} 个子集：{listed}");
    }
}
