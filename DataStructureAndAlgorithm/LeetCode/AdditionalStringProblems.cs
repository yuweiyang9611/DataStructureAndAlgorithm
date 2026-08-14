using System.Text;

namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>LeetCode 100 题专题中的进阶字符串题。</summary>
public static class AdditionalStringProblems
{
    /// <summary>5 - Longest Palindromic Substring。枚举奇偶中心向外扩展，O(n²)。</summary>
    public static string LongestPalindromicSubstring(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var bestStart = 0;
        var bestLength = 0;

        for (var center = 0; center < text.Length; center++)
        {
            Expand(center, center);       // 奇数长度中心。
            Expand(center, center + 1);   // 偶数长度中心位于两个字符之间。
        }

        return text.Substring(bestStart, bestLength);

        void Expand(int left, int right)
        {
            while (left >= 0 && right < text.Length && text[left] == text[right])
            {
                var length = right - left + 1;
                if (length > bestLength)
                {
                    bestStart = left;
                    bestLength = length;
                }

                left--;
                right++;
            }
        }
    }

    /// <summary>8 - String to Integer (atoi)。按状态顺序解析并在越界时钳制，O(n)。</summary>
    public static int StringToInteger(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var index = 0;
        while (index < text.Length && text[index] == ' ') index++;

        var sign = 1;
        if (index < text.Length && text[index] is '+' or '-')
        {
            sign = text[index++] == '-' ? -1 : 1;
        }

        long value = 0;
        while (index < text.Length && text[index] is >= '0' and <= '9')
        {
            value = value * 10 + text[index++] - '0';
            var signedValue = sign * value;
            if (signedValue >= int.MaxValue) return int.MaxValue;
            if (signedValue <= int.MinValue) return int.MinValue;
        }

        return (int)(sign * value);
    }

    /// <summary>14 - Longest Common Prefix。逐步缩短候选前缀，最坏 O(总字符数)。</summary>
    public static string LongestCommonPrefix(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0) return string.Empty;
        if (values.Any(value => value is null)) throw new ArgumentException("Values cannot contain null.", nameof(values));

        var prefixLength = values[0].Length;
        for (var valueIndex = 1; valueIndex < values.Count && prefixLength > 0; valueIndex++)
        {
            prefixLength = Math.Min(prefixLength, values[valueIndex].Length);
            var character = 0;
            while (character < prefixLength && values[0][character] == values[valueIndex][character]) character++;
            prefixLength = character;
        }

