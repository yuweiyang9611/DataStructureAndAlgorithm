using System.Collections;
using System.Text;

namespace DataStructureAndAlgorithm.Probabilistic;

/// <summary>
/// 使用稳定字符串哈希的 Bloom Filter。
/// </summary>
/// <remarks>
/// <para>
/// Bloom Filter 用少量位图回答“某个键一定不存在，还是可能存在”。它允许假阳性，却绝不能产生假阴性；
/// 因此存储引擎只会用它跳过肯定无效的 B+ 树查询，不会把“可能存在”直接当成命中结果。
/// </para>
/// <para>
/// 这里不使用 <see cref="string.GetHashCode()"/>，因为 .NET 会在不同进程中随机化字符串哈希。
/// 教学项目改用确定性的 FNV-1a，并通过双重哈希生成多个位下标，保证测试、Trace 和重启演示可复现。
/// </para>
/// </remarks>
public sealed class StringBloomFilter
{
    private readonly BitArray _bits;

    public StringBloomFilter(int bitCount = 8_192, int hashFunctionCount = 5)
    {
        if (bitCount < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), bitCount, "位图长度必须至少为 8。");
        }

        if (hashFunctionCount is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hashFunctionCount), hashFunctionCount, "哈希函数数量必须位于 [1, 32]。");
        }

        BitCount = bitCount;
        HashFunctionCount = hashFunctionCount;
        _bits = new BitArray(bitCount);
    }

    public int BitCount { get; }

    public int HashFunctionCount { get; }

    /// <summary>把键对应的 k 个位全部设置为 1。</summary>
    public void Add(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var index in GetIndexes(value))
        {
            _bits[index] = true;
        }
    }

    /// <summary>
    /// 返回 <see langword="false"/> 时键一定没有加入过；返回 <see langword="true"/> 时仍需查询真实索引。
    /// </summary>
    public bool MightContain(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var index in GetIndexes(value))
        {
            if (!_bits[index])
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerable<int> GetIndexes(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var first = Fnv1A(bytes, 14_695_981_039_346_656_037UL);
        // 第二个哈希强制为奇数，避免在 2 的幂大小位图上只访问某个偶数子集。
        var second = Fnv1A(bytes, 1_099_511_628_211UL) | 1UL;

        for (var number = 0; number < HashFunctionCount; number++)
        {
            var combined = unchecked(first + (ulong)number * second);
            yield return (int)(combined % (ulong)BitCount);
        }
    }

    private static ulong Fnv1A(ReadOnlySpan<byte> bytes, ulong offsetBasis)
    {
        const ulong prime = 1_099_511_628_211UL;
        var hash = offsetBasis;
        foreach (var value in bytes)
        {
            hash ^= value;
            hash = unchecked(hash * prime);
        }

        return hash;
    }
}
