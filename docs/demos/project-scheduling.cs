// 项目调度 V2：拓扑序决定谁能先开工，关键路径决定最短工期，资源容量决定谁得等。
// 把容量从 3 改成 2，或者把“编译”的时长改成 1，看 makespan 和关键路径怎么变。
using DataStructureAndAlgorithm.Scenarios.ProjectScheduling;

public static class Demo
{
    public static void Run()
    {
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

        var scheduler = new ProjectScheduler(capacity);
        var schedule = scheduler.CreateSchedule(workItems);
        Console.WriteLine($"资源容量 {schedule.Capacity}，拓扑序：{string.Join(" -> ", schedule.TopologicalOrder)}");
        Console.WriteLine($"贪心调度总工期 {schedule.Makespan}，峰值资源 {schedule.PeakResourceUsage}，关键路径：{string.Join(" -> ", schedule.CriticalPath)}");
        foreach (var item in schedule.WorkItems)
        {
            Console.WriteLine($"  {item.Id}：[{item.Start}, {item.End})，占 {item.ResourceDemand} 份资源，剩余关键长度 {item.CriticalRemaining}{(item.IsOnCriticalPath ? "，在关键路径上" : "")}");
        }

        var comparison = scheduler.CompareWithOptimalSchedule(workItems);
        Console.WriteLine($"精确优化器（分支限界）：最优工期 {comparison.OptimalSchedule.Makespan}，探索 {comparison.ExploredNodeCount} 个节点、剪枝 {comparison.PrunedBranchCount} 次");
        Console.WriteLine($"依赖下界 {comparison.DependencyLowerBound}，资源下界 {comparison.ResourceLowerBound}，贪心{(comparison.GreedyIsOptimal ? "已经最优" : $"还能缩短 {comparison.MakespanImprovement}")}");
    }
}
