// 高级字符串与文本索引：Aho-Corasick 一次扫描找多个模式，后缀数组和后缀自动机把文本编成索引。
// 换一段文本或一组模式再跑，看匹配位置和子串数量。
using DataStructureAndAlgorithm.PatternMatching;

public static class Demo
{
    public static void Run()
    {
        var matcher = new AhoCorasickMatcher(new[] { "he", "she", "his", "hers" });
        var matches = matcher.FindAll("ushers");
        Console.WriteLine($"Aho-Corasick 在 ushers 里一次扫描找到 {matches.Count} 处：{string.Join(", ", matches.Select(match => $"{match.Pattern}@{match.StartIndex}"))}");

        var suffixes = SuffixArrayAlgorithms.Build("banana");
        Console.WriteLine($"banana 的后缀数组：{string.Join(" ", suffixes.Suffixes)}，相邻后缀的 LCP：{string.Join(" ", suffixes.LongestCommonPrefixes)}");
        Console.WriteLine($"用后缀数组找 ana 的起点：{string.Join(", ", suffixes.FindAll("banana", "ana"))}");

        const string text = "forgeeksskeegfor";
        var palindrome = P3StringAlgorithms.LongestPalindromeManacher(text);
        Console.WriteLine($"Manacher 最长回文：从 {palindrome.Start} 起 {palindrome.Length} 个字符，即 {text.Substring(palindrome.Start, palindrome.Length)}");

        const string chinese = "数据结构与算法结构";
        var runes = P3StringAlgorithms.LongestSubstringWithoutRepeatedRunes(chinese);
        Console.WriteLine($"最长无重复字符子串：{chinese.Substring(runes.Utf16Start, runes.Utf16Length)}（{runes.RuneLength} 个字符）");

        var automaton = new SuffixAutomaton("abcbc");
        Console.WriteLine($"后缀自动机 abcbc：不同子串 {automaton.CountDistinctSubstrings()} 个，Contains(bcb) = {automaton.Contains("bcb")}，与 cbcab 的最长公共子串长度 {automaton.LongestCommonSubstringLength("cbcab")}");
    }
}
