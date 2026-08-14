using DataStructureAndAlgorithm.Searching;

namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>
/// LeetCode 100 题专题中的进阶数组与矩阵题。
/// 每个方法都保留关键“不变量”注释，帮助理解为什么指针或状态可以这样移动。
/// </summary>
public static class AdditionalArrayProblems
{
    /// <summary>11 - Container With Most Water。两端向内移动较短边，O(n)。</summary>
    public static long MaximumContainerArea(IReadOnlyList<int> heights)
    {
        ArgumentNullException.ThrowIfNull(heights);
        if (heights.Any(height => height < 0))
        {
            throw new ArgumentException("Heights must be non-negative.", nameof(heights));
        }

        var left = 0;
        var right = heights.Count - 1;
        long maximum = 0;

        while (left < right)
        {
            maximum = Math.Max(maximum, (long)(right - left) * Math.Min(heights[left], heights[right]));

            // 面积受较短边限制；移动较高边只会减小宽度，不可能改善当前上界。
            if (heights[left] <= heights[right])
            {
                left++;
            }
            else
            {
                right--;
            }
        }

        return maximum;
    }

    /// <summary>26 - Remove Duplicates from Sorted Array。返回有效前缀长度，O(n)。</summary>
    public static int RemoveDuplicatesFromSortedArray(IList<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        if (numbers.Count == 0)
        {
            return 0;
        }

        var write = 1;
        for (var read = 1; read < numbers.Count; read++)
        {
            if (numbers[read] != numbers[write - 1])
            {
                numbers[write++] = numbers[read];
            }
        }

        return write;
    }

    /// <summary>27 - Remove Element。把不等于 value 的元素稳定写入前缀，O(n)。</summary>
    public static int RemoveElement(IList<int> numbers, int value)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        var write = 0;

        // ??? foreach ?? List<T> ?????????? setter ????????
        for (var read = 0; read < numbers.Count; read++)
        {
            if (numbers[read] != value)
            {
                numbers[write++] = numbers[read];
            }
        }

