# 任务依赖与资源调度综合项目

场景是一条共享有限构建执行单元的软件发布流水线。每个工作项有持续时间、资源需求和前置依赖，目标是在满足依赖与容量上限的前提下生成确定、可解释的计划。

项目提供两条互补路径：

- `CreateSchedule` 用关键路径优先的确定性列表调度快速产生可行计划，适合常规规模。
- `CompareWithOptimalSchedule` / `CreateOptimalSchedule` 对小规模实例运行分支限界搜索，证明最小 makespan，并量化贪心解与最优解的差距。

为什么需要两条路径？资源约束项目调度具有组合爆炸，启发式能快但不保证最优；精确搜索能证明最优，却不能扩展到任意规模。把二者并列，既保留实用的快速路径，又为学习和测试提供独立最优性 oracle。

## 为什么组合这些结构

- `WeightedGraph<string>` 保存“前置项 -> 消费者”的有向边。
- `OpenAddressingHashTable<TKey, TValue>` 保存任务注册表、入度、开始/结束时间和动态规划状态，展示开放寻址、墓碑与重散列在真实状态管理中的用途。
- `BinaryMinHeap<T>` 一次用于按标识打破平局的 Kahn 拓扑排序，一次用于“剩余关键路径最长优先”的 ready queue。比较器最后比较唯一标识，避免相同优先级导致输出不稳定。
- 反向拓扑动态规划计算 `criticalRemaining[item] = duration[item] + max(criticalRemaining[child])`，为贪心优先级和依赖下界提供依据。
- `LazyRangeAddMaxSegmentTree` 在离散时间轴上执行区间最大值查询与区间增加。只有 `QueryMax(start, end) + demand <= capacity` 时才能预订 `[start, end)`。

核心实现没有用 BCL `Dictionary` 或 `PriorityQueue` 替换上述教学结构。`List<T>`、数组和稳定排序只负责输入快照、搜索热路径和结果展示；它们不承担项目要学习的图、堆或区间不变量。

## 为什么资源树使用懒标记

时间槽统一为左闭右开区间 `[start, end)`，因此 `end - start` 恰好等于持续时间，相邻任务可以在同一边界释放和获取资源。

旧的逐点更新会把长度为 `d` 的预订拆成 `d` 次 `O(log H)` 更新。V2 使用“区间加 + 区间最大值”懒标记树：

1. 查询候选窗口的最大占用，判断整个窗口是否有足够容量；
2. 预订时对整个窗口执行一次 `RangeAdd`；
3. 搜索回溯时对同一区间增加相反数，恢复进入分支前的状态。

一次区间增加降为 `O(log H)`。懒标记的核心不变量是：节点最大值已经包含本节点尚未下推的增量；访问子节点前必须下推，回到父节点时必须用两个子节点最大值重新合并。漏下推会读到旧容量，重复下推会把资源占用计算两次。

## 贪心路径为什么不保证最优

列表调度先完成拓扑校验，再让 ready queue 优先选择 `criticalRemaining` 较大的任务，并从依赖完成时刻开始寻找最早可行窗口。这个规则通常能避免长依赖链过晚启动，但资源竞争可能让局部“最紧急”选择阻塞更好的整体组合。

因此 `CreateSchedule` 的契约是“确定、可行”，不是“全局最短”。测试必须验证每个任务恰好出现一次、依赖顺序正确、任何时间槽不超容量和输出顺序稳定，不能把一组样例中的 makespan 当成普遍最优证明。

## 精确路径如何证明最优

`CompareWithOptimalSchedule` 先调用贪心调度，得到一个完整可行解，它的 makespan 是搜索上界。随后分支限界搜索按确定性拓扑顺序，为每个任务枚举所有可能改进当前上界的整数开始时间。

搜索使用两个可独立复算的下界：

- 依赖下界：忽略资源竞争时的关键路径长度；
- 资源下界：`ceil(总资源工作量 / 总容量)`。

当前分支下界不小于已有上界时，该分支不可能产生更短计划，可以安全剪枝。发现更短完整计划后立即收紧上界。搜索穷尽所有未被合法下界排除的候选后，保留下来的计划具有最小 makespan。

`ProjectScheduleOptimalityComparison` 同时返回：

- `GreedySchedule` 与 `OptimalSchedule`；
- `MakespanImprovement` 和 `GreedyIsOptimal`；
- `ExploredNodeCount` 与 `PrunedBranchCount`；
- `DependencyLowerBound` 与 `ResourceLowerBound`。

这些字段不是装饰信息：下界允许测试独立复算，节点/剪枝计数解释证明成本，改进量说明启发式在当前实例上损失了多少。

## 为什么精确 API 有 8 项 / 32 时间轴边界

精确搜索最坏为指数级。为了防止教学程序在看似普通的输入上长时间无响应，公开契约设置：

- 工作项最多 `ProjectScheduler.ExactWorkItemLimit = 8`；
- 所有持续时间总和最多 `ProjectScheduler.ExactHorizonLimit = 32`。

超过任一公开边界时，`CompareWithOptimalSchedule` 和 `CreateOptimalSchedule` 会抛出带说明的 `ArgumentException`，并建议使用 `CreateSchedule`。
搜索内部还保留 2,000,000 个状态节点预算；满足 8/32 边界的病态输入也可能在预算耗尽时抛出 `InvalidOperationException`。
如果调用方启用了追踪，此时会先记录 `ExactSearchAborted`；无论是否追踪，算法都绝不返回一个冒充最优的当前上界，只有正常返回才表示证明已经完成。

这些限制不是“算法复杂度已经解决后随意写的常量”，而是指数算法必须公开的规模契约。真实系统通常使用 CP-SAT、整数规划或专业调度器；教学实现的目标是让最优性证明过程可读、可测试。

## 运行

```powershell
dotnet run --project DataStructureAndAlgorithm.Scenarios.ProjectScheduling -c Release
dotnet run --project DataStructureAndAlgorithm.Scenarios.ProjectScheduling -c Release -- --trace json
dotnet run --project DataStructureAndAlgorithm.Scenarios.ProjectScheduling -c Release -- --trace mermaid
```

程序输出包括原始工作项、确定性拓扑序、依赖关键路径、按开始时间稳定排序的计划、makespan、资源峰值和容量。追踪模式还会解释图构建、贪心任务放置、精确搜索开始/改进/剪枝汇总等阶段；启用追踪不会改变领域结果。

## 建议的验证顺序

1. 手算拓扑序和关键剩余长度。
2. 对每个计划逐时间槽验证资源占用。
3. 在 3～5 个任务上穷举开始时间，作为与分支限界思路不同的最小 oracle。
4. 比较贪心和精确 makespan，尝试构造贪心非最优反例。
5. 验证输入枚举顺序变化不影响确定性结果。
6. 验证超过 8 项或持续时间总和超过 32 时，精确 API 明确拒绝而快速 API仍可使用。

## 设计边界

调度器会在进入核心算法前拒绝：重复工作项、未知依赖、自依赖、重复依赖、依赖环、非正持续时间、非正资源需求，以及超过总容量的单项需求。

当前模型不支持任务抢占、多种独立资源、人员技能、工作日历、切换成本和动态事件。贪心路径不承诺全局最优，精确路径只承诺规模边界内的离散时间最优。生产化时应先扩展领域模型和故障契约，而不是只把搜索上限调大。
