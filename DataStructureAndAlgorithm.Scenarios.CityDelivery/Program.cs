using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Scenarios.CityDelivery;

Console.OutputEncoding = Encoding.UTF8;

// 无参数继续保持原来的 JSON 教学演示；显式 --trace 才收集事件，避免默认执行产生额外对象。
string? traceFormat = null;
if (args.Length != 0)
{
    if (args is ["--trace", var requestedFormat])
    {
        traceFormat = requestedFormat.ToLowerInvariant();
    }

    if (traceFormat is not ("json" or "mermaid"))
    {
        Console.Error.WriteLine("用法: dotnet run --project DataStructureAndAlgorithm.Scenarios.CityDelivery -- [--trace json|mermaid]");
        return 1;
    }
}

// 使用固定时间而不是 DateTimeOffset.Now，让每次运行都产生完全相同的结果，便于学习、测试与文档演示。
var planningTime = new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);

var scenario = new CityDeliveryScenario(
    MapVersion: 1,
    PlanningTime: planningTime,
    Locations:
    [
        new DeliveryLocation("Depot"),
        new DeliveryLocation("A"),
        new DeliveryLocation("B"),
        new DeliveryLocation("C")
    ],
    Roads:
    [
        new DeliveryRoad("Depot", "A", TravelMinutes: 4, MaintenanceCost: 6),
        new DeliveryRoad("Depot", "B", TravelMinutes: 2, MaintenanceCost: 5),
        new DeliveryRoad("B", "A", TravelMinutes: 1, MaintenanceCost: 2),
        new DeliveryRoad("A", "C", TravelMinutes: 3, MaintenanceCost: 4),
        new DeliveryRoad("B", "C", TravelMinutes: 7, MaintenanceCost: 3)
    ],
    Vehicles:
    [
        new DeliveryVehicle("V-01", "Depot", Capacity: 5, Skills: ["ColdChain"]),
        new DeliveryVehicle("V-02", "B", Capacity: 3, Skills: [])
    ],
    Orders:
    [
        // O-001 只能由冷链车 V-01 承担；最短路 Depot -> B -> A -> C，共 6 分钟。
        new DeliveryOrder(
            "O-001", "C", Volume: 2, Deadline: planningTime.AddMinutes(12),
            Urgency: 5, RequiredSkill: "ColdChain"),
        // O-002 两辆车都可承担，最大匹配会把它留给 V-02，从而同时完成两个订单。
        new DeliveryOrder("O-002", "A", Volume: 1, Deadline: planningTime.AddMinutes(6), Urgency: 3),
        // O-003 超过所有车辆容量，示例结果会给出明确的未分配原因。
        new DeliveryOrder("O-003", "C", Volume: 99, Deadline: planningTime.AddMinutes(30), Urgency: 1)
    ]);

var trace = traceFormat is null ? null : new CollectingAlgorithmTraceSink();
var planner = new CityDeliveryPlanner(routeCacheCapacity: 16, trace);
var plan = planner.Plan(scenario);

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    // 默认编码器会把中文写成 \\uXXXX，虽然 JSON 语义正确，却不利于学习者直接阅读控制台输出。
    // 此示例只把 JSON 输出到终端；若将不可信内容嵌入 HTML，仍应使用默认的安全编码器。
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};
var traceJsonOptions = new JsonSerializerOptions(jsonOptions)
{
    // 追踪输出是供工具消费的稳定协议；统一使用 camelCase，默认教学输出则保持既有格式。
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

if (traceFormat == "mermaid")
{
    // Mermaid 与算法解耦：规划器只发布事件，表现层决定如何把同一批事件用于文档或可视化。
    Console.Write(MermaidTraceRenderer.Render(trace!.Events));
    return 0;
}

if (traceFormat == "json")
{
    Console.WriteLine(JsonSerializer.Serialize(new { result = plan, trace = trace!.Events }, traceJsonOptions));
    return 0;
}

Console.WriteLine("城市即时配送调度示例：");
Console.WriteLine(JsonSerializer.Serialize(plan, jsonOptions));
return 0;
