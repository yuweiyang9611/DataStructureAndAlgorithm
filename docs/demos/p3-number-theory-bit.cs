// 数论与位运算：欧几里得、模幂、筛法、质因数分解，再用位运算数子集。
// 把模数换成一个合数，看 ModularInverse 还能不能算出来。
using DataStructureAndAlgorithm.BitManipulation;
using DataStructureAndAlgorithm.NumberTheory;

public static class Demo
{
    public static void Run()
    {
        Console.WriteLine($"gcd(1071, 462) = {NumberTheoryAlgorithms.GreatestCommonDivisor(1071, 462)}，lcm = {NumberTheoryAlgorithms.LeastCommonMultiple(1071, 462)}");
        var bezout = NumberTheoryAlgorithms.ExtendedGreatestCommonDivisor(240, 46);
        Console.WriteLine($"扩展欧几里得：240 × {bezout.X} + 46 × {bezout.Y} = {bezout.GreatestCommonDivisor}");
        Console.WriteLine($"3^200 mod 1_000_000_007 = {NumberTheoryAlgorithms.ModularPower(3, 200, 1_000_000_007)}");
        var inverse = NumberTheoryAlgorithms.ModularInverse(7, 13);
        Console.WriteLine($"7 在模 13 下的逆元 = {inverse}，验算 7 × {inverse} mod 13 = {7 * inverse % 13}");
        Console.WriteLine($"50 以内的素数：{string.Join(" ", NumberTheoryAlgorithms.SieveOfEratosthenes(50))}");
        var factors = NumberTheoryAlgorithms.PrimeFactorization(360).OrderBy(factor => factor.Key).Select(factor => $"{factor.Key}^{factor.Value}");
        Console.WriteLine($"360 的质因数分解：{string.Join(" × ", factors)}");
        Console.WriteLine($"矩阵快速幂 Fibonacci(100) = {NumberTheoryAlgorithms.Fibonacci(100)}");

        Console.WriteLine($"popcount(0b1011_0110) = {BitAlgorithms.CountSetBits(0b1011_0110)}，框架的 PopCount 也是 {BitAlgorithms.CountSetBitsWithFramework(0b1011_0110)}");
        Console.WriteLine($"64 是 2 的幂？{BitAlgorithms.IsPowerOfTwo(64)}；汉明距离(1, 4) = {BitAlgorithms.HammingDistance(1, 4)}；reverse(1) = 0x{BitAlgorithms.ReverseBits(1):X8}");
        var submasks = BitAlgorithms.EnumerateSubmasks(0b1011).Select(mask => Convert.ToString(mask, 2).PadLeft(4, '0'));
        Console.WriteLine($"0b1011 的全部子掩码：{string.Join(" ", submasks)}");
        var subsets = BitAlgorithms.EnumerateSubsets(new[] { "a", "b", "c" }).Select(subset => "{" + string.Join(",", subset) + "}");
        Console.WriteLine($"按位枚举 {{a,b,c}} 的子集：{string.Join(" ", subsets)}");
    }
}
