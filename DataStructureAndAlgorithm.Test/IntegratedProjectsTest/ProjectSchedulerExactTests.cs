using DataStructureAndAlgorithm.Diagnostics;
using DataStructureAndAlgorithm.Scenarios.ProjectScheduling;

namespace DataStructureAndAlgorithm.Test.IntegratedProjectsTest;

/// <summary>
/// ProjectScheduling V2 的精确求解、退化性质、确定性与 Trace 测试。
/// </summary>
public class ProjectSchedulerExactTests
{
    [Fact]
    public void CompareWithOptimalSchedule_ShouldExposeARealGreedyCounterexample()
    {
        // why：启发式不是“写得更复杂就自然最优”。这个反例中，最长剩余路径优先的贪心计划需要 15 个
        // 时间单位，而重新组合资源窗口后只需 14。保留反例能防止文档或 API 将贪心结果误称为最优解。
        WorkItem[] items =
        [
            new("A", Duration: 1, ResourceDemand: 2, Dependencies: []),
            new("B", Duration: 3, ResourceDemand: 1, Dependencies: []),
            new("C", Duration: 1, ResourceDemand: 1, Dependencies: []),
            new("D", Duration: 2, ResourceDemand: 2, Dependencies: ["A", "B"]),
            new("E", Duration: 5, ResourceDemand: 2, Dependencies: []),
            new("F", Duration: 2, ResourceDemand: 1, Dependencies: ["A", "D", "E"]),
            new("G", Duration: 4, ResourceDemand: 1, Dependencies: [])
        ];

        var comparison = new ProjectScheduler(capacity: 2).CompareWithOptimalSchedule(items);

        Assert.Equal(15, comparison.GreedySchedule.Makespan);
        Assert.Equal(14, comparison.OptimalSchedule.Makespan);
        Assert.Equal(1, comparison.MakespanImprovement);
        Assert.False(comparison.GreedyIsOptimal);
        Assert.Equal(7, comparison.DependencyLowerBound);
        Assert.Equal(13, comparison.ResourceLowerBound);
        Assert.True(comparison.ExploredNodeCount > 1);
        Assert.True(comparison.PrunedBranchCount > 0);
        AssertScheduleIsFeasible(items, comparison.OptimalSchedule, capacity: 2);
    }

    [Fact]
    public void ExactSearch_OnRandomSmallDags_ShouldMatchIndependentBruteForceOracle()
    {
        // why：精确求解器与贪心实现共享领域模型，不能只拿两者互相比较，否则同一种遗漏可能让它们一起出错。
        // 测试 oracle 只用数组逐点占用与完整开始时间枚举，不复用线段树、下界或生产搜索代码。
        var random = new Random(20260718);
        for (var caseIndex = 0; caseIndex < 30; caseIndex++)
        {
            var capacity = random.Next(1, 4);
            var count = random.Next(1, 6);
            var items = new List<WorkItem>(count);
            for (var index = 0; index < count; index++)
            {
                var dependencies = new List<string>();
                for (var candidate = 0; candidate < index; candidate++)
                {
                    if (random.Next(4) == 0)
                    {
                        dependencies.Add(Id(candidate));
                    }
                }

                items.Add(new WorkItem(
                    Id(index),
                    Duration: random.Next(1, 4),
                    ResourceDemand: random.Next(1, capacity + 1),
                    Dependencies: dependencies.AsReadOnly()));
            }

            var scheduler = new ProjectScheduler(capacity);
            var comparison = scheduler.CompareWithOptimalSchedule(items);
            var expectedMakespan = CalculateBruteForceMakespan(items, capacity);

            Assert.Equal(expectedMakespan, comparison.OptimalSchedule.Makespan);
            var optimalOnly = scheduler.CreateOptimalSchedule(items);
            Assert.Equal(comparison.OptimalSchedule.Makespan, optimalOnly.Makespan);
            Assert.Equal(ToComparableItems(comparison.OptimalSchedule), ToComparableItems(optimalOnly));
            AssertScheduleIsFeasible(items, comparison.OptimalSchedule, capacity);
        }
    }

