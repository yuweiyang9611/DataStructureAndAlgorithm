using DataStructureAndAlgorithm.Scenarios.MiniSearch;
using Xunit;

namespace DataStructureAndAlgorithm.Test.IntegratedProjectsTest;

/// <summary>
/// MiniSearch 的测试关注多个自实现结构协作后的外部契约，而不是再次复制内部算法。
/// </summary>
public sealed class MiniSearchEngineTests
{
    [Fact]
    public void Search_UsesAndSemanticsAndRanksAndHighlightsAllTerms()
    {
        var engine = new MiniSearchEngine();
        engine.AddDocument(new SearchDocument(101, "Trie Search", "trie search search", Popularity: 1));
        engine.AddDocument(new SearchDocument(102, "Search", "trie search"));
        engine.AddDocument(new SearchDocument(103, "Trie only", "trie"));

        var response = engine.Search("trie search", maxResults: 10);

        // AND 语义要求两个词都存在，所以只含 trie 的 103 不能混入候选集。
        Assert.True(response.Hits.Select(hit => hit.DocumentId).SequenceEqual([101, 102]));
        var first = response.Hits[0];

        // 标题权重为 3、正文权重为 1：trie=4、search=5，再加 Popularity=1，得分应为 10。
        Assert.Equal(10, first.Score);
        // 查询词会先规范排序，保证结果与用户输入词序无关，也让返回值具有确定性。
        Assert.True(first.MatchedTerms.SequenceEqual(["search", "trie"]));
        // Aho-Corasick 必须在一次扫描中同时报告两个模式，而不是只高亮驱动倒排表的那个词。
        Assert.Contains(first.Highlights, match => match.Pattern == "search");
        Assert.Contains(first.Highlights, match => match.Pattern == "trie");
        Assert.All(first.Highlights, match =>
            Assert.Equal(match.Pattern, first.HighlightText[match.StartIndex..match.EndExclusive]));
    }

    [Fact]
    public void Search_TopKUsesDocumentIdToBreakEqualScoreTiesDeterministically()
    {
        var engine = new MiniSearchEngine();
        engine.AddDocument(new SearchDocument(30, "Document thirty", "search"));
        engine.AddDocument(new SearchDocument(10, "Document ten", "search"));
        engine.AddDocument(new SearchDocument(20, "Document twenty", "search"));

        var response = engine.Search("search", maxResults: 2);

        // 三篇文档分数完全相同时，Top-K 堆应淘汰较大的 id，最终归并排序再按 id 升序输出。
        Assert.True(response.Hits.Select(hit => hit.DocumentId).SequenceEqual([10, 20]));
        Assert.All(response.Hits, hit => Assert.Equal(1, hit.Score));
    }

    [Fact]
    public void CompletePrefixAndSuggestCorrectionUseLexiconAndEditDistance()
    {
        var engine = new MiniSearchEngine();
        engine.AddDocument(new SearchDocument(
            1,
            "Vocabulary",
            string.Empty,
            Keywords: ["algorithm", "algebra", "search"]));

        var completions = engine.CompletePrefix("alg", maxResults: 10);
        var correction = engine.SuggestCorrection("algoritm", maximumDistance: 2);

        // Trie 的公共前缀遍历按序号字典序输出，因此不应受哈希槽位顺序影响。
        Assert.True(completions.SequenceEqual(["algebra", "algorithm"]));
        // algoritm 到 algorithm 只需插入 h；该断言证明纠错实际使用了编辑距离阈值。
        Assert.Equal("algorithm", correction);
        Assert.Null(engine.SuggestCorrection("unrelated", maximumDistance: 1));
    }

    [Fact]
    public void Search_CanonicalizesQueryOrderAndIndexVersionInvalidatesCache()
    {
        var engine = new MiniSearchEngine(queryCacheCapacity: 4);
        engine.AddDocument(new SearchDocument(1, "Search Trie", "search trie"));

        var first = engine.Search("search trie", maxResults: 5);
        var second = engine.Search("trie search", maxResults: 5);

        // AND 查询与词序无关；规范键相同，所以第二次查询必须复用第一次计算。
        Assert.False(first.FromCache);
        Assert.True(second.FromCache);
        Assert.Equal(1L, engine.SearchComputationCount);
        Assert.Equal(first.IndexVersion, second.IndexVersion);

        engine.AddDocument(new SearchDocument(2, "Another Search Trie", "search trie"));
        var afterIndexChange = engine.Search("search trie", maxResults: 5);

        // 新文档提升索引版本；即使查询文本相同，也不能返回旧版本的一条命中。
        Assert.False(afterIndexChange.FromCache);
        Assert.Equal(2L, engine.SearchComputationCount);
        Assert.NotEqual(first.IndexVersion, afterIndexChange.IndexVersion);
        Assert.Equal(2, afterIndexChange.Hits.Count);
    }

    [Fact]
    public void Search_CachedResultsCannotBeMutatedThroughReadOnlyInterfaces()
    {
        var engine = new MiniSearchEngine();
        engine.AddDocument(new SearchDocument(1, "Search", "search"));

        var first = engine.Search("search");
        var exposedHits = Assert.IsAssignableFrom<IList<SearchHit>>(first.Hits);
        var exposedTerms = Assert.IsAssignableFrom<IList<string>>(first.Hits[0].MatchedTerms);
        var exposedHighlights = Assert.IsAssignableFrom<IList<DataStructureAndAlgorithm.PatternMatching.PatternMatch>>(
            first.Hits[0].Highlights);

        Assert.Throws<NotSupportedException>(() => exposedHits.Clear());
        Assert.Throws<NotSupportedException>(() => exposedTerms.Clear());
        Assert.Throws<NotSupportedException>(() => exposedHighlights.Clear());

        var cached = engine.Search("search");
        Assert.True(cached.FromCache);
        Assert.Single(cached.Hits);
        Assert.Equal(["search"], cached.Hits[0].MatchedTerms);
        Assert.NotEmpty(cached.Hits[0].Highlights);
    }

    [Fact]
    public void AddDocumentAndSearchRejectInvalidIdentityAndEmptyQuery()
    {
        var engine = new MiniSearchEngine();
        engine.AddDocument(new SearchDocument(7, "First", "search"));

        // 重复 id 若被接受，会在倒排链表中产生两个不可区分的 Posting，导致重复命中和重复计分。
        Assert.Throws<ArgumentException>(() =>
            engine.AddDocument(new SearchDocument(7, "Duplicate", "search")));
        Assert.Equal(1, engine.DocumentCount);

        // 空查询没有可定义的 AND 交集；显式拒绝比悄悄返回全部文档更安全。
        Assert.Throws<ArgumentException>(() => engine.Search(" \t \r\n "));
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.Search("search", maxResults: 0));
    }
}
