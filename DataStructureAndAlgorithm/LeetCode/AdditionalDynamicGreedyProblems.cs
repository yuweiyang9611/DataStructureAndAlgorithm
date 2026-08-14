namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>LeetCode 100 题专题中的动态规划、贪心和单调栈题。</summary>
public static class AdditionalDynamicGreedyProblems
{
    /// <summary>55 - Jump Game。维护当前可达的最远下标，O(n)。</summary>
    public static bool CanJump(IReadOnlyList<int> maximumSteps)
    {
        ArgumentNullException.ThrowIfNull(maximumSteps);
        if (maximumSteps.Any(step => step < 0)) throw new ArgumentException("Steps must be non-negative.", nameof(maximumSteps));
        long farthest = 0;

        for (var index = 0; index < maximumSteps.Count; index++)
        {
            // index 超过 farthest，说明此前所有选择都无法到达这里，后面更不可能到达。
            if (index > farthest) return false;
            farthest = Math.Max(farthest, (long)index + maximumSteps[index]);
            if (farthest >= maximumSteps.Count - 1) return true;
        }

        return maximumSteps.Count == 0;
    }

    /// <summary>62 - Unique Paths。每格路径数等于上方加左方，使用一维 DP，O(mn)。</summary>
    public static long UniquePaths(int rows, int columns)
    {
        if (rows < 1) throw new ArgumentOutOfRangeException(nameof(rows));
        if (columns < 1) throw new ArgumentOutOfRangeException(nameof(columns));
        var paths = Enumerable.Repeat(1L, columns).ToArray();

        for (var row = 1; row < rows; row++)
            for (var column = 1; column < columns; column++)
                paths[column] = checked(paths[column] + paths[column - 1]);
        return paths[^1];
    }

    /// <summary>64 - Minimum Path Sum。当前最优只依赖上方和左方，O(mn)、空间 O(n)。</summary>
    public static long MinimumPathSum(int[][] grid)
    {
        ValidateGrid(grid, out var columns);
        if (grid.Length == 0 || columns == 0) return 0;
        if (grid.Any(row => row.Any(value => value < 0))) throw new ArgumentException("Grid values must be non-negative.", nameof(grid));
        var costs = Enumerable.Repeat(long.MaxValue, columns).ToArray();
        costs[0] = 0;

        foreach (var row in grid)
        {
            for (var column = 0; column < columns; column++)
            {
                var fromAbove = costs[column];
                var fromLeft = column == 0 ? long.MaxValue : costs[column - 1];
                var previous = Math.Min(fromAbove, fromLeft);
                costs[column] = previous == long.MaxValue ? row[column] : checked(previous + row[column]);
            }
        }

        return costs[^1];
    }

    /// <summary>84 - Largest Rectangle in Histogram。单调递增栈在高度出栈时确定最大宽度，O(n)。</summary>
    public static long LargestRectangleArea(IReadOnlyList<int> heights)
    {
        ArgumentNullException.ThrowIfNull(heights);
        if (heights.Any(height => height < 0)) throw new ArgumentException("Heights must be non-negative.", nameof(heights));
        var stack = new Stack<int>();
        long maximum = 0;

        // 末尾虚拟 0 高度强制清空栈，使所有柱都得到结算。
        for (var index = 0; index <= heights.Count; index++)
        {
            var currentHeight = index == heights.Count ? 0 : heights[index];
            while (stack.TryPeek(out var top) && heights[top] > currentHeight)
            {
                var height = heights[stack.Pop()];
                var leftBoundary = stack.TryPeek(out var left) ? left : -1;
                var width = index - leftBoundary - 1;
                maximum = Math.Max(maximum, (long)height * width);
            }

            stack.Push(index);
        }

        return maximum;
    }

    /// <summary>152 - Maximum Product Subarray。负数会交换最大和最小积的角色，O(n)。</summary>
    public static long MaximumProductSubarray(IReadOnlyList<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        if (numbers.Count == 0) throw new ArgumentException("Input cannot be empty.", nameof(numbers));
        long maximumEnding = numbers[0];
        long minimumEnding = numbers[0];
        long best = numbers[0];

        for (var index = 1; index < numbers.Count; index++)
        {
            var value = numbers[index];
            if (value < 0) (maximumEnding, minimumEnding) = (minimumEnding, maximumEnding);
            maximumEnding = Math.Max(value, checked(maximumEnding * value));
            minimumEnding = Math.Min(value, checked(minimumEnding * value));
            best = Math.Max(best, maximumEnding);
        }

        return best;
    }

    /// <summary>209 - Minimum Size Subarray Sum。正数保证窗口和随边界单调变化，O(n)。</summary>
    public static int MinimumSubarrayLength(int target, IReadOnlyList<int> numbers)
    {
        if (target <= 0) throw new ArgumentOutOfRangeException(nameof(target));
        ArgumentNullException.ThrowIfNull(numbers);
        if (numbers.Any(number => number <= 0)) throw new ArgumentException("This sliding-window solution requires positive numbers.", nameof(numbers));
        var left = 0;
        long sum = 0;
        var minimum = int.MaxValue;

        for (var right = 0; right < numbers.Count; right++)
        {
            sum += numbers[right];
            while (sum >= target)
            {
                minimum = Math.Min(minimum, right - left + 1);
                sum -= numbers[left++];
            }
        }

        return minimum == int.MaxValue ? 0 : minimum;
    }

    /// <summary>221 - Maximal Square。dp 表示以当前格为右下角的最大正方形边长，O(mn)。</summary>
    public static int MaximalSquareArea(char[][] matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        var columns = matrix.Length == 0 ? 0 : matrix[0]?.Length ?? throw new ArgumentException("Rows cannot be null.", nameof(matrix));
        if (matrix.Any(row => row is null || row.Length != columns)) throw new ArgumentException("Matrix must be rectangular.", nameof(matrix));
        var dp = new int[columns + 1];
        var maximumSide = 0;

        foreach (var row in matrix)
        {
            var upperLeft = 0;
            for (var column = 1; column <= columns; column++)
            {
                var fromAbove = dp[column];
                if (row[column - 1] == '1')
                {
                    dp[column] = Math.Min(Math.Min(dp[column], dp[column - 1]), upperLeft) + 1;
                    maximumSide = Math.Max(maximumSide, dp[column]);
                }
                else
                {
                    if (row[column - 1] != '0') throw new ArgumentException("Matrix values must be '0' or '1'.", nameof(matrix));
                    dp[column] = 0;
                }

                upperLeft = fromAbove; // 下一格的左上角是当前格更新前的“上方”。
            }
        }

        return maximumSide * maximumSide;
    }

    private static void ValidateGrid(int[][] grid, out int columns)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var columnCount = grid.Length == 0 ? 0 : grid[0]?.Length ?? throw new ArgumentException("Rows cannot be null.", nameof(grid));
        if (grid.Any(row => row is null || row.Length != columnCount)) throw new ArgumentException("Grid must be rectangular.", nameof(grid));
        columns = columnCount;
    }
}