    [Fact]
    public void ExactSearch_ShouldDegenerateToCriticalPathWhenResourcesAreUnlimited()
    {
        WorkItem[] items =
        [
            new("A", 2, 2, []),
            new("B", 4, 1, ["A"]),
            new("C", 3, 3, ["A"]),
            new("D", 1, 1, ["B", "C"])
        ];
        const int capacity = 7;

        var comparison = new ProjectScheduler(capacity).CompareWithOptimalSchedule(items);

        // A -> B -> D 的长度为 7。容量足以让 B、C 并行，因此依赖下界本身就是可行解，根节点即可证明最优。
        Assert.Equal(7, comparison.DependencyLowerBound);
        Assert.Equal(7, comparison.GreedySchedule.Makespan);
        Assert.Equal(7, comparison.OptimalSchedule.Makespan);
        Assert.True(comparison.GreedyIsOptimal);
        Assert.Equal(1, comparison.ExploredNodeCount);
        Assert.Equal(1, comparison.PrunedBranchCount);
    }

    [Fact]
    public void ExactSearch_ShouldHandleEmptyProjectAsTheIdentityCase()
    {
        var comparison = new ProjectScheduler(capacity: 3).CompareWithOptimalSchedule([]);

        Assert.Empty(comparison.GreedySchedule.WorkItems);
        Assert.Same(comparison.GreedySchedule, comparison.OptimalSchedule);
        Assert.Equal(0, comparison.OptimalSchedule.Makespan);
        Assert.Equal(0, comparison.DependencyLowerBound);
        Assert.Equal(0, comparison.ResourceLowerBound);
        Assert.True(comparison.GreedyIsOptimal);
    }

    [Fact]
    public void ExactSearch_ShouldBeIndependentOfInputAndDependencyEnumerationOrder()
    {
        WorkItem[] items =
        [
            new("D", 2, 1, ["B", "A"]),
            new("C", 3, 2, ["A"]),
            new("B", 2, 1, []),
            new("A", 1, 1, [])
        ];
        var scheduler = new ProjectScheduler(capacity: 2);

        var first = scheduler.CompareWithOptimalSchedule(items);
        var second = scheduler.CompareWithOptimalSchedule(
            items.Reverse().Select(item => item with
            {
                Dependencies = item.Dependencies.Reverse().ToArray()
            }));

        Assert.Equal(ToComparableItems(first.GreedySchedule), ToComparableItems(second.GreedySchedule));
        Assert.Equal(ToComparableItems(first.OptimalSchedule), ToComparableItems(second.OptimalSchedule));
        Assert.Equal(first.OptimalSchedule.Makespan, second.OptimalSchedule.Makespan);
        Assert.Equal(first.ExploredNodeCount, second.ExploredNodeCount);
        Assert.Equal(first.PrunedBranchCount, second.PrunedBranchCount);
    }

    [Fact]
    public void ExactApi_ShouldRejectLargeSearchesWithoutRestrictingGreedyApi()
    {
        var tooManyItems = Enumerable.Range(0, ProjectScheduler.ExactWorkItemLimit + 1)
            .Select(index => new WorkItem(Id(index), 1, 1, []))
            .ToArray();
        var tooLong = new[]
        {
            new WorkItem("long", ProjectScheduler.ExactHorizonLimit + 1, 1, [])
        };
        var scheduler = new ProjectScheduler(capacity: 1);

        Assert.Equal(tooManyItems.Length, scheduler.CreateSchedule(tooManyItems).Makespan);
        Assert.Equal(ProjectScheduler.ExactHorizonLimit + 1, scheduler.CreateSchedule(tooLong).Makespan);
        Assert.Throws<ArgumentException>(() => scheduler.CompareWithOptimalSchedule(tooManyItems));
        Assert.Throws<ArgumentException>(() => scheduler.CreateOptimalSchedule(tooLong));
    }

    [Fact]
    public void ExactSearch_WhenNodeBudgetIsExhausted_AbortsInsteadOfReturningAnUnprovenPlan()
    {
        WorkItem[] items =
        [
            new("A", 1, 2, []),
            new("B", 3, 1, []),
            new("C", 1, 1, []),
            new("D", 2, 2, ["A", "B"]),
            new("E", 5, 2, []),
            new("F", 2, 1, ["A", "D", "E"]),
            new("G", 4, 1, [])
        ];
        const int capacity = 2;
        var greedy = new ProjectScheduler(capacity).CreateSchedule(items);
        var trace = new CollectingAlgorithmTraceSink();

        // 生产默认预算仍为 2,000,000。测试只向 internal 优化器注入 1，稳定在第二个状态触发安全阀，
        // 不需要真的消耗数百万节点，也不会把 CI 速度和机器性能混入正确性契约。
        var optimizer = new ExactScheduleOptimizer(capacity, trace, searchNodeLimit: 1);
        var exception = Assert.Throws<InvalidOperationException>(() => optimizer.Compare(items, greedy));

        Assert.Contains("已访问超过 1 个状态节点", exception.Message, StringComparison.Ordinal);
        Assert.True(trace.Events.Select(item => item.Operation).SequenceEqual(
            ["ExactSearchStarted", "ExactSearchAborted"]));
        Assert.Equal("2", trace.Events[^1].State["exploredNodeCount"]);
        Assert.Equal("1", trace.Events[^1].State["nodeLimit"]);
        Assert.DoesNotContain(trace.Events, item => item.Operation == "ExactSearchCompleted");
    }

