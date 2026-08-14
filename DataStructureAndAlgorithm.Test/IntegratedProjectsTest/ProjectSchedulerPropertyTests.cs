using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Scenarios.ProjectScheduling;
using FsCheck.Xunit;

namespace DataStructureAndAlgorithm.Test.IntegratedProjectsTest;

/// <summary>
/// 项目调度的性质测试。
/// </summary>
/// <remarks>
/// 普通示例只能证明某一张 DAG 的输出符合预期；这里把任意整数数组解释成一张小型 DAG，
/// 让 FsCheck 反复验证依赖、资源容量和关键路径不变量。节点数与时长刻意受限，既能覆盖大量形状，
/// 又避免失败缩减时被无关的大输入拖慢。
/// </remarks>
public class ProjectSchedulerPropertyTests
{
    [Property(MaxTest = 75)]
    public bool EverySchedule_RespectsDependenciesCapacityAndPublishedAggregates(int[]? raw)
    {
        var generated = BuildCase(raw);
        var schedule = new ProjectScheduler(generated.Capacity).CreateSchedule(generated.Items);
        var sourceById = generated.Items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var scheduledById = schedule.WorkItems.ToDictionary(item => item.Id, StringComparer.Ordinal);

        if (scheduledById.Count != sourceById.Count ||
            !scheduledById.Keys.Order(StringComparer.Ordinal).SequenceEqual(sourceById.Keys.Order(StringComparer.Ordinal)))
        {
            return false;
        }

        foreach (var item in generated.Items)
        {
            var actual = scheduledById[item.Id];
            if (actual.End - actual.Start != item.Duration ||
                actual.ResourceDemand != item.ResourceDemand ||
                actual.Start < 0 ||
                item.Dependencies.Any(dependency => scheduledById[dependency].End > actual.Start))
            {
                return false;
            }
        }

        var usage = new int[schedule.Makespan];
        foreach (var item in schedule.WorkItems)
        {
            for (var time = item.Start; time < item.End; time++)
            {
                usage[time] += item.ResourceDemand;
            }
        }

        var positions = schedule.TopologicalOrder
            .Select((id, index) => (id, index))
            .ToDictionary(pair => pair.id, pair => pair.index, StringComparer.Ordinal);
        var topologicalOrderIsValid = positions.Count == generated.Items.Count && generated.Items.All(
            item => item.Dependencies.All(dependency => positions[dependency] < positions[item.Id]));

        return topologicalOrderIsValid &&
               usage.All(value => value <= generated.Capacity) &&
               schedule.PeakResourceUsage == usage.Max() &&
               schedule.Makespan == schedule.WorkItems.Max(item => item.End) &&
               schedule.Capacity == generated.Capacity;
    }

    [Property(MaxTest = 75)]
    public bool Schedule_IsIndependentOfInputAndDependencyEnumerationOrder(int[]? raw)
    {
        var generated = BuildCase(raw);
        var first = new ProjectScheduler(generated.Capacity).CreateSchedule(generated.Items);

        // 输入 IEnumerable 的顺序不是领域语义的一部分。反转工作项和依赖列表后结果仍应一致，
        // 否则就说明某个哈希表或图的枚举顺序意外泄漏到了公开 API。
        var permuted = generated.Items
            .Reverse()
            .Select(item => item with { Dependencies = item.Dependencies.Reverse().ToArray() })
            .ToArray();
        var second = new ProjectScheduler(generated.Capacity).CreateSchedule(permuted);

        return first.TopologicalOrder.SequenceEqual(second.TopologicalOrder) &&
               first.CriticalPath.SequenceEqual(second.CriticalPath) &&
               ToComparableItems(first).SequenceEqual(ToComparableItems(second)) &&
               first.Makespan == second.Makespan &&
               first.PeakResourceUsage == second.PeakResourceUsage;
    }

