namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>LeetCode 中经典的网格搜索与拓扑排序题目。</summary>
public static class ClassicGraphProblems
{
    private static readonly (int Row, int Column)[] Directions =
    [
        (-1, 0),
        (1, 0),
        (0, -1),
        (0, 1)
    ];

    /// <summary>
    /// LeetCode 200 - Number of Islands：统计四方向相连的陆地区域数量。
    /// </summary>
    /// <remarks>不修改输入，使用访问矩阵。时间 O(rows × columns)，空间同阶。</remarks>
    public static int NumberOfIslands(char[][] grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (grid.Length == 0)
        {
            return 0;
        }

        var columnCount = grid[0]?.Length ?? throw new ArgumentException("Rows cannot be null.", nameof(grid));
        if (grid.Any(row => row is null || row.Length != columnCount))
        {
            throw new ArgumentException("The grid must be rectangular and contain no null rows.", nameof(grid));
        }

        var visited = new bool[grid.Length, columnCount];
        var islandCount = 0;

        for (var row = 0; row < grid.Length; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                if (grid[row][column] != '1' || visited[row, column])
                {
                    continue;
                }

                islandCount++;
                VisitIsland(row, column);
            }
        }

        return islandCount;

        void VisitIsland(int startRow, int startColumn)
        {
            var stack = new Stack<(int Row, int Column)>();
            stack.Push((startRow, startColumn));
            visited[startRow, startColumn] = true;

            while (stack.TryPop(out var current))
            {
                foreach (var direction in Directions)
                {
                    var nextRow = current.Row + direction.Row;
                    var nextColumn = current.Column + direction.Column;

                    if (nextRow < 0 || nextRow >= grid.Length ||
                        nextColumn < 0 || nextColumn >= columnCount ||
                        visited[nextRow, nextColumn] ||
                        grid[nextRow][nextColumn] != '1')
                    {
                        continue;
                    }

                    visited[nextRow, nextColumn] = true;
                    stack.Push((nextRow, nextColumn));
                }
            }
        }
    }

    /// <summary>
    /// LeetCode 207 - Course Schedule：判断所有课程能否在先修约束下完成。
    /// </summary>
    /// <remarks>Kahn 拓扑排序，时间 O(V + E)，空间 O(V + E)。</remarks>
    public static bool CanFinishCourses(int courseCount, IEnumerable<(int Course, int Prerequisite)> prerequisites)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(courseCount);
        ArgumentNullException.ThrowIfNull(prerequisites);

        var outgoing = Enumerable.Range(0, courseCount)
            .Select(_ => new List<int>())
            .ToArray();
        var inDegrees = new int[courseCount];

        foreach (var (course, prerequisite) in prerequisites)
        {
            if (course < 0 || course >= courseCount || prerequisite < 0 || prerequisite >= courseCount)
            {
                throw new ArgumentOutOfRangeException(nameof(prerequisites), "Course indexes must be within range.");
            }

            outgoing[prerequisite].Add(course);
            inDegrees[course]++;
        }

        var ready = new Queue<int>(Enumerable.Range(0, courseCount).Where(course => inDegrees[course] == 0));
        var completed = 0;

        while (ready.TryDequeue(out var prerequisite))
        {
            completed++;

            foreach (var course in outgoing[prerequisite])
            {
                if (--inDegrees[course] == 0)
                {
                    ready.Enqueue(course);
                }
            }
        }

        return completed == courseCount;
    }
}