    [Fact]
    public void ExactSearch_TraceShouldExplainProofWithoutChangingEitherSchedule()
    {
        WorkItem[] items =
        [
            new("A", 2, 1, []),
            new("B", 2, 1, []),
            new("C", 1, 1, ["A", "B"])
        ];
        var expected = new ProjectScheduler(capacity: 2).CompareWithOptimalSchedule(items);
        var trace = new CollectingAlgorithmTraceSink();

        var actual = new ProjectScheduler(capacity: 2, trace).CompareWithOptimalSchedule(items);

        Assert.Equal(ToComparableItems(expected.GreedySchedule), ToComparableItems(actual.GreedySchedule));
        Assert.Equal(ToComparableItems(expected.OptimalSchedule), ToComparableItems(actual.OptimalSchedule));
        Assert.Equal(expected.ExploredNodeCount, actual.ExploredNodeCount);
        Assert.Equal(expected.PrunedBranchCount, actual.PrunedBranchCount);
        Assert.Equal("ExactSearchStarted", trace.Events[^2].Operation);
        Assert.Equal("ExactSearchCompleted", trace.Events[^1].Operation);
        Assert.Equal("true", trace.Events[^1].State["greedyIsOptimal"]);
        Assert.Equal("3", trace.Events[^1].State["optimalMakespan"]);
        Assert.True(trace.Events.Select(item => item.Step).SequenceEqual(Enumerable.Range(1, trace.Events.Count)));
    }

    private static int CalculateBruteForceMakespan(IReadOnlyList<WorkItem> items, int capacity)
    {
        var horizon = items.Sum(item => item.Duration);
        var usage = new int[horizon];
        var endTimes = new int[items.Count];
        var indexById = items
            .Select((item, index) => (item.Id, index))
            .ToDictionary(pair => pair.Id, pair => pair.index, StringComparer.Ordinal);
        var best = horizon;
        Search(index: 0, currentMakespan: 0);
        return best;

        void Search(int index, int currentMakespan)
        {
            if (currentMakespan >= best)
            {
                return;
            }

            if (index == items.Count)
            {
                best = currentMakespan;
                return;
            }

            var item = items[index];
            var earliestStart = item.Dependencies.Count == 0
                ? 0
                : item.Dependencies.Max(dependency => endTimes[indexById[dependency]]);
            for (var start = earliestStart; start + item.Duration < best; start++)
            {
                var end = start + item.Duration;
                var isFeasible = true;
                for (var time = start; time < end; time++)
                {
                    if (usage[time] + item.ResourceDemand <= capacity)
                    {
                        continue;
                    }

                    isFeasible = false;
                    break;
                }

                if (!isFeasible)
                {
                    continue;
                }

                endTimes[index] = end;
                for (var time = start; time < end; time++) usage[time] += item.ResourceDemand;
                Search(index + 1, Math.Max(currentMakespan, end));
                for (var time = start; time < end; time++) usage[time] -= item.ResourceDemand;
            }
        }
    }

    private static void AssertScheduleIsFeasible(
        IReadOnlyList<WorkItem> source,
        ProjectSchedule schedule,
        int capacity)
    {
        var byId = schedule.WorkItems.ToDictionary(item => item.Id, StringComparer.Ordinal);
        Assert.Equal(source.Count, byId.Count);
        foreach (var item in source)
        {
            Assert.Equal(item.Duration, byId[item.Id].End - byId[item.Id].Start);
            Assert.All(
                item.Dependencies,
                dependency => Assert.True(byId[dependency].End <= byId[item.Id].Start));
        }

        for (var time = 0; time < schedule.Makespan; time++)
        {
            var usage = schedule.WorkItems
                .Where(item => item.Start <= time && time < item.End)
                .Sum(item => item.ResourceDemand);
            Assert.InRange(usage, 0, capacity);
        }
    }

    private static IEnumerable<(string Id, int Start, int End, int Demand)>
        ToComparableItems(ProjectSchedule schedule) => schedule.WorkItems.Select(
            item => (item.Id, item.Start, item.End, item.ResourceDemand));

    private static string Id(int index) => $"T{index:00}";
}
