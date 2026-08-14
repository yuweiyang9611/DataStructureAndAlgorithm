namespace DataStructureAndAlgorithm.PatternMatching;

/// <summary>滚动哈希与 Z Algorithm 字符串算法。</summary>
public static class AdvancedStringAlgorithms
{
    private const long HashBase = 257;
    private const long HashModulus = 1_000_000_007;

    /// <summary>
    /// 使用 Rabin-Karp 滚动哈希返回模式串的全部匹配起点。
    /// </summary>
    /// <remarks>
    /// 平均时间 O(n + m)。哈希相等后仍比较原字符，避免哈希冲突造成错误结果。
    /// </remarks>
    public static IReadOnlyList<int> RabinKarpFindAll(string text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(pattern);

        if (pattern.Length == 0)
        {
            return Enumerable.Range(0, text.Length + 1).ToArray();
        }

        if (pattern.Length > text.Length)
        {
            return [];
        }

        long highestPower = 1;
        long patternHash = 0;
        long windowHash = 0;

        for (var index = 0; index < pattern.Length; index++)
        {
            patternHash = (patternHash * HashBase + pattern[index]) % HashModulus;
            windowHash = (windowHash * HashBase + text[index]) % HashModulus;

            if (index < pattern.Length - 1)
            {
                highestPower = highestPower * HashBase % HashModulus;
            }
        }

        var matches = new List<int>();

        for (var start = 0; start <= text.Length - pattern.Length; start++)
        {
            if (windowHash == patternHash &&
                text.AsSpan(start, pattern.Length).SequenceEqual(pattern.AsSpan()))
            {
                matches.Add(start);
            }

            if (start == text.Length - pattern.Length)
            {
                continue;
            }

            var removed = text[start] * highestPower % HashModulus;
            windowHash = (windowHash - removed + HashModulus) % HashModulus;
            windowHash = (windowHash * HashBase + text[start + pattern.Length]) % HashModulus;
        }

        return matches;
    }

    /// <summary>
    /// 计算 Z 数组：z[i] 表示从 i 开始与整个字符串前缀相同的最长长度。
    /// </summary>
    /// <remarks>通过维护最右匹配区间把复杂度降到 O(n)。约定 z[0] 等于字符串长度。</remarks>
    public static int[] CalculateZFunction(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            return [];
        }

        var z = new int[text.Length];
        z[0] = text.Length;
        var left = 0;
        var right = 0;

        for (var index = 1; index < text.Length; index++)
        {
            if (index < right)
            {
                z[index] = Math.Min(right - index, z[index - left]);
            }

            while (index + z[index] < text.Length && text[z[index]] == text[index + z[index]])
            {
                z[index]++;
            }

            if (index + z[index] > right)
            {
                left = index;
                right = index + z[index];
            }
        }

        return z;
    }
}
