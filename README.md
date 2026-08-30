# DataStructureAndAlgorithm

> 本公开仓库由原私有仓库的当前文件快照重新建立。为保护个人隐私，旧 Git 提交历史和 PR 记录没有迁移。详见 [仓库迁移与隐私说明](MIGRATION_AND_PRIVACY.md)。
>
> 新克隆请按 [Git 隐私 hooks 启用说明](GIT_HOOKS_SETUP.md)执行一次本地配置。

一个基于 `.NET 10` 和 `C# 14` 的数据结构与算法学习项目。仓库包含算法类库、xUnit/FsCheck 测试、可输出 JSON/Mermaid 步骤的 Demo、四个支持结构化追踪的综合场景，以及独立的 BenchmarkDotNet 性能实验；不包含 ASP.NET 服务。

在线浏览与学习：[算法研习室](https://yuweiyang9611.github.io/DataStructureAndAlgorithm/)。可从 [10 分钟快速开始](https://yuweiyang9611.github.io/DataStructureAndAlgorithm/%E5%BC%80%E5%A7%8B%E5%AD%A6%E4%B9%A0.html)、[22 个可验证学习单元（自测、证据与复习）](https://yuweiyang9611.github.io/DataStructureAndAlgorithm/%E5%AD%A6%E4%B9%A0%E5%8D%95%E5%85%83.html)或 [Trace 实验室](https://yuweiyang9611.github.io/DataStructureAndAlgorithm/%E8%BF%BD%E8%B8%AA%E5%AE%9E%E9%AA%8C%E5%AE%A4.html)直接进入。

完整学习顺序、源码导读和 C# 最佳实践请参阅 [C# 数据结构与算法学习指导](docs/CSharp数据结构与算法学习指导.md)。编写新模块前建议先阅读 [API 设计与比较器约定](docs/API设计与比较器约定.md)，避免空值、区间、重复值和比较语义在不同结构间漂移。

专题入口：

- [C# 数据结构与算法进阶学习指导](docs/CSharp数据结构与算法进阶学习指导.md)：哈希、并查集、Trie、缓存、区间结构与高级算法。
- [LeetCode 100 题完整学习指导](docs/LeetCode100题完整学习指导.md)：100 道不重复经典题、逐题“为什么”和 16 周路线。
- [P3 数论、位运算与高级算法学习指导](docs/P3数论位运算与高级算法学习指导.md)：数论、位运算、高级图/字符串/DP 与质量门禁。
- [综合项目实战学习指导](docs/综合项目实战学习指导.md)：城市配送 V2、迷你搜索 V2、项目调度 V2、迷你存储引擎，以及“持续可证明”的证据链。

需要打印或离线阅读时，使用 [C# 数据结构与算法完整学习指导 PDF](output/pdf/CSharp数据结构与算法完整学习指导.pdf)。内容变更后运行 `python tools/generate_integrated_learning_guide_pdf.py` 可重建同一文件。

## 已实现内容

| 分类 | 实现 | 主要操作/学习重点 | 典型复杂度 |
| --- | --- | --- | --- |
| 线性结构 | 单向/双向链表、数组栈、循环队列、数组双端队列 | 两端插入删除、循环数组扩容、逻辑/物理下标 | 主要操作摊还 `O(1)` |
| 堆 | 二叉最小堆、索引优先队列 | 线性建堆、入队、decrease-key、删除最小值 | 建堆 `O(n)`；更新/出队 `O(log n)` |
| 查找与排序 | 二分边界；比较与非比较排序 | 泛型比较器、稳定性、完整 `int` 值域、输入域约束 | `O(log n)`、`O(n log n)` 或依键域而定 |
| 树 | 普通二叉树、BST、AVL、红黑树、跳表、B 树、B+ 树、线索树、Huffman | 遍历/重建、旋转、颜色/黑高、多路分裂、叶链扫描、前缀编码 | 从 `O(n)` 遍历到 `O(log n)` 有序操作 |
| 哈希与集合 | 链地址/开放寻址哈希表、并查集 | 冲突、墓碑、扩容、路径压缩、按大小合并 | 平均 `O(1)`；并查集摊还 `O(α(n))` |
| 字符串索引 | KMP、Rabin-Karp、Z、Aho-Corasick、后缀数组、Trie、BK-tree | 单/多模式匹配、前缀、后缀索引、编辑距离近邻 | 依算法从线性到词典分布相关 |
| 图与网络流 | BFS/DFS、最短路、生成树、拓扑、SCC、A*、Dinic、Hopcroft-Karp、最小费用流 | 路径恢复、连通性、最大流量与最小费用目标 | 可为 `O(V+E)`、`O(E√V)`、`O(V³)`、`O(V²E)` 或 `O(AE log V)`，依算法与增广次数而定 |
| 区间结构 | Fenwick、通用线段树、Sparse Table、懒标记区间和/最大值树 | 单点/区间更新、静态查询、lazy propagation | 查询/更新 `O(1)` 或 `O(log n)` |
| 缓存与概率结构 | LRU、LFU、Bloom Filter | 淘汰策略、派生状态、一定位不存在/可能存在 | 平均常数或对数级，依实现而定 |
| 动态规划与回溯 | LCS、背包、零钱、LIS、编辑距离、矩阵链、树形/状态压缩 DP、n 皇后 | 状态定义、空间压缩、答案恢复、剪枝 | 依状态规模而定，部分为指数级 |
| 内存与性能 | `Span<T>`、`ArrayPool<T>`、BenchmarkDotNet | 零拷贝视图、所有权、时间与分配趋势 | 保持算法复杂度，减少分配 |
| LeetCode 题库 | 100 道主线题 + P3 专题题 | C# 14 实现、选择理由、不变量、复杂度和题号测试 | 覆盖数组、链表、树、图、搜索、贪心与 DP |
| 综合项目 | 配送 V2、搜索 V2、调度 V2、迷你存储 | 最小费用最大流、位置索引/BM25、精确调度、B+ 树/Bloom/LFU/WAL | 由各流水线阶段共同决定 |

其中 `V` 为顶点数，`E` 为边数，`n` 为输入规模。复杂度表给出的是主导量级；精确前置条件、最坏情况和生产限制见对应学习指导。

## 四个综合项目

| 项目 | 组合内容 | V2 重点 |
| --- | --- | --- |
| CityDelivery | 有向最短路、并查集、缓存、匹配、维护森林 | 首先最大化派单数，再用最小费用最大流最小化总旅行时间 |
| MiniSearch | 位置倒排表、Trie、Aho-Corasick、Top-K、缓存 | BM25 排名、短语/邻近检索、BK-tree 纠错 |
| ProjectScheduling | DAG、确定性堆、关键路径 DP、区间资源树 | 懒标记区间加/最大值；小规模精确解对照贪心解 |
| MiniStorage | B+ 树、Bloom Filter、LFU、WAL、tombstone | 点查/范围扫描、先写日志、重放恢复、加速层不拥有业务真相 |

四个项目的共同学习方法是：先写目标函数和跨模块不变量，再观察追踪，最后用简单模型或小规模穷举验证优化实现。不要把“示例能运行”当作“输入族正确”，也不要把覆盖率或一次基准数字当作证明。

## 项目结构

- `DataStructureAndAlgorithm/`：算法与数据结构教学实现。
- `DataStructureAndAlgorithm.Demo/`：排序、图、树和 Huffman 的 JSON 步骤。
- `DataStructureAndAlgorithm.P3Demo/`：gcd、矩阵链和 Dinic 的 JSON/Mermaid 学习步骤。
- `DataStructureAndAlgorithm.Scenarios.CityDelivery/`：最大派单数下最小化总旅行时间，并计算维护森林。
- `DataStructureAndAlgorithm.Scenarios.MiniSearch/`：位置索引、BM25、短语/邻近查询、补全与 BK-tree 纠错。
- `DataStructureAndAlgorithm.Scenarios.ProjectScheduling/`：DAG 约束、懒标记资源树、贪心排期与小规模精确对照。
- `DataStructureAndAlgorithm.Scenarios.MiniStorage/`：B+ 树主索引、Bloom/LFU 加速和 WAL 恢复。
- `DataStructureAndAlgorithm.Benchmarks/`：使用 BenchmarkDotNet 比较教学实现、标准库和场景趋势，并报告内存分配。
- `DataStructureAndAlgorithm/LeetCode/`：按题型组织的经典题 C# 14 解法。
- `DataStructureAndAlgorithm.Test/`：xUnit、FsCheck、模型/差分/状态机测试、追踪契约和结构不变量验证。
- `docs/`：按学习顺序组织的源码导读、复杂度、限制和 C# 最佳实践。
- `.github/workflows/`：Release 构建测试、跨平台契约、质量门禁、变异测试与周期基准。
- `DataStructureAndAlgorithm.slnx`：解决方案入口。

## 构建与测试

```powershell
dotnet build DataStructureAndAlgorithm.slnx --configuration Release
dotnet test DataStructureAndAlgorithm.slnx --configuration Release --no-build
```

CI 使用同一套 Release 契约。当前验证基线（2026-08-15）为 Release 构建 `0 warning / 0 error`、自动化测试 `465/465` 通过、行覆盖率 `91.94%`、分支覆盖率 `84.80%`；最近一次六个关键文件的 Stryker Basic 变异验证（2026-07-18）分数为 `81.07%`。稳定覆盖率门槛分别为 `80%` 和 `70%`，变异测试 break 阈值为 `60%`。快照会随代码与测试变化，后续仍以最新测试、报告和 CI 结果为准。

质量证据分工如下：

- 示例与回归测试固定已知边界；
- 不变量、FsCheck、模型与状态机差分覆盖输入族和操作序列；
- trace schema/CLI contract 固定观察协议，throwing sink 回归证明观察器故障不会改变领域提交；
- Windows/Linux CI 暴露路径、换行、大小写和区域性差异；
- 覆盖率与变异测试检查测试触达和断言敏感度；
- BenchmarkDotNet 观察时间与分配趋势，不在共享 CI 中使用脆弱的毫秒硬阈值。

## 运行 Demo 与综合场景

```powershell
dotnet run --project DataStructureAndAlgorithm.Demo -- all
dotnet run --project DataStructureAndAlgorithm.P3Demo -- dinic mermaid
```

四个综合场景均支持无参数、`--trace json` 和 `--trace mermaid`：

```powershell
dotnet run --project DataStructureAndAlgorithm.Scenarios.CityDelivery -c Release
dotnet run --project DataStructureAndAlgorithm.Scenarios.MiniSearch -c Release
dotnet run --project DataStructureAndAlgorithm.Scenarios.ProjectScheduling -c Release
dotnet run --project DataStructureAndAlgorithm.Scenarios.MiniStorage -c Release

dotnet run --project DataStructureAndAlgorithm.Scenarios.CityDelivery -c Release -- --trace json
dotnet run --project DataStructureAndAlgorithm.Scenarios.MiniSearch -c Release -- --trace mermaid
dotnet run --project DataStructureAndAlgorithm.Scenarios.ProjectScheduling -c Release -- --trace json
dotnet run --project DataStructureAndAlgorithm.Scenarios.MiniStorage -c Release -- --trace mermaid
```

无参数模式保留各项目原有输出契约：CityDelivery 是“标题行 + 领域 JSON”，其余三个项目输出纯领域 JSON；追踪 JSON 顶层固定为 `{ result, trace }`，事件带 schema version；Mermaid 输出用于学习笔记。四个项目共用可选 `IAlgorithmTraceSink`，默认关闭追踪，避免正常路径被迫分配诊断状态；注入后由 `BestEffortAlgorithmTraceSink` 隔离普通观察器异常，防止“领域状态已提交但调用方收到失败”。追踪解释“经过哪些阶段”，不替代结果、不变量或最优性测试。

运行短基准冒烟；正式比较时去掉 `--job Dry`，完成预热和多轮采样：

```powershell
dotnet run --project DataStructureAndAlgorithm.Benchmarks -c Release -- --filter "*Scenario*" --job Dry
```

记录运行时、SDK、机器和输入规模后再比较趋势。一次快慢不能支持优化结论。

## 设计约定

- 公共 API 明确空值、非法范围、重复键和规模上限；内部循环依赖已经验证的前置条件。
- 半开区间 `[start, end)` 用于数组、时间槽和范围 API，避免相邻区间边界重复。
- 泛型有序结构通过 `IComparer<T>` 定义顺序；用户可见结果不依赖哈希枚举顺序。
- 缓存和 Bloom Filter 都是可重建加速层，不能成为领域事实来源。
- WAL 先于内存索引修改，并以 LF 换行作为记录提交标记；重启会忽略并截断最后一个半写片段，但不等于具备事务、校验和或生产级持久性。
- A* 只有在启发函数可采纳时才保证最优；缺少可靠地理下界时使用零启发函数。
- 精确调度只面向有显式任务数/时间跨度上限的小实例；常规规模使用确定性启发式。
- 教学实现用于学习不变量和复杂度，生产项目应优先选择成熟的 .NET 集合、数据库、搜索或调度组件。
