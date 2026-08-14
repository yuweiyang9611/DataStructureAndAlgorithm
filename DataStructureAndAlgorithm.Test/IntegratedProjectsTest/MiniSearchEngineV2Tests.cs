using System;
using System.Collections.Generic;
using System.Linq;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Scenarios.MiniSearch;
using Xunit;

namespace DataStructureAndAlgorithm.Test.IntegratedProjectsTest;

/// <summary>
/// MiniSearch V2 的示例测试解释位置索引、BM25、兼容层和结构化 Trace 为什么这样设计。
/// </summary>
public sealed class MiniSearchEngineV2Tests
{
    [Fact]
    public void CompatibilityApi_PreservesV1SearchCompletionCorrectionAndCacheContracts()
    {
        SearchDocument[] documents =
        [
            new(1, "Search Trie", "search trie search", Popularity: 3, Keywords: ["algorithm"]),
            new(2, "Search", "trie search", Popularity: 1)
        ];
        var v1 = new MiniSearchEngine(queryCacheCapacity: 4);
        var v2 = new MiniSearchEngineV2(queryCacheCapacity: 4);
        foreach (var document in documents)
        {
            v1.AddDocument(document);
            v2.AddDocument(document);
        }

        var expected = v1.Search("search trie", maxResults: 5);
        var actual = v2.Search("search trie", maxResults: 5);
        var cached = v2.Search("TRIE, SEARCH", maxResults: 5);

        Assert.Equal(
            expected.Hits.Select(hit => (hit.DocumentId, hit.Score)),
            actual.Hits.Select(hit => (hit.DocumentId, hit.Score)));
        Assert.True(cached.FromCache);
        Assert.Equal(1, v2.SearchComputationCount);
        Assert.Equal(v1.CompletePrefix("alg"), v2.CompletePrefix("alg"));
        Assert.Equal(v1.SuggestCorrection("algoritm", 2), v2.SuggestCorrection("algoritm", 2));
    }

    [Fact]
    public void PhraseAndProximityQueries_UsePositionsWithoutCrossingFieldBoundaries()
    {
        var engine = new MiniSearchEngineV2();
        engine.AddDocument(new SearchDocument(1, "quick brown fox", "neutral"));
        engine.AddDocument(new SearchDocument(2, "quick agile brown fox", "neutral"));
        engine.AddDocument(new SearchDocument(3, "quick", "brown"));
        engine.AddDocument(new SearchDocument(4, "neutral", string.Empty, Keywords: ["quick", "brown"]));
        engine.AddDocument(new SearchDocument(5, "neutral", string.Empty, Keywords: ["quick brown"]));

        var phrase = engine.SearchPhrase("quick brown", maxResults: 10);
        var near = engine.SearchProximity("brown", "quick", maximumGap: 1, maxResults: 10);

        // 1 在标题中连续出现，5 在同一个关键词字段中连续出现；3 横跨标题/正文、4 横跨两个关键词，均不能命中。
        Assert.Equal([1, 5], phrase.Hits.Select(hit => hit.DocumentId).Order());
        Assert.All(phrase.Hits, hit => Assert.All(hit.PositionalMatches, match =>
            Assert.Equal(1, match.EndTokenOffsetInclusive - match.StartTokenOffset)));

        // 邻近查询无方向：输入 brown/quick 仍能命中 quick ... brown。
        // 文档 2 中间恰好隔一个词，maximumGap=1 时应命中；字段边界仍然不可跨越。
        Assert.Equal([1, 2, 5], near.Hits.Select(hit => hit.DocumentId).Order());
        Assert.DoesNotContain(near.Hits, hit => hit.DocumentId is 3 or 4);
    }

    [Fact]
    public void SearchPhrase_PreservesRepeatedTermsAndReportsEveryStartPosition()
    {
        var engine = new MiniSearchEngineV2();
        engine.AddDocument(new SearchDocument(1, "echo echo echo", string.Empty));
        engine.AddDocument(new SearchDocument(2, "echo pause echo", string.Empty));

        var response = engine.SearchPhrase("echo echo", maxResults: 10);

        var hit = Assert.Single(response.Hits);
        Assert.Equal(1, hit.DocumentId);
        Assert.Equal(
            [new PositionalMatch(0, 0, 1), new PositionalMatch(0, 1, 2)],
            hit.PositionalMatches);
    }

