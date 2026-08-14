using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataStructureAndAlgorithm.Test.QualityTest;

/// <summary>
/// 从进程边界验证四个综合项目的命令行契约。
/// </summary>
/// <remarks>
/// 单元测试只能证明某个类会返回正确对象，却不能发现入口程序把诊断文本混进 JSON、
/// 忘记转发退出码或在不同操作系统上输出两个换行等集成问题。这里直接运行已编译 DLL，
/// 不在测试内部再次构建项目，因此既覆盖真实 CLI，又避免并行执行时争用 obj 目录。
/// </remarks>
public sealed class ScenarioCliContractTests
{
    private static readonly string[] ExpectedTraceEventProperties =
    [
        "algorithm",
        "description",
        "operation",
        "schemaVersion",
        "state",
        "step"
    ];

    public static TheoryData<string> ScenarioProjects => new()
    {
        "DataStructureAndAlgorithm.Scenarios.CityDelivery",
        "DataStructureAndAlgorithm.Scenarios.MiniSearch",
        "DataStructureAndAlgorithm.Scenarios.MiniStorage",
        "DataStructureAndAlgorithm.Scenarios.ProjectScheduling"
    };

    [Theory]
    [MemberData(nameof(ScenarioProjects))]
    public async Task ScenarioCli_KeepsDefaultTraceAndErrorContracts(string projectName)
    {
        var defaultRun = await RunScenarioAsync(projectName);
        Assert.Equal(0, defaultRun.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(defaultRun.StandardError), defaultRun.StandardError);
        AssertDefaultOutput(projectName, defaultRun.StandardOutput);

        var jsonRun = await RunScenarioAsync(projectName, "--trace", "json");
        Assert.Equal(0, jsonRun.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(jsonRun.StandardError), jsonRun.StandardError);
        AssertTraceEnvelope(jsonRun.StandardOutput);

        var mermaidRun = await RunScenarioAsync(projectName, "--trace", "mermaid");
        Assert.Equal(0, mermaidRun.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(mermaidRun.StandardError), mermaidRun.StandardError);
        AssertMermaidDocument(mermaidRun.StandardOutput);

        var invalidRun = await RunScenarioAsync(projectName, "--trace", "yaml");
        Assert.Equal(1, invalidRun.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(invalidRun.StandardOutput), invalidRun.StandardOutput);
        Assert.False(string.IsNullOrWhiteSpace(invalidRun.StandardError));
    }

    private static void AssertDefaultOutput(string projectName, string output)
    {
        Assert.False(string.IsNullOrWhiteSpace(output));

        if (StringComparer.Ordinal.Equals(projectName, "DataStructureAndAlgorithm.Scenarios.CityDelivery"))
        {
            // 城市配送的默认模式历史上先输出教学标题再输出 JSON。这里显式验证这个既有契约，
            // 不用“寻找第一个 {”掩盖任意前缀；机器协议 --trace json 则必须整段都是 JSON。
            Assert.StartsWith($"城市即时配送调度示例：{Environment.NewLine}{{", output, StringComparison.Ordinal);
            return;
        }

        // 另外三个默认模式原本就是纯 JSON，整段解析可以发现前后混入的诊断文本。
        using var defaultJson = JsonDocument.Parse(output);
        Assert.Equal(JsonValueKind.Object, defaultJson.RootElement.ValueKind);
    }

    private static void AssertTraceEnvelope(string output)
    {
        // 必须把进程的整段标准输出交给解析器。若入口在 JSON 前输出标题或在后面追加日志，
        // JsonDocument.Parse 会直接失败，测试不会再像“从第一个 { 截取”那样掩盖协议污染。
        using var json = JsonDocument.Parse(output);
        var root = json.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);

