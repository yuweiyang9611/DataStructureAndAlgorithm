using DataStructureAndAlgorithm.Sorting;

namespace DataStructureAndAlgorithm.Test.SortingTest;

public class SequenceExtensionsTest
{
    [Fact]
    public void IsSorted_ShouldUseCSharp14ExtensionMemberSyntax()
    {
        Assert.True(Array.Empty<int>().IsSorted());
        Assert.True(new[] { 1, 1, 3, 5 }.IsSorted());
        Assert.False(new[] { 1, 4, 2 }.IsSorted());
    }

    [Fact]
    public void IsSorted_ShouldHonorCustomComparer()
    {
        string[] values = ["bbb", "cc", "a"];
        var descendingByLength = Comparer<string>.Create(
            (left, right) => right.Length.CompareTo(left.Length));

        Assert.True(values.IsSorted(descendingByLength));
    }
}