    [Fact]
    public void SearchBm25_MatchesAnIndependentFormulaAndUsesOrSemantics()
    {
        var engine = new MiniSearchEngineV2();
        engine.AddDocument(new SearchDocument(1, string.Empty, "alpha alpha"));
        engine.AddDocument(new SearchDocument(2, string.Empty, "alpha beta beta beta"));
        engine.AddDocument(new SearchDocument(3, string.Empty, "beta"));

        var response = engine.SearchBm25("alpha missing", maxResults: 10, k1: 1.2, b: 0.75);

        Assert.Equal([1, 2], response.Hits.Select(hit => hit.DocumentId));
        var documentCount = 3D;
        var documentFrequency = 2D;
        var averageLength = 7D / 3D;
        var idf = Math.Log(1D + (documentCount - documentFrequency + 0.5D) / (documentFrequency + 0.5D));
        var expectedFirst = Bm25TermScore(idf, termFrequency: 2, documentLength: 2, averageLength, 1.2, 0.75);
        var expectedSecond = Bm25TermScore(idf, termFrequency: 1, documentLength: 4, averageLength, 1.2, 0.75);

        Assert.Equal(expectedFirst, response.Hits[0].Score, precision: 12);
        Assert.Equal(expectedSecond, response.Hits[1].Score, precision: 12);
        Assert.Empty(response.Hits[0].PositionalMatches);
        Assert.Equal(["alpha"], response.Hits[0].MatchedTerms);
    }

    [Fact]
    public void SearchBm25_ExtremeFiniteK1KeepsRepeatedTermScoreFinite()
    {
        var engine = new MiniSearchEngineV2();
        engine.AddDocument(new SearchDocument(1, string.Empty, "alpha alpha"));

        var response = engine.SearchBm25("alpha", k1: double.MaxValue, b: 0);

        var hit = Assert.Single(response.Hits);
        var inverseDocumentFrequency = Math.Log(4D / 3D);
        // b=0 时长度归一化恒为 1；k1 趋近极大值时，tf=2 的饱和项趋近 2。
        // 旧写法会先计算 2 * double.MaxValue 得到 Infinity，稳定等价式应返回有限理论值。
        Assert.True(double.IsFinite(hit.Score));
        Assert.Equal(2D * inverseDocumentFrequency, hit.Score, precision: 12);
    }

    [Fact]
    public void WholeTermHighlights_UseTheSameRuneBoundariesAsTokenization()
    {
        const string astralLetter = "\U00010400"; // DESERET CAPITAL LETTER LONG I，位于基本多文种平面之外。
        var document = new SearchDocument(
            1,
            string.Empty,
            $"alpha {astralLetter}alpha alpha{astralLetter}");
        var v1 = new MiniSearchEngine();
        var v2 = new MiniSearchEngineV2();
        v1.AddDocument(document);
        v2.AddDocument(document);

        var compatibilityHit = Assert.Single(v1.Search("alpha").Hits);
        var advancedHit = Assert.Single(v2.SearchBm25("alpha").Hits);

        // Tokenize 按 Rune 判断，因此“𐐀alpha”和“alpha𐐀”各是一个完整词元，不是独立的 alpha。
        // 高亮索引虽然使用 UTF-16 char 偏移，也必须解码相邻的完整 Rune；否则高/低代理项都会被
        // char.IsLetterOrDigit 错判为分隔符，产生两个并不存在的整词高亮。
        var compatibilityHighlight = Assert.Single(compatibilityHit.Highlights);
        var advancedHighlight = Assert.Single(advancedHit.Highlights);
        Assert.Equal("alpha", compatibilityHit.HighlightText[
            compatibilityHighlight.StartIndex..compatibilityHighlight.EndExclusive]);
        Assert.Equal("alpha", advancedHit.HighlightText[
            advancedHighlight.StartIndex..advancedHighlight.EndExclusive]);
    }

