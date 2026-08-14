using System.Numerics;

namespace DataStructureAndAlgorithm.BitManipulation;

/// <summary>位运算教学算法：使用无符号整数，让右移明确执行逻辑右移而不是符号扩展。</summary>
public static class BitAlgorithms
{
    /// <summary>
    /// Brian Kernighan 算法每次执行 value &amp;= value - 1 都会删除最低位的 1，
    /// 因此循环次数等于置位数，而不是固定扫描 32 位。
    /// </summary>
    public static int CountSetBits(uint value)
    {
        var count = 0;
        while (value != 0)
        {
            value &= value - 1;
            count++;
        }

        return count;
    }

    /// <summary>正的 2 的幂只有一个二进制位为 1，删除最低置位后必为 0。</summary>
    public static bool IsPowerOfTwo(uint value) => value != 0 && (value & (value - 1)) == 0;

    /// <summary>两个数不同的位恰好是异或结果中的置位。</summary>
    public static int HammingDistance(uint left, uint right) => CountSetBits(left ^ right);

    /// <summary>反转 32 个二进制位。固定执行 32 轮，前导零也会被移动到结果低位。</summary>
    public static uint ReverseBits(uint value)
    {
        uint result = 0;
        for (var bit = 0; bit < 32; bit++)
        {
            result = (result << 1) | (value & 1);
            value >>= 1;
        }

        return result;
    }

    /// <summary>
    /// 返回从 mask 到 0 的全部子掩码。公式 (submask - 1) &amp; mask 会跳到严格更小的下一个子集。
    /// </summary>
    public static IEnumerable<uint> EnumerateSubmasks(uint mask)
    {
        var submask = mask;
        while (true)
        {
            yield return submask;
            if (submask == 0) yield break;
            submask = (submask - 1) & mask;
        }
    }

    /// <summary>
    /// 按位掩码枚举集合的全部子集。限制为 30 个元素，避免 1 &lt;&lt; Count 的 int 位移回绕，
    /// 同时提醒调用者输出规模本身就是 O(2^n)。
    /// </summary>
    public static IEnumerable<IReadOnlyList<T>> EnumerateSubsets<T>(IReadOnlyList<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count > 30) throw new ArgumentOutOfRangeException(nameof(values), "At most 30 elements are supported.");
        return Enumerate();

        IEnumerable<IReadOnlyList<T>> Enumerate()
        {
            var subsetCount = 1 << values.Count;
            for (var mask = 0; mask < subsetCount; mask++)
            {
                var subset = new List<T>();
                for (var bit = 0; bit < values.Count; bit++)
                {
                    if ((mask & (1 << bit)) != 0) subset.Add(values[bit]);
                }

                yield return subset;
            }
        }
    }

    /// <summary>教学实现与硬件优化的 BCL 实现作对照；业务代码通常应优先使用后者。</summary>
    public static int CountSetBitsWithFramework(uint value) => BitOperations.PopCount(value);
}
