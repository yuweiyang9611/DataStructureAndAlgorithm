using DataStructureAndAlgorithm.Backtracking;

namespace DataStructureAndAlgorithm.Test.BacktrackingTest;

public class BacktrackingAlgorithmsTest
{
    [Fact]
    public void SolveNQueens_ForFourByFourBoard_ShouldReturnTwoValidSolutions()
    {
        var solutions = BacktrackingAlgorithms.SolveNQueens(4);

        Assert.Equal(2, solutions.Count);
        Assert.All(solutions, AssertValidSolution);
    }

    [Fact]
    public void SolveNQueens_ForSingleCell_ShouldReturnOneSolution()
    {
        var solution = Assert.Single(BacktrackingAlgorithms.SolveNQueens(1));

        Assert.Single(solution);
        Assert.Equal(0, solution[0]);
    }

    private static void AssertValidSolution(int[] columns)
    {
        Assert.Equal(columns.Length, columns.Distinct().Count());

        for (var firstRow = 0; firstRow < columns.Length; firstRow++)
        {
            for (var secondRow = firstRow + 1; secondRow < columns.Length; secondRow++)
            {
                var rowDistance = secondRow - firstRow;
                var columnDistance = Math.Abs(columns[secondRow] - columns[firstRow]);
                Assert.NotEqual(rowDistance, columnDistance);
            }
        }
    }
}
