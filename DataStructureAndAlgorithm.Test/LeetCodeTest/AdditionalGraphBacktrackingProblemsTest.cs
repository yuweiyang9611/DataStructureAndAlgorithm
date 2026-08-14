using DataStructureAndAlgorithm.LeetCode;
using DataStructureAndAlgorithm.LeetCode.Models;

namespace DataStructureAndAlgorithm.Test.LeetCodeTest;

public class AdditionalGraphBacktrackingProblemsTest
{
    [Fact]
    public void P39_CombinationSum()
    {
        var result = AdditionalGraphBacktrackingProblems.CombinationSum([2, 3, 6, 7], 7);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, values => values.SequenceEqual([2, 2, 3]));
        Assert.Contains(result, values => values.SequenceEqual([7]));
    }

    [Fact]
    public void P46_Permutations()
    {
        var result = AdditionalGraphBacktrackingProblems.Permutations([1, 2, 3]);
        Assert.Equal(6, result.Count);
        Assert.Contains(result, values => values.SequenceEqual([3, 2, 1]));
    }

    [Fact]
    public void P79_WordExists()
    {
        char[][] board = [['A', 'B', 'C', 'E'], ['S', 'F', 'C', 'S'], ['A', 'D', 'E', 'E']];
        Assert.True(AdditionalGraphBacktrackingProblems.WordExists(board, "ABCCED"));
        Assert.False(AdditionalGraphBacktrackingProblems.WordExists(board, "ABCB"));
    }

    [Fact]
    public void P133_CloneGraph()
    {
        var first = new GraphNode(1);
        var second = new GraphNode(2);
        first.Neighbors.Add(second);
        second.Neighbors.Add(first);
        var copy = AdditionalGraphBacktrackingProblems.CloneGraph(first)!;
        Assert.NotSame(first, copy);
        Assert.Equal(1, copy.Value);
        Assert.NotSame(second, copy.Neighbors[0]);
        Assert.Same(copy, copy.Neighbors[0].Neighbors[0]);
    }

    [Fact]
    public void P695_MaximumIslandArea()
    {
        int[][] grid = [[0, 0, 1, 0], [1, 1, 1, 0], [0, 1, 0, 0], [1, 0, 0, 1]];
        Assert.Equal(5, AdditionalGraphBacktrackingProblems.MaximumIslandArea(grid));
    }

    [Fact]
    public void P785_IsBipartite()
    {
        Assert.True(AdditionalGraphBacktrackingProblems.IsBipartite([[1, 3], [0, 2], [1, 3], [0, 2]]));
        Assert.False(AdditionalGraphBacktrackingProblems.IsBipartite([[1, 2, 3], [0, 2], [0, 1, 3], [0, 2]]));
    }

    [Fact]
    public void P797_AllPathsSourceToTarget()
    {
        var paths = AdditionalGraphBacktrackingProblems.AllPathsSourceToTarget([[1, 2], [3], [3], []]);
        Assert.Equal(2, paths.Count);
        Assert.Contains(paths, path => path.SequenceEqual([0, 1, 3]));
        Assert.Contains(paths, path => path.SequenceEqual([0, 2, 3]));
    }

    [Fact]
    public void P841_CanVisitAllRooms()
    {
        IReadOnlyList<IReadOnlyList<int>> rooms = [new[] { 1 }, new[] { 2 }, new[] { 3 }, Array.Empty<int>()];
        Assert.True(AdditionalGraphBacktrackingProblems.CanVisitAllRooms(rooms));
    }

    [Fact]
    public void P994_MinutesToRotAllOranges()
    {
        int[][] grid = [[2, 1, 1], [1, 1, 0], [0, 1, 1]];
        Assert.Equal(4, AdditionalGraphBacktrackingProblems.MinutesToRotAllOranges(grid));
        Assert.Equal(1, grid[0][1]); // 实现不修改输入。
    }

    [Fact]
    public void P1971_HasValidPath()
    {
        Assert.True(AdditionalGraphBacktrackingProblems.HasValidPath(3, [(0, 1), (1, 2), (2, 0)], 0, 2));
        Assert.False(AdditionalGraphBacktrackingProblems.HasValidPath(6, [(0, 1), (0, 2), (3, 5), (5, 4), (4, 3)], 0, 5));
    }
}
