# C# 数据结构与算法学习指导

## 1. 这份指导解决什么问题

本仓库是一个 `.NET 10` / `C# 14` 教学项目。目标不只是“记住算法代码”，而是建立以下能力：

1. 能说清数据结构维护了什么不变量。
2. 能从输入规模推导时间、空间复杂度。
3. 能独立处理空输入、重复元素、越界、溢出和无解情况。
4. 能用泛型、比较器、可空引用类型和单元测试写出符合 C# 习惯的实现。
5. 知道教学实现与生产代码之间的边界。

项目以类库为核心，并提供职责单一的算法 Demo、P3 追踪 Demo、基准项目和四个综合场景项目。Demo 用 JSON/Mermaid 展示状态，Benchmarks 用独立进程测量时间和分配；综合项目把多个已学结构组合成端到端应用，并通过统一、可选的追踪 sink 解释流水线。学习时先读源码与测试，再用 Demo 观察单步状态，最后进入配送、搜索、调度和存储场景解释“为什么选这些结构、它们如何共同维护不变量”；不要把输出、计时或 UI 逻辑塞回算法类。四个场景的目标函数、复杂度、限制与正确性证据见 [综合项目实战学习指导](综合项目实战学习指导.md)。

完成本篇主干内容后，继续阅读 [C# 数据结构与算法进阶学习指导](CSharp数据结构与算法进阶学习指导.md)，学习哈希表、并查集、Trie、LRU、区间结构和进阶算法。

需要按经典题型练习时，阅读 [LeetCode 100 题完整学习指导](LeetCode100题完整学习指导.md)。

新增模块应遵循 [API 设计与比较器约定](API设计与比较器约定.md)，其中集中说明空值、半开区间、比较器、集合重复、输入所有权和异常类型为什么这样统一。

## 2. 项目知识地图

| 主题 | 主要源码 | 应掌握的核心概念 |
| --- | --- | --- |
| 单向链表 | `Linear/SinglyLinkedList.cs` | 节点引用、头尾指针、O(1) 头部操作 |
| 双向链表 | `Linear/DoublyLinkedList.cs` | 前驱/后继、O(1) 两端删除、双向链接不变量 |
| 栈 | `Linear/ArrayStack.cs` | LIFO、动态数组、摊还复杂度 |
| 队列 | `Linear/CircularQueue.cs` | FIFO、环形索引、扩容时保持逻辑顺序 |
| 双端队列 | `Linear/ArrayDeque.cs` | 两端 O(1) 操作、循环数组、逻辑/物理下标 |
| 堆 | `Heap/BinaryMinHeap.cs` | 完全二叉树、上浮、下沉、优先队列 |
| 查找 | `Searching/SearchAlgorithms.cs` | 二分查找、半开区间、Lower/Upper Bound |
| 排序 | `Sorting/SortAlgorithms.cs`、`SequenceExtensions.cs` | 稳定性、原地排序、分治、C# 14 扩展块 |
| 字符串匹配 | `PatternMatching/StringPatternMatching.cs` | 朴素匹配、KMP、前缀函数 |
| 普通二叉树 | `Tree/NormalBinaryTree.cs` | 深度优先、广度优先、递归与迭代 |
| 搜索树 | `Tree/BinarySearchTree.cs` | 有序不变量、查找、插入、删除 |
| AVL 树 | `Tree/AvlTree.cs` | 平衡因子、旋转、O(log n) 树高 |
| 红黑树 | `Tree/RedBlackTree.cs` | 左倾红链接、颜色翻转、黑高不变量 |
| 跳表 | `Tree/SkipList.cs` | 随机层高、多级索引、期望 O(log n) |
| B 树 | `Tree/BTree.cs` | 多路平衡、分裂、借位、合并 |
| B+ 树 | `Tree/BPlusTree.cs` | 内部节点分隔键、叶节点有序记录、叶链范围扫描 |
| 线索二叉树 | `Tree/CluesBinaryTree.cs` | 利用空指针保存遍历前驱/后继 |
| 哈夫曼树 | `Tree/HuffmanTree.cs` | 贪心选择、前缀编码、最小堆 |
| 无权图 | `Graph/GraphWithAdjacencyList.cs` 等 | 邻接表/矩阵、BFS、DFS |
| 带权图 | `Graph/WeightedGraph.cs` | 权重、稀疏图、顶点比较规则 |
| 图算法 | `Graph/WeightedGraphAlgorithms.cs` | Dijkstra、拓扑排序、环检测 |
| 动态规划 | `DynamicProgramming/DynamicProgrammingAlgorithms.cs` | 状态、转移、初始化、答案恢复 |
| 回溯 | `Backtracking/BacktrackingAlgorithms.cs` | 选择、约束、递归、撤销选择 |
| 哈希表 | `Hashing/SeparateChainingHashTable.cs`、`OpenAddressingHashTable.cs` | 链地址、线性探测、墓碑、负载因子、重新散列 |
| 概率结构 | `Probabilistic/StringBloomFilter.cs` | 位图、多哈希、允许假阳性但禁止假阴性 |
| 并查集 | `Set/DisjointSet.cs` | 路径压缩、按大小合并、动态连通性 |
| Trie | `Tree/Trie.cs` | 前缀共享、单词结束标记、安全删除 |
| 编辑距离索引 | `Tree/BkTree.cs` | 度量空间、三角不等式、近似词剪枝 |
| 缓存 | `Caching/LruCache.cs`、`LfuCache.cs` | 最近使用与最少使用淘汰、缓存一致性 |
| 区间结构 | `Range/FenwickTree.cs`、`SegmentTree.cs`、`SparseTable.cs`、`LazyRangeSumSegmentTree.cs`、`LazyRangeAddMaxSegmentTree.cs` | lowbit、静态 O(1) 查询、单点/区间更新、懒标记 |
| 进阶图算法 | `Graph/AdvancedGraphAlgorithms.cs`、`ComprehensiveGraphAlgorithms.cs`、`MinCostFlowAlgorithms.cs` | Bellman-Ford、生成树、全源最短路、A*、最大流、最小费用流 |
| 进阶动态规划 | `DynamicProgramming/AdvancedDynamicProgrammingAlgorithms.cs` | 编辑距离、LIS、空间压缩、结果恢复 |
| 进阶字符串 | `PatternMatching/AdvancedStringAlgorithms.cs`、`AhoCorasickMatcher.cs`、`SuffixArrayAlgorithms.cs` | 滚动哈希、Z 区间、多模式匹配、后缀索引与 LCP |
| 内存与性能 | `Memory/SpanAlgorithms.cs`、`DataStructureAndAlgorithm.Benchmarks/` | Span 零拷贝视图、ArrayPool 所有权、基准与分配诊断 |
| 数组模式 | `ArrayAlgorithms/ArrayAlgorithms.cs` | 哈希、单调队列、Kadane |
| 贪心 | `Greedy/GreedyAlgorithms.cs` | 区间调度、交换论证 |
| 非比较排序 | `Sorting/NonComparisonSortAlgorithms.cs` | 计数、桶、LSD 基数排序与输入域约束 |
| 组合搜索 | `Backtracking/CombinatorialAlgorithms.cs` | 括号剪枝、幂集生成 |
| LeetCode 100 题 | `LeetCode/Classic*Problems.cs`、`Additional*Problems.cs` | 题型识别、不变量、逐题测试、API 适配 |
| 综合项目 | `DataStructureAndAlgorithm.Scenarios.CityDelivery/`、`MiniSearch/`、`ProjectScheduling/`、`MiniStorage/` | 组合最小费用流、位置索引/BM25、精确调度、B+ 树/WAL，并区分事实来源与加速层 |
| 统一追踪 | `Diagnostics/AlgorithmTracing.cs`、`Diagnostics/MermaidTraceRenderer.cs` | 可选 sink、schema version、只读状态快照、稳定步骤、JSON/Mermaid 与领域结果解耦 |

测试项目保持相同主题目录。遇到看不懂的实现时，先看测试构造了什么输入、期待什么输出，再回到源码追踪状态变化。

## 3. 环境与常用命令

仓库通过 `global.json` 请求稳定的 .NET 10 SDK，并允许在 .NET 10 内滚动到最新 feature band；类库、测试、演示、基准和综合场景项目统一以 .NET 10 / C# 14 为基线。确认环境：

```powershell
dotnet --info
```

运行全部测试：

```powershell
dotnet test DataStructureAndAlgorithm.slnx --configuration Release
```

观察排序、Dijkstra、红黑树和 Huffman 的 JSON 步骤：

```powershell
dotnet run --project DataStructureAndAlgorithm.Demo -- all
```

运行四个综合项目，观察同一种结构在不同领域约束下承担的职责。无参数模式保留领域结果 JSON；另外两种模式共享同一个算法实现，只改变观察方式：

```powershell
dotnet run --project DataStructureAndAlgorithm.Scenarios.CityDelivery
dotnet run --project DataStructureAndAlgorithm.Scenarios.MiniSearch
dotnet run --project DataStructureAndAlgorithm.Scenarios.ProjectScheduling
dotnet run --project DataStructureAndAlgorithm.Scenarios.MiniStorage

dotnet run --project DataStructureAndAlgorithm.Scenarios.CityDelivery -- --trace json
dotnet run --project DataStructureAndAlgorithm.Scenarios.MiniSearch -- --trace mermaid
dotnet run --project DataStructureAndAlgorithm.Scenarios.ProjectScheduling -- --trace json
dotnet run --project DataStructureAndAlgorithm.Scenarios.MiniStorage -- --trace json
```

`--trace json` 的顶层固定为 `{ result, trace }`，每个事件带 schema version，适合程序处理和契约测试；`--trace mermaid` 适合把有序事件直接放入学习笔记。追踪默认关闭，是为了让观察功能不改变领域 API，也不强迫正常路径承担状态字典与快照开销；启用时使用 best-effort 装饰器隔离普通接收器异常，避免日志失败反过来破坏领域提交语义。

不要一开始就背综合项目的完整实现。先写下输入契约和跨结构不变量，再逐段替换为自己实现的数据结构；这样能分辨“算法本身正确”和“组合边界正确”是两层不同证据。

先用短任务确认基准可启动，再去掉 `--job Dry` 做正式测量：

```powershell
dotnet run --project DataStructureAndAlgorithm.Benchmarks -c Release -- --filter "*Sorting*" --job Dry
```

只运行一个主题：

```powershell
dotnet test DataStructureAndAlgorithm.slnx --filter FullyQualifiedName~SortingTest
```

仓库的 `.github/workflows/ci.yml` 会在 push 和 pull request 上使用相同的 .NET 10 Release 流程执行 restore、build、test 与覆盖率收集。把构建和测试拆开并在后续命令使用 `--no-restore`、`--no-build`，可以明确区分“依赖恢复失败”“编译失败”和“行为测试失败”，也避免重复工作。

学习时建议先使用 Debug 断点或 Demo 追踪理解过程，完成后再跑 Release 全量测试。算法性能比较不能只看一次运行时间；仓库已经把 BenchmarkDotNet 放在独立项目中，避免在单元测试里用 `Stopwatch` 得出受 JIT、预热、GC 和机器负载影响的结论。

## 4. 学习每个算法的固定步骤

### 4.1 明确输入、输出和契约

先回答：

- 输入允许为空吗？
- 是否允许重复元素？
- 是否原地修改输入？
- 比较顺序来自哪里？
- 找不到答案时返回 `-1`、`null`、空集合，还是抛异常？
- 输入不满足前置条件时由谁负责？

例如二分查找的前置条件是序列已按同一个 `IComparer<T>` 排序。每次查找都先验证有序性需要 O(n)，会失去二分查找的复杂度优势，因此该条件应写入文档，由调用方保证。

### 4.2 找到不变量

不变量是算法执行过程中始终为真的条件：

- 栈：有效元素位于 `[0, Count)`，栈顶是 `Count - 1`。
- 循环队列：第 i 个逻辑元素位于 `(_head + i) % Capacity`。
- 最小堆：任意父节点都不大于其子节点。
- BST：左子树元素小于节点，右子树元素大于节点。
- AVL：除 BST 不变量外，每个节点左右子树高度差不超过 1。
- 红黑树：根为黑色、红节点没有红孩子、右侧没有红链接、任意根到空叶路径黑节点数相同。
- 开放寻址哈希表：查找必须越过 Deleted 墓碑，只能在真正 Empty 槽位终止。
- 双向链表：`head.Previous` 和 `tail.Next` 为空，正向与反向链接互相对应。
- Dijkstra：从优先队列取出的当前最小有效距离不会再被非负边缩短。
- 动态规划表：填 `dp[i, j]` 时，它依赖的子问题已经计算完成。

调试时不要只盯最终输出；在循环或递归的关键位置检查不变量，错误会更容易定位。

### 4.3 手算最小例子

至少覆盖：

1. 空集合或最小合法输入。
2. 单个元素。
3. 重复元素。
4. 已有序与逆序输入。
5. 触发关键分支的输入，例如 AVL 四种旋转、循环队列绕回、B 树合并。
6. 非法输入和无解输入。

### 4.4 推导复杂度

分别写时间和额外空间复杂度，并说明平均、最坏或摊还情形。

常见判断：

- 连续嵌套两层、每层最多 n 次，通常是 O(n²)。
- 每次把问题规模减半，通常出现 O(log n)。
- 分成两个一半子问题并线性合并，通常是 O(n log n)。
- 图遍历访问每个顶点和每条边一次，是 O(V + E)。
- 递归函数除显式容器外，还要计算调用栈空间。

复杂度忽略常数但不等于性能。缓存局部性、分支预测、对象分配和比较器成本都会影响实际运行速度。

### 4.5 用测试固定理解

一个好的算法测试应表达一个规则，而不是复制实现过程。比如测试最小堆时，连续出队结果必须有序，比断言某一时刻内部数组恰好长什么样更稳健。

## 5. 推荐学习顺序

### 第一阶段：语言与复杂度基础

先掌握数组、引用、值类型、泛型、委托、异常、`IEnumerable<T>`、Big-O。练习手写数组遍历、交换、最大值和前缀和。

验收标准：能解释数组随机访问为什么是 O(1)，链表按下标访问为什么是 O(n)。

### 第二阶段：线性数据结构

依次阅读：

1. `SinglyLinkedList<T>`
2. `DoublyLinkedList<T>`
3. `ArrayStack<T>`
4. `CircularQueue<T>`
5. `ArrayDeque<T>`

重点观察头尾指针、`Count`、空结构和扩容。独立练习：

- 链表反转。
- 快慢指针检测环。
- 比较单向链表和双向链表在尾部删除时的差异。
- 用两个栈实现队列。
- 用队列实现二叉树层序遍历。
- 手算双端队列在物理数组末尾绕回到下标 0 的过程。

### 第三阶段：查找与排序

先学插入、选择、冒泡，理解循环不变量；再学归并、快速和堆排序，理解分治与堆。

需要能够比较：

| 算法 | 最好 | 平均/最坏 | 额外空间 | 稳定 |
| --- | --- | --- | --- | --- |
| 冒泡 | O(n) | O(n²) | O(1) | 是 |
| 选择 | O(n²) | O(n²) | O(1) | 否 |
| 插入 | O(n) | O(n²) | O(1) | 是 |
| 归并 | O(n log n) | O(n log n) | O(n) | 是 |
| 快速 | O(n log n) | 最坏 O(n²) | 平均 O(log n) 栈 | 否 |
| 堆排序 | O(n log n) | O(n log n) | O(1) | 否 |

随后学习 `BinarySearch`、`LowerBound`、`UpperBound`。重点掌握 `[left, right)` 半开区间，避免死循环和边界越界。

最后学习非比较排序：计数排序依赖较窄整数值域；桶排序依赖较均匀分布并在桶内使用比较排序；LSD 基数排序按低位到高位进行稳定分配。本项目的基数排序用 `value ^ int.MinValue` 翻转符号位，从而覆盖完整的有符号 `int` 值域。

### 第四阶段：树与堆

推荐顺序：普通二叉树 → BST → AVL → 红黑树 → 最小堆 → 哈夫曼树 → B 树 → 线索二叉树 → 跳表。

不要把“二叉堆”和“二叉搜索树”混为一谈：堆只保证父子顺序，不能高效查找任意值；BST 保证整棵左、右子树的有序关系。

练习时为结构写验证函数：

- BST 中序遍历严格递增。
- AVL 每个节点平衡因子合法，保存高度与实际高度一致。
- 红黑树根黑、无连续红、红链接左倾且所有路径黑高一致。
- B 树节点键数、孩子数、键区间和叶子深度符合约束。
- 跳表第 0 层严格有序，高层节点一定也出现在全部更低层。

### 第五阶段：字符串与图

先用朴素匹配理解问题，再学习 KMP 如何利用已经匹配的信息减少回退。

图按以下顺序学习：

1. 邻接矩阵与邻接表。
2. BFS 与 DFS。
3. 拓扑排序及有向环检测。
4. Dijkstra 非负权最短路。
5. 继续扩展：Prim、Floyd-Warshall、强连通分量、桥与割点、A*。

Dijkstra 遇到负权边会抛异常。负权边不是“小概率特殊情况”，而是算法前置条件不成立，应改用 Bellman-Ford 等算法。

### 第六阶段：动态规划与回溯

动态规划不要从背代码开始。对每个问题写出：

1. 状态含义。
2. 状态转移方程。
3. 初始状态。
4. 填表顺序。
5. 最终答案位置。
6. 是否需要恢复具体选择。
### 第七阶段：四个综合项目

按“配送 V2 → 搜索 V2 → 调度 V2 → 迷你存储”的顺序学习。前三个项目分别训练网络优化、信息检索和资源约束；存储项目再把有序索引、概率预筛、缓存和恢复协议组合到同一读写路径。

每个项目不要从 `Program.cs` 的最终 JSON 开始背诵，而要依次回答：

1. 业务目标是什么，多个目标的优先级是什么？
2. 哪个结构保存事实，哪个结构只是加速层？
3. 跨模块不变量是什么，哪个简单模型可以充当 oracle？
4. 复杂度瓶颈在哪里，教学实现距离生产系统还缺什么？

完整八周路线和手算练习见 [综合项目实战学习指导](综合项目实战学习指导.md)。

### 第八阶段：持续正确性与有证据的优化

把回归、不变量、模型/差分、FsCheck、状态机、trace contract、跨平台 CI、变异测试和 BenchmarkDotNet 串成持续证据链。先证明优化前后的领域结果等价，再比较时间和分配趋势；不要用覆盖率代替正确性，也不要用共享 CI 上的一次毫秒数字决定优化成败。

验收标准：能够为一个新模块同时写出契约、不变量、独立 oracle、异常测试、跨平台输出约定和基准假设，并解释每种证据能发现什么、不能证明什么。


本项目中的斐波那契、最长公共子序列、0/1 背包和零钱兑换分别展示了空间压缩、二维状态、答案恢复和不可达哨兵。

回溯以 n 皇后为例。每层递归选择一行中的列，集合用于剪枝，返回上一层前必须撤销所有状态修改。

## 6. C# 最佳实践指南

### 6.1 开启可空引用类型

项目使用 `<Nullable>enable</Nullable>`。`T?`、空检查和编译器警告是在表达 API 契约，不应通过到处添加 `!` 来消除警告。

适合使用 `!` 的情形很少，例如数组槽位已在逻辑上移除，需要写入 `default!` 以释放引用，而调用方永远不会访问该槽位。此时应有注释说明不变量。

### 6.2 参数验证应靠近公共 API 边界

现代 .NET 可使用：

```csharp
ArgumentNullException.ThrowIfNull(values);
ArgumentOutOfRangeException.ThrowIfNegative(capacity);
```

参数本身非法用 `ArgumentException` 系列；对象当前状态不允许操作用 `InvalidOperationException`；查找普通失败通常返回约定值，不要把异常当正常分支。

### 6.3 泛型算法使用比较器

不要假设元素一定是 `int`，也不要强制所有类型实现 `IComparable<T>`。推荐接受可选的 `IComparer<T>`：

```csharp
comparer ??= Comparer<T>.Default;
```

判断“身份相等”使用 `IEqualityComparer<T>`；判断“排序先后”使用 `IComparer<T>`。两者语义不同。图一旦接受自定义相等比较器，内部所有字典、去重和路径恢复都必须沿用同一个比较器。

### 6.4 暴露最小能力接口

只读输入优先考虑 `IReadOnlyList<T>`；需要原地赋值的排序算法使用 `IList<T>`；只需枚举一次则用 `IEnumerable<T>`。参数接口越小，调用方选择越多，方法契约也越清楚。

不要直接暴露可变的内部 `List<T>`。可以返回 `IReadOnlyList<T>`，但要注意它只是接口只读，不一定是数据快照。安全边界要求更高时应返回副本或不可变集合。

### 6.5 封装并维护不变量

节点、数组和索引通常是实现细节：

- 节点类型可以是私有嵌套类。
- `Count` 使用 `private set`。
- 不让调用方直接修改堆数组或链表的 `Next`。
- 公共方法完成后，结构必须立即回到合法状态。

教学项目可以公开少量状态帮助观察，但要明确这是否会允许调用方破坏结构。

### 6.6 优先清楚的代码，再做有证据的优化

先实现容易证明正确的版本，再用基准数据判断是否需要：

- `Span<T>` 或 `Memory<T>`。
- `ArrayPool<T>` 减少大数组分配。
- 结构体避免对象分配。
- 迭代替代深递归。
- 专用比较器减少委托开销。

这些工具会增加生命周期、所有权或复制语义的复杂度。没有测量结果时，不要为了“看起来高性能”牺牲正确性和可读性。

### 6.7 理解值类型与引用类型

`record struct KnapsackItem` 适合小型、不可变、值语义的数据。树节点包含可变引用和身份，适合 `class`。大型可变结构体容易产生隐式复制，不适合作为节点。

`record` 自动提供值相等和易读的 `ToString()`，适合算法结果 DTO；它不会自动让其中引用的集合变成不可变集合。

### 6.8 使用现代语法但保持意图清晰

项目显式使用 C# 14，并在适合的位置采用现代语法：

- 集合表达式 `[]` 简化数组、列表和空集合初始化。
- 主构造函数适合依赖少、初始化关系清晰的节点类型。
- `record` / `record struct` 表达值语义的数据和算法结果。
- 模式匹配、元组交换、局部函数和 `using var` 减少样板代码。
- C# 14 扩展块把同一接收者类型的扩展成员组织在一起。

`Sorting/SequenceExtensions.cs` 是扩展块的实际示例：

```csharp
public static class SequenceExtensions
{
    extension<T>(IEnumerable<T> source)
    {
        public bool IsSorted(IComparer<T>? comparer = null) { /* ... */ }
    }
}
```

C# 14 的 `field` 关键字、空条件赋值等语法只应在确实减少字段或分支噪声时使用。算法项目不能为了展示语法而隐藏关键状态变化。

选择标准不是“语法最新”，而是它是否让契约和算法意图更清楚。团队项目还应由 `LangVersion` 和 CI 固定语言版本，避免不同 SDK 产生不一致结果。

算法中的关键状态最好使用有意义的名称，例如 `remainingCapacity`，不要为了短而全部写成 `i`、`j`。只有很小、含义明确的循环下标适合短名称。

### 6.9 注释解释原因和约束

低价值注释：

```csharp
index++; // index 加一
```

高价值注释应解释：

- 为什么选择半开区间。
- 为什么相等时先取归并左侧元素才能保持稳定。
- 为什么清空数组槽位可以释放引用。
- 为什么优先队列中存在过期条目以及如何跳过。
- 某个前置条件不满足会导致什么错误。

公共 API 使用 XML 文档；复杂循环和不变量使用局部注释。注释必须随代码更新，错误注释比没有注释更危险。

### 6.10 避免隐藏的多次枚举

`IEnumerable<T>` 可能来自数据库、网络或一次性生成器。不要在不知道来源时反复调用 `Count()`、`Any()` 和遍历。若算法确实需要随机访问，可以在入口明确物化一次：

```csharp
var items = source as IReadOnlyList<T> ?? source.ToArray();
```

同时要意识到物化会消耗 O(n) 额外空间。

### 6.11 处理整数与浮点边界

中点使用 `left + (right - left) / 2`，避免 `left + right` 溢出。斐波那契明确限制到 `long` 可表示的 F(92)。金额、容量和数组长度通常应拒绝负数。

浮点权重需要拒绝 `NaN` 和无穷值。涉及货币时通常用 `decimal` 或最小货币单位的整数，不应直接套用 `double`。

### 6.12 优先使用标准库完成生产任务

手写集合和排序用于学习。生产代码通常优先：

- `List<T>`、`LinkedList<T>`、`Stack<T>`、`Queue<T>`。
- `Dictionary<TKey,TValue>`、`HashSet<T>`。
- `PriorityQueue<TElement,TPriority>`。
- `Array.Sort`、`List<T>.Sort`、LINQ 的 `OrderBy`。

标准库实现经过广泛测试和优化。只有明确的性能、内存布局、领域约束或教学需求才手写。

## 7. 测试最佳实践

### 7.1 一个测试表达一个行为

测试名称建议采用：`Method_Condition_ExpectedBehavior`。准备、执行、断言三个阶段保持清晰。

### 7.2 同时测试结果和不变量

排序不仅检查示例结果，还要覆盖空集合、单元素、重复数、负数和自定义比较器。树和堆除了返回值，还要验证结构约束。

### 7.3 不复制被测算法

测试快速排序时不要在测试里再写一遍快速排序。可以使用明确的期望数组，或用标准库排序产生参考结果，再对随机样本进行差分测试。

### 7.4 随机测试必须可复现

固定随机种子，并在失败信息中输出种子和输入。无种子的偶发失败很难调试。

### 7.5 单元测试不是性能测试

CI 机器负载不稳定。单元测试负责正确性，BenchmarkDotNet 负责预热、重复、统计和运行时诊断。

### 7.6 用性质测试验证规则族

示例测试回答“这个输入是否正确”，FsCheck 性质测试回答“这一族输入是否始终满足同一规则”。综合场景应覆盖：配送的最大流量与最小费用、搜索的位置列表与 BM25 排序、调度的依赖/容量与精确最优性、存储的操作序列与恢复等价。测试数量不应成为目标；每条性质都应能说清它覆盖的规则和独立 oracle。

性质测试应遵循四条规则：

1. 把随机值规范化为合法的小输入，让失败样本能够被 FsCheck 缩减到可读反例。
2. oracle 使用完整排序、直接扫描、简单数组、`SortedDictionary` 或小规模穷举等不同思路，不复制被测优化算法。
3. 先验证不变量，再验证方便展示的聚合结果，避免多个错误字段彼此“自证”。
4. 保留固定回归与异常测试；随机样本不是穷举证明，也不擅长精确验证异常类型和参数名。

### 7.7 追踪也需要测试

观察代码如果不受约束，也会产生误导证据。当前测试会确认：

- 注入 `IAlgorithmTraceSink` 前后领域结果相同；
- 步骤从 1 连续递增，事件停留在阶段和领域决策粒度；
- 收集器复制状态并只读公开，后续修改原字典不会篡改历史；
- Mermaid 按 Ordinal 排序 state key，并稳定转义引号、换行和反斜线；
- JSON 顶层保持 `{ result, trace }`，事件 schema version 明确且可验证；
- 四个 CLI 的普通、JSON、Mermaid 和非法模式保持各自约定的退出码与输出通道；Mermaid 额外保证恰好一个平台原生末尾换行。

追踪测试只证明观察契约，不证明最短路、匹配、调度或恢复本身正确。算法正确性仍由示例、性质、差分和不变量测试共同承担。

### 7.8 建立持续运行的证据链

“持续可证明”不是把测试称为形式化证明，而是让每次修改都重新运行彼此独立的证据：

1. 回归测试固定已知边界和历史缺陷。
2. 结构不变量检查内部状态是否自洽。
3. 模型、差分和状态机测试把优化实现与简单参考模型逐步比较。
4. 跨平台 CI 在 Windows/Linux 暴露路径、换行、大小写和区域性差异。
5. 变异测试检查断言能否识别有意义的错误。
6. BenchmarkDotNet 保存时间和分配趋势，但不在共享 CI 上设置脆弱的毫秒硬阈值。

覆盖率只说明哪些代码被执行，变异分数只说明现有变体是否被测试识别，基准只说明性能；三者都不能替代领域契约和独立 oracle。完整证据矩阵见 [综合项目实战学习指导](综合项目实战学习指导.md)。

## 8. 建议的练习方式

对每个主题完成三轮：

1. 阅读：逐行跟踪示例输入，写出每一步状态。
2. 默写：只看 API 和测试，独立实现。
3. 变式：修改约束，例如降序比较器、稳定排序、返回路径、恢复选择。

每个实现完成后回答：

- 不变量是什么？
- 最坏输入是什么？
- 复杂度是什么？
- 哪些地方可能溢出或越界？
- API 对空值和非法状态怎么处理？
- 能否用 BCL 类型完成，为什么这里仍然手写？

## 9. P0-P4、综合应用完成度与后续路线

仓库已经完成 P0/P1/P2：正确性修复、核心数据结构扩展、算法追踪、FsCheck 性质测试、状态机差分测试、BenchmarkDotNet、覆盖率与变异测试，以及高级图、字符串、区间和内存专题。

P3 继续补齐此前缺失的数学推理、算法族比较和可视化证据，并保持“不盲目堆数量”的原则。

### P3 已完成内容

- 数论与位运算：gcd/lcm、扩展欧几里得、模快速幂、模逆元、筛法、质因数分解、矩阵快速幂、置位与子集枚举。
- 图：Johnson、Hopcroft-Karp、欧拉路径、倍增 LCA，以及可恢复最小割并输出步骤的 Dinic。
- 字符串：Manacher、后缀自动机和基于 `Rune` 的 Unicode 滑动窗口。
- 动态规划：矩阵链区间 DP、树上最大权独立集和 Held-Karp 状态压缩 TSP。
- 题库：新增 14 道与原 100 题不重复的数值、位运算和高级专题题。
- 可视化：P3 Demo 支持 JSON 与 Mermaid，展示余数、区间分割、层次图、增广流和最小割。
- 工程质量：统一格式、默认分析器、编译器警告、80% 行覆盖率和 70% 分支覆盖率门禁。

完整推导、源码索引、逐题解释和八周计划见 [P3 数论、位运算与高级算法学习指导](P3数论位运算与高级算法学习指导.md)。

### 综合应用阶段已完成

- 城市配送 V2：先最大化派单数量，再以最小费用最大流最小化总旅行时间；最大匹配成为第一层目标的独立证据。
- 迷你搜索 V2：位置倒排表支持短语和邻近检索，BM25 改善相关性排序，BK-tree 用编辑距离三角不等式剪枝纠错候选。
- 项目调度 V2：懒标记树同时维护区间最大/最小值并用 `Int128` 安全组合延迟增量，小规模精确解为贪心工期提供最优性 oracle。
- 迷你存储引擎：B+ 树承担可惰性早停的有序事实索引，Bloom 与 LFU 分别加速负查询和热点读取，WAL/tombstone 及尾部提交标记提供可重放的恢复证据。

这一阶段的重点不是记住四套代码，而是练习从目标函数和领域约束选择结构：先定义跨模块不变量，再让每个结构只承担自己擅长的职责，最后用端到端证据同时验证结果、最优目标、依赖、容量和恢复等约束。详细场景、复杂度、限制和扩展练习集中在 [综合项目实战学习指导](综合项目实战学习指导.md)。

### P4：从可观察性升级为持续可证明

P4 不只增加结构，也把四个综合项目的正确性组织成可持续运行的证据链：

- 统一依赖 `IAlgorithmTraceSink`，默认 `null`；领域结果不包含诊断字段，开启观察前后使用同一算法路径，普通接收器异常由 `BestEffortAlgorithmTraceSink` 隔离。
- 四个控制台统一支持 `--trace json` 与 `--trace mermaid`；JSON 顶层固定为 `{ result, trace }`，事件带显式 schema version。
- 事件只覆盖流水线阶段和领域决策；数值使用 `InvariantCulture`，Mermaid state key 按 Ordinal 排序，输出可跨平台复现。
- `CollectingAlgorithmTraceSink` 复制只读快照，避免可变字典篡改已经发生的历史事件。
- 模型、性质、差分和状态机测试使用简单数组、完整排序、小规模穷举或标准有序字典作为独立 oracle。
- CI 在 Windows/Linux 运行相同 Release 契约，BenchmarkDotNet 记录趋势和分配，不使用受机器噪声影响的硬时间阈值。

这样组织是因为结构越多，组合边界越容易隐藏错误。追踪回答“错误发生在哪个阶段”，性质和差分回答“问题是否存在于输入族”，跨平台 CI 防止环境契约漂移，基准趋势回答“优化是否仍值得”；它们互相补充而不彼此替代。

### P4 后续路线

- 数据结构：Treap、可回滚并查集、持久化线段树、LSM Tree 与磁盘页模型。
- 算法：计算几何、二维区间结构、树链剖分，以及 cost-scaling、容量缩放或整数费用版本的最小费用流。
- 工程：显式栈 DFS、枚举器修改版本、WAL 校验/checkpoint、性能历史基线，并继续控制追踪事件粒度。
- 题库：只按新算法族加入配套题，不再以凑整数题量为目标。

“数据结构与算法”没有绝对完成状态。每增加一个主题，都应同时加入选择理由、核心不变量、复杂度推导、边界契约、正常/异常测试，以及适合该主题的差分、性质或变异证据。

## 10. 阶段验收清单

完成本仓库主干学习后，应能够：

- 从零实现链表、栈、队列、堆和二分查找。
- 实现双端队列，并解释循环数组扩容为什么必须按逻辑顺序复制。
- 比较六种排序的复杂度、稳定性和适用场景。
- 说明计数、桶和基数排序分别依赖什么输入条件。
- 写出前、中、后、层序遍历并说明递归栈空间。
- 解释 BST 退化原因与 AVL 旋转如何恢复平衡。
- 解释红黑树如何用颜色约束限制树高，并比较跳表的随机平衡策略。
- 比较链地址与开放寻址，说明墓碑删除为什么不可省略。
- 根据图的稠密度选择邻接表或邻接矩阵。
- 写出 BFS、DFS、拓扑排序和 Dijkstra，并说明各自前置条件。
- 能根据权重、查询次数和图结构选择 Bellman-Ford、Floyd、A*、Prim/Kruskal、Tarjan 或 Dinic，并说明为什么不能互换。
- 区分性质测试、状态机差分测试、变异测试、覆盖率和性能基准各自能证明什么、不能证明什么。
- 设计可缩减的小输入生成器，并用不同于被测实现的简单 oracle 验证综合场景不变量。
- 解释可选追踪 sink 为什么不应污染领域结果、best-effort 异常隔离为何避免“已提交却报失败”，以及默认关闭为什么能降低正常路径成本。
- 使用 `--trace json` 与 `--trace mermaid` 观察四个综合项目，并说明结构化数据与流程图的不同用途。
- 验证追踪前后领域结果一致，并解释 schema version、快照复制、InvariantCulture、Ordinal key 排序和特殊字符转义的必要性。
- 解释最小费用最大流为何能在最大派单数下继续优化总旅行时间。
- 从位置倒排表推导短语/邻近检索，并解释 BM25 和 BK-tree 的适用边界。
- 写出懒标记区间加/最大值不变量，并区分贪心可行解与小规模精确最优解。
- 说明 B+ 树、Bloom、LFU 和 WAL 在迷你存储引擎中分别承担事实索引、概率预筛、热点缓存和恢复证据。
- 解释 AC 自动机失败指针、后缀数组排名倍增、Sparse Table 幂等性和线段树懒标记为何正确。
- 正确管理 Span 与 ArrayPool 的内存所有权，并用基准数据决定是否采用池化版本。
- 为动态规划问题定义状态、写转移并恢复具体解。
- 用回溯模板表达选择、剪枝和撤销。
- 使用泛型比较器、可空引用类型、异常契约和 xUnit 测试组织 C# 算法代码。
- 知道何时应使用 .NET 标准集合，而不是把教学实现直接带入生产环境。

真正掌握的标志不是看懂代码，而是面对新约束时仍能从契约、不变量和复杂度重新推导出正确实现。
