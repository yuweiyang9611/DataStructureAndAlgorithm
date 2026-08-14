using DataStructureAndAlgorithm.BitManipulation;
using DataStructureAndAlgorithm.NumberTheory;

namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>P3 数论与位运算专题中的 6 道经典题。</summary>
public static class NumericAndBitProblems
{
    /// <summary>50 - Pow(x, n)。二进制快速幂把 O(|n|) 次乘法降为 O(log |n|)。</summary>
    public static double Power(double value, int exponent)
    {
        // 必须先提升为 long 再取负；直接对 int.MinValue 取负仍会溢出。
        long remaining = exponent;
        if (remaining < 0)
        {
            value = 1 / value;
            remaining = -remaining;
        }

        var result = 1d;
        while (remaining > 0)
        {
            if ((remaining & 1) != 0) result *= value;
            value *= value;
            remaining >>= 1;
        }

        return result;
    }

    /// <summary>69 - Sqrt(x)。在整数域二分查找 floor(sqrt(x))，用除法避免 middle² 溢出。</summary>
    public static int IntegerSquareRoot(int value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        if (value < 2) return value;
        var left = 1;
        var right = value / 2;
        var answer = 1;
        while (left <= right)
        {
            var middle = left + (right - left) / 2;
            if (middle <= value / middle)
            {
                answer = middle;
                left = middle + 1;
            }
            else
            {
                right = middle - 1;
            }
        }

        return answer;
    }

    /// <summary>191 - Number of 1 Bits。</summary>
    public static int HammingWeight(uint value) => BitAlgorithms.CountSetBits(value);

    /// <summary>204 - Count Primes。筛出严格小于 n 的质数。</summary>
    public static int CountPrimes(int exclusiveUpperBound)
    {
        if (exclusiveUpperBound < 0) throw new ArgumentOutOfRangeException(nameof(exclusiveUpperBound));
        return exclusiveUpperBound <= 2
            ? 0
            : NumberTheoryAlgorithms.SieveOfEratosthenes(exclusiveUpperBound - 1).Count;
    }

    /// <summary>231 - Power of Two。</summary>
    public static bool IsPowerOfTwo(int value) => value > 0 && BitAlgorithms.IsPowerOfTwo((uint)value);

    /// <summary>338 - Counting Bits。删除最低置位后得到更小状态：bits[i] = bits[i &amp; (i-1)] + 1。</summary>
    public static int[] CountBits(int maximum)
    {
        if (maximum < 0) throw new ArgumentOutOfRangeException(nameof(maximum));
        var counts = new int[maximum + 1];
        for (var value = 1; value <= maximum; value++)
        {
            counts[value] = counts[value & (value - 1)] + 1;
        }

        return counts;
    }
}
