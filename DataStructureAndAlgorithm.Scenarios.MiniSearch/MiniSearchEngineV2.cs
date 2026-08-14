using System.Globalization;
using System.Text;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.DynamicProgramming;
using DataStructureAndAlgorithm.Hashing;
using DataStructureAndAlgorithm.Linear;
using DataStructureAndAlgorithm.PatternMatching;
using DataStructureAndAlgorithm.Sorting;
using DataStructureAndAlgorithm.Tree;

namespace DataStructureAndAlgorithm.Scenarios.MiniSearch;

/// <summary>
/// 在 <see cref="MiniSearchEngine"/> 兼容 API 之上增加位置倒排索引、BM25、短语/邻近查询和 BK-tree 纠错。
/// </summary>
/// <remarks>
/// <para>
/// V1 的 <see cref="Search(string, int)"/>、<see cref="CompletePrefix(string, int)"/> 和
/// <see cref="SuggestCorrection(string, int)"/> 入口全部保留。旧的 AND 搜索仍使用“字段权重 + 热度”评分和 LRU 缓存，
/// 因而已有调用方无需迁移；需要更接近真实信息检索的相关性时，再显式调用 <see cref="SearchBm25"/>。
/// </para>
/// <para>
/// V2 为每个倒排记录额外保存“字段序号 + 字段内词元位置”。短语查询要求位置严格连续，
/// 邻近查询则允许两个词之间出现有限个其他词。位置不跨字段，因此标题末尾与正文开头不会被错误拼成短语。
/// </para>
/// <para>
/// 该类仍是单线程、只追加的教学实现。它刻意复用项目自己的开放寻址哈希表、单向链表、归并排序、
/// Aho-Corasick 与编辑距离，而不是把关键步骤委托给现成搜索框架。
/// </para>
/// </remarks>
public sealed class MiniSearchEngineV2
{
    private const string TraceAlgorithm = "MiniSearchV2";
    private const int TitleWeight = 3;
    private const int ContentWeight = 1;
    private const int KeywordWeight = 5;
    private const double DefaultK1 = 1.2;
    private const double DefaultB = 0.75;

    // 兼容引擎是旧 API 的唯一实现来源，避免 V1 和 V2 分别维护两份缓存、Top-K 与补全语义后逐渐漂移。
    private readonly MiniSearchEngine _compatibilityEngine;
    private readonly OpenAddressingHashTable<int, IndexedDocument> _documents = new();
    private readonly OpenAddressingHashTable<string, SinglyLinkedList<PositionPosting>> _positionalIndex =
        new(comparer: StringComparer.Ordinal);
    private readonly BkTree _correctionIndex = new(
        AdvancedDynamicProgrammingAlgorithms.EditDistance,
        StringComparer.Ordinal);
    private readonly IAlgorithmTraceSink? _trace;
    private long _indexVersion;
    private long _totalDocumentLength;

    /// <summary>创建一台 V2 搜索引擎，并为兼容查询配置 LRU 容量。</summary>
    public MiniSearchEngineV2(int queryCacheCapacity = 32, IAlgorithmTraceSink? trace = null)
    {
        var safeTrace = BestEffortAlgorithmTraceSink.Wrap(trace);
        _compatibilityEngine = new MiniSearchEngine(queryCacheCapacity, safeTrace);
        _trace = safeTrace;
    }

    /// <summary>当前文档数；该值与兼容引擎和位置索引保持一致。</summary>
    public int DocumentCount => _documents.Count;

    /// <summary>兼容 AND 查询真正计算的次数；缓存命中不增加该值。</summary>
    public long SearchComputationCount => _compatibilityEngine.SearchComputationCount;

