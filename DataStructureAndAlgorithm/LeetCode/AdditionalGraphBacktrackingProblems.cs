using DataStructureAndAlgorithm.LeetCode.Models;

namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>LeetCode 100 题专题中的图搜索与回溯题。</summary>
public static class AdditionalGraphBacktrackingProblems
{
    private static readonly (int Row, int Column)[] Directions = [(-1, 0), (1, 0), (0, -1), (0, 1)];

    /// <summary>39 - Combination Sum。按非递减下标搜索，既允许复用又避免排列重复。</summary>
    public static IReadOnlyList<IReadOnlyList<int>> CombinationSum(IEnumerable<int> candidates, int target)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentOutOfRangeException.ThrowIfNegative(target);
        var values = candidates.Distinct().Order().ToArray();
        if (values.Any(value => value <= 0)) throw new ArgumentException("Candidates must be positive.", nameof(candidates));

        var result = new List<IReadOnlyList<int>>();
        var selected = new List<int>();
        Search(0, target);
        return result;

        void Search(int start, int remaining)
        {
            if (remaining == 0)
            {
                result.Add(selected.ToArray());
                return;
            }

            for (var index = start; index < values.Length && values[index] <= remaining; index++)
            {
                selected.Add(values[index]);
                Search(index, remaining - values[index]); // 仍传 index，所以当前数可重复使用。
                selected.RemoveAt(selected.Count - 1);
            }
        }
    }

    /// <summary>46 - Permutations。used 标记位置是否已选，生成 n! 个排列。</summary>
    public static IReadOnlyList<IReadOnlyList<int>> Permutations(IReadOnlyList<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        if (numbers.Distinct().Count() != numbers.Count)
            throw new ArgumentException("This problem variant requires distinct values.", nameof(numbers));

        var result = new List<IReadOnlyList<int>>();
        var selected = new int[numbers.Count];
        var used = new bool[numbers.Count];
        Search(0);
        return result;

        void Search(int depth)
        {
            if (depth == numbers.Count)
            {
                result.Add((int[])selected.Clone());
                return;
            }

            for (var index = 0; index < numbers.Count; index++)
            {
                if (used[index]) continue;
                used[index] = true;
                selected[depth] = numbers[index];
                Search(depth + 1);
                used[index] = false;
            }
        }
    }

    /// <summary>79 - Word Search。DFS 只在当前路径标记访问，回溯时撤销。</summary>
    public static bool WordExists(char[][] board, string word)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(word);
        if (word.Length == 0) return true;
        if (board.Length == 0) return false;
        var columns = board[0]?.Length ?? throw new ArgumentException("Rows cannot be null.", nameof(board));
        if (board.Any(row => row is null || row.Length != columns)) throw new ArgumentException("Board must be rectangular.", nameof(board));
        var visited = new bool[board.Length, columns];

        for (var row = 0; row < board.Length; row++)
            for (var column = 0; column < columns; column++)
                if (Search(row, column, 0)) return true;
        return false;

        bool Search(int row, int column, int index)
        {
            if (index == word.Length) return true;
            if (row < 0 || row >= board.Length || column < 0 || column >= columns ||
                visited[row, column] || board[row][column] != word[index]) return false;

            visited[row, column] = true;
            var found = Directions.Any(direction => Search(row + direction.Row, column + direction.Column, index + 1));
            visited[row, column] = false;
            return found;
        }
    }

    /// <summary>133 - Clone Graph。字典同时防止重复遍历并保存原节点到副本的映射。</summary>
    public static GraphNode? CloneGraph(GraphNode? start)
    {
        if (start is null) return null;
        var copies = new Dictionary<GraphNode, GraphNode> { [start] = new GraphNode(start.Value) };
        var queue = new Queue<GraphNode>();
        queue.Enqueue(start);

        while (queue.TryDequeue(out var node))
        {
            foreach (var neighbor in node.Neighbors)
            {
                if (!copies.TryGetValue(neighbor, out var neighborCopy))
                {
                    neighborCopy = new GraphNode(neighbor.Value);
                    copies.Add(neighbor, neighborCopy);
                    queue.Enqueue(neighbor);
                }

                copies[node].Neighbors.Add(neighborCopy);
            }
        }

        return copies[start];
    }

    /// <summary>695 - Max Area of Island。每次 DFS 返回当前连通分量面积，O(mn)。</summary>
    public static int MaximumIslandArea(int[][] grid)
    {
        ValidateGrid(grid, out var columns);
        var visited = new bool[grid.Length, columns];
        var maximum = 0;

        for (var row = 0; row < grid.Length; row++)
            for (var column = 0; column < columns; column++)
                if (grid[row][column] == 1 && !visited[row, column]) maximum = Math.Max(maximum, Area(row, column));
        return maximum;

        int Area(int row, int column)
        {
            if (row < 0 || row >= grid.Length || column < 0 || column >= columns ||
                visited[row, column] || grid[row][column] != 1) return 0;
            visited[row, column] = true;
            return 1 + Directions.Sum(direction => Area(row + direction.Row, column + direction.Column));
        }
    }

    /// <summary>785 - Is Graph Bipartite。BFS 给相邻节点染相反颜色，冲突即失败。</summary>
    public static bool IsBipartite(int[][] graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var colors = new int[graph.Length];

        for (var start = 0; start < graph.Length; start++)
        {
            if (colors[start] != 0) continue;
            colors[start] = 1;
            var queue = new Queue<int>();
            queue.Enqueue(start);

            while (queue.TryDequeue(out var node))
            {
                foreach (var neighbor in graph[node] ?? throw new ArgumentException("Rows cannot be null.", nameof(graph)))
                {
                    if (neighbor < 0 || neighbor >= graph.Length) throw new ArgumentOutOfRangeException(nameof(graph));
                    if (colors[neighbor] == colors[node]) return false;
                    if (colors[neighbor] == 0)
                    {
                        colors[neighbor] = -colors[node];
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return true;
    }

    /// <summary>797 - All Paths From Source to Target。DAG 中回溯枚举所有路径。</summary>
    public static IReadOnlyList<IReadOnlyList<int>> AllPathsSourceToTarget(int[][] graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (graph.Length == 0) return [];
        var result = new List<IReadOnlyList<int>>();
        var path = new List<int> { 0 };
        var onPath = new bool[graph.Length];
        Search(0);
        return result;

        void Search(int node)
        {
            if (node == graph.Length - 1)
            {
                result.Add(path.ToArray());
                return;
            }

            onPath[node] = true;
            foreach (var neighbor in graph[node] ?? throw new ArgumentException("Rows cannot be null.", nameof(graph)))
            {
                if (neighbor < 0 || neighbor >= graph.Length) throw new ArgumentOutOfRangeException(nameof(graph));
                if (onPath[neighbor]) throw new InvalidOperationException("The input must be a DAG.");
                path.Add(neighbor);
                Search(neighbor);
                path.RemoveAt(path.Count - 1);
            }

            onPath[node] = false;
        }
    }

    /// <summary>841 - Keys and Rooms。从房间 0 遍历所有可达房间，O(V+E)。</summary>
    public static bool CanVisitAllRooms(IReadOnlyList<IReadOnlyList<int>> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        if (rooms.Count == 0) return true;
        var visited = new bool[rooms.Count];
        var stack = new Stack<int>();
        stack.Push(0);
        visited[0] = true;
        var count = 1;

        while (stack.TryPop(out var room))
        {
            foreach (var key in rooms[room])
            {
                if (key < 0 || key >= rooms.Count) throw new ArgumentOutOfRangeException(nameof(rooms));
                if (!visited[key])
                {
                    visited[key] = true;
                    count++;
                    stack.Push(key);
                }
            }
        }

        return count == rooms.Count;
    }

    /// <summary>994 - Rotting Oranges。多源 BFS 每层代表一分钟，不修改输入。</summary>
    public static int MinutesToRotAllOranges(int[][] grid)
    {
        ValidateGrid(grid, out var columns);
        var state = grid.Select(row => (int[])row.Clone()).ToArray();
        var queue = new Queue<(int Row, int Column)>();
        var fresh = 0;

        for (var row = 0; row < state.Length; row++)
            for (var column = 0; column < columns; column++)
                if (state[row][column] == 2) queue.Enqueue((row, column));
                else if (state[row][column] == 1) fresh++;

        var minutes = 0;
        while (queue.Count > 0 && fresh > 0)
        {
            var count = queue.Count;
            minutes++;
            for (var index = 0; index < count; index++)
            {
                var current = queue.Dequeue();
                foreach (var direction in Directions)
                {
                    var row = current.Row + direction.Row;
                    var column = current.Column + direction.Column;
                    if (row < 0 || row >= state.Length || column < 0 || column >= columns || state[row][column] != 1) continue;
                    state[row][column] = 2;
                    fresh--;
                    queue.Enqueue((row, column));
                }
            }
        }

        return fresh == 0 ? minutes : -1;
    }

    /// <summary>1971 - Find if Path Exists。无向邻接表 + DFS，O(V+E)。</summary>
    public static bool HasValidPath(int vertexCount, IEnumerable<(int First, int Second)> edges, int source, int destination)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(vertexCount);
        ArgumentNullException.ThrowIfNull(edges);
        if (source < 0 || source >= vertexCount || destination < 0 || destination >= vertexCount)
            throw new ArgumentOutOfRangeException(nameof(source));

        var adjacency = Enumerable.Range(0, vertexCount).Select(_ => new List<int>()).ToArray();
        foreach (var (first, second) in edges)
        {
            if (first < 0 || first >= vertexCount || second < 0 || second >= vertexCount)
                throw new ArgumentOutOfRangeException(nameof(edges));
            adjacency[first].Add(second);
            adjacency[second].Add(first);
        }

        var visited = new bool[vertexCount];
        var stack = new Stack<int>();
        stack.Push(source);
        visited[source] = true;

        while (stack.TryPop(out var node))
        {
            if (node == destination) return true;
            foreach (var neighbor in adjacency[node])
                if (!visited[neighbor])
                {
                    visited[neighbor] = true;
                    stack.Push(neighbor);
                }
        }

        return false;
    }

    private static void ValidateGrid(int[][] grid, out int columns)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var columnCount = grid.Length == 0 ? 0 : grid[0]?.Length ?? throw new ArgumentException("Rows cannot be null.", nameof(grid));
        if (grid.Any(row => row is null || row.Length != columnCount)) throw new ArgumentException("Grid must be rectangular.", nameof(grid));
        columns = columnCount;
    }
}