        // 字段名区分大小写且集合固定；不能先转小写，否则 Result/Trace 这样的破坏性变更也会通过。
        var propertyNames = root.EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] expectedRootProperties = ["result", "trace"];
        Assert.True(
            propertyNames.SequenceEqual(expectedRootProperties, StringComparer.Ordinal),
            $"追踪 JSON 顶层必须且只能包含小写 result、trace；实际为：{string.Join(", ", propertyNames)}");

        var result = root.GetProperty("result");
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
        AssertCamelCasePropertyNames(result, "$.result");

        var trace = root.GetProperty("trace");
        Assert.Equal(JsonValueKind.Array, trace.ValueKind);
        Assert.NotEmpty(trace.EnumerateArray());

        var expectedStep = 1;
        foreach (var traceEvent in trace.EnumerateArray())
        {
            AssertTraceEvent(traceEvent, expectedStep);
            expectedStep++;
        }
    }

    private static void AssertTraceEvent(JsonElement traceEvent, int expectedStep)
    {
        Assert.Equal(JsonValueKind.Object, traceEvent.ValueKind);

        var propertyNames = traceEvent.EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.True(
            propertyNames.SequenceEqual(ExpectedTraceEventProperties, StringComparer.Ordinal),
            "每个追踪事件必须且只能包含小写 step、algorithm、operation、description、state、schemaVersion；" +
            $"实际为：{string.Join(", ", propertyNames)}");

        var step = traceEvent.GetProperty("step");
        Assert.Equal(JsonValueKind.Number, step.ValueKind);
        Assert.Equal(expectedStep, step.GetInt32());

        AssertNonEmptyString(traceEvent, "algorithm");
        AssertNonEmptyString(traceEvent, "operation");

        var description = traceEvent.GetProperty("description");
        Assert.Equal(JsonValueKind.String, description.ValueKind);
        Assert.NotNull(description.GetString());

        var state = traceEvent.GetProperty("state");
        Assert.Equal(JsonValueKind.Object, state.ValueKind);
        foreach (var stateEntry in state.EnumerateObject())
        {
            Assert.Equal(JsonValueKind.String, stateEntry.Value.ValueKind);
        }

        // 版本属于每个事件而不是外层包装：事件被单独写入日志或消息队列后仍可自描述。
        var schemaVersion = traceEvent.GetProperty("schemaVersion");
        Assert.Equal(JsonValueKind.Number, schemaVersion.ValueKind);
        Assert.Equal(1, schemaVersion.GetInt32());
    }

    private static void AssertNonEmptyString(JsonElement element, string propertyName)
    {
        var property = element.GetProperty(propertyName);
        Assert.Equal(JsonValueKind.String, property.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(property.GetString()));
    }

    private static void AssertCamelCasePropertyNames(JsonElement element, string path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var expectedName = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
                    Assert.True(
                        StringComparer.Ordinal.Equals(expectedName, property.Name),
                        $"{path}.{property.Name} 必须使用 camelCase，期望字段名为 {expectedName}。");
                    AssertCamelCasePropertyNames(property.Value, $"{path}.{property.Name}");
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    AssertCamelCasePropertyNames(item, $"{path}[{index}]");
                    index++;
                }

                break;
        }
    }

    private static void AssertMermaidDocument(string output)
    {
        Assert.StartsWith("flowchart TD", output, StringComparison.Ordinal);

        // MermaidTraceRenderer 使用平台原生换行；只约束“恰好一个结尾换行”，
        // 从而同时兼容 Linux 的 LF 和 Windows 的 CRLF，又能发现多输出一个空行的回归。
        var newLine = Environment.NewLine;
        Assert.EndsWith(newLine, output, StringComparison.Ordinal);
        Assert.False(output[..^newLine.Length].EndsWith(newLine, StringComparison.Ordinal));
        Assert.Contains("[\"", output, StringComparison.Ordinal);
    }

    private static async Task<ProcessResult> RunScenarioAsync(string projectName, params string[] arguments)
    {
        var repositoryRoot = FindRepositoryRoot();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name
            ?? throw new InvalidOperationException("无法从测试输出目录判断构建配置。");
        var scenarioDll = Path.Combine(
            repositoryRoot,
            projectName,
            "bin",
            configuration,
            "net10.0",
            $"{projectName}.dll");

        Assert.True(File.Exists(scenarioDll), $"场景程序集尚未构建：{scenarioDll}");

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(scenarioDll);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"无法启动场景进程：{projectName}");
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        // 超时不仅要让测试失败，还要清理整棵进程树；否则异常场景可能占住 CI Runner，
        // 并让后续测试得到与当前用例无关的随机失败。
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            throw new TimeoutException($"场景进程在 30 秒内未退出：{projectName}");
        }

        return new ProcessResult(
            process.ExitCode,
            await standardOutputTask,
            await standardErrorTask);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DataStructureAndAlgorithm.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("无法从测试输出目录向上找到仓库根目录。");
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
