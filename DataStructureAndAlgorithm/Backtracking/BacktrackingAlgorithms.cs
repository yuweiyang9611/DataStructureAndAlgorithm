namespace DataStructureAndAlgorithm.Backtracking;

/// <summary>经典回溯算法。</summary>
public static class BacktrackingAlgorithms
{
    /// <summary>
    /// 返回 n 皇后问题的全部解。每个解的数组下标表示行，数组值表示该行皇后的列。
    /// </summary>
    /// <remarks>
    /// 回溯的核心是“选择、递归、撤销选择”。集合用于 O(1) 判断列和两条对角线冲突。
    /// </remarks>
    public static IReadOnlyList<int[]> SolveNQueens(int size)
    {
        if (size < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Board size must be positive.");
        }

        var solutions = new List<int[]>();
        var queenColumns = Enumerable.Repeat(-1, size).ToArray();
        var occupiedColumns = new HashSet<int>();
        var occupiedDownDiagonals = new HashSet<int>();
        var occupiedUpDiagonals = new HashSet<int>();

        Search(0);
        return solutions;

        void Search(int row)
        {
            if (row == size)
            {
                // 必须复制；queenColumns 会在后续回溯中继续被修改。
                solutions.Add((int[])queenColumns.Clone());
                return;
            }

            for (var column = 0; column < size; column++)
            {
                var downDiagonal = row - column;
                var upDiagonal = row + column;

                if (occupiedColumns.Contains(column) ||
                    occupiedDownDiagonals.Contains(downDiagonal) ||
                    occupiedUpDiagonals.Contains(upDiagonal))
                {
                    continue;
                }

                queenColumns[row] = column;
                occupiedColumns.Add(column);
                occupiedDownDiagonals.Add(downDiagonal);
                occupiedUpDiagonals.Add(upDiagonal);

                Search(row + 1);

                // 撤销本层选择，使下一次循环回到完全相同的搜索状态。
                queenColumns[row] = -1;
                occupiedColumns.Remove(column);
                occupiedDownDiagonals.Remove(downDiagonal);
                occupiedUpDiagonals.Remove(upDiagonal);
            }
        }
    }
}
