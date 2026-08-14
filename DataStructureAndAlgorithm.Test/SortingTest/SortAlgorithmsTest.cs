using DataStructureAndAlgorithm.Sorting;

namespace DataStructureAndAlgorithm.Test.SortingTest;

public class SortAlgorithmsTest
{
    public static IEnumerable<object[]> SortingAlgorithms()
    {
        yield return [new Action<IList<int>>(values => SortAlgorithms.BubbleSort(values))];
        yield return [new Action<IList<int>>(values => SortAlgorithms.SelectionSort(values))];
        yield return [new Action<IList<int>>(values => SortAlgorithms.InsertionSort(values))];
        yield return [new Action<IList<int>>(values => SortAlgorithms.MergeSort(values))];
        yield return [new Action<IList<int>>(values => SortAlgorithms.QuickSort(values))];
        yield return [new Action<IList<int>>(values => SortAlgorithms.HeapSort(values))];
    }

    [Theory]
    [MemberData(nameof(SortingAlgorithms))]
    public void Sort_ShouldHandleDuplicatesAndNegativeNumbers(Action<IList<int>> sort)
    {
        ArgumentNullException.ThrowIfNull(sort);

        var values = new List<int> { 5, -1, 3, 3, 0, -7, 9 };

        sort(values);

        Assert.Equal([-7, -1, 0, 3, 3, 5, 9], values);
    }

    [Theory]
    [MemberData(nameof(SortingAlgorithms))]
    public void Sort_ShouldHandleEmptyAndSingleElementCollections(Action<IList<int>> sort)
    {
        ArgumentNullException.ThrowIfNull(sort);

        var empty = new List<int>();
        var single = new List<int> { 42 };

        sort(empty);
        sort(single);

        Assert.Empty(empty);
        Assert.Equal([42], single);
    }

    [Fact]
    public void MergeSort_ShouldHonorCustomComparer()
    {
        var values = new List<string> { "bbb", "a", "cc" };
        var byLength = Comparer<string>.Create((left, right) => left.Length.CompareTo(right.Length));

        SortAlgorithms.MergeSort(values, byLength);

        Assert.Equal(["a", "cc", "bbb"], values);
    }
}
