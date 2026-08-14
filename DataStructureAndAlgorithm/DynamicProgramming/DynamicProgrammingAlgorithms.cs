namespace DataStructureAndAlgorithm.DynamicProgramming;

/// <summary>
/// 经典动态规划问题。
/// </summary>
/// <remarks>
/// 动态规划适用于“最优子结构”和“重复子问题”同时存在的场景：先定义状态，
/// 再写出状态转移方程，最后决定自顶向下记忆化还是自底向上填表。
/// </remarks>
public static class DynamicProgrammingAlgorithms
{
    /// <summary>
    /// 返回第 n 个斐波那契数，F(0)=0，F(1)=1。
    /// </summary>
    /// <remarks>时间 O(n)，空间 O(1)。long 能安全保存的最大项是 F(92)。</remarks>
    public static long Fibonacci(int n)
    {
        if (n is < 0 or > 92)
        {
            throw new ArgumentOutOfRangeException(
                nameof(n),
                n,
                "n must be between 0 and 92 so the result fits in Int64.");
        }

        long previous = 0;
        long current = 1;

        for (var index = 0; index < n; index++)
        {
            (previous, current) = (current, previous + current);
        }

        return previous;
    }

    /// <summary>
    /// 求两个字符串的一个最长公共子序列（LCS）。子序列不要求字符连续。
    /// </summary>
    /// <remarks>时间和空间复杂度均为 O(nm)。</remarks>
    public static string LongestCommonSubsequence(string first, string second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        // dp[i, j] 表示 first 前 i 个字符与 second 前 j 个字符的 LCS 长度。
        var dp = new int[first.Length + 1, second.Length + 1];

        for (var firstLength = 1; firstLength <= first.Length; firstLength++)
        {
            for (var secondLength = 1; secondLength <= second.Length; secondLength++)
            {
                dp[firstLength, secondLength] = first[firstLength - 1] == second[secondLength - 1]
                    ? dp[firstLength - 1, secondLength - 1] + 1
                    : Math.Max(dp[firstLength - 1, secondLength], dp[firstLength, secondLength - 1]);
            }
        }

        // 从右下角反向追踪选择，恢复一个实际子序列，而不只返回长度。
        var result = new char[dp[first.Length, second.Length]];
        var firstIndex = first.Length;
        var secondIndex = second.Length;
        var resultIndex = result.Length - 1;

        while (firstIndex > 0 && secondIndex > 0)
        {
            if (first[firstIndex - 1] == second[secondIndex - 1])
            {
                result[resultIndex--] = first[firstIndex - 1];
                firstIndex--;
                secondIndex--;
            }
            else if (dp[firstIndex - 1, secondIndex] >= dp[firstIndex, secondIndex - 1])
            {
                firstIndex--;
            }
            else
            {
                secondIndex--;
            }
        }

        return new string(result);
    }

    /// <summary>
    /// 求解 0/1 背包：每件物品只能选择一次，返回不超过容量时的最大价值。
    /// </summary>
    public static KnapsackResult ZeroOneKnapsack(
        IReadOnlyList<KnapsackItem> items,
        int capacity)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        for (var index = 0; index < items.Count; index++)
        {
            if (items[index].Weight <= 0)
            {
                throw new ArgumentException("Every item weight must be greater than zero.", nameof(items));
            }

            if (items[index].Value < 0)
            {
                throw new ArgumentException("Every item value must be non-negative.", nameof(items));
            }
        }

        // dp[i, c]：只考虑前 i 件物品、容量为 c 时能得到的最大价值。
        var dp = new int[items.Count + 1, capacity + 1];

        for (var itemCount = 1; itemCount <= items.Count; itemCount++)
        {
            var item = items[itemCount - 1];

            for (var currentCapacity = 0; currentCapacity <= capacity; currentCapacity++)
            {
                dp[itemCount, currentCapacity] = dp[itemCount - 1, currentCapacity];

                if (item.Weight <= currentCapacity)
                {
                    dp[itemCount, currentCapacity] = Math.Max(
                        dp[itemCount, currentCapacity],
                        dp[itemCount - 1, currentCapacity - item.Weight] + item.Value);
                }
            }
        }

        var selectedIndexes = new List<int>();
        var remainingCapacity = capacity;

        // 若当前行和上一行的最优值不同，说明当前物品被选中。
        for (var itemCount = items.Count; itemCount > 0; itemCount--)
        {
            if (dp[itemCount, remainingCapacity] == dp[itemCount - 1, remainingCapacity])
            {
                continue;
            }

            var selectedIndex = itemCount - 1;
            selectedIndexes.Add(selectedIndex);
            remainingCapacity -= items[selectedIndex].Weight;
        }

        selectedIndexes.Reverse();
        return new KnapsackResult(dp[items.Count, capacity], selectedIndexes);
    }

    /// <summary>
    /// 使用给定面额凑出目标金额所需的最少硬币数；无法凑出时返回 null。
    /// </summary>
    /// <remarks>每种面额可以使用任意次，时间 O(amount × coinCount)。</remarks>
    public static int? MinimumCoins(IReadOnlyList<int> coins, int amount)
    {
        ArgumentNullException.ThrowIfNull(coins);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        if (coins.Any(coin => coin <= 0))
        {
            throw new ArgumentException("Coin denominations must be greater than zero.", nameof(coins));
        }

        // amount + 1 是不可能成为有效答案的哨兵值：最坏情况也只需 amount 枚 1 元硬币。
        var unreachable = amount + 1;
        var dp = Enumerable.Repeat(unreachable, amount + 1).ToArray();
        dp[0] = 0;

        for (var currentAmount = 1; currentAmount <= amount; currentAmount++)
        {
            foreach (var coin in coins)
            {
                if (coin <= currentAmount && dp[currentAmount - coin] != unreachable)
                {
                    dp[currentAmount] = Math.Min(dp[currentAmount], dp[currentAmount - coin] + 1);
                }
            }
        }

        return dp[amount] == unreachable ? null : dp[amount];
    }
}
