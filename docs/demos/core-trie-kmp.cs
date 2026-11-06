// Trie 与 KMP：前缀树按字符分叉存词，KMP 用 next 数组让文本指针永不回退。
// 换一个模式串，看 next 数组和匹配起点怎么变。
using DataStructureAndAlgorithm.PatternMatching;
using DataStructureAndAlgorithm.Tree;

public static class Demo
{
    public static void Run()
    {
        var trie = new Trie();
        foreach (var word in new[] { "tree", "trie", "trip", "trap", "heap", "tree" }) trie.Add(word);
        Console.WriteLine($"Trie 收录 {trie.Count} 个词（重复的 tree 只算一次）");
        Console.WriteLine($"Contains(tri) = {trie.Contains("tri")}，StartsWith(tri) = {trie.StartsWith("tri")}");
        Console.WriteLine($"前缀 tr 的词：{string.Join(", ", trie.GetWordsWithPrefix("tr"))}");
        trie.Remove("trip");
        Console.WriteLine($"删除 trip 后前缀 tri 的词：{string.Join(", ", trie.GetWordsWithPrefix("tri"))}");

        var matcher = new StringPatternMatching("ABABDABACDABABCABAB", "ABABCABAB");
        Console.WriteLine($"模式串 ABABCABAB 的 next 数组：{string.Join(" ", matcher.CalculateNext())}");
        var kmpFirst = matcher.KmpAlgorithm(out var kmpPositions);
        Console.WriteLine($"KMP 在文本里找到 {kmpPositions.Count} 处，起点 {string.Join(", ", kmpPositions)}（返回值 {kmpFirst} 是第一处）");
        var naiveFirst = matcher.SimpleStringPatternMatching(out var naivePositions);
        Console.WriteLine($"朴素匹配同样找到 {naivePositions.Count} 处，第一处在 {naiveFirst}，但文本指针会回退");
    }
}
