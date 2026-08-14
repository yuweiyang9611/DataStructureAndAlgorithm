using DataStructureAndAlgorithm.Scenarios.ProjectScheduling;

namespace DataStructureAndAlgorithm.Test.IntegratedProjectsTest;

public class ProjectSchedulerTests
{
    [Fact]
    public void CreateSchedule_ShouldProduceDeterministicTopologicalOrderAndCriticalPath()
    {
        // why：合法拓扑序可能不唯一。综合示例必须固定平局规则，否则同一输入在教学演示、
        // JSON 快照和 CI 中可能产生不同结果，让读者误以为算法不稳定。
        WorkItem[] items =
        [
            new("D", 1, 1, ["B", "C"]),
            new("C", 4, 1, ["A"]),
            new("B", 4, 1, ["A"]),
            new("A", 2, 1, [])
        ];

        var schedule = new ProjectScheduler(capacity: 2).CreateSchedule(items);

        Assert.Equal(["A", "B", "C", "D"], schedule.TopologicalOrder);
        // B 与 C 的剩余路径长度相同，按标识选择 B，使关键路径也具有确定性。
        Assert.Equal(["A", "B", "D"], schedule.CriticalPath);
        Assert.Equal(7, schedule.Makespan);
        Assert.Equal(2, schedule.PeakResourceUsage);
    }

    [Fact]
    public void CreateSchedule_ShouldRespectEveryDependencyAndCapacityAtEveryTimeSlot()
    {
        // why：只检查 makespan 会漏掉两类严重错误：消费者提前开始，以及某个中间时间槽超容量。
        // 因此测试从公开结果反向验证两个核心不变量，而不是复制调度器的启发式过程。
        WorkItem[] items =
        [
            new("A", 3, 2, []),
            new("B", 2, 2, []),
            new("C", 2, 1, ["A"]),
            new("D", 1, 3, ["B", "C"])
        ];
        const int capacity = 3;

        var schedule = new ProjectScheduler(capacity).CreateSchedule(items);
        var byId = schedule.WorkItems.ToDictionary(item => item.Id, StringComparer.Ordinal);

        foreach (var item in items)
        {
            foreach (var dependency in item.Dependencies)
            {
                Assert.True(
                    byId[dependency].End <= byId[item.Id].Start,
                    $"Expected dependency {dependency} to finish before {item.Id} starts.");
            }
        }

        for (var time = 0; time < schedule.Makespan; time++)
        {
            var usage = schedule.WorkItems
                .Where(item => item.Start <= time && time < item.End)
                .Sum(item => item.ResourceDemand);
            Assert.InRange(usage, 0, capacity);
        }

        Assert.Equal(capacity, schedule.PeakResourceUsage);
    }

    [Fact]
    public void CreateSchedule_WhenIndependentItemsFitCapacity_ShouldRunThemInParallel()
    {
        // why：调度器不能因为按堆逐个取出工作项，就退化为串行执行。线段树查询的是整段峰值，
        // 两个独立任务合计未超容量时应占用同一个半开时间区间 [0, 3)。
        WorkItem[] items =
        [
            new("B", 3, 1, []),
            new("A", 3, 1, [])
        ];

        var schedule = new ProjectScheduler(capacity: 2).CreateSchedule(items);

        Assert.All(schedule.WorkItems, item => Assert.Equal(0, item.Start));
        Assert.All(schedule.WorkItems, item => Assert.Equal(3, item.End));
        Assert.Equal(3, schedule.Makespan);
        Assert.Equal(2, schedule.PeakResourceUsage);
    }

    [Fact]
    public void CreateSchedule_WhenDependencyGraphContainsCycle_ShouldThrow()
    {
        // why：Kahn 过程处理数量不足正是环检测不变量，不能返回一份缺少部分任务的“半成品”计划。
        WorkItem[] items =
        [
            new("A", 1, 1, ["B"]),
            new("B", 1, 1, ["A"])
        ];

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ProjectScheduler(capacity: 1).CreateSchedule(items));

        Assert.Contains("环", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateSchedule_WhenDependencyIsUnknown_ShouldThrowBeforeScheduling()
    {
        // why：未知前置项不能被当成已经完成，否则计划虽然看似可运行，却破坏了领域数据完整性。
        WorkItem[] items = [new("发布", 1, 1, ["不存在的构建任务"])];

        var exception = Assert.Throws<ArgumentException>(
            () => new ProjectScheduler(capacity: 1).CreateSchedule(items));

        Assert.Contains("未知", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateSchedule_WhenSingleItemDemandExceedsCapacity_ShouldThrow()
    {
        // why：单项需求超过总容量时，不存在任何可行时间窗口。应在建图前快速失败，
        // 而不是让区间扫描走完整条时间轴后抛出含糊的内部错误。
        WorkItem[] items = [new("大型链接任务", 2, 3, [])];

        var exception = Assert.Throws<ArgumentException>(
            () => new ProjectScheduler(capacity: 2).CreateSchedule(items));

        Assert.Contains("超过总容量", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("self")]
    [InlineData("duplicate")]
    public void CreateSchedule_WhenDependenciesViolateLocalContract_ShouldThrow(string caseName)
    {
        // why：自依赖是长度为 1 的环，重复依赖会错误增加入度；在加边前给出领域错误比让图结构失败更清楚。
        WorkItem[] items = caseName == "self"
            ? [new("A", 1, 1, ["A"])]
            : [new("A", 1, 1, []), new("B", 1, 1, ["A", "A"])];

        Assert.Throws<ArgumentException>(
            () => new ProjectScheduler(capacity: 1).CreateSchedule(items));
    }
}