        return write;
    }

    /// <summary>31 - Next Permutation。原地生成字典序中的下一个排列，O(n)。</summary>
    public static void NextPermutation(IList<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        var pivot = numbers.Count - 2;

        while (pivot >= 0 && numbers[pivot] >= numbers[pivot + 1])
        {
            pivot--;
        }

        if (pivot >= 0)
        {
            var successor = numbers.Count - 1;
            while (numbers[successor] <= numbers[pivot])
            {
                successor--;
            }

            Swap(numbers, pivot, successor);
        }

        // 后缀原本非递增，反转后成为最小的非递减排列。
        Reverse(numbers, pivot + 1, numbers.Count - 1);
    }

    /// <summary>33 - Search in Rotated Sorted Array。每轮至少有一半仍有序，O(log n)。</summary>
    public static int SearchRotatedSortedArray(IReadOnlyList<int> numbers, int target)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        var left = 0;
        var right = numbers.Count - 1;

        while (left <= right)
        {
            var middle = left + (right - left) / 2;
            if (numbers[middle] == target)
            {
                return middle;
            }

            if (numbers[left] <= numbers[middle])
            {
                if (numbers[left] <= target && target < numbers[middle])
                {
                    right = middle - 1;
                }
                else
                {
                    left = middle + 1;
                }
            }
            else if (numbers[middle] < target && target <= numbers[right])
            {
                left = middle + 1;
            }
            else
            {
                right = middle - 1;
            }
        }

        return -1;
    }

    /// <summary>34 - Find First and Last Position。用两个边界二分避免向两侧线性扫描，O(log n)。</summary>
    public static int[] SearchRange(IReadOnlyList<int> numbers, int target)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        var first = SearchAlgorithms.LowerBound(numbers, target);
        if (first == numbers.Count || numbers[first] != target)
        {
            return [-1, -1];
        }

        return [first, SearchAlgorithms.UpperBound(numbers, target) - 1];
    }

    /// <summary>36 - Valid Sudoku。位掩码同时检查行、列和九宫格，O(81)。</summary>
    public static bool IsValidSudoku(char[][] board)
    {
        ArgumentNullException.ThrowIfNull(board);
        ValidateMatrix(board, 9, 9, nameof(board));
        var rows = new int[9];
        var columns = new int[9];
        var boxes = new int[9];

        for (var row = 0; row < 9; row++)
        {
            for (var column = 0; column < 9; column++)
            {
                var character = board[row][column];
                if (character == '.')
                {
                    continue;
                }

                if (character is < '1' or > '9')
                {
                    return false;
                }

                var bit = 1 << (character - '1');
                var box = row / 3 * 3 + column / 3;
                if ((rows[row] & bit) != 0 || (columns[column] & bit) != 0 || (boxes[box] & bit) != 0)
                {
                    return false;
                }

                rows[row] |= bit;
                columns[column] |= bit;
                boxes[box] |= bit;
            }
        }

        return true;
    }

    /// <summary>48 - Rotate Image。先转置再反转每行，实现原地顺时针旋转，O(n²)。</summary>
    public static void RotateImage(int[][] matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ValidateMatrix(matrix, matrix.Length, matrix.Length, nameof(matrix));

        for (var row = 0; row < matrix.Length; row++)
        {
            for (var column = row + 1; column < matrix.Length; column++)
            {
                (matrix[row][column], matrix[column][row]) = (matrix[column][row], matrix[row][column]);
            }

            Array.Reverse(matrix[row]);
        }
    }

    /// <summary>54 - Spiral Matrix。收缩四条边界，每个元素访问一次，O(mn)。</summary>
    public static IReadOnlyList<int> SpiralOrder(int[][] matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        if (matrix.Length == 0)
        {
            return [];
        }

        var columnCount = matrix[0]?.Length ?? throw new ArgumentException("Rows cannot be null.", nameof(matrix));
        ValidateMatrix(matrix, matrix.Length, columnCount, nameof(matrix));
        var result = new List<int>(matrix.Length * columnCount);
        var top = 0;
        var bottom = matrix.Length - 1;
        var left = 0;
        var right = columnCount - 1;

        while (top <= bottom && left <= right)
        {
            for (var column = left; column <= right; column++) result.Add(matrix[top][column]);
            top++;
            for (var row = top; row <= bottom; row++) result.Add(matrix[row][right]);
            right--;

            if (top <= bottom)
            {
                for (var column = right; column >= left; column--) result.Add(matrix[bottom][column]);
                bottom--;
            }

            if (left <= right)
            {
                for (var row = bottom; row >= top; row--) result.Add(matrix[row][left]);
                left++;
            }
        }

        return result;
    }

    /// <summary>56 - Merge Intervals。按起点排序后只需与最后一个结果比较，O(n log n)。</summary>
    public static int[][] MergeIntervals(IEnumerable<int[]> intervals)
    {
        ArgumentNullException.ThrowIfNull(intervals);
        var ordered = intervals.Select(interval =>
            interval is { Length: 2 } && interval[0] <= interval[1]
                ? (int[])interval.Clone()
                : throw new ArgumentException("Each interval must be [start, end] with start <= end.", nameof(intervals)))
            .OrderBy(interval => interval[0])
            .ToList();

        var merged = new List<int[]>();
        foreach (var interval in ordered)
        {
            if (merged.Count == 0 || merged[^1][1] < interval[0])
            {
                merged.Add(interval);
            }
            else
            {
                merged[^1][1] = Math.Max(merged[^1][1], interval[1]);
            }
        }

        return [.. merged];
    }

    /// <summary>66 - Plus One。从最低位传播进位，不修改输入，O(n)。</summary>
    public static int[] PlusOne(IReadOnlyList<int> digits)
    {
        ArgumentNullException.ThrowIfNull(digits);
        if (digits.Count == 0 || digits.Any(digit => digit is < 0 or > 9))
        {
            throw new ArgumentException("Digits must contain at least one decimal digit.", nameof(digits));
        }

        var result = digits.ToArray();
        for (var index = result.Length - 1; index >= 0; index--)
        {
            if (result[index] < 9)
            {
                result[index]++;
                return result;
            }

            result[index] = 0;
        }

        return [1, .. result];
    }

    /// <summary>73 - Set Matrix Zeroes。用首行首列保存标记，额外空间 O(1)。</summary>
    public static void SetMatrixZeroes(int[][] matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        if (matrix.Length == 0)
        {
            return;
        }

        var columns = matrix[0]?.Length ?? throw new ArgumentException("Rows cannot be null.", nameof(matrix));
        ValidateMatrix(matrix, matrix.Length, columns, nameof(matrix));
        if (columns == 0) return;
        var firstRowZero = matrix[0].Contains(0);
        var firstColumnZero = matrix.Any(row => row[0] == 0);

        for (var row = 1; row < matrix.Length; row++)
        {
            for (var column = 1; column < columns; column++)
            {
                if (matrix[row][column] == 0)
                {
                    matrix[row][0] = 0;
                    matrix[0][column] = 0;
                }
            }
        }

        for (var row = 1; row < matrix.Length; row++)
        {
            for (var column = 1; column < columns; column++)
            {
                if (matrix[row][0] == 0 || matrix[0][column] == 0) matrix[row][column] = 0;
            }
        }

        if (firstRowZero) Array.Fill(matrix[0], 0);
        if (firstColumnZero)
        {
            for (var row = 0; row < matrix.Length; row++) matrix[row][0] = 0;
        }
    }

    /// <summary>75 - Sort Colors。荷兰国旗三指针，一趟完成，O(n)。</summary>
    public static void SortColors(IList<int> colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        var low = 0;
        var current = 0;
        var high = colors.Count - 1;

        while (current <= high)
        {
            switch (colors[current])
            {
                case 0:
                    Swap(colors, low++, current++);
                    break;
                case 1:
                    current++;
                    break;
                case 2:
                    Swap(colors, current, high--);
                    break; // 换回来的值尚未检查，所以 current 不前进。
                default:
                    throw new ArgumentException("Colors must be 0, 1, or 2.", nameof(colors));
            }
        }
    }

    /// <summary>88 - Merge Sorted Array。从尾部写入，避免覆盖 first 中尚未处理的值，O(m+n)。</summary>
    public static void MergeSortedArrays(int[] first, int firstCount, int[] second, int secondCount)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        if (firstCount < 0 || secondCount < 0 || firstCount + secondCount > first.Length || secondCount > second.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(firstCount));
        }

        var firstIndex = firstCount - 1;
        var secondIndex = secondCount - 1;
        var write = firstCount + secondCount - 1;

        while (secondIndex >= 0)
        {
            first[write--] = firstIndex >= 0 && first[firstIndex] > second[secondIndex]
                ? first[firstIndex--]
                : second[secondIndex--];
        }
    }

    /// <summary>128 - Longest Consecutive Sequence。只从没有前驱的数开始扩展，平均 O(n)。</summary>
    public static int LongestConsecutiveSequence(IEnumerable<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        var set = numbers.ToHashSet();
        var best = 0;

        foreach (var number in set)
        {
            if (number != int.MinValue && set.Contains(number - 1))
            {
                continue;
            }

            var length = 1;
            var current = number;
            while (current != int.MaxValue && set.Contains(current + 1))
            {
                current++;
                length++;
            }

            best = Math.Max(best, length);
        }

        return best;
    }

    /// <summary>136 - Single Number。相同数字异或抵消，时间 O(n)、空间 O(1)。</summary>
    public static int SingleNumber(IEnumerable<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        var result = 0;
        foreach (var number in numbers) result ^= number;
        return result;
    }

    /// <summary>169 - Majority Element。Boyer-Moore 抵消不同元素，并验证候选，O(n)。</summary>
    public static int MajorityElement(IReadOnlyList<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        if (numbers.Count == 0) throw new ArgumentException("Input cannot be empty.", nameof(numbers));

        var candidate = 0;
        var votes = 0;
        foreach (var number in numbers)
        {
            if (votes == 0) candidate = number;
            votes += number == candidate ? 1 : -1;
        }

        return numbers.Count(number => number == candidate) > numbers.Count / 2
            ? candidate
            : throw new InvalidOperationException("The input has no majority element.");
    }

    /// <summary>189 - Rotate Array。三次反转完成右旋，O(n)、空间 O(1)。</summary>
    public static void RotateArray(IList<int> numbers, int steps)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        ArgumentOutOfRangeException.ThrowIfNegative(steps);
        if (numbers.Count == 0) return;

        steps %= numbers.Count;
        Reverse(numbers, 0, numbers.Count - 1);
        Reverse(numbers, 0, steps - 1);
        Reverse(numbers, steps, numbers.Count - 1);
    }

    /// <summary>217 - Contains Duplicate。HashSet.Add 失败即表示重复，平均 O(n)。</summary>
    public static bool ContainsDuplicate(IEnumerable<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        var seen = new HashSet<int>();
        return numbers.Any(number => !seen.Add(number));
    }

    /// <summary>238 - Product of Array Except Self。前缀积乘后缀积，不使用除法，O(n)。</summary>
    public static long[] ProductExceptSelf(IReadOnlyList<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        var result = new long[numbers.Count];
        long prefix = 1;

        for (var index = 0; index < numbers.Count; index++)
        {
            result[index] = prefix;
            prefix *= numbers[index];
        }

        long suffix = 1;
        for (var index = numbers.Count - 1; index >= 0; index--)
        {
            result[index] *= suffix;
            suffix *= numbers[index];
        }

        return result;
    }

    private static void Swap(IList<int> values, int first, int second) =>
        (values[first], values[second]) = (values[second], values[first]);

    private static void Reverse(IList<int> values, int left, int right)
    {
        while (left < right) Swap(values, left++, right--);
    }

    private static void ValidateMatrix<T>(T[][] matrix, int rows, int columns, string parameterName)
    {
        if (matrix.Length != rows || matrix.Any(row => row is null || row.Length != columns))
        {
            throw new ArgumentException($"Matrix must be {rows} x {columns}.", parameterName);
        }
    }
}
