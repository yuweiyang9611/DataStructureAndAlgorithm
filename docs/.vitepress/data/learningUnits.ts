export type Difficulty = '入门' | '进阶' | '高级' | '混合'

export interface LearningUnit {
  id: string
  title: string
  stage: string
  stageLabel: string
  category: string
  difficulty: Difficulty
  prerequisites: string[]
  invariant: string
  complexity: string
  sources: string[]
  tests: string[]
  testCommand: string
  trace?: string
  guide: string
}

export const repository = 'https://github.com/yuweiyang9611/DataStructureAndAlgorithm'

export const stageLabels: Record<string, string> = {
  S02: '第二阶段 · 线性结构',
  S03: '第三阶段 · 查找与排序',
  S04: '第四阶段 · 树与堆',
  S05: '第五阶段 · 字符串与图',
  S06: '第六阶段 · 动态规划与回溯',
  A01: '进阶数据结构',
  A02: '进阶算法',
  P03: 'P3 高级专题',
  PRACTICE: '题库实践',
  S07: '第七阶段 · 综合项目'
}

export const learningUnits: LearningUnit[] = [
  {
    id: 'core-array-search',
    title: '数组模式、Span 与二分查找',
    stage: 'S03', stageLabel: stageLabels.S03, category: '数组 / 查找 / 内存', difficulty: '入门',
    prerequisites: [],
    invariant: '窗口、Kadane 与二分都必须固定区间语义；池化缓冲区必须在 finally 中归还。',
    complexity: 'Two Sum、滑窗、Kadane 为 O(n)；二分为 O(log n)。',
    sources: ['DataStructureAndAlgorithm/ArrayAlgorithms/ArrayAlgorithms.cs', 'DataStructureAndAlgorithm/Searching/SearchAlgorithms.cs', 'DataStructureAndAlgorithm/Memory/SpanAlgorithms.cs'],
    tests: ['DataStructureAndAlgorithm.Test/ArrayAlgorithmsTest/ArrayAlgorithmsTest.cs', 'DataStructureAndAlgorithm.Test/ArrayAlgorithmsTest/SpanAlgorithmsTest.cs', 'DataStructureAndAlgorithm.Test/SearchingTest/SearchAlgorithmsTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.ArrayAlgorithmsTest|FullyQualifiedName~DataStructureAndAlgorithm.Test.SearchingTest"',
    guide: '/主线/阶段一至三'
  },
  {
    id: 'core-linear-structures',
    title: '链表、栈、队列与双端队列',
    stage: 'S02', stageLabel: stageLabels.S02, category: '线性数据结构', difficulty: '入门',
    prerequisites: [],
    invariant: '相邻节点的双向引用必须一致；循环数组的逻辑顺序不受底层绕回影响。',
    complexity: '端点操作 O(1)，按值查找 O(n)，扩容摊还 O(1)。',
    sources: ['DataStructureAndAlgorithm/Linear/SinglyLinkedList.cs', 'DataStructureAndAlgorithm/Linear/DoublyLinkedList.cs', 'DataStructureAndAlgorithm/Linear/ArrayStack.cs', 'DataStructureAndAlgorithm/Linear/CircularQueue.cs', 'DataStructureAndAlgorithm/Linear/ArrayDeque.cs'],
    tests: ['DataStructureAndAlgorithm.Test/LinearTest/LinearStructuresTest.cs', 'DataStructureAndAlgorithm.Test/LinearTest/AdvancedLinearStructuresTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.LinearTest"',
    guide: '/主线/阶段一至三'
  },
  {
    id: 'core-sorting',
    title: '比较排序与非比较排序',
    stage: 'S03', stageLabel: stageLabels.S03, category: '排序', difficulty: '入门',
    prerequisites: ['core-array-search'],
    invariant: '归并两侧始终有序；LSD radix 的每一位分配必须保持此前低位的稳定顺序。',
    complexity: '归并/堆排序 O(n log n)，计数 O(n+k)，radix O(d(n+b))。',
    sources: ['DataStructureAndAlgorithm/Sorting/SortAlgorithms.cs', 'DataStructureAndAlgorithm/Sorting/NonComparisonSortAlgorithms.cs'],
    tests: ['DataStructureAndAlgorithm.Test/SortingTest/SortAlgorithmsTest.cs', 'DataStructureAndAlgorithm.Test/SortingTest/SortAlgorithmsDifferentialTest.cs', 'DataStructureAndAlgorithm.Test/SortingTest/NonComparisonSortAlgorithmsTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.SortingTest"',
    trace: 'radix', guide: '/主线/阶段一至三'
  },
  {
    id: 'advanced-hash-tables',
    title: '链地址与开放寻址哈希表',
    stage: 'A01', stageLabel: stageLabels.A01, category: '哈希', difficulty: '进阶',
    prerequisites: ['core-array-search'],
    invariant: '哈希与 Equals 必须来自同一比较器；查找跨过 Deleted，但在 Empty 终止。',
    complexity: '操作平均/摊还 O(1)，扩容 O(n)，极端冲突最坏 O(n)。',
    sources: ['DataStructureAndAlgorithm/Hashing/SeparateChainingHashTable.cs', 'DataStructureAndAlgorithm/Hashing/OpenAddressingHashTable.cs'],
    tests: ['DataStructureAndAlgorithm.Test/HashingTest/SeparateChainingHashTableTest.cs', 'DataStructureAndAlgorithm.Test/HashingTest/OpenAddressingHashTableTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.HashingTest"',
    guide: '/进阶/核心数据结构'
  },
  {
    id: 'advanced-ordered-trees',
    title: 'BST、AVL、红黑树与跳表',
    stage: 'S04', stageLabel: stageLabels.S04, category: '有序集合 / 树', difficulty: '高级',
    prerequisites: ['core-linear-structures'],
    invariant: '比较器定义顺序与重复；平衡树维护高度/颜色约束，跳表高层是低层的有序子集。',
    complexity: 'AVL/红黑树保证 O(log n)，跳表期望 O(log n)。',
    sources: ['DataStructureAndAlgorithm/Collections/IOrderedSet.cs', 'DataStructureAndAlgorithm/Tree/BinarySearchTree.cs', 'DataStructureAndAlgorithm/Tree/AvlTree.cs', 'DataStructureAndAlgorithm/Tree/RedBlackTree.cs', 'DataStructureAndAlgorithm/Tree/SkipList.cs'],
    tests: ['DataStructureAndAlgorithm.Test/TreeTest/BinarySearchTreeTest.cs', 'DataStructureAndAlgorithm.Test/TreeTest/AvlTreeTest.cs', 'DataStructureAndAlgorithm.Test/TreeTest/AdvancedOrderedStructuresTest.cs', 'DataStructureAndAlgorithm.Test/SetTest/OrderedSetContractTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.TreeTest.AdvancedOrderedStructuresTest|FullyQualifiedName~DataStructureAndAlgorithm.Test.SetTest.OrderedSetContractTest"',
    trace: 'red-black', guide: '/进阶/正确性修复与结构补强'
  },
  {
    id: 'advanced-heap-ipq',
    title: '二叉堆与索引优先队列',
    stage: 'S04', stageLabel: stageLabels.S04, category: '堆 / 优先队列', difficulty: '进阶',
    prerequisites: ['core-array-search'],
    invariant: '父节点优先级不劣于孩子；heap[index] 与 indexes[key] 每次交换后仍双向一致。',
    complexity: '建堆 O(n)，Peek O(1)，入队、出队和 decrease-key O(log n)。',
    sources: ['DataStructureAndAlgorithm/Heap/BinaryMinHeap.cs', 'DataStructureAndAlgorithm/Heap/IndexedPriorityQueue.cs'],
    tests: ['DataStructureAndAlgorithm.Test/HeapTest/BinaryMinHeapTest.cs', 'DataStructureAndAlgorithm.Test/HeapTest/IndexedPriorityQueueTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.HeapTest"',
    guide: '/进阶/高阶算法与内存'
  },
  {
    id: 'core-trie-kmp',
    title: 'Trie 与 KMP 单模式匹配',
    stage: 'S05', stageLabel: stageLabels.S05, category: '基础字符串', difficulty: '进阶',
    prerequisites: ['core-linear-structures'],
    invariant: 'Trie 路径表示前缀且删除不破坏共享路径；KMP 文本下标不回退。',
    complexity: 'Trie 操作 O(L)，KMP O(n+m)。',
    sources: ['DataStructureAndAlgorithm/Tree/Trie.cs', 'DataStructureAndAlgorithm/PatternMatching/StringPatternMatching.cs'],
    tests: ['DataStructureAndAlgorithm.Test/TreeTest/TrieTest.cs', 'DataStructureAndAlgorithm.Test/PatternMatchingTest/StringPatternMatchingTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.TreeTest.TrieTest|FullyQualifiedName~DataStructureAndAlgorithm.Test.PatternMatchingTest.StringPatternMatchingTest"',
    guide: '/主线/阶段四至六'
  },
  {
    id: 'advanced-disjoint-set',
    title: '并查集与动态连通性',
    stage: 'A01', stageLabel: stageLabels.A01, category: '集合 / 图基础', difficulty: '进阶',
    prerequisites: ['core-linear-structures'],
    invariant: '每个集合有唯一自指根；有效 Union 使 SetCount 恰减 1。',
    complexity: '路径压缩与按大小合并后，Find/Union 摊还 O(α(n))。',
    sources: ['DataStructureAndAlgorithm/Set/DisjointSet.cs'],
    tests: ['DataStructureAndAlgorithm.Test/SetTest/DisjointSetTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.SetTest.DisjointSetTest"',
    guide: '/进阶/核心数据结构'
  },
  {
    id: 'core-weighted-graphs',
    title: '加权图、最短路与最小生成森林',
    stage: 'S05', stageLabel: stageLabels.S05, category: '图算法', difficulty: '高级',
    prerequisites: ['advanced-heap-ipq', 'advanced-disjoint-set'],
    invariant: 'Dijkstra 只接受非负边；距离改进同步更新前驱；Kruskal 已选边始终不成环。',
    complexity: 'Dijkstra O((V+E) log V)，Bellman-Ford O(VE)，Kruskal O(E log E)。',
    sources: ['DataStructureAndAlgorithm/Graph/WeightedGraph.cs', 'DataStructureAndAlgorithm/Graph/WeightedGraphAlgorithms.cs', 'DataStructureAndAlgorithm/Graph/AdvancedGraphAlgorithms.cs'],
    tests: ['DataStructureAndAlgorithm.Test/GraphTest/WeightedGraphAlgorithmsTest.cs', 'DataStructureAndAlgorithm.Test/GraphTest/AdvancedGraphAlgorithmsTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.GraphTest.WeightedGraph|FullyQualifiedName~DataStructureAndAlgorithm.Test.GraphTest.AdvancedGraphAlgorithmsTest"',
    trace: 'dijkstra', guide: '/主线/阶段四至六'
  },
  {
    id: 'advanced-connectivity-flow',
    title: '全源最短路、低链接、A* 与费用流',
    stage: 'A02', stageLabel: stageLabels.A02, category: '高级图算法', difficulty: '高级',
    prerequisites: ['core-weighted-graphs'],
    invariant: 'A* 最优性依赖启发值不高估；费用流的正反残量边必须同步。',
    complexity: 'Floyd O(V³)，低链接 O(V+E)，费用流约 O(F·E log V)。',
    sources: ['DataStructureAndAlgorithm/Graph/ComprehensiveGraphAlgorithms.cs', 'DataStructureAndAlgorithm/Graph/FlowNetwork.cs', 'DataStructureAndAlgorithm/Graph/MinCostFlowAlgorithms.cs'],
    tests: ['DataStructureAndAlgorithm.Test/GraphTest/ComprehensiveGraphAlgorithmsTest.cs', 'DataStructureAndAlgorithm.Test/GraphTest/MinCostFlowAlgorithmsTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.GraphTest.ComprehensiveGraphAlgorithmsTest|FullyQualifiedName~DataStructureAndAlgorithm.Test.GraphTest.MinCostFlowAlgorithmsTest"',
    guide: '/进阶/高阶算法与内存'
  },
  {
    id: 'core-dynamic-programming',
    title: '动态规划：线性、区间、树形与状态压缩',
    stage: 'S06', stageLabel: stageLabels.S06, category: '动态规划', difficulty: '进阶',
    prerequisites: ['core-array-search', 'core-linear-structures'],
    invariant: '每个 DP 单元含义固定，填表顺序保证依赖已完成，方案恢复与状态同步。',
    complexity: 'LCS O(nm)，背包 O(nC)，矩阵链 O(n³)，Held-Karp O(n²2ⁿ)。',
    sources: ['DataStructureAndAlgorithm/DynamicProgramming/DynamicProgrammingAlgorithms.cs', 'DataStructureAndAlgorithm/DynamicProgramming/AdvancedDynamicProgrammingAlgorithms.cs', 'DataStructureAndAlgorithm/DynamicProgramming/P3DynamicProgrammingAlgorithms.cs'],
    tests: ['DataStructureAndAlgorithm.Test/DynamicProgrammingTest/DynamicProgrammingAlgorithmsTest.cs', 'DataStructureAndAlgorithm.Test/DynamicProgrammingTest/AdvancedDynamicProgrammingAlgorithmsTest.cs', 'DataStructureAndAlgorithm.Test/DynamicProgrammingTest/P3DynamicProgrammingAlgorithmsTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.DynamicProgrammingTest"',
    trace: 'matrix-chain', guide: '/主线/阶段四至六'
  },
  {
    id: 'core-backtracking',
    title: '回溯与组合搜索',
    stage: 'S06', stageLabel: stageLabels.S06, category: '回溯', difficulty: '进阶',
    prerequisites: ['core-linear-structures'],
    invariant: '递归返回前恢复进入本层前的状态；只扩展满足约束的候选。',
    complexity: '依问题呈指数或阶乘级；输出规模本身可能达到 Θ(2ⁿ)。',
    sources: ['DataStructureAndAlgorithm/Backtracking/BacktrackingAlgorithms.cs', 'DataStructureAndAlgorithm/Backtracking/CombinatorialAlgorithms.cs'],
    tests: ['DataStructureAndAlgorithm.Test/BacktrackingTest/BacktrackingAlgorithmsTest.cs', 'DataStructureAndAlgorithm.Test/BacktrackingTest/CombinatorialAlgorithmsTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.BacktrackingTest"',
    guide: '/主线/阶段四至六'
  },
  {
    id: 'advanced-caching',
    title: 'LRU 与 LFU 缓存',
    stage: 'A01', stageLabel: stageLabels.A01, category: '缓存', difficulty: '进阶',
    prerequisites: ['core-linear-structures', 'advanced-hash-tables'],
    invariant: '字典与链表节点一一对应；LFU 最小频率桶准确，同频率按 LRU 淘汰。',
    complexity: 'Get、Set 和淘汰平均 O(1)。',
    sources: ['DataStructureAndAlgorithm/Caching/LruCache.cs', 'DataStructureAndAlgorithm/Caching/LfuCache.cs'],
    tests: ['DataStructureAndAlgorithm.Test/CachingTest/LruCacheTest.cs', 'DataStructureAndAlgorithm.Test/CachingTest/LfuCacheTests.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.CachingTest"',
    guide: '/进阶/核心数据结构'
  },
  {
    id: 'advanced-range-query',
    title: 'Fenwick、线段树、Sparse Table 与懒标记',
    stage: 'A01', stageLabel: stageLabels.A01, category: '区间结构', difficulty: '高级',
    prerequisites: ['core-array-search'],
    invariant: '父节点聚合与子节点一致；懒标记已计入当前节点，Push/Pull 与失败回滚保持原子性。',
    complexity: 'Fenwick/线段树 O(log n)，Sparse Table 查询 O(1)。',
    sources: ['DataStructureAndAlgorithm/Range/FenwickTree.cs', 'DataStructureAndAlgorithm/Range/SegmentTree.cs', 'DataStructureAndAlgorithm/Range/SparseTable.cs', 'DataStructureAndAlgorithm/Range/LazyRangeAddMaxSegmentTree.cs'],
    tests: ['DataStructureAndAlgorithm.Test/RangeTest/RangeStructuresTest.cs', 'DataStructureAndAlgorithm.Test/RangeTest/AdvancedRangeQueryTest.cs', 'DataStructureAndAlgorithm.Test/RangeTest/LazyRangeAddMaxSegmentTreeTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.RangeTest"',
    guide: '/进阶/核心数据结构'
  },
  {
    id: 'p3-number-theory-bit',
    title: '数论与位运算',
    stage: 'P03', stageLabel: stageLabels.P03, category: '数论 / 位运算', difficulty: '高级',
    prerequisites: ['core-array-search'],
    invariant: 'GCD 的非零余数严格变小；快速幂每轮处理指数一个二进制位。',
    complexity: 'GCD/快速幂 O(log n)，筛法 O(n log log n)。',
    sources: ['DataStructureAndAlgorithm/NumberTheory/NumberTheoryAlgorithms.cs', 'DataStructureAndAlgorithm/BitManipulation/BitAlgorithms.cs'],
    tests: ['DataStructureAndAlgorithm.Test/NumberTheoryTest/NumberTheoryAlgorithmsTest.cs', 'DataStructureAndAlgorithm.Test/BitManipulationTest/BitAlgorithmsTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.NumberTheoryTest|FullyQualifiedName~DataStructureAndAlgorithm.Test.BitManipulationTest"',
    trace: 'gcd', guide: '/P3数论位运算与高级算法学习指导'
  },
  {
    id: 'p3-string-indexes',
    title: '高级字符串与文本索引',
    stage: 'P03', stageLabel: stageLabels.P03, category: '高级字符串', difficulty: '高级',
    prerequisites: ['core-trie-kmp', 'core-dynamic-programming'],
    invariant: '自动机 failure/suffix link 指向可复用后缀；后缀数组保持所有后缀的字典序。',
    complexity: 'AC O(text+matches)，教学后缀数组 O(n log² n)，Manacher/后缀自动机 O(n)。',
    sources: ['DataStructureAndAlgorithm/PatternMatching/AhoCorasickMatcher.cs', 'DataStructureAndAlgorithm/PatternMatching/SuffixArrayAlgorithms.cs', 'DataStructureAndAlgorithm/PatternMatching/P3StringAlgorithms.cs'],
    tests: ['DataStructureAndAlgorithm.Test/PatternMatchingTest/AdvancedIndexesTest.cs', 'DataStructureAndAlgorithm.Test/PatternMatchingTest/P3StringAlgorithmsTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.PatternMatchingTest.AdvancedIndexesTest|FullyQualifiedName~DataStructureAndAlgorithm.Test.PatternMatchingTest.P3StringAlgorithmsTest"',
    guide: '/进阶/高阶算法与内存'
  },
  {
    id: 'p3-graph-algorithms',
    title: 'Johnson、匹配、欧拉路径、LCA 与最大流',
    stage: 'P03', stageLabel: stageLabels.P03, category: '高级图算法', difficulty: '高级',
    prerequisites: ['core-weighted-graphs', 'advanced-connectivity-flow'],
    invariant: '匹配端点唯一；Dinic 只沿层次递增边发送阻塞流，残量源侧恢复最小割。',
    complexity: 'Hopcroft-Karp O(E√V)，欧拉路径 O(V+E)，Dinic 一般 O(V²E)。',
    sources: ['DataStructureAndAlgorithm/Graph/P3GraphAlgorithms.cs', 'DataStructureAndAlgorithm/Graph/P3FlowAlgorithms.cs'],
    tests: ['DataStructureAndAlgorithm.Test/GraphTest/P3GraphAlgorithmsTest.cs', 'DataStructureAndAlgorithm.Test/QualityTest/P3TracingTest.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.GraphTest.P3GraphAlgorithmsTest|FullyQualifiedName~DataStructureAndAlgorithm.Test.QualityTest.P3TracingTest"',
    guide: '/P3数论位运算与高级算法学习指导'
  },
  {
    id: 'practice-leetcode-100',
    title: 'LeetCode 100 题分型训练',
    stage: 'PRACTICE', stageLabel: stageLabels.PRACTICE, category: '题库实践', difficulty: '混合',
    prerequisites: ['core-array-search', 'core-linear-structures', 'core-trie-kmp', 'core-dynamic-programming', 'core-backtracking'],
    invariant: '每题先固定输入副作用、空输入、重复值与溢出契约，再选择模板。',
    complexity: '按题目从 O(1) 到指数级；每题单独写出推导。',
    sources: [
      'DataStructureAndAlgorithm/LeetCode/ClassicArrayProblems.cs',
      'DataStructureAndAlgorithm/LeetCode/ClassicStringProblems.cs',
      'DataStructureAndAlgorithm/LeetCode/ClassicLinkedListProblems.cs',
      'DataStructureAndAlgorithm/LeetCode/ClassicTreeProblems.cs',
      'DataStructureAndAlgorithm/LeetCode/ClassicGraphProblems.cs',
      'DataStructureAndAlgorithm/LeetCode/ClassicDynamicProgrammingProblems.cs',
      'DataStructureAndAlgorithm/LeetCode/AdditionalArrayProblems.cs',
      'DataStructureAndAlgorithm/LeetCode/AdditionalStringProblems.cs',
      'DataStructureAndAlgorithm/LeetCode/AdditionalLinkedListProblems.cs',
      'DataStructureAndAlgorithm/LeetCode/AdditionalTreeProblems.cs',
      'DataStructureAndAlgorithm/LeetCode/AdditionalGraphBacktrackingProblems.cs',
      'DataStructureAndAlgorithm/LeetCode/AdditionalDynamicGreedyProblems.cs',
      'DataStructureAndAlgorithm/LeetCode/AdvancedTopicProblems.cs',
      'DataStructureAndAlgorithm/LeetCode/NumericAndBitProblems.cs',
      'DataStructureAndAlgorithm/LeetCode/MinStack.cs',
      'DataStructureAndAlgorithm/LeetCode/KthLargestStream.cs',
      'DataStructureAndAlgorithm/LeetCode/Models/ListNode.cs',
      'DataStructureAndAlgorithm/LeetCode/Models/TreeNode.cs',
      'DataStructureAndAlgorithm/LeetCode/Models/GraphNode.cs',
      'DataStructureAndAlgorithm/LeetCode/Models/RandomListNode.cs'
    ],
    tests: [
      'DataStructureAndAlgorithm.Test/LeetCodeTest/ClassicArrayProblemsTest.cs',
      'DataStructureAndAlgorithm.Test/LeetCodeTest/ClassicStringProblemsTest.cs',
      'DataStructureAndAlgorithm.Test/LeetCodeTest/ClassicLinkedListProblemsTest.cs',
      'DataStructureAndAlgorithm.Test/LeetCodeTest/ClassicTreeProblemsTest.cs',
      'DataStructureAndAlgorithm.Test/LeetCodeTest/ClassicGraphProblemsTest.cs',
      'DataStructureAndAlgorithm.Test/LeetCodeTest/ClassicDynamicProgrammingProblemsTest.cs',
      'DataStructureAndAlgorithm.Test/LeetCodeTest/AdditionalArrayProblemsTest.cs',
      'DataStructureAndAlgorithm.Test/LeetCodeTest/AdditionalStringProblemsTest.cs',
      'DataStructureAndAlgorithm.Test/LeetCodeTest/AdditionalLinkedListProblemsTest.cs',
      'DataStructureAndAlgorithm.Test/LeetCodeTest/AdditionalTreeProblemsTest.cs',
      'DataStructureAndAlgorithm.Test/LeetCodeTest/AdditionalGraphBacktrackingProblemsTest.cs',
      'DataStructureAndAlgorithm.Test/LeetCodeTest/AdditionalDynamicGreedyProblemsTest.cs',
      'DataStructureAndAlgorithm.Test/LeetCodeTest/P3TopicProblemsTest.cs'
    ],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.LeetCodeTest"',
    guide: '/LeetCode100题完整学习指导'
  },
  {
    id: 'project-city-delivery',
    title: '城市即时配送 V2',
    stage: 'S07', stageLabel: stageLabels.S07, category: '综合项目 / 图与优化', difficulty: '高级',
    prerequisites: ['advanced-hash-tables', 'advanced-caching', 'advanced-disjoint-set', 'advanced-heap-ipq', 'advanced-connectivity-flow'],
    invariant: '先最大化分配数，再最小化总旅行时间；容量、方向、截止时间和稳定排序全部满足。',
    complexity: '候选最坏 O(CO(V+E)logV)，费用流约 O(FE logV)。',
    sources: ['DataStructureAndAlgorithm.Scenarios.CityDelivery/CityDeliveryPlanner.cs', 'DataStructureAndAlgorithm.Scenarios.CityDelivery/DomainModels.cs'],
    tests: ['DataStructureAndAlgorithm.Test/IntegratedProjectsTest/CityDeliveryPlannerTests.cs', 'DataStructureAndAlgorithm.Test/IntegratedProjectsTest/CityDeliveryPlannerPropertyTests.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.IntegratedProjectsTest.CityDelivery"',
    guide: '/综合项目/城市即时配送'
  },
  {
    id: 'project-mini-search',
    title: '迷你搜索引擎 V2',
    stage: 'S07', stageLabel: stageLabels.S07, category: '综合项目 / 信息检索', difficulty: '高级',
    prerequisites: ['core-linear-structures', 'advanced-hash-tables', 'core-trie-kmp', 'advanced-heap-ipq', 'advanced-caching', 'p3-string-indexes'],
    invariant: '位置列表有序且不跨字段；BM25 只在分数完全相等时用文档编号决胜。',
    complexity: '建索引平均 O(T)，Top-K O(N logK)，V2 BM25 O(N logN)。',
    sources: ['DataStructureAndAlgorithm.Scenarios.MiniSearch/MiniSearchEngineV2.cs', 'DataStructureAndAlgorithm.Scenarios.MiniSearch/SearchTextRules.cs'],
    tests: ['DataStructureAndAlgorithm.Test/IntegratedProjectsTest/MiniSearchEngineV2Tests.cs', 'DataStructureAndAlgorithm.Test/IntegratedProjectsTest/MiniSearchV2PropertyTests.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.IntegratedProjectsTest.MiniSearch"',
    guide: '/综合项目/迷你搜索引擎'
  },
  {
    id: 'project-scheduling',
    title: '项目调度 V2',
    stage: 'S07', stageLabel: stageLabels.S07, category: '综合项目 / DAG 与资源调度', difficulty: '高级',
    prerequisites: ['advanced-hash-tables', 'advanced-heap-ipq', 'advanced-range-query', 'core-weighted-graphs', 'core-dynamic-programming'],
    invariant: '任务满足依赖和容量；区间预留失败整体回滚；预算耗尽不能冒充已证明最优。',
    complexity: 'Kahn O((V+E)logV)，区间操作 O(logH)，精确搜索最坏指数级。',
    sources: ['DataStructureAndAlgorithm.Scenarios.ProjectScheduling/ProjectScheduler.cs', 'DataStructureAndAlgorithm.Scenarios.ProjectScheduling/ExactScheduleOptimizer.cs'],
    tests: ['DataStructureAndAlgorithm.Test/IntegratedProjectsTest/ProjectSchedulerTests.cs', 'DataStructureAndAlgorithm.Test/IntegratedProjectsTest/ProjectSchedulerExactTests.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.IntegratedProjectsTest.ProjectScheduler"',
    trace: 'project-scheduling', guide: '/综合项目/项目调度'
  },
  {
    id: 'project-mini-storage',
    title: '迷你存储引擎',
    stage: 'S07', stageLabel: stageLabels.S07, category: '综合项目 / 索引与恢复', difficulty: '高级',
    prerequisites: ['advanced-ordered-trees', 'advanced-caching', 'advanced-hash-tables'],
    invariant: 'WAL 先于内存修改；Bloom 不得假阴性；缓存与概率结构都不是事实来源。',
    complexity: 'B+ 树点查/写入 O(logn)，范围扫描 O(logn+p)，LFU 平均 O(1)。',
    sources: ['DataStructureAndAlgorithm.Scenarios.MiniStorage/MiniStorageEngine.cs', 'DataStructureAndAlgorithm.Scenarios.MiniStorage/WriteAheadLog.cs'],
    tests: ['DataStructureAndAlgorithm.Test/IntegratedProjectsTest/MiniStorageEngineTests.cs', 'DataStructureAndAlgorithm.Test/IntegratedProjectsTest/MiniStorageWalModelTests.cs'],
    testCommand: 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release --filter "FullyQualifiedName~DataStructureAndAlgorithm.Test.IntegratedProjectsTest.MiniStorage"',
    guide: '/综合项目/迷你存储引擎'
  }
]
