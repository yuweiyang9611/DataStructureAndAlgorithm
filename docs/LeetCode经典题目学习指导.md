# LeetCode 经典题目 C# 学习指导

## 1. 目标与使用方式

本专题不是按题号背答案，而是用经典题学习可迁移的算法模式。题目描述均为学习化概述，源码位于 `DataStructureAndAlgorithm/LeetCode/`，测试位于 `DataStructureAndAlgorithm.Test/LeetCodeTest/`。

题库现已扩展到 100 道；完整题号地图、新增 81 题的逐题“为什么”解释和 16 周路线见 [LeetCode 100 题完整学习指导](LeetCode100题完整学习指导.md)。

建议每道题执行五步：

1. 写出暴力解法及其复杂度。
2. 找出重复计算、单调性或可复用状态。
3. 明确循环/递归不变量。
4. 独立写代码，先覆盖最小与异常输入。
5. 对照项目实现，总结可迁移的模板而不是记代码行。

## 2. 题目地图

| 题号 | 题目 | 模式 | 项目方法 | 复杂度 |
| --- | --- | --- | --- | --- |
| 1 | Two Sum | 哈希表 | `ClassicArrayProblems.TwoSum` | O(n) |
| 3 | Longest Substring Without Repeating Characters | 滑动窗口 | `LongestSubstringWithoutRepeatingCharacters` | O(n) |
| 15 | 3Sum | 排序、双指针、去重 | `ThreeSum` | O(n²) |
| 20 | Valid Parentheses | 栈 | `HasValidParentheses` | O(n) |
| 21 | Merge Two Sorted Lists | 链表双指针 | `MergeTwoSortedLists` | O(n + m) |
| 42 | Trapping Rain Water | 左右双指针 | `TrapRainWater` | O(n) |
| 70 | Climbing Stairs | 一维动态规划 | `ClimbStairs` | O(n) |
| 76 | Minimum Window Substring | 可变滑动窗口 | `MinimumWindowSubstring` | O(n + m) |
| 102 | Binary Tree Level Order Traversal | BFS | `ClassicTreeProblems.LevelOrder` | O(n) |
| 121 | Best Time to Buy and Sell Stock | 前缀最优状态 | `MaximumStockProfit` | O(n) |
| 139 | Word Break | 前缀 DP | `WordBreak` | 最坏 O(n²) |
| 141 | Linked List Cycle | 快慢指针 | `HasCycle` | O(n) |
| 198 | House Robber | 状态压缩 DP | `HouseRobber` | O(n) |
| 200 | Number of Islands | 网格 DFS | `NumberOfIslands` | O(rows × columns) |
| 206 | Reverse Linked List | 指针重连 | `ReverseList` | O(n) |
| 207 | Course Schedule | 拓扑排序 | `CanFinishCourses` | O(V + E) |
| 215 | Kth Largest Element | Quickselect | `FindKthLargest` | 平均 O(n) |
| 236 | Lowest Common Ancestor | 树递归 | `LowestCommonAncestor` | O(n) |
| 322 | Coin Change | 完全背包 DP | `CoinChange` | O(amount × coinCount) |

本章精讲最初的 19 道核心题；项目现有 100 个不重复题号。部分算法与通用实现呼应，例如 322 通过适配器复用 `MinimumCoins`，展示“刷题 API”和“可复用算法库 API”的区别。

## 3. 哈希表题型

### 3.1 LeetCode 1 - Two Sum

目标概述：在整数序列中找到两个不同位置，使两数之和等于目标。

暴力解法枚举所有下标对，时间 O(n²)。优化思路是：扫描到 `value` 时，不需要回头逐个比较，只要询问此前是否见过 `target - value`。

不变量：在处理下标 `i` 前，字典只保存 `[0, i)` 中已经访问过的值。

```text
numbers = [2, 7, 11, 15], target = 9
i=0, value=2, 查找7失败，记录 2->0
i=1, value=7, 查找2成功，返回 [0,1]
```

为什么先查再加入：如果目标是 6、当前值是 3，先加入会把同一个下标同时作为两个答案。

