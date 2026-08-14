using DataStructureAndAlgorithm.Sorting;

namespace DataStructureAndAlgorithm.Test.SortingTest;

public class NonComparisonSortAlgorithmsTest
{
    [Fact]
    public void CountingSort_ShouldHandleDuplicatesAndNegativeValues()
    {
        var values = new List<int> { 4, -2, 2, 8, 3, 3, 1, -2 };

        NonComparisonSortAlgorithms.CountingSort(values);

        Assert.Equal([-2, -2, 1, 2, 3, 3, 4, 8], values);
    }

    [Fact]
    public void CountingSort_ShouldRejectImpracticallyLargeRange()
    {
        var values = new List<int> { int.MinValue, int.MaxValue };

        Assert.Throws<ArgumentException>(() => NonComparisonSortAlgorithms.CountingSort(values));
    }

    [Fact]
    public void BucketSort_ShouldHandleNegativeFractionsDuplicatesAndCustomBucketCount()
    {
        var values = new List<double> { 0.42, -3.5, 1.0, 0.42, 9.1, -0.01, 2.7 };

        NonComparisonSortAlgorithms.BucketSort(values, bucketCount: 4);

        Assert.Equal([-3.5, -0.01, 0.42, 0.42, 1.0, 2.7, 9.1], values);
    }

    [Fact]
    public void BucketSort_ShouldRejectNonFiniteValuesAndInvalidBucketCount()
    {
        Assert.Throws<ArgumentException>(() =>
            NonComparisonSortAlgorithms.BucketSort(new List<double> { 1, double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NonComparisonSortAlgorithms.BucketSort(new List<double> { 1, 2 }, 0));
    }

    [Fact]
    public void RadixSort_ShouldHandleTheEntireSignedIntegerDomain()
    {
        var values = new List<int>
        {
            0, -1, 1, int.MinValue, int.MaxValue, -1000, 1000, -1, 256, -256
        };

        NonComparisonSortAlgorithms.RadixSort(values);

        Assert.Equal(
            [int.MinValue, -1000, -256, -1, -1, 0, 1, 256, 1000, int.MaxValue],
            values);
    }
}
