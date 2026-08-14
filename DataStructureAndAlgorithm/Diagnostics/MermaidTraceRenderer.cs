using System.Text;

namespace DataStructureAndAlgorithm.Diagnostics;

/// <summary>把线性的算法追踪事件渲染成 Mermaid 流程图，便于嵌入 Markdown 学习笔记。</summary>
public static class MermaidTraceRenderer
{
    public static string Render(IReadOnlyList<AlgorithmTraceEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var builder = new StringBuilder("flowchart TD");
        builder.AppendLine();
        for (var index = 0; index < events.Count; index++)
        {
            var traceEvent = events[index];
            // State 的语义是“某一步的状态快照”，字典的枚举顺序不应该影响最终学习资料。
            // 按 Ordinal 排序可以让相同事件在不同运行环境中生成完全一致的 Mermaid，
            // 既便于读者比较前后两次追踪，也避免文档和快照测试产生无意义的差异。
            var state = string.Join(", ", traceEvent.State
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value}"));
            var label = Escape($"{traceEvent.Step}. {traceEvent.Algorithm}/{traceEvent.Operation}<br/>{traceEvent.Description}<br/>{state}");
            builder.Append("    S").Append(index).Append("[\"").Append(label).AppendLine("\"]");
            if (index > 0) builder.Append("    S").Append(index - 1).Append(" --> S").Append(index).AppendLine();
        }
        return builder.ToString();
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal)
        .Replace("\r", string.Empty, StringComparison.Ordinal)
        .Replace("\n", "<br/>", StringComparison.Ordinal);
}