C# 注意：`target - value` 可能发生 `int` 溢出。项目先提升到 `long`，确认补数仍在 int 范围内再查字典。

常见错误：

- 返回值而不是下标。
- 重复使用同一下标。
- 使用 `ContainsValue` 导致 O(n²)。
- 忽略整数回绕造成伪匹配。

扩展练习：Two Sum II（有序数组）、返回全部下标对、允许重复元素时如何去重。

## 4. 排序与双指针

### 4.1 LeetCode 15 - 3Sum

排序后固定第一个数，在其右侧用左右指针寻找另外两个数。当前和小于 0 时左指针右移；大于 0 时右指针左移。

关键不变量：固定 `first` 后，答案只在 `(first, left, right)` 的有序搜索区间内。

去重分两层：

1. `first` 与前一个值相同则跳过。
2. 找到答案后，左右指针分别跨过所有相同值。

项目复制输入后排序，避免题解悄悄修改调用方数组。代价是 O(n) 额外空间。

常见错误：只对结果使用 HashSet 去重，掩盖指针逻辑问题并增加空间；三数相加直接使用 int，极端值可能溢出。

### 4.2 LeetCode 42 - Trapping Rain Water

位置 i 的水量由左右最高边界的较小者决定：

```text
water[i] = min(leftMax[i], rightMax[i]) - height[i]
```

预计算左右数组需要 O(n) 空间。双指针把空间降到 O(1)：当 `leftMaximum <= rightMaximum` 时，左侧水量已经由左最高边界确定，因为右侧至少存在不低于它的边界。

不变量：指针外侧位置已经完成计水，`leftMaximum` 与 `rightMaximum` 分别是已扫描区域最高柱。

常见错误：只比较当前左右柱，而没有维护历史最高值；把负高度当合法输入；水量累计仍使用 int 导致大输入溢出。

### 4.3 LeetCode 121 - Best Time to Buy and Sell Stock

卖出日扫描到 `price` 时，最佳买入价就是此前最小价格。维护：

```text
minimumPrice = min(此前价格)
maximumProfit = max(maximumProfit, price - minimumPrice)
```

买入必须早于卖出，因此应在同一次扫描中维护前缀最小值，不能简单使用全局最小和最大。

扩展：允许多次交易、含手续费、冷冻期时，问题会转成状态机动态规划。

### 4.4 LeetCode 215 - Kth Largest Element

完整排序是 O(n log n)。Quickselect 只递归包含目标下标的一侧，平均 O(n)。

将“第 k 大”转换为升序下标：

```text
targetIndex = length - k
```

Partition 完成后，pivot 已位于最终排序位置：左侧不大于它，右侧大于它。比较 pivotIndex 与 targetIndex 决定继续哪一侧。

常见错误：k 是 1 基排名而数组是 0 基；重复值导致分区死循环；直接修改输入却没有写入契约。

## 5. 滑动窗口与字符串

### 5.1 LeetCode 3 - 无重复字符最长子串

窗口 `[windowStart, index]` 始终没有重复字符。字典保存字符最近出现位置。发现字符在当前窗口中出现过时：

```text
windowStart = previousIndex + 1
```

必须保证 `previousIndex >= windowStart`，否则旧位置已经在窗口外，不应让左边界倒退。

C# 字符串按 `char` 遍历，即 UTF-16 code unit。若题目扩展到完整 Unicode 文本，应考虑 Rune 和组合字符。

### 5.2 LeetCode 76 - Minimum Window Substring

这是“满足条件时收缩”的可变滑动窗口：

1. 右边界扩张，把新字符加入计数。
2. 当所有目标字符种类都达到所需数量，移动左边界缩短窗口。
3. 移除某字符使其低于需求时，窗口重新变为不满足，继续扩张。

`satisfiedKinds` 统计达到目标数量的字符种类，而不是目标字符总数。目标含重复字符时，例如 `AABC`，A 必须出现两次才算满足。

常见错误：每移动一次都完整比较两个字典，导致额外字符集成本；只判断字符是否出现而忽略数量；收缩后忘记更新满足状态。

### 5.3 LeetCode 20 - Valid Parentheses

