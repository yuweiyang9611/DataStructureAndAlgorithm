// 迷你搜索引擎 V2：倒排索引打分，Trie 补全前缀，编辑距离纠错，LRU 缓存记住查过的词序无关查询。
// 多加一篇文档，或者把查询词换成只出现一次的词，看命中和分数。
using DataStructureAndAlgorithm.Scenarios.MiniSearch;

public static class Demo
{
    public static void Run()
    {
        var engine = new MiniSearchEngineV2(queryCacheCapacity: 4);
        engine.AddDocument(new SearchDocument(101, "Trie 自动补全",
            "A trie shares common prefixes. A search box can use trie autocomplete without scanning every word.",
            Popularity: 12, Keywords: ["trie", "prefix", "autocomplete"]));
        engine.AddDocument(new SearchDocument(102, "哈希索引与 LRU 缓存",
            "An inverted search index maps each term to postings. An LRU cache keeps hot query results.",
            Popularity: 18, Keywords: ["hash", "index", "search", "cache"]));
        engine.AddDocument(new SearchDocument(103, "多模式字符串匹配",
            "Aho Corasick scans text once and highlights several search terms, including trie and cache.",
            Popularity: 8, Keywords: ["aho", "corasick", "pattern", "search"]));
        Console.WriteLine($"索引了 {engine.DocumentCount} 篇文档");

        var first = engine.Search("search trie", maxResults: 3);
        Console.WriteLine($"查询“search trie”：{first.Hits.Count} 条命中，来自缓存 {first.FromCache}");
        foreach (var hit in first.Hits)
        {
            Console.WriteLine($"  #{hit.DocumentId} {hit.Title}（分数 {hit.Score}，命中词 {string.Join("/", hit.MatchedTerms)}，高亮 {hit.Highlights.Count} 处）");
        }
        var again = engine.Search("trie search", maxResults: 3);
        Console.WriteLine($"再查“trie search”：来自缓存 {again.FromCache}，真正计算过 {engine.SearchComputationCount} 次");

        Console.WriteLine($"前缀 aut 的补全：{string.Join(", ", engine.CompletePrefix("aut", maxResults: 5))}");
        Console.WriteLine($"拼写纠错 serch -> {engine.SuggestCorrection("serch") ?? "无建议"}，cach -> {engine.SuggestCorrection("cach") ?? "无建议"}");

        var bm25 = engine.SearchBm25("cache", maxResults: 2);
        Console.WriteLine($"BM25 查询“cache”（k1 = {bm25.K1}，b = {bm25.B}）：{string.Join("；", bm25.Hits.Select(hit => $"#{hit.DocumentId} {hit.Score:0.000}"))}");
    }
}
