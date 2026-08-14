using DataStructureAndAlgorithm.BitManipulation;

namespace DataStructureAndAlgorithm.Test.BitManipulationTest;

public class BitAlgorithmsTest
{
    [Theory]
    [InlineData(0U, 0)]
    [InlineData(11U, 3)]
    [InlineData(uint.MaxValue, 32)]
    public void CountSetBits_MatchesExpected(uint value, int expected)
    {
        Assert.Equal(expected, BitAlgorithms.CountSetBits(value));
        Assert.Equal(BitAlgorithms.CountSetBitsWithFramework(value), BitAlgorithms.CountSetBits(value));
    }

    [Fact]
    public void PowerDistanceAndReverse_RespectBitPositions()
    {
        Assert.True(BitAlgorithms.IsPowerOfTwo(1U << 31));
        Assert.False(BitAlgorithms.IsPowerOfTwo(0));
        Assert.Equal(2, BitAlgorithms.HammingDistance(1, 4));
        Assert.Equal(964_176_192U, BitAlgorithms.ReverseBits(43_261_596U));
    }

    [Fact]
    public void Enumerators_ProduceEachSubsetOnce()
    {
        Assert.True(BitAlgorithms.EnumerateSubmasks(0b101).SequenceEqual([0b101U, 0b100U, 0b001U, 0U]));
        var subsets = BitAlgorithms.EnumerateSubsets(new[] { "A", "B", "C" })
            .Select(values => string.Concat(values))
            .ToArray();
        Assert.Equal(8, subsets.Length);
        Assert.Contains(string.Empty, subsets);
        Assert.Contains("ABC", subsets);
    }
}