遇到左括号入栈，右括号必须与栈顶匹配。最终栈必须为空。

为什么是栈：最近打开的括号必须最先关闭，正是 LIFO。

项目使用数组和计数器实现栈，避免为单个字符再创建节点；遇到非括号字符按契约返回 false。

## 6. 链表题型

### 6.1 公共节点模型

`LeetCode/Models/ListNode.cs` 提供 `Value`、`Next`、`FromValues` 和 `ToArray`。`FromValues` 使用现代 C# 的 `params ReadOnlySpan<int>`，调用时仍可写：

```csharp
var head = ListNode.FromValues(1, 2, 3);
```

### 6.2 LeetCode 21 - Merge Two Sorted Lists

两个指针分别指向两个链表尚未处理的最小节点，每次把较小者接到结果尾部。

项目复用并重新连接原节点，因此额外空间 O(1)，但会改变输入链表的链接关系。生产 API 必须明确这一副作用；若要求输入不可变，应复制节点。

### 6.3 LeetCode 141 - Linked List Cycle

慢指针每次走一步，快指针每次走两步。有环时，两者在环内的相对距离每轮缩短一步，最终相遇。

判断必须使用节点引用身份 `ReferenceEquals`，不能比较节点值。不同节点完全可以保存相同值。

扩展：相遇后让一个指针回到头部，两者都每次走一步，再次相遇的位置就是环入口。

### 6.4 LeetCode 206 - Reverse Linked List

每轮维护：

```text
previous：已经反转部分的头
current：尚未处理部分的头
next：修改 current.Next 前保存的后继
```

最常见错误是先修改 `current.Next` 再保存后继，导致未处理链表丢失。

## 7. 二叉树题型

### 7.1 LeetCode 102 - Level Order Traversal

队列中存放下一批待访问节点。每层开始时记录 `levelSize = queue.Count`，只处理这么多个节点；处理过程中加入的是下一层。

不变量：一次外层循环恰好消费一层，并把下一层完整放入队列。

常见错误：内层循环直接以不断变化的 `queue.Count` 为上限，导致多层混在一起。

### 7.2 LeetCode 236 - Lowest Common Ancestor

递归返回值表示“当前子树中找到的目标或公共祖先”：

- 当前节点就是目标时返回当前节点。
- 左右子树都返回非空，当前节点是首次汇合点。
- 只有一侧非空，把它向上传递。
- 两侧都为空，返回 null。

项目遵循题目常见前置条件：两个目标都存在于树中。若用于通用库，应同时返回“找到几个目标”，否则只有一个目标存在时会误把它当答案。

## 8. 图与网格

### 8.1 LeetCode 200 - Number of Islands

二维网格可视为隐式图：每个陆地格是顶点，上下左右陆地之间有边。扫描到未访问陆地时，岛屿数加一，并用 DFS 标记整个连通分量。

项目使用独立 visited 数组，不修改输入。常见题解把陆地改成水以节省空间，但那会产生输入副作用。

常见错误：把对角线也视为连通；访问入栈时不标记，导致同一节点重复入栈；不验证锯齿数组是否为矩形。

### 8.2 LeetCode 207 - Course Schedule

先修关系构成有向图。若图有环，就无法完成所有课程。

Kahn 算法：

1. 统计每门课程入度。
2. 把所有入度为 0 的课程入队。
3. 完成课程并删除其出边，使后继入度减一。
4. 最终完成数量等于课程总数，说明无环。

易错点：边方向应是 `prerequisite -> course`；重复先修边会重复增加入度，若允许重复输入应先去重。

## 9. 动态规划

### 9.1 LeetCode 70 - Climbing Stairs

到达第 n 阶的最后一步来自 n-1 或 n-2：

```text
ways[n] = ways[n - 1] + ways[n - 2]
```

只依赖前两项，可压缩为两个变量。项目把 0 阶定义为一种“什么也不做”的方案，并使用 `checked` 暴露 int 溢出。

### 9.2 LeetCode 198 - House Robber

处理当前房屋时只有两种互斥选择：

```text
不抢当前：oneStepBack
抢当前：twoStepsBack + currentValue
```

