using System.Globalization;
using DataStructureAndAlgorithm.Diagnostics;

namespace DataStructureAndAlgorithm.Graph;

/// <summary>带最小割恢复的 Dinic 结果。</summary>
public sealed record P3MaximumFlowResult<TVertex>(
    double Value,
    IReadOnlyList<FlowEdgeResult<TVertex>> Edges,
    IReadOnlyList<TVertex> SourceSideMinimumCut)
    where TVertex : notnull;

/// <summary>可选追踪的最大流算法；用于观察层次图、增广流与最终最小割。</summary>
public static class P3FlowAlgorithms
{
    /// <summary>
    /// Dinic 用 BFS 构建层次图，再用 DFS 发送阻塞流。追踪器为 null 时仅有一次空值判断，
    /// 不要求核心算法依赖控制台或 UI。一般网络复杂度 O(V²E)。
    /// </summary>
    public static P3MaximumFlowResult<TVertex> DinicMaximumFlowWithMinimumCut<TVertex>(
        FlowNetwork<TVertex> network,
        TVertex source,
        TVertex sink,
        IAlgorithmTraceSink? trace = null)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(network);
        var residual = network.Clone();
        if (!residual.Adjacency.ContainsKey(source)) throw new ArgumentException("The source vertex does not exist.", nameof(source));
        if (!residual.Adjacency.ContainsKey(sink)) throw new ArgumentException("The sink vertex does not exist.", nameof(sink));
        if (residual.Comparer.Equals(source, sink)) throw new ArgumentException("Source and sink must be different.");

        var level = new Dictionary<TVertex, int>(residual.Comparer);
        var phase = 0;

        bool BuildLevelGraph()
        {
            level.Clear();
            var queue = new Queue<TVertex>();
            level[source] = 0;
            queue.Enqueue(source);
            while (queue.TryDequeue(out var current))
            {
                foreach (var edge in residual.Adjacency[current])
                {
                    if (edge.ResidualCapacity <= 0 || level.ContainsKey(edge.To)) continue;
                    level[edge.To] = level[current] + 1;
                    queue.Enqueue(edge.To);
                }
            }

            phase++;
            trace?.Record("Dinic", "BuildLevelGraph", "BFS 只沿正残量边建立严格递增的层次。",
                new Dictionary<string, string>
                {
                    ["phase"] = phase.ToString(CultureInfo.InvariantCulture),
                    ["reachableVertices"] = level.Count.ToString(CultureInfo.InvariantCulture),
                    ["sinkLevel"] = level.TryGetValue(sink, out var sinkLevel)
                        ? sinkLevel.ToString(CultureInfo.InvariantCulture)
                        : "unreachable"
                });
            return level.ContainsKey(sink);
        }

        double Send(TVertex current, double available, Dictionary<TVertex, int> nextEdge)
        {
            if (residual.Comparer.Equals(current, sink)) return available;
            var edges = residual.Adjacency[current];
            while (nextEdge[current] < edges.Count)
            {
                var edge = edges[nextEdge[current]];
                if (edge.ResidualCapacity > 0 && level.TryGetValue(edge.To, out var nextLevel) &&
                    nextLevel == level[current] + 1)
                {
                    var sent = Send(edge.To, Math.Min(available, edge.ResidualCapacity), nextEdge);
                    if (sent > 0)
                    {
                        edge.ResidualCapacity -= sent;
                        residual.Adjacency[edge.To][edge.ReverseIndex].ResidualCapacity += sent;
                        return sent;
                    }
                }
                nextEdge[current]++;
            }
            return 0;
        }

        var maximumFlow = 0d;
        while (BuildLevelGraph())
        {
            var nextEdge = residual.Vertices.ToDictionary(vertex => vertex, _ => 0, residual.Comparer);
            double sent;
            while ((sent = Send(source, double.PositiveInfinity, nextEdge)) > 0)
            {
                maximumFlow += sent;
                trace?.Record("Dinic", "Augment", "沿层次图发送流，并同步更新正向与反向残量。",
                    new Dictionary<string, string>
                    {
                        ["sent"] = sent.ToString("G17", CultureInfo.InvariantCulture),
                        ["total"] = maximumFlow.ToString("G17", CultureInfo.InvariantCulture)
                    });
            }
        }

        var sourceSide = new HashSet<TVertex>(residual.Comparer) { source };
        var reachable = new Queue<TVertex>();
        reachable.Enqueue(source);
        while (reachable.TryDequeue(out var current))
        {
            foreach (var edge in residual.Adjacency[current])
            {
                if (edge.ResidualCapacity > 0 && sourceSide.Add(edge.To)) reachable.Enqueue(edge.To);
            }
        }

        trace?.Record("Dinic", "MinimumCut", "无增广路后，残量网络中源点可达集合就是最小割源侧。",
            new Dictionary<string, string>
            {
                ["maximumFlow"] = maximumFlow.ToString("G17", CultureInfo.InvariantCulture),
                ["sourceSideCount"] = sourceSide.Count.ToString(CultureInfo.InvariantCulture)
            });
        var flows = residual.Adjacency.Values.SelectMany(value => value)
            .Where(edge => edge.IsOriginal)
            .Select(edge => new FlowEdgeResult<TVertex>(edge.From, edge.To, edge.OriginalCapacity,
                edge.OriginalCapacity - edge.ResidualCapacity))
            .ToArray();
        return new P3MaximumFlowResult<TVertex>(maximumFlow, flows, sourceSide.ToArray());
    }
}
