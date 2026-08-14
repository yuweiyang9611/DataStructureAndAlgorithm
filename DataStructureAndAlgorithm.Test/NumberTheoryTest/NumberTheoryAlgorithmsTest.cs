using System.Numerics;
using DataStructureAndAlgorithm.NumberTheory;

namespace DataStructureAndAlgorithm.Test.NumberTheoryTest;

public class NumberTheoryAlgorithmsTest
{
    [Theory]
    [InlineData(54, 24, 6)]
    [InlineData(-54, 24, 6)]
    [InlineData(0, 0, 0)]
    [InlineData(long.MinValue, 0, 9_223_372_036_854_775_808UL)]
    public void GreatestCommonDivisor_HandlesSignsAndLongMinValue(long left, long right, ulong expected) =>
        Assert.Equal(expected, NumberTheoryAlgorithms.GreatestCommonDivisor(left, right));

    [Fact]
    public void ExtendedGcd_SatisfiesBezoutIdentity()
    {
        var result = NumberTheoryAlgorithms.ExtendedGreatestCommonDivisor(240, 46);

        Assert.Equal(new BigInteger(2), result.GreatestCommonDivisor);
        Assert.Equal(result.GreatestCommonDivisor, 240 * result.X + 46 * result.Y);
    }

    [Fact]
    public void ModularOperations_RespectCoprimeContract()
    {
        Assert.Equal(445, NumberTheoryAlgorithms.ModularPower(4, 13, 497));
        Assert.Equal(4, NumberTheoryAlgorithms.ModularInverse(3, 11));
        Assert.Throws<ArgumentException>(() => NumberTheoryAlgorithms.ModularInverse(6, 9));
    }

    [Fact]
    public void SieveAndFactorization_ReturnExpectedPrimes()
    {
        Assert.True(NumberTheoryAlgorithms.SieveOfEratosthenes(20).SequenceEqual([2, 3, 5, 7, 11, 13, 17, 19]));
        Assert.Equal(new Dictionary<long, int> { [2] = 3, [3] = 2, [5] = 1 },
            NumberTheoryAlgorithms.PrimeFactorization(360));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(10, "55")]
    [InlineData(100, "354224848179261915075")]
    public void Fibonacci_UsesUnboundedIntegerResult(int n, string expected) =>
        Assert.Equal(BigInteger.Parse(expected), NumberTheoryAlgorithms.Fibonacci(n));
}