取两者最大值。这里的关键不是记公式，而是识别“当前选择与前一个位置冲突”。

### 9.3 LeetCode 139 - Word Break

`reachable[end]` 表示前 end 个字符可以由词典拼成。枚举最后一个单词的起点 start：

```text
reachable[start] && text[start..end] in dictionary
```

项目写法强调状态转移，但切片会创建新字符串。性能进阶可按最大单词长度限制 start，使用 Trie，或通过 Span 避免部分分配。

### 9.4 LeetCode 322 - Coin Change

状态是凑出金额 a 的最少硬币数：

```text
dp[a] = min(dp[a - coin] + 1)
```

每种硬币可重复使用，这是完全背包。无法到达的状态使用大于任何合法答案的哨兵值。

LeetCode API 用 -1 表示无解；项目通用算法用 `int?` 表示无解。适配器把 null 转成 -1，体现领域 API 与通用 API 的差异。

## 10. 如何独立解题

拿到新题时按以下顺序写在纸上：

1. 输入规模和数据范围。
2. 暴力解法及复杂度。
3. 是否存在有序性、单调性、重复子问题或局部状态。
4. 需要哪种结构：哈希表、栈、队列、堆、树还是图。
5. 循环或递归不变量。
6. 空输入、单元素、重复值、全负数、溢出和无解情况。
7. 是否修改输入，返回值所有权属于谁。

模式识别速查：

| 题目特征 | 优先考虑 |
| --- | --- |
| 查找补数、频次、最近位置 | 哈希表 |
| 有序数组中寻找组合 | 双指针、二分 |
| 连续子串/子数组条件 | 滑动窗口、前缀和 |
| 最近打开先关闭 | 栈 |
| 按层、最少步数 | BFS |
| 连通块 | DFS、BFS、并查集 |
| 依赖关系 | 拓扑排序 |
| 最优值由更小前缀决定 | 动态规划 |
| 第 k 大、不要求完整排序 | 堆、Quickselect |
| 枚举所有组合且可提前排除 | 回溯 |

## 11. 测试策略

每题至少覆盖：

- 官方风格示例。
- 空输入或最小合法输入。
- 重复元素。
- 无解输入。
- 极端数值和整数溢出。
- 不应修改输入时的输入快照。
- 链表和树使用节点身份，而不是只比较值。

可以进一步加入确定性随机差分测试：

- Two Sum 与 O(n²) 暴力解比较。
- 3Sum 与三重循环 + 集合规范化比较。
- Quickselect 与完整排序比较。
- Minimum Window 与枚举全部子串比较。
- Number of Islands 与并查集实现比较。

## 12. 四周练习计划

### 第 1 周：数组与字符串

按顺序完成 1、121、3、20、15、76。重点掌握哈希表、固定/可变窗口和双指针去重。

### 第 2 周：链表与树

完成 21、206、141、102、236。每题先画指针或递归返回值，不直接写代码。

### 第 3 周：图

完成 200、207，再回到项目中的 Dijkstra、Bellman-Ford 和 Kruskal，比较网格图、依赖图和带权图。

### 第 4 周：动态规划与选择

完成 70、198、139、322、215。每道 DP 题必须写状态定义、转移、初始化和答案位置。

## 13. 验收标准

完成专题后，应能够：

- 不看代码写出 Two Sum、无重复窗口和有效括号。
- 正确处理 3Sum 去重和雨水双指针不变量。
- 原地反转链表并解释为什么不会丢失后继。
- 使用快慢指针按节点身份检测环。
- 使用队列按层遍历树并递归求 LCA。
- 把岛屿问题建模为连通分量，把课程问题建模为拓扑排序。
- 为爬楼梯、打家劫舍、单词拆分和零钱兑换定义 DP 状态。
- 解释 Quickselect 为什么平均 O(n)，以及它的最坏情况。
- 在 C# 中正确处理 nullable 节点、集合表达式、Span、比较器和整数溢出。
- 为新题先写不变量与边界测试，再写实现。

刷题的最终目标不是提高已完成题目数量，而是面对新题时能从约束、结构和不变量推导出解法。
