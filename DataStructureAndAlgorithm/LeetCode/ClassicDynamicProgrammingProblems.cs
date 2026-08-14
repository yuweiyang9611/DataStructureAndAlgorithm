using DataStructureAndAlgorithm.DynamicProgramming;

namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>LeetCode 中经典的一维动态规划和可达性题目。</summary>
public static class ClassicDynamicProgrammingProblems
{
    /// <summary>
    /// LeetCode 70 - Climbing Stairs：每次走 1 或 2 阶的方案数。
    /// </summary>
    /// <remarks>状态只依赖前两项，时间 O(n)，空间 O(1)。</remarks>
    public static int ClimbStairs(int stairCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stairCount);
        var previous = 1;
        var current = 1;

        for (var stair = 1; stair <= stairCount; stair++)
        {
            (previous, current) = (current, checked(previous + current));
        }

        return previous;
    }

    /// <summary>
    /// LeetCode 198 - House Robber：不能选择相邻房屋时的最大收益。
    /// </summary>
    /// <remarks>dp[i] = max(不选当前，选择当前 + i-2)，时间 O(n)，空间 O(1)。</remarks>
    public static int HouseRobber(IReadOnlyList<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Any(value => value < 0))
        {
            throw new ArgumentException("House values must be non-negative.", nameof(values));
        }

        var twoStepsBack = 0;
        var oneStepBack = 0;

        foreach (var value in values)
        {
            var current = Math.Max(oneStepBack, checked(twoStepsBack + value));
            twoStepsBack = oneStepBack;
            oneStepBack = current;
        }

        return oneStepBack;
    }

    /// <summary>
    /// LeetCode 139 - Word Break：判断字符串能否由词典单词连续拼接。
    /// </summary>
    /// <remarks>dp[end] 表示前 end 个字符是否可拆分，最坏时间 O(n²)。</remarks>
    public static bool WordBreak(string text, IEnumerable<string> words)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(words);
        var dictionary = words.ToHashSet(StringComparer.Ordinal);

        if (dictionary.Any(string.IsNullOrEmpty))
        {
            throw new ArgumentException("Dictionary words must be non-empty.", nameof(words));
        }

        var reachable = new bool[text.Length + 1];
        reachable[0] = true;

        for (var end = 1; end <= text.Length; end++)
        {
            for (var start = 0; start < end; start++)
            {
                if (reachable[start] && dictionary.Contains(text[start..end]))
                {
                    reachable[end] = true;
                    break;
                }
            }
        }

        return reachable[text.Length];
    }

    /// <summary>
    /// LeetCode 322 - Coin Change：无法凑出时返回 -1。
    /// </summary>
    /// <remarks>复用项目通用动态规划实现，展示题目 API 与可复用算法之间的适配。</remarks>
    public static int CoinChange(IReadOnlyList<int> coins, int amount)
    {
        return DynamicProgrammingAlgorithms.MinimumCoins(coins, amount) ?? -1;
    }
}
