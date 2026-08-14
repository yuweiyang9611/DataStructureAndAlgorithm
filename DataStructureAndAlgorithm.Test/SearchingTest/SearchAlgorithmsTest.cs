using DataStructureAndAlgorithm.Searching;

namespace DataStructureAndAlgorithm.Test.SearchingTest;

public class SearchAlgorithmsTest
{
    [Fact]
    public void BinarySearch_ShouldFindExistingValueAndRejectMissingValue()
    {
        int[] values = [1, 3, 5, 7, 9];

        Assert.Equal(2, SearchAlgorithms.BinarySearch(values, 5));
        Assert.Equal(-1, SearchAlgorithms.BinarySearch(values, 6));
    }

    [Fact]
    public void Bounds_ShouldLocateDuplicateRange()
    {
        int[] values = [1, 2, 2, 2, 5];

        Assert.Equal(1, SearchAlgorithms.LowerBound(values, 2));
        Assert.Equal(4, SearchAlgorithms.UpperBound(values, 2));
        Assert.Equal(5, SearchAlgorithms.LowerBound(values, 10));
    }

    [Fact]
    public void Search_ShouldHonorCustomComparer()
    {
        string[] values = ["A", "b", "C"];

        Assert.Equal(
            1,
            SearchAlgorithms.BinarySearch(values, "B", StringComparer.OrdinalIgnoreCase));
    }
}
