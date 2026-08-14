using DataStructureAndAlgorithm.PatternMatching;

namespace DataStructureAndAlgorithm.Scenarios.MiniSearch;

/// <summary>
/// 位置查询在文档某个字段中的一个词元区间。
/// </summary>
/// <param name="FieldOrdinal">
/// 字段序号：0 表示标题，1 表示正文，2 开始依次表示人工关键词。
/// 保留关键词各自的字段边界，可以避免把两个独立关键词误判成同一个短语。
/// </param>
/// <param name="StartTokenOffset">区间首词在字段内的从零开始词元下标。</param>
/// <param name="EndTokenOffsetInclusive">区间末词在字段内的下标，包含该位置。</param>
public readonly record struct PositionalMatch(
    int FieldOrdinal,
    int StartTokenOffset,
    int EndTokenOffsetInclusive);

/// <summary>
/// BM25、短语查询和邻近查询共用的高级检索命中。
/// </summary>
/// <param name="DocumentId">文档编号。</param>
/// <param name="Title">文档标题。</param>
/// <param name="Score">
/// BM25 相关性分数。它由词频、文档频率和长度归一化共同决定，不混入业务热度。
/// </param>
/// <param name="DocumentLength">建立索引时统计的文档词元总数。</param>
/// <param name="HighlightText">用于字符级高亮的规范化文本。</param>
/// <param name="MatchedTerms">本篇文档实际命中的规范化查询词，按序号字典序排列。</param>
/// <param name="Highlights">Aho-Corasick 得到的字符级整词命中。</param>
/// <param name="PositionalMatches">
/// 短语或邻近查询的词元区间；普通 BM25 查询没有额外位置约束，因此这里为空。
/// </param>
public sealed record Bm25SearchHit(
    int DocumentId,
    string Title,
    double Score,
    int DocumentLength,
    string HighlightText,
    IReadOnlyList<string> MatchedTerms,
    IReadOnlyList<PatternMatch> Highlights,
    IReadOnlyList<PositionalMatch> PositionalMatches);

/// <summary>
/// 一次 BM25 或位置约束查询的响应。
/// </summary>
/// <param name="Query">调用方输入；位置查询会保留原始短语或构造可读的 NEAR 表达式。</param>
/// <param name="IndexVersion">计算时使用的索引版本。</param>
/// <param name="K1">BM25 词频饱和参数。</param>
/// <param name="B">BM25 文档长度归一化参数。</param>
/// <param name="Hits">按 BM25 分数降序、文档编号升序确定性排列的命中。</param>
public sealed record Bm25SearchResponse(
    string Query,
    long IndexVersion,
    double K1,
    double B,
    IReadOnlyList<Bm25SearchHit> Hits);
