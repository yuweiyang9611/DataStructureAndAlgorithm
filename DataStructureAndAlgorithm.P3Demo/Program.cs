using System.Text.Encodings.Web;
using System.Text.Json;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Graph;

var scenario = args.FirstOrDefault()?.ToLowerInvariant() ?? "all";
var format = args.Skip(1).FirstOrDefault()?.ToLowerInvariant() ?? "json";
var supportedScenarios = new HashSet<string>(["all", "gcd", "matrix-chain", "dinic"]);
if (!supportedScenarios.Contains(scenario) || format is not ("json" or "mermaid"))
{
    Console.Error.WriteLine("用法: dotnet run --project DataStructureAndAlgorithm.P3Demo -- [all|gcd|matrix-chain|dinic] [json|mermaid]");
    return 1;
}

var scenarios = scenario == "all" ? supportedScenarios.Where(value => value != "all") : [scenario];
var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

foreach (var current in scenarios)
{
    var trace = new CollectingAlgorithmTraceSink();
    object result = current switch
    {
        "gcd" => new { gcd = P3TraceScenarios.GreatestCommonDivisor(1_071, 462, trace) },
        "matrix-chain" => new { minimumCost = P3TraceScenarios.MinimumMatrixChainMultiplications([40, 20, 30, 10, 30], trace) },
        "dinic" => RunDinic(trace),
        _ => throw new UnreachableException()
    };

    Console.WriteLine(format == "mermaid"
        ? MermaidTraceRenderer.Render(trace.Events)
        : JsonSerializer.Serialize(new { scenario = current, result, trace = trace.Events }, jsonOptions));
}

return 0;

static object RunDinic(IAlgorithmTraceSink trace)
{
    var network = new FlowNetwork<string>();
    network.AddEdge("s", "a", 3);
    network.AddEdge("s", "b", 2);
    network.AddEdge("a", "b", 1);
    network.AddEdge("a", "t", 2);
    network.AddEdge("b", "t", 3);
    var result = P3FlowAlgorithms.DinicMaximumFlowWithMinimumCut(network, "s", "t", trace);
    return new { result.Value, result.SourceSideMinimumCut, result.Edges };
}
