using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Scenarios.MiniSearch;

Console.OutputEncoding = Encoding.UTF8;

// 默认运行仍只输出原有场景 JSON，避免给初学者增加额外噪声；只有显式传入
// “--trace json|mermaid” 才分配收集器，这也演示了可选诊断不应污染业务 API。
string? traceFormat = null;
if (args.Length > 0)
{
    if (args.Length != 2 || !string.Equals(args[0], "--trace", StringComparison.Ordinal))
    {
        Console.Error.WriteLine("用法: dotnet run --project DataStructureAndAlgorithm.Scenarios.MiniSearch -- [--trace json|mermaid]");
        return 1;
    }

    traceFormat = args[1].ToLowerInvariant();
    if (traceFormat is not ("json" or "mermaid"))
    {
        Console.Error.WriteLine("追踪格式必须是 json 或 mermaid。");
        Console.Error.WriteLine("用法: dotnet run --project DataStructureAndAlgorithm.Scenarios.MiniSearch -- [--trace json|mermaid]");
        return 1;
    }
}

var trace = traceFormat is null ? null : new CollectingAlgorithmTraceSink();
var engine = new MiniSearchEngineV2(queryCacheCapacity: 4, trace);

engine.AddDocument(new SearchDocument(
    101,
    "Trie 自动补全",
    "A trie shares common prefixes. A search box can use trie autocomplete without scanning every word.",
    Popularity: 12,
    Keywords: ["trie", "prefix", "autocomplete", "前缀树", "自动补全"]));

engine.AddDocument(new SearchDocument(
    102,
    "哈希索引与 LRU 缓存",
    "An inverted search index maps each term to postings. An LRU cache keeps hot query results.",
    Popularity: 18,
    Keywords: ["hash", "index", "search", "cache", "倒排索引"]));

engine.AddDocument(new SearchDocument(
    103,
    "多模式字符串匹配",
    "Aho Corasick scans text once and highlights several search terms, including trie and cache.",
    Popularity: 8,
    Keywords: ["aho", "corasick", "pattern", "search", "highlight"]));

engine.AddDocument(new SearchDocument(
    104,
    "迷你搜索引擎综合实践",
    "The search scenario combines a trie, inverted index, min heap, merge sort and query cache.",
    Popularity: 25,
    Keywords: ["search", "trie", "index", "heap", "cache", "综合项目"]));

var beforeFirstSearch = engine.SearchComputationCount;
var firstSearch = engine.Search("search trie", maxResults: 3);
var afterFirstSearch = engine.SearchComputationCount;

// AND 查询与词序无关；反转词序后仍会命中相同的“规范查询 + 索引版本”缓存键。
var cachedSearch = engine.Search("trie search", maxResults: 3);
var afterCachedSearch = engine.SearchComputationCount;

var output = new
{
    scenario = "本地算法学习笔记：全文检索、自动补全与拼写纠错",
    engine.DocumentCount,
    cacheVerification = new
    {
        beforeFirstSearch,
        afterFirstSearch,
        afterCachedSearch,
        cachedSearch.FromCache
    },
    firstSearch,
    cachedSearch,
    completions = engine.CompletePrefix("aut", maxResults: 5),
    correction = new
    {
        input = "serch",
        suggestion = engine.SuggestCorrection("serch", maximumDistance: 2)
    },
    // V1 的布尔 AND 搜索继续保留，便于逐步学习；下面三项展示 V2 为什么需要位置索引和 BM25。
    // BM25 用词频、文档频率和长度归一化排序；短语/邻近查询则用倒排项中的位置列表验证词序与间距。
    bm25 = engine.SearchBm25("search trie", maxResults: 3),
    phrase = engine.SearchPhrase("inverted search index", maxResults: 3),
    proximity = engine.SearchProximity("search", "cache", maximumGap: 4, maxResults: 3)
};

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};
var traceJsonOptions = new JsonSerializerOptions(jsonOptions)
{
    // 默认演示的字段形状不变；只有机器可读的追踪协议统一为严格 camelCase。
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

switch (traceFormat)
{
    case null:
        Console.WriteLine(JsonSerializer.Serialize(output, jsonOptions));
        break;
    case "json":
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            result = output,
            trace = trace!.Events
        }, traceJsonOptions));
        break;
    case "mermaid":
        Console.Write(MermaidTraceRenderer.Render(trace!.Events));
        break;
}

return 0;
