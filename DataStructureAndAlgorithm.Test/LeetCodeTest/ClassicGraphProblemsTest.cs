using DataStructureAndAlgorithm.LeetCode;

namespace DataStructureAndAlgorithm.Test.LeetCodeTest;

public class ClassicGraphProblemsTest
{
    [Fact]
    public void NumberOfIslands_ShouldCountFourDirectionComponentsWithoutMutatingGrid()
    {
        char[][] grid =
        [
            ['1', '1', '0', '0', '0'],
            ['1', '1', '0', '0', '0'],
            ['0', '0', '1', '0', '0'],
            ['0', '0', '0', '1', '1']
        ];

        Assert.Equal(3, ClassicGraphProblems.NumberOfIslands(grid));
        Assert.Equal('1', grid[0][0]);
    }

    [Fact]
    public void CourseSchedule_ShouldDetectDependencyCycle()
    {
        Assert.True(ClassicGraphProblems.CanFinishCourses(2, [(1, 0)]));
        Assert.False(ClassicGraphProblems.CanFinishCourses(2, [(1, 0), (0, 1)]));
    }
}