    [Property(MaxTest = 75)]
    public bool UnlimitedResources_ReduceSchedulingToAsSoonAsPossibleCriticalPath(int[]? raw)
    {
        var generated = BuildCase(raw);
        var unlimitedCapacity = generated.Items.Sum(item => item.ResourceDemand);
        var schedule = new ProjectScheduler(unlimitedCapacity).CreateSchedule(generated.Items);
        var actualById = schedule.WorkItems.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var expectedEnd = new Dictionary<string, int>(StringComparer.Ordinal);

        // 生成器只允许 Ti 依赖编号更小的任务，因此按编号递推就是一个与调度器实现无关的
        // ASAP oracle：start[v] = max(end[dependency])，end[v] = start[v] + duration[v]。
        foreach (var item in generated.Items)
        {
            var expectedStart = item.Dependencies.Count == 0
                ? 0
                : item.Dependencies.Max(dependency => expectedEnd[dependency]);
            expectedEnd[item.Id] = expectedStart + item.Duration;
            if (actualById[item.Id].Start != expectedStart)
            {
                return false;
            }
        }

        var expectedMakespan = expectedEnd.Values.Max();
        var sourceById = generated.Items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var criticalDuration = schedule.CriticalPath.Sum(id => sourceById[id].Duration);
        var criticalEdgesAreDependencies = schedule.CriticalPath
            .Zip(schedule.CriticalPath.Skip(1))
            .All(pair => sourceById[pair.Second].Dependencies.Contains(pair.First, StringComparer.Ordinal));

        return criticalEdgesAreDependencies &&
               criticalDuration == expectedMakespan &&
               schedule.Makespan == expectedMakespan;
    }

    [Fact]
    public void CreateSchedule_TraceExplainsStagesWithoutChangingTheResult()
    {
        WorkItem[] items =
        [
            new("A", Duration: 2, ResourceDemand: 1, Dependencies: []),
            new("B", Duration: 1, ResourceDemand: 1, Dependencies: ["A"]),
            new("C", Duration: 2, ResourceDemand: 1, Dependencies: ["A"])
        ];
        var expected = new ProjectScheduler(capacity: 2).CreateSchedule(items);
        var trace = new CollectingAlgorithmTraceSink();

        var actual = new ProjectScheduler(capacity: 2, trace).CreateSchedule(items);

        Assert.Equal(ToComparableItems(expected), ToComparableItems(actual));
        Assert.Equal(expected.TopologicalOrder, actual.TopologicalOrder);
        Assert.Equal(expected.CriticalPath, actual.CriticalPath);
        Assert.Equal(expected.Makespan, actual.Makespan);
        Assert.Equal(expected.PeakResourceUsage, actual.PeakResourceUsage);
        Assert.Equal(expected.Capacity, actual.Capacity);
        Assert.True(trace.Events.Select(item => item.Step).SequenceEqual(Enumerable.Range(1, trace.Events.Count)));
        var expectedOperations = new List<string>
        {
            "ScheduleStarted", "GraphBuilt", "TopologicalSortCompleted", "CriticalPathCalculated"
        };
        expectedOperations.AddRange(Enumerable.Repeat("TaskScheduled", items.Length));
        expectedOperations.Add("ScheduleCompleted");
        Assert.True(trace.Events.Select(item => item.Operation).SequenceEqual(expectedOperations));
    }

    private static GeneratedCase BuildCase(int[]? raw)
    {
        var values = new DeterministicValues(raw);
        var count = 1 + values.Next(7);
        var capacity = 1 + values.Next(4);
        var result = new List<WorkItem>(count);

        for (var index = 0; index < count; index++)
        {
            var dependencies = new List<string>();
            for (var candidate = 0; candidate < index; candidate++)
            {
                if (values.Next(3) == 0)
                {
                    dependencies.Add(Id(candidate));
                }
            }

            result.Add(new WorkItem(
                Id(index),
                Duration: 1 + values.Next(4),
                ResourceDemand: 1 + values.Next(capacity),
                Dependencies: dependencies.AsReadOnly()));
        }

        return new GeneratedCase(capacity, result.AsReadOnly());
    }

    private static IEnumerable<(string Id, int Start, int End, int Demand, int Remaining, bool Critical)>
        ToComparableItems(ProjectSchedule schedule) => schedule.WorkItems.Select(
            item => (item.Id, item.Start, item.End, item.ResourceDemand, item.CriticalRemaining, item.IsOnCriticalPath));

    private static string Id(int index) => $"T{index:00}";

    private sealed record GeneratedCase(int Capacity, IReadOnlyList<WorkItem> Items);

    private sealed class DeterministicValues(int[]? source)
    {
        private readonly int[] _source = source is { Length: > 0 } ? source : [0];
        private int _index;

        public int Next(int exclusiveUpperBound)
        {
            // uint 取模能覆盖 int.MinValue，避免 Math.Abs(int.MinValue) 仍为负数的经典陷阱。
            var value = unchecked((uint)_source[_index++ % _source.Length]);
            return (int)(value % (uint)exclusiveUpperBound);
        }
    }
}