        return values[0][..prefixLength];
    }

    /// <summary>49 - Group Anagrams。排序后的字符序列作为规范键，O(n·k log k)。</summary>
    public static IReadOnlyList<IReadOnlyList<string>> GroupAnagrams(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var groups = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var value in values)
        {
            ArgumentNullException.ThrowIfNull(value);
            var characters = value.ToCharArray();
            Array.Sort(characters);
            var key = new string(characters);
            (groups.TryGetValue(key, out var group) ? group : groups[key] = []).Add(value);
        }

        return [.. groups.Values];
    }

    /// <summary>125 - Valid Palindrome。跳过非字母数字并从两端比较，O(n)。</summary>
    public static bool IsPalindromeIgnoringSymbols(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var left = 0;
        var right = text.Length - 1;

        while (left < right)
        {
            while (left < right && !char.IsLetterOrDigit(text[left])) left++;
            while (left < right && !char.IsLetterOrDigit(text[right])) right--;

            if (char.ToUpperInvariant(text[left]) != char.ToUpperInvariant(text[right])) return false;
            left++;
            right--;
        }

        return true;
    }

    /// <summary>202 - Happy Number。HashSet 检测数字状态是否进入循环。</summary>
    public static bool IsHappyNumber(int number)
    {
        if (number <= 0) return false;
        var seen = new HashSet<int>();

        while (number != 1 && seen.Add(number))
        {
            var next = 0;
            while (number > 0)
            {
                var digit = number % 10;
                next += digit * digit;
                number /= 10;
            }

            number = next;
        }

        return number == 1;
    }

    /// <summary>242 - Valid Anagram。字符频次增加再减少，O(n)。</summary>
    public static bool IsAnagram(string first, string second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        if (first.Length != second.Length) return false;

        var counts = new Dictionary<char, int>();
        foreach (var character in first) counts[character] = counts.GetValueOrDefault(character) + 1;
        foreach (var character in second)
        {
            if (!counts.TryGetValue(character, out var count) || count == 0) return false;
            counts[character] = count - 1;
        }

        return true;
    }

    /// <summary>387 - First Unique Character。先计数，再按原顺序找第一次频次为 1 的位置。</summary>
    public static int FirstUniqueCharacter(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var counts = text.GroupBy(character => character).ToDictionary(group => group.Key, group => group.Count());
        for (var index = 0; index < text.Length; index++)
        {
            if (counts[text[index]] == 1) return index;
        }

        return -1;
    }

    /// <summary>394 - Decode String。栈保存外层重复次数和已构建前缀，O(输出长度)。</summary>
    public static string DecodeString(string encoded)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        var countStack = new Stack<int>();
        var textStack = new Stack<StringBuilder>();
        var current = new StringBuilder();
        var repeat = 0;

        foreach (var character in encoded)
        {
            if (char.IsDigit(character))
            {
                repeat = checked(repeat * 10 + character - '0');
            }
            else if (character == '[')
            {
                if (repeat <= 0) throw new FormatException("A bracket must follow a positive repeat count.");
                countStack.Push(repeat);
                textStack.Push(current);
                current = new StringBuilder();
                repeat = 0;
            }
            else if (character == ']')
            {
                if (!countStack.TryPop(out var count) || !textStack.TryPop(out var outer))
                    throw new FormatException("Unbalanced brackets.");

                for (var index = 0; index < count; index++) outer.Append(current);
                current = outer;
            }
            else
            {
                if (repeat != 0) throw new FormatException("A repeat count must be followed by '['.");
                current.Append(character);
            }
        }

        if (countStack.Count != 0 || repeat != 0) throw new FormatException("Incomplete encoded string.");
        return current.ToString();
    }

    /// <summary>409 - Longest Palindrome。偶数频次全用，最多一个奇数放中心。</summary>
    public static int LongestPalindromeLength(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var counts = text.GroupBy(character => character).Select(group => group.Count());
        var length = 0;
        var hasOdd = false;

        foreach (var count in counts)
        {
            length += count / 2 * 2;
            hasOdd |= (count & 1) == 1;
        }

        return length + (hasOdd ? 1 : 0);
    }

    /// <summary>438 - Find All Anagrams。固定长度窗口维护仍缺少的字符数，O(n)。</summary>
    public static IReadOnlyList<int> FindAnagramStartIndexes(string text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(pattern);
        if (pattern.Length == 0 || pattern.Length > text.Length) return [];

        var counts = new Dictionary<char, int>();
        foreach (var character in pattern) counts[character] = counts.GetValueOrDefault(character) + 1;
        var missing = pattern.Length;
        var result = new List<int>();

        for (var right = 0; right < text.Length; right++)
        {
            var added = text[right];
            var beforeAdd = counts.GetValueOrDefault(added);
            if (beforeAdd > 0) missing--;
            counts[added] = beforeAdd - 1;

            if (right >= pattern.Length)
            {
                var removed = text[right - pattern.Length];
                var afterRemove = counts.GetValueOrDefault(removed) + 1;
                counts[removed] = afterRemove;
                if (afterRemove > 0) missing++;
            }

            if (missing == 0) result.Add(right - pattern.Length + 1);
        }

        return result;
    }

    /// <summary>567 - Permutation in String。存在任一固定长度异位词窗口即可。</summary>
    public static bool ContainsPermutation(string pattern, string text)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(text);
        if (pattern.Length == 0) return true;
        return FindAnagramStartIndexes(text, pattern).Count > 0;
    }

    /// <summary>647 - Palindromic Substrings。每个回文由一个奇数或偶数中心唯一确定，O(n²)。</summary>
    public static int CountPalindromicSubstrings(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var count = 0;

        for (var center = 0; center < text.Length; center++)
        {
            CountFrom(center, center);
            CountFrom(center, center + 1);
        }

        return count;

        void CountFrom(int left, int right)
        {
            while (left >= 0 && right < text.Length && text[left--] == text[right++]) count++;
        }
    }

    /// <summary>763 - Partition Labels。片段右边界是片段内字符最后位置的最大值，O(n)。</summary>
    public static IReadOnlyList<int> PartitionLabels(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var last = new Dictionary<char, int>();
        for (var index = 0; index < text.Length; index++) last[text[index]] = index;

        var result = new List<int>();
        var start = 0;
        var end = 0;
        for (var index = 0; index < text.Length; index++)
        {
            end = Math.Max(end, last[text[index]]);
            if (index == end)
            {
                result.Add(end - start + 1);
                start = index + 1;
            }
        }

        return result;
    }

    /// <summary>1768 - Merge Strings Alternately。每轮按相同下标依次取两个字符串字符。</summary>
    public static string MergeAlternately(string first, string second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        var result = new StringBuilder(first.Length + second.Length);

        for (var index = 0; index < Math.Max(first.Length, second.Length); index++)
        {
            if (index < first.Length) result.Append(first[index]);
            if (index < second.Length) result.Append(second[index]);
        }

        return result.ToString();
    }
}
