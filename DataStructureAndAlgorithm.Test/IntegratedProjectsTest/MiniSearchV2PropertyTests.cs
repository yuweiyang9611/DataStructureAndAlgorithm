using System;
using System.Collections.Generic;
using System.Linq;
using DataStructureAndAlgorithm.DynamicProgramming;
using DataStructureAndAlgorithm.Scenarios.MiniSearch;
using DataStructureAndAlgorithm.Tree;
using FsCheck.Xunit;

namespace DataStructureAndAlgorithm.Test.IntegratedProjectsTest;

/// <summary>
/// MiniSearch V2 的性质测试把优化实现与容易理解的穷举模型持续对照。
/// </summary>
/// <remarks>
/// 示例测试负责解释一条路径，性质测试则让 FsCheck 生成大量词典、文档长度、词频和位置组合。
/// 一旦优化破坏不变量，FsCheck 会把输入缩减成便于加入回归测试的最小反例。
/// </remarks>
public sealed class MiniSearchV2PropertyTests
{
    [Property(MaxTest = 75)]
    public bool BkTree_SearchMatchesFullDictionaryOracle(int[]? source, int queryValue, int radiusValue)
    {
        var words = Normalize(source)
            .Select(ToWord)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var tree = new BkTree(AdvancedDynamicProgrammingAlgorithms.EditDistance);
        foreach (var word in words)
        {
            tree.Add(word);
        }

        var query = ToWord(queryValue);
        var radius = (int)(unchecked((uint)radiusValue) % 4);
        var actual = tree.Search(query, radius);
        var expected = words
            // oracle 完全不使用树结构：逐词计算距离，因此能发现 BK-tree 剪枝区间漏掉合法子树的问题。
            .Select(word => new BkTreeMatch(word, LocalEditDistance(query, word)))
            .Where(match => match.Distance <= radius)
            .OrderBy(match => match.Distance)
            .ThenBy(match => match.Value, StringComparer.Ordinal);

        return actual.SequenceEqual(expected);
    }

    [Property(MaxTest = 75)]
    public bool SuggestCorrection_MatchesFullLexiconDistanceAndFrequencyOracle(
        int[]? source,
        int queryValue,
        int radiusValue)
    {
        var values = Normalize(source);
        var engine = new MiniSearchEngineV2();
        var documentFrequency = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < values.Count; index++)
        {
            var word = ToWord(values[index]);
            documentFrequency[word] = documentFrequency.GetValueOrDefault(word) + 1;
            engine.AddDocument(new SearchDocument(index + 1, string.Empty, string.Empty, Keywords: [word]));
        }

        var query = ToWord(queryValue);
        var radius = (int)(unchecked((uint)radiusValue) % 4);
        var actual = engine.SuggestCorrection(query, radius);
        var expected = documentFrequency.Keys
            .Select(word => new
            {
                Word = word,
                Distance = LocalEditDistance(query, word),
                Frequency = documentFrequency[word]
            })
            .Where(candidate => candidate.Distance <= radius)
            .OrderBy(candidate => candidate.Distance)
            .ThenByDescending(candidate => candidate.Frequency)
            .ThenBy(candidate => candidate.Word, StringComparer.Ordinal)
            .Select(candidate => candidate.Word)
            .FirstOrDefault();

