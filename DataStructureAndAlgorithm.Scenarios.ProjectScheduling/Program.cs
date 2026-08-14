using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Scenarios.ProjectScheduling;

Console.OutputEncoding = Encoding.UTF8;

var traceFormat = ParseTraceFormat(args);
if (traceFormat == TraceFormat.Invalid)
{
    Console.Error.WriteLine(
        "用法: dotnet run --project DataStructureAndAlgorithm.Scenarios.ProjectScheduling -- [--trace json|mermaid]");
    return 1;
}

// 应用场景：一条软件发布流水线共享 3 个构建执行单元。
// 工作项既有先后依赖，也有不同的并行资源需求；调度器需要在不突破容量的前提下，
// 优先保护剩余关键路径较长的工作，最后输出可供 CLI、Web API 或可视化前端消费的 JSON。
const int capacity = 3;
WorkItem[] workItems =
[
    new("准备源码", Duration: 2, ResourceDemand: 2, Dependencies: []),
    new("编译", Duration: 4, ResourceDemand: 1, Dependencies: ["准备源码"]),
    new("静态分析", Duration: 3, ResourceDemand: 2, Dependencies: ["准备源码"]),
    new("单元测试", Duration: 2, ResourceDemand: 2, Dependencies: ["编译"]),
    new("打包", Duration: 1, ResourceDemand: 1, Dependencies: ["编译", "静态分析"]),
    new("发布", Duration: 2, ResourceDemand: 3, Dependencies: ["单元测试", "打包"])
];

var trace = traceFormat == TraceFormat.None ? null : new CollectingAlgorithmTraceSink();
var scheduler = new ProjectScheduler(capacity, trace);
var schedule = scheduler.CreateSchedule(workItems);
var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

var output = new
{
    scenario = "软件发布流水线",
    capacity,
    input = workItems,
    schedule
};

if (traceFormat == TraceFormat.Mermaid)
{
    Console.Write(MermaidTraceRenderer.Render(trace!.Events));
    return 0;
}

Console.WriteLine(traceFormat == TraceFormat.Json
    ? JsonSerializer.Serialize(new { result = output, trace = trace!.Events }, jsonOptions)
    : JsonSerializer.Serialize(output, jsonOptions));

return 0;

static TraceFormat ParseTraceFormat(string[] arguments)
{
    if (arguments.Length == 0) return TraceFormat.None;
    if (arguments.Length != 2 || !StringComparer.Ordinal.Equals(arguments[0], "--trace"))
    {
        return TraceFormat.Invalid;
    }

    return arguments[1].ToLowerInvariant() switch
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