    [Fact]
    public void SearchBm25_IsDeterministicAcrossDocumentInsertionOrders()
    {
        SearchDocument[] documents =
        [
            new(30, string.Empty, "alpha beta"),
            new(10, string.Empty, "alpha beta"),
            new(20, string.Empty, "alpha beta")
        ];
        var forward = new MiniSearchEngineV2();
        var reverse = new MiniSearchEngineV2();
        foreach (var document in documents)
        {
            forward.AddDocument(document);
        }

        foreach (var document in documents.Reverse())
        {
            reverse.AddDocument(document);
        }

        var left = forward.SearchBm25("beta alpha", maxResults: 2);
        var right = reverse.SearchBm25("ALPHA, BETA", maxResults: 2);

        Assert.Equal([10, 20], left.Hits.Select(hit => hit.DocumentId));
        Assert.Equal(
            left.Hits.Select(hit => (hit.DocumentId, hit.Score)),
            right.Hits.Select(hit => (hit.DocumentId, hit.Score)));
    }

    [Fact]
    public void SuggestCorrection_UsesDistanceThenDocumentFrequencyThenOrdinalTieBreak()
    {
        var engine = new MiniSearchEngineV2();
        engine.AddDocument(new SearchDocument(1, string.Empty, string.Empty, Keywords: ["cat", "cut"]));
        engine.AddDocument(new SearchDocument(2, string.Empty, string.Empty, Keywords: ["cut"]));
        engine.AddDocument(new SearchDocument(3, string.Empty, string.Empty, Keywords: ["cot"]));

        // cit 到三个词的距离都是 1；cut 的文档频率为 2，必须胜过只出现于一篇文档的 cat/cot。
        Assert.Equal("cut", engine.SuggestCorrection("cit", maximumDistance: 1));
        Assert.Equal("cat", engine.SuggestCorrection("cat", maximumDistance: int.MaxValue));
        Assert.Equal("cat", engine.SuggestCorrection("cat", maximumDistance: 0));
        Assert.Null(engine.SuggestCorrection("unrelated", maximumDistance: 1));
    }

    [Fact]
    public void StructuredTrace_ExplainsIndexBm25PositionAndBkTreeStages()
    {
        var trace = new CollectingAlgorithmTraceSink();
        var engine = new MiniSearchEngineV2(queryCacheCapacity: 4, trace);
        engine.AddDocument(new SearchDocument(1, "alpha beta", "alpha gamma", Keywords: ["algorithm"]));

        engine.SearchBm25("alpha beta");
        engine.SearchPhrase("alpha beta");
        engine.SearchProximity("alpha", "gamma", maximumGap: 0);
        engine.SuggestCorrection("algoritm", maximumDistance: 2);

        var v2Events = trace.Events.Where(traceEvent => traceEvent.Algorithm == "MiniSearchV2").ToArray();
        Assert.Contains(v2Events, traceEvent => traceEvent.Operation == "PositionalDocumentIndexed");
        Assert.Contains(v2Events, traceEvent => traceEvent.Operation == "AdvancedQueryNormalized");
        Assert.Contains(v2Events, traceEvent => traceEvent.Operation == "Bm25RankingCompleted");
        Assert.Contains(v2Events, traceEvent => traceEvent.Operation == "PhraseRankingCompleted");
        Assert.Contains(v2Events, traceEvent => traceEvent.Operation == "ProximityRankingCompleted");
        Assert.Contains(v2Events, traceEvent => traceEvent.Operation == "BkTreeCorrectionCompleted");
        Assert.All(v2Events, traceEvent => Assert.NotEmpty(traceEvent.State));
    }

    [Fact]
    public void AdvancedQueries_RejectParametersThatWouldMakeRankingUndefined()
    {
        var engine = new MiniSearchEngineV2();

        Assert.Throws<ArgumentException>(() => engine.SearchBm25("---"));
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.SearchBm25("alpha", maxResults: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.SearchBm25("alpha", k1: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.SearchBm25("alpha", b: 1.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.SearchProximity("a", "b", maximumGap: -1));
        Assert.Throws<ArgumentException>(() => engine.SearchProximity("two words", "b"));
    }

    private static double Bm25TermScore(
        double inverseDocumentFrequency,
        int termFrequency,
        int documentLength,
        double averageLength,
        double k1,
        double b)
    {
        var denominator = termFrequency + k1 * (1D - b + b * documentLength / averageLength);
        return inverseDocumentFrequency * termFrequency * (k1 + 1D) / denominator;
    }
}