        return StringComparer.Ordinal.Equals(actual, expected);
    }

    [Property(MaxTest = 75)]
    public bool PhraseAndProximityQueries_MatchBruteForceTokenWindows(int[]? source, int gapValue)
    {
        var values = Normalize(source);
        var tokenDocuments = new List<string[]>(values.Count);
        var engine = new MiniSearchEngineV2();
        for (var index = 0; index < values.Count; index++)
        {
            var tokens = ToTokens(values[index]);
            tokenDocuments.Add(tokens);
            engine.AddDocument(new SearchDocument(index + 1, string.Empty, string.Join(' ', tokens)));
        }

        var maximumGap = (int)(unchecked((uint)gapValue) % 3);
        var actualPhrase = engine.SearchPhrase("alpha beta", maxResults: values.Count + 1)
            .Hits.Select(hit => hit.DocumentId).Order();
        var expectedPhrase = tokenDocuments
            .Select((tokens, index) => (tokens, Id: index + 1))
            .Where(item => ContainsPhrase(item.tokens, "alpha", "beta"))
            .Select(item => item.Id)
            .Order();

        var actualNear = engine.SearchProximity("alpha", "beta", maximumGap, values.Count + 1)
            .Hits.Select(hit => hit.DocumentId).Order();
        var expectedNear = tokenDocuments
            .Select((tokens, index) => (tokens, Id: index + 1))
            .Where(item => ContainsNear(item.tokens, "alpha", "beta", maximumGap))
            .Select(item => item.Id)
            .Order();

        return actualPhrase.SequenceEqual(expectedPhrase) && actualNear.SequenceEqual(expectedNear);
    }

    [Property(MaxTest = 75)]
    public bool Bm25_RankingAndScoresMatchIndependentFormula(int[]? source)
    {
        var values = Normalize(source);
        var model = new List<(int Id, int TermFrequency, int Length)>(values.Count);
        var engine = new MiniSearchEngineV2();
        for (var index = 0; index < values.Count; index++)
        {
            var bits = unchecked((uint)values[index]);
            var termFrequency = (int)(bits % 4) + 1;
            var padding = (int)((bits >> 3) % 5);
            var terms = Enumerable.Repeat("alpha", termFrequency)
                .Concat(Enumerable.Repeat("neutral", padding))
                .ToArray();
            model.Add((index + 1, termFrequency, terms.Length));
            engine.AddDocument(new SearchDocument(index + 1, string.Empty, string.Join(' ', terms)));
        }

        const double k1 = 1.4;
        const double b = 0.6;
        var actual = engine.SearchBm25("alpha", maxResults: values.Count + 1, k1, b).Hits;
        var averageLength = model.Average(item => item.Length);
        var idf = Math.Log(1D + 0.5D / (model.Count + 0.5D));
        var expected = model
            .ToDictionary(
                item => item.Id,
                item => Score(idf, item.TermFrequency, item.Length, averageLength, k1, b));

        // 独立公式允许最后几位因乘除结合顺序不同而有误差；先逐文档验证分数，再单独验证
        // 公开结果对“实际返回的 double 分数”严格降序。这样既不会复制实现公式的求值顺序，
        // 也不会用不具传递性的 epsilon 比较器污染生产排序。
        if (actual.Count != expected.Count ||
            actual.Any(hit => Math.Abs(hit.Score - expected[hit.DocumentId]) > 1e-12))
        {
            return false;
        }

        return actual.Zip(actual.Skip(1)).All(pair =>
            pair.First.Score > pair.Second.Score ||
            pair.First.Score.Equals(pair.Second.Score) && pair.First.DocumentId < pair.Second.DocumentId);
    }

    private static List<int> Normalize(int[]? source)
    {
        var values = source?.Take(18).ToList() ?? [];
        if (values.Count == 0)
        {
            values.Add(0);
        }

        return values;
    }

    private static string ToWord(int value)
    {
        var bits = unchecked((uint)value);
        var length = (int)(bits % 5) + 1;
        Span<char> characters = stackalloc char[length];
        for (var index = 0; index < length; index++)
        {
            characters[index] = (char)('a' + bits % 4);
            bits = bits / 4 + 1;
        }

        return new string(characters);
    }

    private static string[] ToTokens(int value)
    {
        string[] vocabulary = ["alpha", "beta", "gamma"];
        var bits = unchecked((uint)value);
        var length = (int)(bits % 6) + 2;
        var tokens = new string[length];
        for (var index = 0; index < length; index++)
        {
            tokens[index] = vocabulary[bits % (uint)vocabulary.Length];
            bits = bits / (uint)vocabulary.Length + 1;
        }

        return tokens;
    }

    private static bool ContainsPhrase(IReadOnlyList<string> tokens, string first, string second)
    {
        for (var index = 0; index + 1 < tokens.Count; index++)
        {
            if (tokens[index] == first && tokens[index + 1] == second)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsNear(
        IReadOnlyList<string> tokens,
        string first,
        string second,
        int maximumGap)
    {
        for (var left = 0; left < tokens.Count; left++)
        {
            for (var right = left + 1; right < tokens.Count; right++)
            {
                if (right - left - 1 > maximumGap)
                {
                    break;
                }

                if (tokens[left] == first && tokens[right] == second ||
                    tokens[left] == second && tokens[right] == first)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int LocalEditDistance(string left, string right)
    {
        var previous = new int[right.Length + 1];
        for (var column = 0; column <= right.Length; column++)
        {
            previous[column] = column;
        }

        for (var row = 1; row <= left.Length; row++)
        {
            var current = new int[right.Length + 1];
            current[0] = row;
            for (var column = 1; column <= right.Length; column++)
            {
                var replacement = previous[column - 1] + (left[row - 1] == right[column - 1] ? 0 : 1);
                current[column] = Math.Min(
                    Math.Min(previous[column] + 1, current[column - 1] + 1),
                    replacement);
            }

            previous = current;
        }

        return previous[right.Length];
    }

    private static double Score(
        double idf,
        int termFrequency,
        int documentLength,
        double averageLength,
        double k1,
        double b)
    {
        var denominator = termFrequency + k1 * (1D - b + b * documentLength / averageLength);
        return idf * termFrequency * (k1 + 1D) / denominator;
    }
}
