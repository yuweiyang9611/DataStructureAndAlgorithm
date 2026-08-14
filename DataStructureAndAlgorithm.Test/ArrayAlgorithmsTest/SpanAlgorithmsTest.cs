using System.Buffers;
using DataStructureAndAlgorithm.Memory;

namespace DataStructureAndAlgorithm.Test.ArrayAlgorithmsTest;

public class SpanAlgorithmsTest
{
    [Fact]
    public void BinarySearch_ReturnsIndexOrComplementedInsertionPoint()
    {
        int[] values = [1, 3, 5, 7];

        Assert.Equal(2, SpanAlgorithms.BinarySearch<int>(values, 5));
        Assert.Equal(~2, SpanAlgorithms.BinarySearch<int>(values, 4));
        Assert.Equal(~0, SpanAlgorithms.BinarySearch<int>(values, 0));
        Assert.Equal(~4, SpanAlgorithms.BinarySearch<int>(values, 9));
    }

    [Fact]
    public void PooledRadixSort_HandlesSignedDomainAndReturnsRentedBuffer()
    {
        int[] values = [0, -1, int.MaxValue, int.MinValue, 42, -42, 42];
        var pool = new TrackingIntPool();

        SpanAlgorithms.PooledRadixSort(values, pool);

        Assert.True(values.SequenceEqual([int.MinValue, -42, -1, 0, 42, 42, int.MaxValue]));
        Assert.Equal(1, pool.RentCount);
        Assert.Equal(1, pool.ReturnCount);
    }

    [Fact]
    public void PooledRadixSort_RandomInputsMatchArraySort()
    {
        var random = new Random(42);
        for (var sample = 0; sample < 100; sample++)
        {
            var values = Enumerable.Range(0, random.Next(0, 500)).Select(_ => random.Next()).ToArray();
            if (sample % 2 == 0)
            {
                for (var index = 0; index < values.Length; index++) values[index] = -values[index];
            }

            var expected = (int[])values.Clone();
            Array.Sort(expected);
            SpanAlgorithms.PooledRadixSort(values);
            Assert.True(values.SequenceEqual(expected));
        }
    }

    private sealed class TrackingIntPool : ArrayPool<int>
    {
        public int RentCount { get; private set; }
        public int ReturnCount { get; private set; }

        public override int[] Rent(int minimumLength)
        {
            RentCount++;
            return new int[minimumLength + 3]; // 模拟共享池可能返回更大的数组。
        }

        public override void Return(int[] array, bool clearArray = false)
        {
            ReturnCount++;
            if (clearArray) Array.Clear(array);
        }
    }
}
