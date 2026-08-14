using System.Collections.ObjectModel;

namespace DataStructureAndAlgorithm.PatternMatching;

/// <summary>后缀数组和相邻后缀最长公共前缀（LCP）数组。</summary>
public sealed class SuffixArrayResult
{
    private readonly int[] _suffixes;

    internal SuffixArrayResult(int[] suffixes, int[] longestCommonPrefixes)
    {
        _suffixes = suffixes;
        Suffixes = Array.AsReadOnly(suffixes);
        LongestCommonPrefixes = Array.AsReadOnly(longestCommonPrefixes);
    }

    /// <summary>后缀起点按字典序排列的只读视图。</summary>
    public IReadOnlyList<int> Suffixes { get; }

    /// <summary>LCP[i] 是 Suffixes[i-1] 与 Suffixes[i] 的最长公共前缀长度；LCP[0] 为 0。</summary>
    public IReadOnlyList<int> LongestCommonPrefixes { get; }

    /// <summary>通过二分查找返回非空 pattern 的全部后缀起点，结果按后缀字典序排列。</summary>
    public IReadOnlyList<int> FindAll(string text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(pattern);
        if (pattern.Length == 0) throw new ArgumentException("Pattern cannot be empty.", nameof(pattern));
        if (text.Length != _suffixes.Length) throw new ArgumentException("Text does not match this suffix array.", nameof(text));

        var lower = LowerBound(matchUpperBoundary: false);
        var upper = LowerBound(matchUpperBoundary: true);
        return Array.AsReadOnly(_suffixes[lower..upper]);

        int LowerBound(bool matchUpperBoundary)
        {
            var left = 0;
            var right = _suffixes.Length;
            while (left < right)
            {
                var middle = left + (right - left) / 2;
                var suffix = text.AsSpan(_suffixes[middle]);
                var prefixLength = Math.Min(suffix.Length, pattern.Length);
                var comparison = suffix[..prefixLength].CompareTo(pattern.AsSpan(0, prefixLength), StringComparison.Ordinal);
                if (comparison == 0)
                {
                    comparison = suffix.Length < pattern.Length ? -1 : matchUpperBoundary ? -1 : 0;
                }

                if (comparison < 0) left = middle + 1;
                else right = middle;
            }

            return left;
        }
    }
}

public static class SuffixArrayAlgorithms
{
    /// <summary>
    /// 使用倍增法构建后缀数组，再用 Kasai 算法在线性时间构建 LCP。
    /// 倍增轮次为 O(log n)，每轮比较两个整数排名并排序；本实现总体 O(n log² n)，便于理解和验证。
    /// </summary>
    public static SuffixArrayResult Build(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0) return new SuffixArrayResult([], []);

        var suffixes = Enumerable.Range(0, text.Length).ToArray();
        var ranks = text.Select(character => (int)character).ToArray();
        var nextRanks = new int[text.Length];

        for (var width = 1; width < text.Length; width = checked(width * 2))
        {
            Array.Sort(suffixes, (left, right) =>
            {
                var first = ranks[left].CompareTo(ranks[right]);
                if (first != 0) return first;
                var leftSecond = left + width < text.Length ? ranks[left + width] : -1;
                var rightSecond = right + width < text.Length ? ranks[right + width] : -1;
                return leftSecond.CompareTo(rightSecond);
            });

            nextRanks[suffixes[0]] = 0;
            for (var order = 1; order < suffixes.Length; order++)
            {
                var previous = suffixes[order - 1];
                var current = suffixes[order];
                var sameFirst = ranks[previous] == ranks[current];
                var previousSecond = previous + width < text.Length ? ranks[previous + width] : -1;
                var currentSecond = current + width < text.Length ? ranks[current + width] : -1;
                nextRanks[current] = nextRanks[previous] + (sameFirst && previousSecond == currentSecond ? 0 : 1);
            }

            (ranks, nextRanks) = (nextRanks, ranks);
            if (ranks[suffixes[^1]] == text.Length - 1 || width > text.Length / 2) break;
        }

        var inverse = new int[text.Length];
        for (var order = 0; order < suffixes.Length; order++) inverse[suffixes[order]] = order;
        var lcp = new int[text.Length];
        var common = 0;
        for (var start = 0; start < text.Length; start++)
        {
            var order = inverse[start];
            if (order == 0) continue;
            var previousStart = suffixes[order - 1];
            while (start + common < text.Length && previousStart + common < text.Length &&
                   text[start + common] == text[previousStart + common])
            {
                common++;
            }

            lcp[order] = common;
            if (common > 0) common--;
        }

        return new SuffixArrayResult(suffixes, lcp);
    }
}
