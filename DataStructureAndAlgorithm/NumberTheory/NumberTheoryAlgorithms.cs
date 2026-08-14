using System.Collections.ObjectModel;
using System.Numerics;

namespace DataStructureAndAlgorithm.NumberTheory;

/// <summary>扩展欧几里得算法返回的贝祖等式：Left * X + Right * Y = GreatestCommonDivisor。</summary>
public readonly record struct BezoutIdentity(
    BigInteger GreatestCommonDivisor,
    BigInteger X,
    BigInteger Y);

/// <summary>
/// 数论基础算法。实现刻意保留推导步骤，适合学习整数边界、模运算和对数级迭代。
/// </summary>
public static class NumberTheoryAlgorithms
{
    /// <summary>
    /// 使用欧几里得算法计算最大公约数，时间 O(log(min(|a|, |b|)))。
    /// 返回 <see cref="ulong"/>，因为 |long.MinValue| 无法由 <see cref="long"/> 表示。
    /// </summary>
    public static ulong GreatestCommonDivisor(long left, long right)
    {
        var a = Magnitude(left);
        var b = Magnitude(right);
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return a;
    }

    /// <summary>
    /// 根据 lcm(a,b) = |a| / gcd(a,b) * |b| 计算最小公倍数。
    /// 先除后乘可降低中间结果溢出的概率；最终结果超出 ulong 时明确抛出异常。
    /// </summary>
    public static ulong LeastCommonMultiple(long left, long right)
    {
        var a = Magnitude(left);
        var b = Magnitude(right);
        if (a == 0 || b == 0) return 0;
        return checked(a / GreatestCommonDivisor(left, right) * b);
    }

    /// <summary>
    /// 扩展欧几里得算法同时求 gcd 与贝祖系数。使用 BigInteger 保存中间系数，
    /// 避免 long 边界输入在回代过程中溢出。
    /// </summary>
    public static BezoutIdentity ExtendedGreatestCommonDivisor(long left, long right)
    {
        BigInteger oldR = left;
        BigInteger r = right;
        BigInteger oldX = 1;
        BigInteger x = 0;
        BigInteger oldY = 0;
        BigInteger y = 1;

        while (r != 0)
        {
            var quotient = oldR / r;
            (oldR, r) = (r, oldR - quotient * r);
            (oldX, x) = (x, oldX - quotient * x);
            (oldY, y) = (y, oldY - quotient * y);
        }

        // gcd 约定为非负数；若最后余数为负，三个量必须同时取反才能保持等式成立。
        if (oldR < 0) (oldR, oldX, oldY) = (-oldR, -oldX, -oldY);
        return new BezoutIdentity(oldR, oldX, oldY);
    }

    /// <summary>
    /// 二进制快速幂：每轮把指数减半，只在当前二进制位为 1 时把底数乘入结果。
    /// 模数必须为正；乘法通过 BigInteger 保证 long 全域输入不会在取模前溢出。
    /// </summary>
    public static long ModularPower(long value, ulong exponent, long modulus)
    {
        if (modulus <= 0) throw new ArgumentOutOfRangeException(nameof(modulus), "Modulus must be positive.");
        BigInteger result = 1 % modulus;
        BigInteger factor = ((BigInteger)value % modulus + modulus) % modulus;

        while (exponent > 0)
        {
            if ((exponent & 1) != 0) result = result * factor % modulus;
            factor = factor * factor % modulus;
            exponent >>= 1;
        }

        return (long)result;
    }

    /// <summary>
    /// 求 value 在模 modulus 下的乘法逆元。逆元存在当且仅当 gcd(value, modulus) = 1。
    /// </summary>
    public static long ModularInverse(long value, long modulus)
    {
        if (modulus <= 1) throw new ArgumentOutOfRangeException(nameof(modulus), "Modulus must be greater than one.");
        var identity = ExtendedGreatestCommonDivisor(value, modulus);
        if (identity.GreatestCommonDivisor != BigInteger.One)
        {
            throw new ArgumentException("A modular inverse exists only for coprime values.", nameof(value));
        }

        return (long)((identity.X % modulus + modulus) % modulus);
    }

    /// <summary>
    /// 埃氏筛返回不大于 maxInclusive 的全部质数。每个合数从 p² 开始标记，
    /// 因为更小的 p 倍数已经被更小质因子处理。时间 O(n log log n)，空间 O(n)。
    /// </summary>
    public static IReadOnlyList<int> SieveOfEratosthenes(int maxInclusive)
    {
        if (maxInclusive < 0) throw new ArgumentOutOfRangeException(nameof(maxInclusive));
        if (maxInclusive < 2) return [];

        var composite = new bool[maxInclusive + 1];
        for (var prime = 2; prime <= maxInclusive / prime; prime++)
        {
            if (composite[prime]) continue;
            for (var multiple = prime * prime; multiple <= maxInclusive; multiple += prime)
            {
                composite[multiple] = true;
            }
        }

        var primes = new List<int>();
        for (var candidate = 2; candidate <= maxInclusive; candidate++)
        {
            if (!composite[candidate]) primes.Add(candidate);
        }

        return primes.AsReadOnly();
    }

    /// <summary>试除法分解正整数，返回“质因子 - 指数”的只读映射，时间 O(sqrt(n))。</summary>
    public static IReadOnlyDictionary<long, int> PrimeFactorization(long value)
    {
        if (value < 2) throw new ArgumentOutOfRangeException(nameof(value), "Value must be at least two.");
        var factors = new Dictionary<long, int>();
        var remaining = value;

        AddFactor(2);
        for (long candidate = 3; candidate <= remaining / candidate; candidate += 2) AddFactor(candidate);
        if (remaining > 1) factors.Add(remaining, 1);
        return new ReadOnlyDictionary<long, int>(factors);

        void AddFactor(long factor)
        {
            if (remaining % factor != 0) return;
            var count = 0;
            do
            {
                remaining /= factor;
                count++;
            } while (remaining % factor == 0);
            factors.Add(factor, count);
        }
    }

    /// <summary>
    /// 用矩阵快速幂计算第 n 个 Fibonacci 数（F(0)=0、F(1)=1），时间 O(log n)。
    /// BigInteger 让示例关注算法本身，而不是在第 93 项被 ulong 上限截断。
    /// </summary>
    public static BigInteger Fibonacci(int n)
    {
        if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
        if (n == 0) return BigInteger.Zero;

        var result = Matrix.Identity;
        var factor = new Matrix(1, 1, 1, 0);
        var exponent = n - 1;
        while (exponent > 0)
        {
            if ((exponent & 1) != 0) result *= factor;
            factor *= factor;
            exponent >>= 1;
        }

        return result.M00;
    }

    private static ulong Magnitude(long value) => value >= 0
        ? (ulong)value
        : (ulong)(-(value + 1)) + 1;

    private readonly record struct Matrix(BigInteger M00, BigInteger M01, BigInteger M10, BigInteger M11)
    {
        public static Matrix Identity => new(1, 0, 0, 1);

        public static Matrix operator *(Matrix left, Matrix right) => new(
            left.M00 * right.M00 + left.M01 * right.M10,
            left.M00 * right.M01 + left.M01 * right.M11,
            left.M10 * right.M00 + left.M11 * right.M10,
            left.M10 * right.M01 + left.M11 * right.M11);
    }
}
