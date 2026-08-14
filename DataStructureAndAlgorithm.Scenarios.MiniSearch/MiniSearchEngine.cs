using System.Globalization;
using System.Text;
using DataStructureAndAlgorithm.Caching;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.DynamicProgramming;
using DataStructureAndAlgorithm.Hashing;
using DataStructureAndAlgorithm.Heap;
using DataStructureAndAlgorithm.Linear;
using DataStructureAndAlgorithm.PatternMatching;
using DataStructureAndAlgorithm.Sorting;
using DataStructureAndAlgorithm.Tree;

namespace DataStructureAndAlgorithm.Scenarios.MiniSearch;

/// <summary>
/// 面向本地学习笔记的迷你全文检索、自动补全和拼写纠错引擎。
/// </summary>
/// <remarks>
/// <para>
/// 这个类刻意组合仓库中的教学实现：开放寻址哈希表保存文档表和倒排索引，单向链表保存倒排记录，
/// Trie 保存词汇表，二叉最小堆选择 Top-K，LRU 缓存重复查询，Aho-Corasick 完成多词高亮，
/// 编辑距离负责纠错，归并排序负责确定性的最终顺序。
/// </para>
/// <para>
/// 示例是单线程内存模型，不负责持久化和并发写入。生产搜索系统通常还需要分词器、磁盘段、压缩倒排表、
/// BM25、并发快照和故障恢复；这里优先展示各种数据结构怎样协作。
/// </para>
/// </remarks>
public sealed class MiniSearchEngine
{
    private const string TraceAlgorithm = "MiniSearch";
    private const int TitleWeight = 3;
    private const int ContentWeight = 1;
    private const int KeywordWeight = 5;

    // 核心文档表和倒排索引都使用项目自己实现的开放寻址哈希表，
    // 不用 Dictionary 隐藏哈希冲突、墓碑和扩容这些值得学习的细节。
    private readonly OpenAddressingHashTable<int, IndexedDocument> _documents = new();
    private readonly OpenAddressingHashTable<string, SinglyLinkedList<Posting>> _invertedIndex =
        new(comparer: StringComparer.Ordinal);
    private readonly Trie _lexicon = new();
    private readonly IAlgorithmTraceSink? _trace;

    // 缓存键包含索引版本。新增文档后无需遍历并清空旧缓存：版本变化会让旧键自然失效，
    // 同时 LRU 的固定容量保证旧版本条目最终会被淘汰，不会无限增长。
    private readonly LruCache<SearchCacheKey, SearchResponse> _queryCache;
    private long _indexVersion;

    public MiniSearchEngine(int queryCacheCapacity = 32, IAlgorithmTraceSink? trace = null)
    {
        _queryCache = new LruCache<SearchCacheKey, SearchResponse>(queryCacheCapacity);
        // 追踪器是可选依赖：不传入时，教学算法的返回值和复杂度特征保持不变；
        // 注入后再套 best-effort 装饰器，观察器自身异常也不能把已修改索引变成“对调用方失败”。
        _trace = BestEffortAlgorithmTraceSink.Wrap(trace);
    }

    /// <summary>当前已收录的文档数。</summary>
    public int DocumentCount => _documents.Count;

    /// <summary>
    /// 实际执行过的查询计算次数。缓存命中不会增加它，便于 Demo 和测试验证版本缓存确实生效。
    /// </summary>
    public long SearchComputationCount { get; private set; }

