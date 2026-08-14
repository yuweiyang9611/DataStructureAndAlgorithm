using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Scenarios.MiniSearch;
using FsCheck.Xunit;
using Xunit;

namespace DataStructureAndAlgorithm.Test.IntegratedProjectsTest;

/// <summary>
/// 用性质测试验证 MiniSearch 的跨结构不变量。
/// </summary>
/// <remarks>
/// 手写示例擅长解释一条执行路径，FsCheck 则会生成大量不同的词频、热度和 Top-K 大小，
/// 并在失败时自动缩减为较小反例。两者组合后，既保留“为什么”的可读性，又能覆盖人工容易漏掉的边界。
/// </remarks>
public sealed class MiniSearchPropertyTests
{
    [Property(MaxTest = 75)]
    public bool Search_AndSemanticsAndRankingMatchAnIndependentModel(int[]? source)
    {
        var documents = BuildModelDocuments(source);
        var engine = BuildEngine(documents);

        var actual = engine.Search("alpha beta", maxResults: 25).Hits
            .Select(hit => (hit.DocumentId, hit.Score));
        var expected = documents
            // 独立模型不调用引擎的分词、倒排表或堆：只有两个词的权重都大于零才满足 AND。
            .Where(document => document.AlphaWeight > 0 && document.BetaWeight > 0)
            .Select(document => (
                document.Id,
                Score: (long)document.AlphaWeight + document.BetaWeight + document.Popularity))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Id);