    /// <summary>
    /// 建立一篇文档的普通倒排记录与位置倒排记录。
    /// </summary>
    public void AddDocument(SearchDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(document.Title);
        ArgumentNullException.ThrowIfNull(document.Content);
        ArgumentOutOfRangeException.ThrowIfNegative(document.Popularity);

        // 在修改任何全局状态之前完整分析并验证输入。这样即使某个关键词为 null，
        // 两套索引也都不会得到“只写入一半”的文档。
        var analysis = AnalyzeDocument(document);
        _compatibilityEngine.AddDocument(document);

        var indexedDocument = new IndexedDocument(document, BuildHighlightText(document), analysis.Length);
        if (!_documents.TryAdd(document.Id, indexedDocument))
        {
            // 正常情况下兼容引擎已先拒绝重复 id；该异常用于暴露两套索引意外失配，而不是悄悄继续。
            throw new InvalidOperationException("兼容索引与位置索引的文档编号状态不一致。");
        }

        // 哈希表的枚举顺序不是 API 契约。先按 term 排序再建树，使 BK-tree 形状、Trace 和调试过程可复现。
        var orderedTerms = analysis.Terms.Select(pair => pair.Key).ToList();
        SortAlgorithms.MergeSort(orderedTerms, StringComparer.Ordinal);
        foreach (var term in orderedTerms)
        {
            var statistics = analysis.Terms[term];
            if (!_positionalIndex.TryGetValue(term, out var postings))
            {
                postings = new SinglyLinkedList<PositionPosting>();
                _positionalIndex.TryAdd(term, postings);
                _correctionIndex.Add(term);
            }

            postings.AddLast(new PositionPosting(
                document.Id,
                statistics.RawFrequency,
                statistics.WeightedFrequency,
                [.. statistics.Positions]));
        }

        _totalDocumentLength = checked(_totalDocumentLength + analysis.Length);
        _indexVersion = checked(_indexVersion + 1);

        if (_trace is not null)
        {
            _trace.Record(
                TraceAlgorithm,
                "PositionalDocumentIndexed",
                "每个词同时记录字段、词元位置、原始词频和兼容字段权重，供短语、邻近与 BM25 复用。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["documentCount"] = Format(DocumentCount),
                    ["documentId"] = Format(document.Id),
                    ["documentLength"] = Format(analysis.Length),
                    ["indexVersion"] = Format(_indexVersion),
                    ["lexiconSize"] = Format(_correctionIndex.Count),
                    ["uniqueTerms"] = Format(analysis.Terms.Count)
                });
        }
    }

    /// <summary>
    /// 保留 V1 的 AND 语义、整数评分、Top-K 和版本化 LRU 缓存。
    /// </summary>
    public SearchResponse Search(string query, int maxResults = 10) =>
        _compatibilityEngine.Search(query, maxResults);

    /// <summary>保留 V1 基于 Trie 的确定性前缀补全。</summary>
    public IReadOnlyList<string> CompletePrefix(string prefix, int maxResults = 10) =>
        _compatibilityEngine.CompletePrefix(prefix, maxResults);

    /// <summary>
    /// 用 BK-tree 查询编辑距离半径内的候选，再沿用 V1 的确定性规则选择答案：
    /// 距离更小优先；距离相同时文档频率更高优先；仍相同时按序号字典序。
    /// </summary>
    public string? SuggestCorrection(string term, int maximumDistance = 2)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumDistance);
        var normalizedTerm = NormalizeSingleTerm(term, allowEmpty: false, nameof(term));
        var candidates = _correctionIndex.Search(normalizedTerm, maximumDistance);

        string? bestTerm = null;
        var bestDistance = 0;
        var bestDocumentFrequency = -1;
        foreach (var candidate in candidates)
        {
            _positionalIndex.TryGetValue(candidate.Value, out var postings);
            var documentFrequency = postings?.Count ?? 0;
            // 用“尚无候选”作为哨兵，避免 maximumDistance == int.MaxValue 时执行 +1 溢出。
            // 这也允许距离恰好等于 int.MaxValue 的合法候选参与比较。
            var isBetter = bestTerm is null || candidate.Distance < bestDistance ||
                           candidate.Distance == bestDistance && documentFrequency > bestDocumentFrequency ||
                           candidate.Distance == bestDistance && documentFrequency == bestDocumentFrequency &&
                           string.CompareOrdinal(candidate.Value, bestTerm) < 0;
            if (!isBetter)
            {
                continue;
            }

            bestTerm = candidate.Value;
            bestDistance = candidate.Distance;
            bestDocumentFrequency = documentFrequency;
        }

        if (_trace is not null)
        {
            _trace.Record(
                TraceAlgorithm,
                "BkTreeCorrectionCompleted",
                "BK-tree 用三角不等式剪枝候选；最终平局规则与全词典穷举 oracle 完全相同。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["candidateCount"] = Format(candidates.Count),
                    ["distance"] = bestTerm is null ? "none" : Format(bestDistance),
                    ["lexiconSize"] = Format(_correctionIndex.Count),
                    ["maximumDistance"] = Format(maximumDistance),
                    ["normalizedTerm"] = normalizedTerm,
                    ["suggestion"] = bestTerm ?? "none"
                });
        }

        return bestTerm;
    }

    /// <summary>
    /// 使用标准 BM25 公式执行 OR 查询，并以文档编号稳定地打破同分平局。
    /// </summary>
    /// <remarks>
    /// IDF 使用 <c>ln(1 + (N-df+0.5)/(df+0.5))</c>，始终为正；词频部分使用 K1 控制饱和速度，
    /// B 控制文档长度归一化。查询词会去重并按序号字典序计算，保证浮点加法顺序固定。
    /// </remarks>
    public Bm25SearchResponse SearchBm25(
        string query,
        int maxResults = 10,
        double k1 = DefaultK1,
        double b = DefaultB)
    {
        ValidateAdvancedSearchArguments(query, maxResults, k1, b);
        var queryTerms = GetUniqueTerms(query);
        if (queryTerms.Count == 0)
        {
            throw new ArgumentException("查询至少要包含一个可索引词元。", nameof(query));
        }

        SortAlgorithms.MergeSort(queryTerms, StringComparer.Ordinal);
        RecordAdvancedQueryNormalized("bm25", queryTerms, maxResults, k1, b);
        return ComputeBm25(
            query,
            queryTerms,
            maxResults,
            k1,
            b,
            positionSelector: null,
            "Bm25RankingCompleted");
    }

    /// <summary>
    /// 查找词元顺序与输入完全相同且在同一字段内连续出现的文档，并用 BM25 排序。
    /// </summary>
    public Bm25SearchResponse SearchPhrase(
        string phrase,
        int maxResults = 10,
        double k1 = DefaultK1,
        double b = DefaultB)
    {
        ValidateAdvancedSearchArguments(phrase, maxResults, k1, b);
        var orderedPhraseTerms = Tokenize(phrase);
        if (orderedPhraseTerms.Count == 0)
        {
            throw new ArgumentException("短语至少要包含一个可索引词元。", nameof(phrase));
        }

        var scoringTerms = GetUniqueTerms(phrase);
        SortAlgorithms.MergeSort(scoringTerms, StringComparer.Ordinal);
        RecordAdvancedQueryNormalized("phrase", scoringTerms, maxResults, k1, b);
        return ComputeBm25(
            phrase,
            scoringTerms,
            maxResults,
            k1,
            b,
            documentId => FindPhraseMatches(documentId, orderedPhraseTerms),
            "PhraseRankingCompleted");
    }

    /// <summary>
    /// 查找两个词在同一字段内相距不超过指定间隔的文档，并用 BM25 排序。
    /// </summary>
    /// <param name="maximumGap">
    /// 两个词之间最多允许出现多少个其他词；0 表示必须相邻。查询是无方向的，左右次序都可命中。
    /// </param>
    public Bm25SearchResponse SearchProximity(
        string firstTerm,
        string secondTerm,
        int maximumGap = 3,
        int maxResults = 10,
        double k1 = DefaultK1,
        double b = DefaultB)
    {
        ArgumentNullException.ThrowIfNull(firstTerm);
        ArgumentNullException.ThrowIfNull(secondTerm);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumGap);
        ValidateAdvancedSearchArguments(firstTerm, maxResults, k1, b);

        var normalizedFirst = NormalizeSingleTerm(firstTerm, allowEmpty: false, nameof(firstTerm));
        var normalizedSecond = NormalizeSingleTerm(secondTerm, allowEmpty: false, nameof(secondTerm));
        var scoringTerms = new List<string> { normalizedFirst };
        if (!StringComparer.Ordinal.Equals(normalizedFirst, normalizedSecond))
        {
            scoringTerms.Add(normalizedSecond);
            SortAlgorithms.MergeSort(scoringTerms, StringComparer.Ordinal);
        }

        RecordAdvancedQueryNormalized("proximity", scoringTerms, maxResults, k1, b);
        var query = $"{firstTerm} NEAR/{maximumGap.ToString(CultureInfo.InvariantCulture)} {secondTerm}";
        return ComputeBm25(
            query,
            scoringTerms,
            maxResults,
            k1,
            b,
            documentId => FindProximityMatches(documentId, normalizedFirst, normalizedSecond, maximumGap),
            "ProximityRankingCompleted");
    }

    private Bm25SearchResponse ComputeBm25(
        string query,
        IReadOnlyList<string> queryTerms,
        int maxResults,
        double k1,
        double b,
        Func<int, IReadOnlyList<PositionalMatch>>? positionSelector,
        string completedOperation)
    {
        var candidateIds = new OpenAddressingHashTable<int, byte>();
        var indexedQueryTerms = new List<TermPostings>(queryTerms.Count);
        foreach (var term in queryTerms)
        {
            if (!_positionalIndex.TryGetValue(term, out var postings))
            {
                continue;
            }

            indexedQueryTerms.Add(new TermPostings(term, postings));
            foreach (var posting in postings)
            {
                candidateIds.TryAdd(posting.DocumentId, 0);
            }
        }

        var averageDocumentLength = DocumentCount == 0
            ? 0D
            : (double)_totalDocumentLength / DocumentCount;
        var candidates = new List<Bm25Candidate>(candidateIds.Count);
        foreach (var (documentId, _) in candidateIds)
        {
            var positionalMatches = positionSelector?.Invoke(documentId) ?? [];
            if (positionSelector is not null && positionalMatches.Count == 0)
            {
                continue;
            }

            var document = _documents[documentId];
            var matchedTerms = new List<string>(indexedQueryTerms.Count);
            double score = 0;
            foreach (var (term, postings) in indexedQueryTerms)
            {
                if (!TryFindPosting(postings, documentId, out var posting))
                {
                    continue;
                }

                matchedTerms.Add(term);
                // Robertson/Sparck Jones IDF 的平滑版本避免高频词得到负权重。
                var inverseDocumentFrequency = Math.Log(
                    1D + (DocumentCount - postings.Count + 0.5D) / (postings.Count + 0.5D));
                var lengthRatio = document.Length / averageDocumentLength;
                var lengthNormalization = 1D - b + b * lengthRatio;
                var saturatedTermFrequency = ComputeSaturatedTermFrequency(
                    posting.RawTermFrequency, k1, lengthNormalization);
                score += inverseDocumentFrequency * saturatedTermFrequency;
            }

            candidates.Add(new Bm25Candidate(documentId, score, matchedTerms, positionalMatches));
        }

        // 哈希表只负责去重候选，最终顺序必须显式排序；否则扩容或哈希实现变化会改变公开 API。
        SortAlgorithms.MergeSort(candidates, Bm25CandidateComparer.Instance);
        var resultCount = Math.Min(maxResults, candidates.Count);
        var hits = new List<Bm25SearchHit>(resultCount);
        var matcher = new AhoCorasickMatcher(queryTerms);
        for (var index = 0; index < resultCount; index++)
        {
            var candidate = candidates[index];
            var document = _documents[candidate.DocumentId];
            var highlights = FilterWholeTermMatches(matcher.FindAll(document.HighlightText), document.HighlightText);
            hits.Add(new Bm25SearchHit(
                candidate.DocumentId,
                document.Source.Title,
                candidate.Score,
                document.Length,
                document.HighlightText,
                candidate.MatchedTerms,
                highlights,
                candidate.PositionalMatches));
        }

        if (_trace is not null)
        {
            _trace.Record(
                TraceAlgorithm,
                completedOperation,
                "位置约束先过滤候选，BM25 再按固定词序计分，最后以文档编号稳定打破同分平局。",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["averageDocumentLength"] = Format(averageDocumentLength),
                    ["candidateCount"] = Format(candidateIds.Count),
                    ["eligibleCount"] = Format(candidates.Count),
                    ["indexVersion"] = Format(_indexVersion),
                    ["resultCount"] = Format(hits.Count)
                });
        }

        return new Bm25SearchResponse(query, _indexVersion, k1, b, hits);
    }

    /// <summary>用按 K1 缩放的等价公式计算 BM25 词频饱和项。</summary>
    private static double ComputeSaturatedTermFrequency(
        int termFrequency,
        double k1,
        double lengthNormalization)
    {
        // 标准公式是 tf * (k1 + 1) / (tf + k1 * normalization)。当 k1 接近
        // double.MaxValue 且 tf >= 2 时，先算 tf * (k1 + 1) 会溢出为正无穷，
        // 尽管最终商在数学上仍是有限值。
        //
        // 分子、分母同时除以 max(1, k1) 不改变结果：
        //   tf * (k1/scale + 1/scale)
        //   --------------------------------
        //   tf/scale + (k1/scale) * normalization
        // k1 很大时 k1/scale 恒为 1；k1 很小时 scale 恒为 1。因此两个方向都不会制造
        // “无穷/无穷”，也不需要人为收窄公开参数契约。
        var scale = Math.Max(1D, k1);
        var scaledK1 = k1 / scale;
        var numerator = termFrequency * (scaledK1 + 1D / scale);
        var denominator = termFrequency / scale + scaledK1 * lengthNormalization;
        return numerator / denominator;
    }

    private IReadOnlyList<PositionalMatch> FindPhraseMatches(
        int documentId,
        IReadOnlyList<string> orderedTerms)
    {
        var postings = new List<PositionPosting>(orderedTerms.Count);
        foreach (var term in orderedTerms)
        {
            if (!_positionalIndex.TryGetValue(term, out var termPostings) ||
                !TryFindPosting(termPostings, documentId, out var posting))
            {
                return [];
            }

            postings.Add(posting);
        }

        var result = new List<PositionalMatch>();
        foreach (var start in postings[0].Positions)
        {
            var matched = true;
            for (var termIndex = 1; termIndex < postings.Count; termIndex++)
            {
                if (!ContainsPosition(
                        postings[termIndex].Positions,
                        start.FieldOrdinal,
                        checked(start.TokenOffset + termIndex)))
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                result.Add(new PositionalMatch(
                    start.FieldOrdinal,
                    start.TokenOffset,
                    checked(start.TokenOffset + orderedTerms.Count - 1)));
            }
        }

        return result;
    }

    private IReadOnlyList<PositionalMatch> FindProximityMatches(
        int documentId,
        string firstTerm,
        string secondTerm,
        int maximumGap)
    {
        if (!_positionalIndex.TryGetValue(firstTerm, out var firstPostings) ||
            !_positionalIndex.TryGetValue(secondTerm, out var secondPostings) ||
            !TryFindPosting(firstPostings, documentId, out var first) ||
            !TryFindPosting(secondPostings, documentId, out var second))
        {
            return [];
        }

        var result = new List<PositionalMatch>();
        foreach (var left in first.Positions)
        {
            foreach (var right in second.Positions)
            {
                if (left.FieldOrdinal != right.FieldOrdinal || left.TokenOffset == right.TokenOffset)
                {
                    continue;
                }

                var distance = Math.Abs((long)left.TokenOffset - right.TokenOffset);
                if (distance - 1 > maximumGap)
                {
                    continue;
                }

                // 相同词会让两个位置集合相同，只保留 offset 递增的一半，避免每对位置重复报告两次。
                if (StringComparer.Ordinal.Equals(firstTerm, secondTerm) && left.TokenOffset > right.TokenOffset)
                {
                    continue;
                }

                result.Add(new PositionalMatch(
                    left.FieldOrdinal,
                    Math.Min(left.TokenOffset, right.TokenOffset),
                    Math.Max(left.TokenOffset, right.TokenOffset)));
            }
        }

        return result;
    }

    private void RecordAdvancedQueryNormalized(
        string queryKind,
        IReadOnlyList<string> terms,
        int maxResults,
        double k1,
        double b)
    {
        if (_trace is null)
        {
            return;
        }

        _trace.Record(
            TraceAlgorithm,
            "AdvancedQueryNormalized",
            "高级查询固定参数与评分词顺序，使 BM25 浮点累加和最终排名可以跨运行复现。",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["b"] = Format(b),
                ["k1"] = Format(k1),
                ["maxResults"] = Format(maxResults),
                ["queryKind"] = queryKind,
                ["terms"] = string.Join(' ', terms)
            });
    }

    private static DocumentAnalysis AnalyzeDocument(SearchDocument document)
    {
        var terms = new OpenAddressingHashTable<string, MutableTermStatistics>(
            comparer: StringComparer.Ordinal);
        var length = 0;
        length = checked(length + AccumulateField(document.Title, TitleWeight, fieldOrdinal: 0, terms));
        length = checked(length + AccumulateField(document.Content, ContentWeight, fieldOrdinal: 1, terms));

        if (document.Keywords is not null)
        {
            for (var index = 0; index < document.Keywords.Count; index++)
            {
                var keyword = document.Keywords[index];
                ArgumentNullException.ThrowIfNull(keyword);
                length = checked(length + AccumulateField(
                    keyword,
                    KeywordWeight,
                    checked(index + 2),
                    terms));
            }
        }

        return new DocumentAnalysis(terms, length);
    }

    private static int AccumulateField(
        string text,
        int weight,
        int fieldOrdinal,
        OpenAddressingHashTable<string, MutableTermStatistics> destination)
    {
        var tokens = Tokenize(text);
        for (var offset = 0; offset < tokens.Count; offset++)
        {
            var term = tokens[offset];
            if (!destination.TryGetValue(term, out var statistics))
            {
                statistics = new MutableTermStatistics();
                destination.TryAdd(term, statistics);
            }

            statistics.RawFrequency = checked(statistics.RawFrequency + 1);
            statistics.WeightedFrequency = checked(statistics.WeightedFrequency + weight);
            statistics.Positions.Add(new TokenPosition(fieldOrdinal, offset));
        }

        return tokens.Count;
    }

    private static void ValidateAdvancedSearchArguments(string query, int maxResults, double k1, double b)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);
        if (!double.IsFinite(k1) || k1 <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(k1), "K1 必须是有限正数。");
        }

        if (!double.IsFinite(b) || b is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(b), "B 必须是 0 到 1 之间的有限数。");
        }
    }

    private static List<string> GetUniqueTerms(string text)
    {
        var result = new List<string>();
        var seen = new OpenAddressingHashTable<string, byte>(comparer: StringComparer.Ordinal);
        foreach (var term in Tokenize(text))
        {
            if (seen.TryAdd(term, 0))
            {
                result.Add(term);
            }
        }

        return result;
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

    private static bool ContainsPosition(
        IReadOnlyList<TokenPosition> positions,
        int fieldOrdinal,
        int tokenOffset)
    {
        foreach (var position in positions)
        {
            if (position.FieldOrdinal == fieldOrdinal && position.TokenOffset == tokenOffset)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindPosting(
        SinglyLinkedList<PositionPosting> postings,
        int documentId,
        out PositionPosting found)
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

    private static string Format<T>(T value) where T : IFormattable =>
        value.ToString(null, CultureInfo.InvariantCulture);

    private sealed class MutableTermStatistics
    {
        public int RawFrequency { get; set; }

        public int WeightedFrequency { get; set; }

        public List<TokenPosition> Positions { get; } = [];
    }

    private sealed record DocumentAnalysis(
        OpenAddressingHashTable<string, MutableTermStatistics> Terms,
        int Length);

    private sealed record IndexedDocument(SearchDocument Source, string HighlightText, int Length);

    private readonly record struct TokenPosition(int FieldOrdinal, int TokenOffset);

    private readonly record struct PositionPosting(
        int DocumentId,
        int RawTermFrequency,
        int WeightedTermFrequency,
        IReadOnlyList<TokenPosition> Positions);

    private readonly record struct TermPostings(
        string Term,
        SinglyLinkedList<PositionPosting> Postings);

    private sealed record Bm25Candidate(
        int DocumentId,
        double Score,
        IReadOnlyList<string> MatchedTerms,
        IReadOnlyList<PositionalMatch> PositionalMatches);

    private sealed class Bm25CandidateComparer : IComparer<Bm25Candidate>
    {
        public static Bm25CandidateComparer Instance { get; } = new();

        public int Compare(Bm25Candidate? left, Bm25Candidate? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            // 比较器必须满足传递性。绝对 epsilon 会形成 A≈B、B≈C、A<C 的比较环，
            // 让归并排序结果依赖候选枚举顺序。因此这里只对二进制分数完全相等时使用文档编号决胜；
            // 测试 oracle 可以用容差核对公式值，但不能把容差塞进排序关系。
            var byScoreDescending = right.Score.CompareTo(left.Score);
            return byScoreDescending != 0 ? byScoreDescending : left.DocumentId.CompareTo(right.DocumentId);
        }
    }
}