    /// <summary>
    /// 添加一篇文档并更新倒排索引；重复编号会被拒绝，避免同一文档被重复计分。
    /// </summary>
    public void AddDocument(SearchDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(document.Title);
        ArgumentNullException.ThrowIfNull(document.Content);
        ArgumentOutOfRangeException.ThrowIfNegative(document.Popularity);

        if (_documents.ContainsKey(document.Id))
        {
            throw new ArgumentException($"文档编号 {document.Id} 已存在。", nameof(document));
        }

        // 先在临时的自实现哈希表中汇总词频，再触碰全局索引。
        // 标题和人工关键词权重更高，所以同一个词出现在这些位置时会得到更高相关性分数。
        var weightedFrequencies = new OpenAddressingHashTable<string, int>(comparer: StringComparer.Ordinal);
        AccumulateWeightedTerms(document.Title, TitleWeight, weightedFrequencies);
        AccumulateWeightedTerms(document.Content, ContentWeight, weightedFrequencies);

        if (document.Keywords is not null)
        {
            foreach (var keyword in document.Keywords)
            {
                ArgumentNullException.ThrowIfNull(keyword);
                AccumulateWeightedTerms(keyword, KeywordWeight, weightedFrequencies);
            }
        }

        var indexedDocument = new IndexedDocument(
            document,
            BuildHighlightText(document));

        // AddDocument 是本示例唯一的写入口，且在真正修改倒排表前已经验证重复 id 和所有输入。
        // 先登记文档，再把每个不同词加入倒排表；每篇文档对每个词只产生一个 Posting。
        if (!_documents.TryAdd(document.Id, indexedDocument))
        {
            throw new InvalidOperationException("文档表在单线程写入期间出现了意外的重复编号。");
        }

        foreach (var (term, weightedFrequency) in weightedFrequencies)
        {
            if (!_invertedIndex.TryGetValue(term, out var postings))
            {
                postings = new SinglyLinkedList<Posting>();
                // TryAdd 与前面的 TryGetValue 组成明确的“先查后增”流程，避免把教学哈希表
                // 误当作 Dictionary。单线程写入下这里必然新增成功。
                _invertedIndex.TryAdd(term, postings);
                _lexicon.Add(term);
            }

            // 维护尾指针的单向链表可在 O(1) 时间追加倒排记录。
            // 本示例只新增、不原地更新文档，因此无需在链表中删除旧记录。
            postings.AddLast(new Posting(document.Id, weightedFrequency));
        }

        _indexVersion = checked(_indexVersion + 1);
        _trace?.Record(
            TraceAlgorithm,
            "DocumentIndexed",
            "文档表、倒排表和 Trie 已完成同一版本的更新。",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["documentCount"] = Format(DocumentCount),
                ["documentId"] = Format(document.Id),
                ["indexVersion"] = Format(_indexVersion),
                ["uniqueTerms"] = Format(weightedFrequencies.Count)
            });
    }

    /// <summary>
    /// 使用 AND 语义搜索：文档必须包含所有去重后的查询词。
    /// </summary>
    /// <param name="query">以空白或标点分隔的查询文本。</param>
    /// <param name="maxResults">最多返回多少条结果，必须为正数。</param>
    public SearchResponse Search(string query, int maxResults = 10)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);

        var queryTerms = GetUniqueTerms(query);
        if (queryTerms.Count == 0)
        {
            throw new ArgumentException("查询至少要包含一个字母、数字或受支持的标识符字符。", nameof(query));
        }

        // AND 查询与词的书写顺序无关。先归并排序生成规范键，使 "trie search" 和 "search trie"
        // 能命中同一缓存条目；归并排序的稳定性也让相等元素的行为容易推导。
        SortAlgorithms.MergeSort(queryTerms, StringComparer.Ordinal);
        var canonicalQuery = string.Join('\u001f', queryTerms);
        var cacheKey = new SearchCacheKey(canonicalQuery, maxResults, _indexVersion);
        _trace?.Record(
            TraceAlgorithm,
            "QueryNormalized",
            "查询词已去重并按序号字典序排序，因此 AND 查询的缓存键与输入词序无关。",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["canonicalQuery"] = string.Join(' ', queryTerms),
                ["indexVersion"] = Format(_indexVersion),
                ["maxResults"] = Format(maxResults),
                ["termCount"] = Format(queryTerms.Count)
            });

        if (_queryCache.TryGetValue(cacheKey, out var cached))
        {
            var cachedResponse = cached with { Query = query, FromCache = true };
            _trace?.Record(
                TraceAlgorithm,
                "CacheHit",
                "规范查询、Top-K 大小和索引版本均相同，直接复用 LRU 中的不可变响应。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["hitCount"] = Format(cachedResponse.Hits.Count),
                    ["indexVersion"] = Format(_indexVersion)
                });
            RecordSearchCompleted(cachedResponse);
            return cachedResponse;
        }

        _trace?.Record(
            TraceAlgorithm,
            "CacheMiss",
            "缓存中没有当前索引版本的结果，需要执行倒排表交集和 Top-K 选择。",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["indexVersion"] = Format(_indexVersion),
                ["searchComputation"] = Format(SearchComputationCount + 1)
            });
        SearchComputationCount = checked(SearchComputationCount + 1);
        var response = ComputeSearch(query, queryTerms, maxResults);
        _queryCache.Set(cacheKey, response);
        RecordSearchCompleted(response);
        return response;
    }

    /// <summary>
    /// 按序号字典序返回前缀补全结果。Trie 的查询成本取决于前缀长度，而不是词汇总量。
    /// </summary>
    public IReadOnlyList<string> CompletePrefix(string prefix, int maxResults = 10)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentOutOfRangeException.ThrowIfNegative(maxResults);
        if (maxResults == 0)
        {
            _trace?.Record(
                TraceAlgorithm,
                "PrefixCompleted",
                "调用方请求零条结果，因此无需遍历 Trie。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["maxResults"] = "0",
                    ["resultCount"] = "0"
                });
            return [];
        }

        var normalizedPrefix = NormalizeSingleTerm(prefix, allowEmpty: true, nameof(prefix));
        var completions = _lexicon.GetWordsWithPrefix(normalizedPrefix, maxResults);
        _trace?.Record(
            TraceAlgorithm,
            "PrefixCompleted",
            "Trie 只沿前缀路径下探，并按序号字典序截取补全结果。",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["maxResults"] = Format(maxResults),
                ["normalizedPrefix"] = normalizedPrefix,
                ["resultCount"] = Format(completions.Count)
            });
        return completions;
    }

    /// <summary>
    /// 在词汇表中寻找编辑距离不超过阈值的最接近词；完全匹配时直接返回规范化单词。
    /// </summary>
    /// <remarks>
    /// 编辑距离允许插入、删除和替换。相同距离时优先选择文档频率更高的词，再按序号字典序打破平局，
    /// 因而输出不依赖哈希表枚举顺序。
    /// </remarks>
    public string? SuggestCorrection(string term, int maximumDistance = 2)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumDistance);
        var normalizedTerm = NormalizeSingleTerm(term, allowEmpty: false, nameof(term));

        if (_lexicon.Contains(normalizedTerm))
        {
            RecordCorrectionCompleted(normalizedTerm, normalizedTerm, distance: 0, maximumDistance);
            return normalizedTerm;
        }

        string? bestTerm = null;
        var bestDistance = maximumDistance + 1;
        var bestDocumentFrequency = -1;

        foreach (var candidate in _lexicon.GetWordsWithPrefix(string.Empty, _lexicon.Count))
        {
            var distance = AdvancedDynamicProgrammingAlgorithms.EditDistance(normalizedTerm, candidate);
            if (distance > maximumDistance)
            {
                continue;
            }

            _invertedIndex.TryGetValue(candidate, out var postings);
            var documentFrequency = postings?.Count ?? 0;
            var isBetter = distance < bestDistance ||
                           distance == bestDistance && documentFrequency > bestDocumentFrequency ||
                           distance == bestDistance && documentFrequency == bestDocumentFrequency &&
                           string.CompareOrdinal(candidate, bestTerm) < 0;

            if (!isBetter)
            {
                continue;
            }

            bestTerm = candidate;
            bestDistance = distance;
            bestDocumentFrequency = documentFrequency;
        }

        RecordCorrectionCompleted(normalizedTerm, bestTerm, bestTerm is null ? null : bestDistance, maximumDistance);
        return bestTerm;
    }

    private SearchResponse ComputeSearch(string query, List<string> queryTerms, int maxResults)
    {
        // 先找最短倒排表作为驱动集合。AND 查询不可能产生比最稀有词更多的候选，
        // 从它开始能显著减少随后在其他链表中的查找次数。
        SinglyLinkedList<Posting>? seedPostings = null;
        string? seedTerm = null;
        var termPostings = new List<TermPostings>(queryTerms.Count);

        foreach (var term in queryTerms)
        {
            if (!_invertedIndex.TryGetValue(term, out var postings))
            {
                _trace?.Record(
                    TraceAlgorithm,
                    "SeedPostingSelected",
                    "某个查询词没有倒排记录，AND 交集必为空，可提前结束。",
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["missingTerm"] = term,
                        ["postingCount"] = "0"
                    });
                RecordTopKSelected(candidateCount: 0, resultCount: 0, maxResults);
                return new SearchResponse(query, _indexVersion, false, []);
            }

            termPostings.Add(new TermPostings(term, postings));
            if (seedPostings is null || postings.Count < seedPostings.Count)
            {
                seedPostings = postings;
                seedTerm = term;
            }
        }

        _trace?.Record(
            TraceAlgorithm,
            "SeedPostingSelected",
            "选择最短倒排表驱动 AND 交集，减少在其他链表中查找文档编号的次数。",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["postingCount"] = Format(seedPostings!.Count),
                ["seedTerm"] = seedTerm!
            });

        // 最小堆只保留当前最好的 K 条。与“收集全部结果再排序”相比，候选很多时空间从 O(n)
        // 降到 O(K)。比较器把低分、同分但 id 较大的记录定义为“更小”，堆顶恰好是最差候选。
        var topResults = new BinaryMinHeap<RankedDocument>(WorstCandidateFirstComparer.Instance);

        foreach (var seed in seedPostings!)
        {
            long score = 0;
            var containsEveryTerm = true;

            foreach (var (_, postings) in termPostings)
            {
                if (!TryFindPosting(postings, seed.DocumentId, out var posting))
                {
                    containsEveryTerm = false;
                    break;
                }

                score = checked(score + posting.WeightedTermFrequency);
            }

            if (!containsEveryTerm || !_documents.TryGetValue(seed.DocumentId, out var document))
            {
                continue;
            }

            score = checked(score + document.Source.Popularity);
            topResults.Enqueue(new RankedDocument(seed.DocumentId, score));
            if (topResults.Count > maxResults)
            {
                topResults.Dequeue();
            }
        }

        var ranked = new List<RankedDocument>(topResults.Count);
        while (topResults.Count > 0)
        {
            ranked.Add(topResults.Dequeue());
        }

        // 堆只能保证堆顶最小，不能保证整体顺序；最终再用自实现归并排序得到可复现的 API 输出。
        SortAlgorithms.MergeSort(ranked, FinalResultComparer.Instance);
        RecordTopKSelected(seedPostings.Count, ranked.Count, maxResults);

        var matcher = new AhoCorasickMatcher(queryTerms);
        var matchedTerms = queryTerms.AsReadOnly();
        var hits = new List<SearchHit>(ranked.Count);
        foreach (var item in ranked)
        {
            var document = _documents[item.DocumentId];
            var highlights = FilterWholeTermMatches(matcher.FindAll(document.HighlightText), document.HighlightText);
            hits.Add(new SearchHit(
                item.DocumentId,
                document.Source.Title,
                item.Score,
                document.Source.Popularity,
                document.HighlightText,
                matchedTerms,
                Array.AsReadOnly(highlights.ToArray())));
        }

        return new SearchResponse(query, _indexVersion, false, hits.AsReadOnly());
    }

    private void RecordTopKSelected(int candidateCount, int resultCount, int maxResults) =>
        _trace?.Record(
            TraceAlgorithm,
            "TopKSelected",
            "最小堆只保留当前最优的 K 个候选，随后归并排序产生确定性的最终顺序。",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["candidateCount"] = Format(candidateCount),
                ["maxResults"] = Format(maxResults),
                ["resultCount"] = Format(resultCount)
            });

    private void RecordSearchCompleted(SearchResponse response) =>
        _trace?.Record(
            TraceAlgorithm,
            "SearchCompleted",
            "搜索响应已形成；追踪只描述过程，不参与排名或缓存判定。",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["fromCache"] = response.FromCache.ToString(CultureInfo.InvariantCulture),
                ["hitCount"] = Format(response.Hits.Count),
                ["indexVersion"] = Format(response.IndexVersion)
            });

    private void RecordCorrectionCompleted(
        string normalizedTerm,
        string? suggestion,
        int? distance,
        int maximumDistance) =>
        _trace?.Record(
            TraceAlgorithm,
            "CorrectionCompleted",
            "编辑距离先决定相似度，再用文档频率和序号字典序稳定地打破平局。",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["distance"] = distance?.ToString(CultureInfo.InvariantCulture) ?? "none",
                ["maximumDistance"] = Format(maximumDistance),
                ["normalizedTerm"] = normalizedTerm,
                ["suggestion"] = suggestion ?? "none"
            });

    private static string Format<T>(T value) where T : IFormattable =>
        value.ToString(null, CultureInfo.InvariantCulture);

    private static List<string> GetUniqueTerms(string text)
    {
        var terms = Tokenize(text);
        var uniqueTerms = new List<string>(terms.Count);
        var seen = new OpenAddressingHashTable<string, byte>(comparer: StringComparer.Ordinal);

        foreach (var term in terms)
        {
            if (seen.TryAdd(term, 0))
            {
                uniqueTerms.Add(term);
            }
        }

        return uniqueTerms;
    }

    private static void AccumulateWeightedTerms(
        string text,
        int weight,
        OpenAddressingHashTable<string, int> frequencies)
    {
        foreach (var term in Tokenize(text))
        {
            frequencies[term] = frequencies.TryGetValue(term, out var current)
                ? checked(current + weight)
                : weight;
        }
    }

    private static List<string> Tokenize(string text)
    {
        var result = new List<string>();
        var buffer = new StringBuilder();

        foreach (var rune in text.EnumerateRunes())
        {
            if (SearchTextRules.IsTokenCharacter(rune))
            {
                buffer.Append(Rune.ToLowerInvariant(rune).ToString());
                continue;
            }

            FlushToken(buffer, result);
        }

        FlushToken(buffer, result);
        return result;
    }

    private static void FlushToken(StringBuilder buffer, List<string> destination)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        destination.Add(buffer.ToString());
        buffer.Clear();
    }

    private static string NormalizeSingleTerm(string value, bool allowEmpty, string parameterName)
    {
        if (allowEmpty && string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var terms = Tokenize(value);
        if (terms.Count != 1)
        {
            throw new ArgumentException("值必须规范化为且仅为一个词。", parameterName);
        }

        return terms[0];
    }

    private static string BuildHighlightText(SearchDocument document)
    {
        var builder = new StringBuilder();
        builder.Append(document.Title.ToLowerInvariant());
        builder.Append('\n');
        builder.Append(document.Content.ToLowerInvariant());

        if (document.Keywords is not null)
        {
            foreach (var keyword in document.Keywords)
            {
                builder.Append('\n');
                builder.Append(keyword.ToLowerInvariant());
            }
        }

        return builder.ToString();
    }

    private static IReadOnlyList<PatternMatch> FilterWholeTermMatches(
        IReadOnlyList<PatternMatch> matches,
        string text)
    {
        var result = new List<PatternMatch>();
        foreach (var match in matches)
        {
            var startsAtBoundary = match.StartIndex == 0 ||
                                   !SearchTextRules.IsTokenCharacterBefore(text, match.StartIndex);
            var endsAtBoundary = match.EndExclusive == text.Length ||
                                 !SearchTextRules.IsTokenCharacterAt(text, match.EndExclusive);
            if (startsAtBoundary && endsAtBoundary)
            {
                result.Add(match);
            }
        }

        return result;
    }

    private static bool TryFindPosting(
        SinglyLinkedList<Posting> postings,
        int documentId,
        out Posting found)
    {
        foreach (var posting in postings)
        {
            if (posting.DocumentId == documentId)
            {
                found = posting;
                return true;
            }
        }

        found = default;
        return false;
    }

    private sealed record IndexedDocument(SearchDocument Source, string HighlightText);

    private readonly record struct Posting(int DocumentId, int WeightedTermFrequency);

    private readonly record struct TermPostings(string Term, SinglyLinkedList<Posting> Postings);

    private readonly record struct RankedDocument(int DocumentId, long Score);

    private readonly record struct SearchCacheKey(string CanonicalQuery, int MaxResults, long IndexVersion);

    private sealed class WorstCandidateFirstComparer : IComparer<RankedDocument>
    {
        public static WorstCandidateFirstComparer Instance { get; } = new();

        public int Compare(RankedDocument left, RankedDocument right)
        {
            var byScore = left.Score.CompareTo(right.Score);
            return byScore != 0
                ? byScore
                : right.DocumentId.CompareTo(left.DocumentId);
        }
    }

    private sealed class FinalResultComparer : IComparer<RankedDocument>
    {
        public static FinalResultComparer Instance { get; } = new();

        public int Compare(RankedDocument left, RankedDocument right)
        {
            var byScoreDescending = right.Score.CompareTo(left.Score);
            return byScoreDescending != 0
                ? byScoreDescending
                : left.DocumentId.CompareTo(right.DocumentId);
        }
    }
}