        return actual.SequenceEqual(expected);
    }

    [Property(MaxTest = 75)]
    public bool Search_TopKIsAlwaysAPrefixOfTheFullRanking(int[]? source, int requestedSize)
    {
        var documents = BuildModelDocuments(source);
        var engine = BuildEngine(documents);
        // 把任意 int 映射到小而合法的 K，控制性质测试成本，同时仍覆盖 K=1 和 K 大于命中数。
        var maxResults = (int)(unchecked((uint)requestedSize) % 8) + 1;

        var actual = engine.Search("alpha", maxResults).Hits
            .Select(hit => (hit.DocumentId, hit.Score));
        var expected = documents
            .Where(document => document.AlphaWeight > 0)
            .Select(document => (
                document.Id,
                Score: (long)document.AlphaWeight + document.Popularity))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Id)
            .Take(maxResults);

        // Top-K 堆可以改变“怎样选”，却不能改变最终排名契约；结果必须等于完整排序的前 K 项。
        return actual.SequenceEqual(expected);
    }

    [Property(MaxTest = 75)]
    public bool Search_NormalizedWordOrderAndDuplicatesShareOneCacheEntry(int[]? source)
    {
        var engine = BuildEngine(BuildModelDocuments(source));

        var first = engine.Search("alpha beta alpha", maxResults: 20);
        var second = engine.Search("BETA, ALPHA", maxResults: 20);

        // 大小写、标点、词序和重复词都不应改变规范 AND 查询；第二次调用应只改 Query/FromCache 元数据。
        return !first.FromCache &&
               second.FromCache &&
               engine.SearchComputationCount == 1 &&
               first.IndexVersion == second.IndexVersion &&
               HaveEquivalentHits(first, second);
    }

    [Property(MaxTest = 75)]
    public bool CompletePrefix_ReturnsDistinctOrdinalWordsAndHonorsTheLimit(int[]? source, int requestedSize)
    {
        var values = NormalizeSource(source);
        var engine = new MiniSearchEngine();
        var expectedWords = new List<string>(values.Count);

        for (var index = 0; index < values.Count; index++)
        {
            var word = "alg" + (unchecked((uint)values[index]) % 30).ToString("D2", CultureInfo.InvariantCulture);
            expectedWords.Add(word);
            engine.AddDocument(new SearchDocument(index + 1, "neutral", string.Empty, Keywords: [word]));
        }

        var maxResults = (int)(unchecked((uint)requestedSize) % 8) + 1;
        var expected = expectedWords
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Take(maxResults);

        // Trie 的遍历顺序是 API 契约的一部分，不能依赖倒排哈希表当前的槽位排列。
        return engine.CompletePrefix("ALG", maxResults).SequenceEqual(expected);
    }

    [Fact]
    public void OptionalTracing_ExplainsThePipelineWithoutChangingSearchResults()
    {
        SearchDocument[] documents =
        [
            new(1, "Alpha Beta", "alpha beta beta", Popularity: 3, Keywords: ["algorithm"]),
            new(2, "Alpha", "alpha beta", Popularity: 1),
            new(3, "Beta", "beta", Popularity: 8)
        ];

        var plain = new MiniSearchEngine(queryCacheCapacity: 4);
        var trace = new CollectingAlgorithmTraceSink();
        var traced = new MiniSearchEngine(queryCacheCapacity: 4, trace);
        foreach (var document in documents)
        {
            plain.AddDocument(document);
            traced.AddDocument(document);
        }

        var plainResponse = plain.Search("alpha beta", maxResults: 2);
        var tracedResponse = traced.Search("alpha beta", maxResults: 2);
        var cachedResponse = traced.Search("BETA ALPHA", maxResults: 2);
        var plainPrefix = plain.CompletePrefix("alg", maxResults: 3);
        var tracedPrefix = traced.CompletePrefix("alg", maxResults: 3);
        var plainCorrection = plain.SuggestCorrection("algoritm", maximumDistance: 2);
        var tracedCorrection = traced.SuggestCorrection("algoritm", maximumDistance: 2);

        Assert.True(HaveEquivalentHits(plainResponse, tracedResponse));
        Assert.True(HaveEquivalentHits(tracedResponse, cachedResponse));
        Assert.True(plainPrefix.SequenceEqual(tracedPrefix));
        Assert.Equal(plainCorrection, tracedCorrection);

        var operations = trace.Events.Select(traceEvent => traceEvent.Operation).ToArray();
        Assert.Contains("DocumentIndexed", operations);
        Assert.Contains("QueryNormalized", operations);
        Assert.Contains("CacheMiss", operations);
        Assert.Contains("CacheHit", operations);
        Assert.Contains("SeedPostingSelected", operations);
        Assert.Contains("TopKSelected", operations);
        Assert.Contains("SearchCompleted", operations);
        Assert.Contains("PrefixCompleted", operations);
        Assert.Contains("CorrectionCompleted", operations);

        // 连续步号使 JSON 与 Mermaid 能复原同一条时间线，也能发现收集器复用时的漏记或重复编号。
        Assert.True(trace.Events.Select(traceEvent => traceEvent.Step)
            .SequenceEqual(Enumerable.Range(1, trace.Events.Count)));
        Assert.All(trace.Events, traceEvent => Assert.Equal("MiniSearch", traceEvent.Algorithm));
    }

    private static MiniSearchEngine BuildEngine(IReadOnlyList<ModelDocument> documents)
    {
        var engine = new MiniSearchEngine(queryCacheCapacity: 8);
        foreach (var document in documents)
        {
            engine.AddDocument(new SearchDocument(
                document.Id,
                document.Title,
                document.Content,
                document.Popularity,
                document.Keywords));
        }

        return engine;
    }

    private static List<ModelDocument> BuildModelDocuments(int[]? source)
    {
        var values = NormalizeSource(source);
        var documents = new List<ModelDocument>(values.Count);

        for (var index = 0; index < values.Count; index++)
        {
            // 通过位段派生标题命中、正文词频、关键词和热度；每一项都可由测试模型直接计算，
            // 因此预期值不会偷偷复用被测引擎的实现逻辑。
            var bits = unchecked((uint)values[index]);
            var alphaInTitle = (bits & 1) != 0;
            var alphaInContent = (int)((bits >> 1) % 3);
            var betaInContent = (int)((bits >> 3) % 3);
            var betaInKeywords = (bits & 0x20) != 0;
            var popularity = (int)((bits >> 6) % 11);

            var title = alphaInTitle ? "alpha lesson" : "neutral lesson";
            var contentTerms = Enumerable.Repeat("alpha", alphaInContent)
                .Concat(Enumerable.Repeat("beta", betaInContent))
                .ToArray();
            var content = contentTerms.Length == 0 ? "neutral" : string.Join(' ', contentTerms);
            IReadOnlyList<string>? keywords = betaInKeywords ? ["beta"] : null;

            documents.Add(new ModelDocument(
                index + 1,
                title,
                content,
                popularity,
                keywords,
                AlphaWeight: (alphaInTitle ? 3 : 0) + alphaInContent,
                BetaWeight: betaInContent + (betaInKeywords ? 5 : 0)));
        }

        return documents;
    }

    private static List<int> NormalizeSource(int[]? source)
    {
        // 空数组仍生成一个中性文档，让每次性质运行都真正经过索引和查询路径；最多二十篇可避免反例过大。
        var values = source?.Take(20).ToList() ?? [];
        if (values.Count == 0)
        {
            values.Add(0);
        }

        return values;
    }

    private static bool HaveEquivalentHits(SearchResponse left, SearchResponse right) =>
        left.Hits.Select(hit => (hit.DocumentId, hit.Score, hit.Popularity, hit.Title))
            .SequenceEqual(right.Hits.Select(hit => (hit.DocumentId, hit.Score, hit.Popularity, hit.Title)));

    private sealed record ModelDocument(
        int Id,
        string Title,
        string Content,
        int Popularity,
        IReadOnlyList<string>? Keywords,
        int AlphaWeight,
        int BetaWeight);
}
