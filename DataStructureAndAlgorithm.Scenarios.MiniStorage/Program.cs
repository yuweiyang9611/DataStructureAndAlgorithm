using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Scenarios.MiniStorage;

Console.OutputEncoding = Encoding.UTF8;

var traceFormat = ParseTraceFormat(args);
if (traceFormat == TraceFormat.Invalid)
{
    Console.Error.WriteLine(
        "用法: dotnet run --project DataStructureAndAlgorithm.Scenarios.MiniStorage -- [--trace json|mermaid]");
    return 1;
}

var trace = traceFormat == TraceFormat.None ? null : new CollectingAlgorithmTraceSink();
var walPath = Path.Combine(Path.GetTempPath(), $"mini-storage-{Guid.NewGuid():N}.wal");

try
{
    object result;
    StorageStatistics beforeRestart;
    IReadOnlyList<StorageEntry> activeUsers;

    var options = new MiniStorageOptions(
        BPlusTreeOrder: 4,
        BloomBitCount: 512,
        BloomHashFunctionCount: 4,
        CacheCapacity: 2,
        WriteAheadLogPath: walPath);

    // 第一段模拟正常服务：WAL 先落盘，索引再更新；重复读取 user:001 会提高它的 LFU 频率。
    using (var engine = new MiniStorageEngine(options, trace))
    {
        engine.Put("user:001", "Ada");
        engine.Put("user:002", "Edsger");
        engine.Put("user:003", "Grace");
        engine.TryGet("user:001", out _);
        engine.TryGet("user:001", out _);
        engine.Delete("user:002");

        activeUsers = engine.RangeScan("user:000", "user:999");
        beforeRestart = engine.GetStatistics();
    }

    // 第二段重新构造引擎，证明结果来自 WAL 回放，而不是仍然存活的旧对象。
    using (var recovered = new MiniStorageEngine(options, trace))
    {
        var adaRecovered = recovered.TryGet("user:001", out var ada);
        var deletedKeyRecovered = recovered.TryGet("user:002", out _);
        var unknownKeyFound = recovered.TryGet("user:999", out _);

        result = new
        {
            scenario = "嵌入式用户资料键值库：索引、缓存、概率预筛与崩溃恢复",
            activeUsers,
            recovery = new
            {
                adaRecovered,
                ada,
                deletedKeyRecovered,
                unknownKeyFound
            },
            beforeRestart,
            afterRestart = recovered.GetStatistics(),
            indexInvariantsValid = recovered.HasValidIndexInvariants()
        };
    }

    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    var traceJsonOptions = new JsonSerializerOptions(jsonOptions)
    {
        // --trace json 是跨场景统一的机器协议；默认教学输出继续沿用原字段命名。
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    switch (traceFormat)
    {
        case TraceFormat.None:
            Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
            break;
        case TraceFormat.Json:
            Console.WriteLine(JsonSerializer.Serialize(new { result, trace = trace!.Events }, traceJsonOptions));
            break;
        case TraceFormat.Mermaid:
            Console.Write(MermaidTraceRenderer.Render(trace!.Events));
            break;
    }

    return 0;
}
finally
{
    // Demo 的 WAL 只用于一次可复现演示；真实应用应把路径指向持久数据目录并保留文件。
    File.Delete(walPath);
}

static TraceFormat ParseTraceFormat(string[] arguments)
{
    if (arguments.Length == 0)
    {
        return TraceFormat.None;
    }

    if (arguments is not ["--trace", var requested])
    {
        return TraceFormat.Invalid;
    }

    return requested.ToLowerInvariant() switch
    {
        "json" => TraceFormat.Json,
        "mermaid" => TraceFormat.Mermaid,
        _ => TraceFormat.Invalid
    };
}

enum TraceFormat
{
    None,
    Json,
    Mermaid,
    Invalid
}
