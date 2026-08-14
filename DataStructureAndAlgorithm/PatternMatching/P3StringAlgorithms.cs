using System.Text;

namespace DataStructureAndAlgorithm.PatternMatching;

/// <summary>字符串中的半开回文区间 [Start, EndExclusive)。</summary>
public readonly record struct PalindromeRange(int Start, int Length)
{
    public int EndExclusive => Start + Length;
}

/// <summary>按 Unicode 标量值计数的最长无重复子串位置。</summary>
public readonly record struct RuneSubstringRange(int Utf16Start, int Utf16Length, int RuneLength)
{
    public int Utf16EndExclusive => Utf16Start + Utf16Length;
}

/// <summary>Manacher、后缀自动机与 Unicode Rune 教学算法。</summary>
public static class P3StringAlgorithms
{
    /// <summary>
    /// Manacher 同时维护每个奇数/偶数回文中心的半径，并复用当前最右回文的镜像半径。
    /// 每次字符比较要么立即失败，要么推动最右边界，因此总时间 O(n)、空间 O(n)。
    /// </summary>
    public static PalindromeRange LongestPalindromeManacher(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0) return new PalindromeRange(0, 0);
        var odd = new int[text.Length];
        var left = 0;
        var right = -1;
        var best = new PalindromeRange(0, 1);

        for (var center = 0; center < text.Length; center++)
        {
            var radius = center > right ? 1 : Math.Min(odd[left + right - center], right - center + 1);
            while (center - radius >= 0 && center + radius < text.Length &&
                   text[center - radius] == text[center + radius]) radius++;
            odd[center] = radius;
            var length = radius * 2 - 1;
            if (length > best.Length) best = new PalindromeRange(center - radius + 1, length);
            if (center + radius - 1 > right)
            {
                left = center - radius + 1;
                right = center + radius - 1;
            }
        }

        var even = new int[text.Length];
        left = 0;
        right = -1;
        for (var center = 0; center < text.Length; center++)
        {
            var radius = center > right ? 0 : Math.Min(even[left + right - center + 1], right - center + 1);
            while (center - radius - 1 >= 0 && center + radius < text.Length &&
                   text[center - radius - 1] == text[center + radius]) radius++;
            even[center] = radius;
            var length = radius * 2;
            if (length > best.Length) best = new PalindromeRange(center - radius, length);
            if (center + radius - 1 > right)
            {
                left = center - radius;
                right = center + radius - 1;
            }
        }

        return best;
    }

    /// <summary>
    /// 按 <see cref="Rune"/> 而不是 UTF-16 char 计算最长无重复子串。
    /// 一个 emoji 等非 BMP 字符只算一个标量值，同时结果保留可用于 Substring 的 UTF-16 范围。
    /// </summary>
    public static RuneSubstringRange LongestSubstringWithoutRepeatedRunes(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var runes = new List<(Rune Value, int Utf16Start, int Utf16Length)>();
        var offset = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            runes.Add((rune, offset, rune.Utf16SequenceLength));
            offset += rune.Utf16SequenceLength;
        }

        var lastSeen = new Dictionary<Rune, int>();
        var left = 0;
        var bestLeft = 0;
        var bestLength = 0;
        for (var right = 0; right < runes.Count; right++)
        {
            if (lastSeen.TryGetValue(runes[right].Value, out var previous)) left = Math.Max(left, previous + 1);
            lastSeen[runes[right].Value] = right;
            if (right - left + 1 > bestLength)
            {
                bestLeft = left;
                bestLength = right - left + 1;
            }
        }

        if (bestLength == 0) return new RuneSubstringRange(0, 0, 0);
        var start = runes[bestLeft].Utf16Start;
        var last = runes[bestLeft + bestLength - 1];
        return new RuneSubstringRange(start, last.Utf16Start + last.Utf16Length - start, bestLength);
    }
}

/// <summary>
/// 后缀自动机把原字符串的全部子串压缩进至多 2n-1 个状态。
/// 状态的 Length 与 suffix link 之间的长度差，正好是该状态新贡献的不同子串数量。
/// </summary>
public sealed class SuffixAutomaton
{
    private sealed class State
    {
        public int MaximumLength { get; set; }
        public int SuffixLink { get; set; } = -1;
        public Dictionary<char, int> Transitions { get; } = [];

        public State Clone(int maximumLength) => new()
        {
            MaximumLength = maximumLength,
            SuffixLink = SuffixLink
        };
    }

    private readonly List<State> _states = [new State()];

    public SuffixAutomaton(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var last = 0;
        foreach (var character in text)
        {
            var current = _states.Count;
            _states.Add(new State { MaximumLength = _states[last].MaximumLength + 1 });
            var cursor = last;
            while (cursor >= 0 && !_states[cursor].Transitions.ContainsKey(character))
            {
                _states[cursor].Transitions[character] = current;
                cursor = _states[cursor].SuffixLink;
            }

            if (cursor < 0)
            {
                _states[current].SuffixLink = 0;
            }
            else
            {
                var target = _states[cursor].Transitions[character];
                if (_states[cursor].MaximumLength + 1 == _states[target].MaximumLength)
                {
                    _states[current].SuffixLink = target;
                }
                else
                {
                    var clone = _states[target].Clone(_states[cursor].MaximumLength + 1);
                    foreach (var transition in _states[target].Transitions) clone.Transitions.Add(transition.Key, transition.Value);
                    var cloneIndex = _states.Count;
                    _states.Add(clone);
                    while (cursor >= 0 && _states[cursor].Transitions.GetValueOrDefault(character, -1) == target)
                    {
                        _states[cursor].Transitions[character] = cloneIndex;
                        cursor = _states[cursor].SuffixLink;
                    }
                    _states[target].SuffixLink = cloneIndex;
                    _states[current].SuffixLink = cloneIndex;
                }
            }

            last = current;
        }
    }

    /// <summary>沿自动机转移读取 pattern；能完整读取就说明 pattern 是原串的子串，时间 O(m)。</summary>
    public bool Contains(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var state = 0;
        foreach (var character in pattern)
        {
            if (!_states[state].Transitions.TryGetValue(character, out state)) return false;
        }
        return true;
    }

    /// <summary>计算不同子串数量，结果可能达到 n(n+1)/2，因此返回 long。</summary>
    public long CountDistinctSubstrings()
    {
        long count = 0;
        for (var state = 1; state < _states.Count; state++)
        {
            count += _states[state].MaximumLength - _states[_states[state].SuffixLink].MaximumLength;
        }
        return count;
    }

    /// <summary>把另一个字符串流过自动机，在线维护当前可匹配后缀，时间 O(m)。</summary>
    public int LongestCommonSubstringLength(string other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var state = 0;
        var length = 0;
        var best = 0;
        foreach (var character in other)
        {
            while (state != 0 && !_states[state].Transitions.ContainsKey(character))
            {
                state = _states[state].SuffixLink;
                length = _states[state].MaximumLength;
            }
            if (_states[state].Transitions.TryGetValue(character, out var next))
            {
                state = next;
                length++;
                best = Math.Max(best, length);
            }
            else
            {
                length = 0;
            }
        }
        return best;
    }
}
