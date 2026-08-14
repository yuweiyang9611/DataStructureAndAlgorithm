using DataStructureAndAlgorithm.Sorting;

namespace DataStructureAndAlgorithm.Test.SortingTest;

public class SortAlgorithmsDifferentialTest
{
    [Fact]
    public void AllSorts_ShouldMatchStandardLibraryForDeterministicRandomInputs()
    {
        const int seed = 20260712;
        var random = new Random(seed);
        Action<IList<int>>[] algorithms =
        [
            values => SortAlgorithms.BubbleSort(values),
            values => SortAlgorithms.SelectionSort(values),
            values => SortAlgorithms.InsertionSort(values),
            values => SortAlgorithms.MergeSort(values),
            values => SortAlgorithms.QuickSort(values),
            values => SortAlgorithms.HeapSort(values)
        ];

        for (var sample = 0; sample < 100; sample++)
        {
            var input = Enumerable.Range(0, random.Next(0, 40))
                .Select(_ => random.Next(-20, 21))
                .ToArray();
            var expected = input.Order().ToArray();

            foreach (var algorithm in algorithms)
            {
                var actual = input.ToList();
                algorithm(actual);
                Assert.Equal(expected, actual);
            }
        }
    }
}
