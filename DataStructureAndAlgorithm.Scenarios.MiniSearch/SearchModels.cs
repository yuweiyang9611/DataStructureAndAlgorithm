using DataStructureAndAlgorithm.PatternMatching;

namespace DataStructureAndAlgorithm.Scenarios.MiniSearch;

/// <summary>
/// 一篇可检索文档。
/// </summary>
/// <param name="Id">文档的稳定唯一编号。</param>
/// <param name="Title">标题；标题中的词会获得更高的词频权重。</param>
/// <param name="Content">正文。</param>
/// <param name="Popularity">业务热度，例如阅读次数或点赞数；必须为非负数。</param>
/// <param name="Keywords">
/// 可选人工关键词。中文自然语言分词不是本示例的重点，因此可用关键词补充“前缀树”等语义词。
/// </param>
public sealed record SearchDocument(
    int Id,
    string Title,
    string Content,
    int Popularity = 0,
    IReadOnlyList<string>? Keywords = null);

/// <summary>
/// 一条命中结果。
/// </summary>
/// <param name="DocumentId">文档编号。</param>
/// <param name="Title">文档标题。</param>
/// <param name="Score">所有查询词的加权词频之和，再加上文档热度。</param>
/// <param name="Popularity">文档热度。</param>
/// <param name="HighlightText">
/// 用于高亮的规范化文本，依次包含标题、正文和关键词；<paramref name="Highlights"/> 的下标以它为准。
/// </param>
/// <param name="MatchedTerms">按序号字典序排列的去重查询词。</param>
/// <param name="Highlights">
/// Aho-Corasick 一次扫描得到的多关键词命中位置。下标是 .NET 字符串的 UTF-16 下标。
/// </param>
public sealed record SearchHit(
    int DocumentId,
    string Title,
    long Score,
    int Popularity,
    string HighlightText,
    IReadOnlyList<string> MatchedTerms,
    IReadOnlyList<PatternMatch> Highlights);

/// <summary>
/// 一次查询的完整响应。
/// </summary>
/// <param name="Query">调用方本次传入的原始查询。</param>
/// <param name="IndexVersion">计算结果时使用的索引版本。</param>
/// <param name="FromCache">本次响应是否来自 LRU 查询缓存。</param>
/// <param name="Hits">按分数降序、文档编号升序排列的结果。</param>
public sealed record SearchResponse(
    string Query,
    long IndexVersion,
    bool FromCache,
    IReadOnlyList<SearchHit> Hits);
