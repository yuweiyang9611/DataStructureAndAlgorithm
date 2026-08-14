using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Graph;
using DataStructureAndAlgorithm.Sorting;
using DataStructureAndAlgorithm.Tree;

// Demo 项目故意只负责“调用 + 展示”，不把教学输出写进算法内部。
// 这样算法仍可被单元测试、Web API 或桌面程序复用，而 TraceSink 可以按场景替换为 JSON、日志或 UI 动画。
var requestedDemo = args.FirstOrDefault()?.ToLowerInvariant() ?? "all";
var supported = new HashSet<string>(["all", "radix", "dijkstra", "red-black", "huffman"]);
if (!supported.Contains(requestedDemo))
{
    Console.Error.WriteLine("用法: dotnet run --project DataStructureAndAlgorithm.Demo -- [all|radix|dijkstra|red-black|huffman]");
    return 1;
}

// 默认 JSON 编码器会把中文转成 \uXXXX，机器可读但不利于教学观察；Demo 只输出可信的本地算法状态，
// 因此使用较宽松编码保留中文。若把任意用户文本嵌入 HTML，仍应使用默认编码器防止注入。
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};
var demos = requestedDemo == "all"
    ? supported.Where(name => name != "all")
    : [requestedDemo];

foreach (var demo in demos)
{
    var trace = new CollectingAlgorithmTraceSink();
    object result = demo switch
    {
        "radix" => RunRadix(trace),
        "dijkstra" => RunDijkstra(trace),
        "red-black" => RunRedBlackTree(trace),
        "huffman" => RunHuffman(trace),
        _ => throw new UnreachableException()
    };

    Console.WriteLine(JsonSerializer.Serialize(new { demo, result, trace = trace.Events }, options));
}

return 0;

static object RunRadix(IAlgorithmTraceSink trace)
{
    int[] values = [170, -45, 75, -90, 802, 24, 2, 66];
    NonComparisonSortAlgorithms.RadixSort(values, trace);
    return new { sorted = values };
}

static object RunDijkstra(IAlgorithmTraceSink trace)
{
    var graph = new WeightedGraph<string>();
    graph.AddEdge("A", "B", 4);
    graph.AddEdge("A", "C", 1);
    graph.AddEdge("C", "B", 2);
    graph.AddEdge("B", "D", 1);
    graph.AddEdge("C", "D", 5);
    var result = WeightedGraphAlgorithms.Dijkstra(graph, "A", trace);
    return new { distance = result.Distances["D"], path = result.GetPathTo("D") };
}

static object RunRedBlackTree(IAlgorithmTraceSink trace)
{
    var tree = new RedBlackTree<int>(trace: trace);
    foreach (var value in new[] { 30, 10, 20, 40, 50, 5 }) tree.Add(value);
    tree.Remove(30);
    return new { values = tree.ToArray(), tree.Count, tree.Height, valid = tree.HasValidInvariants() };
}

static object RunHuffman(IAlgorithmTraceSink trace)
{
    var tree = new HuffmanTree<char>();
    var codes = tree.CalculateHuffmanCode(
        new Dictionary<char, uint> { ['A'] = 5, ['B'] = 2, ['C'] = 1, ['D'] = 1 }, trace);
    var bits = tree.Encode("ABCD");
    return new { codes, bits, decoded = new string(tree.Decode(bits).ToArray()) };
}
