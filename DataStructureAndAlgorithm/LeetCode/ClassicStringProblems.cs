namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>LeetCode 中经典的字符串、栈和滑动窗口题目。</summary>
public static class ClassicStringProblems
{
    /// <summary>
    /// LeetCode 3 - Longest Substring Without Repeating Characters。
    /// </summary>
    /// <remarks>滑动窗口记录字符最近位置，时间 O(n)，空间 O(字符集大小)。</remarks>
    public static int LongestSubstringWithoutRepeatingCharacters(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lastIndexes = new Dictionary<char, int>();
        var windowStart = 0;
        var maximumLength = 0;

        for (var index = 0; index < text.Length; index++)
        {
            if (lastIndexes.TryGetValue(text[index], out var previousIndex) && previousIndex >= windowStart)
            {
                windowStart = previousIndex + 1;
            }

            lastIndexes[text[index]] = index;
            maximumLength = Math.Max(maximumLength, index - windowStart + 1);
        }

        return maximumLength;
    }

    /// <summary>
    /// LeetCode 20 - Valid Parentheses：判断括号是否正确嵌套并全部闭合。
    /// </summary>
    /// <remarks>栈中保存尚未匹配的左括号，时间 O(n)，空间 O(n)。</remarks>
    public static bool HasValidParentheses(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var stack = new char[text.Length];
        var count = 0;

        foreach (var character in text)
        {
            if (character is '(' or '[' or '{')
            {
                stack[count++] = character;
                continue;
            }

            if (character is not ')' and not ']' and not '}')
            {
                return false;
            }

            if (count == 0 || !IsMatchingPair(stack[--count], character))
            {
                return false;
            }
        }

        return count == 0;
    }

    /// <summary>
    /// LeetCode 76 - Minimum Window Substring：包含目标全部字符的最短窗口。
    /// </summary>
    /// <remarks>可变长度滑动窗口，时间 O(n + m)，空间 O(字符集大小)。</remarks>
    public static string MinimumWindowSubstring(string text, string target)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(target);
        if (target.Length == 0 || text.Length < target.Length)
        {
            return string.Empty;
        }

        var requiredCounts = target.GroupBy(character => character)
            .ToDictionary(group => group.Key, group => group.Count());
        var windowCounts = new Dictionary<char, int>();
        var satisfiedKinds = 0;
        var windowStart = 0;
        var bestStart = 0;
        var bestLength = int.MaxValue;

        for (var windowEnd = 0; windowEnd < text.Length; windowEnd++)
        {
            var added = text[windowEnd];
            windowCounts[added] = windowCounts.GetValueOrDefault(added) + 1;

            if (requiredCounts.TryGetValue(added, out var required) && windowCounts[added] == required)
            {
                satisfiedKinds++;
            }

            while (satisfiedKinds == requiredCounts.Count)
            {
                var length = windowEnd - windowStart + 1;
                if (length < bestLength)
                {
                    bestLength = length;
                    bestStart = windowStart;
                }

                var removed = text[windowStart++];
                if (requiredCounts.TryGetValue(removed, out required) && windowCounts[removed] == required)
                {
                    satisfiedKinds--;
                }

                windowCounts[removed]--;
            }
        }

        return bestLength == int.MaxValue ? string.Empty : text.Substring(bestStart, bestLength);
    }

    private static bool IsMatchingPair(char open, char close)
    {
        return (open, close) is ('(', ')') or ('[', ']') or ('{', '}');
    }
}
