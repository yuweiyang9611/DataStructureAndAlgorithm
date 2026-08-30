export const masteryEvidenceIds = ['invariant', 'test', 'transfer'] as const

export type MasteryEvidenceId = typeof masteryEvidenceIds[number]

// 掌握内容或判定规则发生语义变化时递增；旧验证会回到“学习中”，避免沿用失效证据。
export const masteryRevision = 1

export interface MasteryContent {
  selfCheck: {
    question: string
    options: [string, string, string, string]
    correctIndex: number
    explanation: string
  }
  evidence: Record<MasteryEvidenceId, string>
}

export const masteryByUnitId: Record<string, MasteryContent> = {
  'core-array-search': {
    selfCheck: {
      question: '二分查找使用半开区间 [low, high)，当 target > values[mid] 时应如何更新？',
      options: ['high = mid - 1', 'low = mid + 1', 'low = mid', 'high = mid + 1'],
      correctIndex: 1,
      explanation: 'mid 已确定不可能是答案，新的候选区间是 [mid + 1, high)，同时保持 0 ≤ low ≤ high ≤ Length。'
    },
    evidence: {
      invariant: '用空数组和普通数组解释滑窗、Kadane、二分的区间语义，并说明池化缓冲区为何必须在 finally 中归还。',
      test: '运行本单元测试；补充空数组、单元素、目标不存在和重复元素的二分契约测试。',
      transfer: '把闭区间二分迁移为半开区间，构造“最后一个元素被漏查”的混用反例并修复。'
    }
  },
  'core-linear-structures': {
    selfCheck: {
      question: '容量为 capacity、队头为 head 的循环队列中，第 i 个逻辑元素的物理下标是什么？',
      options: ['head + i', '(head + i) % capacity', '(tail + i) % Count', '(head - i + capacity) % capacity'],
      correctIndex: 1,
      explanation: '逻辑顺序从 head 开始，取模才能跨过数组末端后从下标 0 继续。'
    },
    evidence: {
      invariant: '画出链表头尾插删前后的引用关系，并解释循环队列绕回后为什么逻辑顺序不随物理布局改变。',
      test: '运行本单元测试；增加“绕回 → 扩容 → 继续出队”，并与 Queue 或 LinkedList 参考模型对拍。',
      transfer: '构造扩容时只从 head 连续复制而丢失绕回段的反例，再改为按逻辑下标迁移元素。'
    }
  },
  'core-sorting': {
    selfCheck: {
      question: 'LSD radix sort 为什么要求每一位的分配过程稳定？',
      options: ['把空间复杂度降为 O(1)', '保留此前较低位已经建立的顺序', '自动支持所有负数表示', '保证时间复杂度为 O(n log n)'],
      correctIndex: 1,
      explanation: '处理更高位时，相同高位元素必须保留低位轮次的相对次序，否则此前排序结果会被破坏。'
    },
    evidence: {
      invariant: '分别解释归并阶段“两侧已有序”和 radix 阶段“此前低位顺序被稳定保留”的不变量。',
      test: '运行本单元测试；覆盖空数组、重复值、已排序、逆序，并与 Array.Sort 做随机差分。',
      transfer: '把稳定桶收集改成会反转同桶元素的实现，用最小反例展示错误后恢复稳定收集。'
    }
  },
  'advanced-hash-tables': {
    selfCheck: {
      question: '开放寻址查找遇到 Deleted 槽位时应如何处理？',
      options: ['立即判定键不存在', '继续沿探测序列查找', '把该槽位当作任意键匹配', '立即执行全表扩容'],
      correctIndex: 1,
      explanation: 'Deleted 表示此处曾有元素，后续槽位仍可能属于同一探测链；只有 Empty 才能终止未命中查找。'
    },
    evidence: {
      invariant: '解释 comparer 的 Equals 与哈希码必须一致，以及 Empty、Occupied、Deleted 如何维持探测链。',
      test: '运行本单元测试；用恒定哈希的键覆盖冲突、删除中间键、继续查找和复用墓碑槽。',
      transfer: '迁移到大小写不敏感 comparer，构造“Equals 相等但哈希不同”导致查找失败的反例。'
    }
  },
  'advanced-ordered-trees': {
    selfCheck: {
      question: 'BST、AVL、红黑树和跳表共同必须满足哪项有序结构契约？',
      options: ['根节点始终为黑色', '每个叶节点深度完全相同', '遍历结果遵守各自定义的比较顺序，且都拒绝重复值', '每个节点拥有固定层数'],
      correctIndex: 2,
      explanation: '根颜色只属于红黑树，严格高度和固定层数都不是共同约束；BST/AVL 使用 IComparable，红黑树/跳表可注入 IComparer，但都维护有序且不重复。'
    },
    evidence: {
      invariant: '分别说明 BST 顺序、AVL 高度差、红黑树黑高和跳表高层为低层有序子集的约束。',
      test: '运行本单元测试；随机执行增删查并与 SortedSet 对拍，同时逐步验证内部不变量。',
      transfer: '用同一组含重复值的操作验证四种结构的有序与唯一契约，并单独用降序 comparer 检查红黑树和跳表。'
    }
  },
  'advanced-heap-ipq': {
    selfCheck: {
      question: '索引优先队列交换 heap 中两个元素时，为什么必须同步更新 indexes？',
      options: ['保持数组容量不变', '让键仍能定位到自己新的堆下标', '避免所有相同优先级元素', '把建堆复杂度降为 O(log n)'],
      correctIndex: 1,
      explanation: 'heap 保存位置到键，indexes 保存键到位置；交换后若只更新一侧，decrease-key 会修改错误元素。'
    },
    evidence: {
      invariant: '解释父子优先级约束和 heap/indexes 双向映射，并逐步演示一次上浮交换。',
      test: '运行本单元测试；随机混合入队、出队和 decrease-key，与线性取最小值模型对拍。',
      transfer: '故意省略一次 inverse-index 更新，构造 decrease-key 作用于错误节点的最小反例并修复。'
    }
  },
  'core-trie-kmp': {
    selfCheck: {
      question: 'KMP 在 text[i] 与 pattern[j] 失配且 j > 0 时应执行什么操作？',
      options: ['i 回退一位且 j 清零', 'j = lps[j - 1] 且 i 不变', 'i 与 j 都加一', '删除 pattern[j]'],
      correctIndex: 1,
      explanation: 'LPS 给出已匹配前缀中可复用的最长真前后缀，因此文本下标无需回退。'
    },
    evidence: {
      invariant: '解释 Trie 删除共享前缀时的剪枝边界，以及 KMP 如何借助 LPS 保持文本下标单调前进。',
      test: '运行本单元测试；覆盖删除 car 后保留 card，以及在 aaaaa 中匹配 aaa 的重叠场景。',
      transfer: '构造删除某词时无条件删除整条 Trie 路径的反例，再改为只剪掉安全节点。'
    }
  },
  'advanced-disjoint-set': {
    selfCheck: {
      question: 'Union(a, b) 发现 a、b 已属于同一根时，正确行为是什么？',
      options: ['SetCount 减 1 并再次挂接', 'SetCount 不变并报告未发生合并', '创建一个新根', '清空两个集合的大小'],
      correctIndex: 1,
      explanation: '两个元素已经连通，没有集合被合并；重复减少 SetCount 会破坏组件计数。'
    },
    evidence: {
      invariant: '解释唯一自指根、根节点大小，以及路径压缩为何不改变集合成员关系。',
      test: '运行本单元测试；随机执行 Union/Find，与朴素连通分量模型对拍并核对 SetCount。',
      transfer: '构造不先 Find 根就直接互挂元素形成父指针环的反例，再改为只合并两个根。'
    }
  },
  'core-weighted-graphs': {
    selfCheck: {
      question: '图中可能存在负权边且需要检测负环时，应优先使用哪种算法？',
      options: ['Dijkstra', 'Bellman–Ford', 'Kruskal', 'BFS'],
      correctIndex: 1,
      explanation: 'Dijkstra 的贪心定型依赖边权非负；Bellman–Ford 可处理负权并检测可达负环。'
    },
    evidence: {
      invariant: '解释最短路松弛时距离与前驱为何同步，以及 Kruskal 每次选边为何不能连接同一分量。',
      test: '运行本单元测试；在随机非负图上让 Dijkstra 与 Bellman–Ford 对拍，并覆盖非连通图。',
      transfer: '给 Dijkstra 加入负权边，构造贪心定型失效的反例，并把输入路由到 Bellman–Ford。'
    }
  },
  'advanced-connectivity-flow': {
    selfCheck: {
      question: '费用流沿正向残量边发送 f 单位流量后，反向边应如何变化？',
      options: ['反向容量减少 f 且费用相同', '反向容量增加 f 且费用取反', '反向边保持不变', '删除反向边'],
      correctIndex: 1,
      explanation: '反向残量边表示撤销已发送流量的能力，其容量增加 f，费用必须抵消正向费用。'
    },
    evidence: {
      invariant: '解释 A* 启发值不高估的作用，以及费用流正反残量边在容量和费用上的对应关系。',
      test: '运行本单元测试；覆盖流量守恒、正反容量同步、低链接朴素校验和小图费用流穷举。',
      transfer: '把 A* 启发函数迁移为 0，验证退化为 Dijkstra；再用高估值构造次优路径反例。'
    }
  },
  'core-dynamic-programming': {
    selfCheck: {
      question: '将 0/1 背包压缩为一维数组后，容量为什么要从大到小遍历？',
      options: ['优先选择更重物品', '避免同一物品在本轮被重复使用', '把复杂度降为 O(n)', '保证背包恰好装满'],
      correctIndex: 1,
      explanation: '降序更新使 dp[c - weight] 仍来自上一轮；升序会读取本轮新值，等价于允许重复选取。'
    },
    evidence: {
      invariant: '为一个线性 DP 和区间 DP 写清状态、转移、边界、填表顺序及方案恢复关系。',
      test: '运行本单元测试；对小规模背包、矩阵链或 TSP 使用穷举结果做随机差分。',
      transfer: '把 0/1 背包迁移为完全背包并改成容量升序，用单个物品可重复选择证明差异。'
    }
  },
  'core-backtracking': {
    selfCheck: {
      question: '全量枚举中，递归搜索一个候选返回后通常必须做什么？',
      options: ['保留候选并继续下一分支', '恢复到选择该候选之前的状态', '清空全部结果', '永久删除其余候选'],
      correctIndex: 1,
      explanation: '兄弟分支应从相同父状态出发；若不撤销选择，前一分支会污染后续搜索。'
    },
    evidence: {
      invariant: '用“选择—约束检查—递归—撤销”描述一层调用，并指出必须成对恢复的状态。',
      test: '运行本单元测试；覆盖 N 皇后已知解数、输出合法性及调用前后输入未被污染。',
      transfer: '把排列迁移为组合搜索，说明 startIndex 如何消除重复，并展示遗漏撤销的最小反例。'
    }
  },
  'advanced-caching': {
    selfCheck: {
      question: 'LFU 中多个键频率相同且需要淘汰时，应淘汰哪个键？',
      options: ['数值最小的键', '同频率桶中最久未使用的键', '最近刚访问的键', '随机键'],
      correctIndex: 1,
      explanation: 'LFU 先比较频率，最低频率相同时按 LRU 次序淘汰，才能得到确定且符合契约的结果。'
    },
    evidence: {
      invariant: '解释 LRU 字典与链表的一一对应，以及 LFU 频率桶、桶内顺序和 minFrequency 的同步。',
      test: '运行本单元测试；随机执行 Get/Set，与简单扫描模型对拍，并覆盖容量 0 和更新已有键。',
      transfer: '构造提升最后一个最小频率键后未更新 minFrequency 的反例并修复。'
    }
  },
  'advanced-range-query': {
    selfCheck: {
      question: '懒标记“区间加、区间最大值”树遇到完全覆盖更新时应怎么做？',
      options: ['只修改 lazy', '立即递归所有叶节点', '当前最大值与 lazy 都加 delta', '只修改两个子节点'],
      correctIndex: 2,
      explanation: '当前节点聚合值必须立即反映整段更新，同时记录 lazy 供未来访问子节点时下推。'
    },
    evidence: {
      invariant: '统一说明区间边界，并解释线段树 Push、Pull 与失败回滚如何保持父子聚合一致。',
      test: '运行本单元测试；随机混合区间更新和查询，与朴素数组对拍并验证失败原子性。',
      transfer: '把区间加最大值迁移为区间加求和，说明 sum 节点为何要加 delta × 区间长度。'
    }
  },
  'p3-number-theory-bit': {
    selfCheck: {
      question: '对非零整数 x，表达式 x & (x - 1) 的作用是什么？',
      options: ['设置最低的 0 位', '清除最低的 1 位', '反转所有位', '只保留最高的 1 位'],
      correctIndex: 1,
      explanation: 'x - 1 会翻转最低 1 位及其右侧位，与 x 按位与后恰好清除最低置位。'
    },
    evidence: {
      invariant: '解释 Euclid 中非零余数严格变小，以及快速幂当前底数与剩余指数的关系。',
      test: '运行本单元测试；随机验证 GCD 的整除性与对称性，并与 BitOperations.PopCount 对拍。',
      transfer: '把普通快速幂迁移为模快速幂，构造中间乘法溢出的反例并明确安全策略。'
    }
  },
  'p3-string-indexes': {
    selfCheck: {
      question: 'Aho–Corasick 当前状态不存在字符转移时，应如何处理？',
      options: ['立即结束整个匹配', '沿 failure 链回退直到找到转移或根', '回退文本下标并清空结果', '删除当前状态'],
      correctIndex: 1,
      explanation: 'failure 链复用当前串的最长可用后缀，因此文本仍只扫描一次，也不会遗漏后缀模式。'
    },
    evidence: {
      invariant: '解释 failure/suffix link 如何复用后缀，以及后缀数组必须保持的字典序关系。',
      test: '运行本单元测试；用朴素逐模式匹配和后缀排序作为随机小文本的差分 oracle。',
      transfer: '用 he、she 和文本 she 构造未合并 failure 输出链时漏报 he 的反例并修复。'
    }
  },
  'p3-graph-algorithms': {
    selfCheck: {
      question: 'Dinic 在一次 BFS 分层阶段中只应沿哪类边发送阻塞流？',
      options: ['任意正容量边', 'level[v] = level[u] + 1 的正残量边', '只沿反向边', '只沿费用最小的边'],
      correctIndex: 1,
      explanation: '严格沿层次递增边构造无环层次网络，阻塞流结束后再重建层次。'
    },
    evidence: {
      invariant: '解释匹配端点唯一、Dinic 层次边和反向边，以及残量源侧为何对应最小割。',
      test: '运行本单元测试；验证流量守恒与 max-flow=min-cut，并与朴素匹配搜索对拍。',
      transfer: '把欧拉路径迁移到含平行边的图，构造按端点标记而误删平行边的反例，改用边 ID。'
    }
  },
  'practice-leetcode-100': {
    selfCheck: {
      question: '开始一道新题、尚未选择算法模板前，最应先确定什么？',
      options: ['代码尽量压缩到一行', '输入副作用、空输入、重复值和溢出等契约', '必须使用哈希表', '只需让给定样例通过'],
      correctIndex: 1,
      explanation: '契约决定可用算法、边界行为和测试 oracle；只通过样例无法证明隐藏边界正确。'
    },
    evidence: {
      invariant: '任选三题，先写输入输出、副作用、重复值与溢出契约，再说明模板不变量。',
      test: '运行本单元测试；任选五题补充边界测试，并至少为两题增加朴素解随机对拍。',
      transfer: '将 Two Sum 从哈希迁移为排序加双指针，比较索引恢复、输入修改和复杂度契约。'
    }
  },
  'project-city-delivery': {
    selfCheck: {
      question: '方案 A 分配 10 单、总时间 100；方案 B 分配 9 单、总时间 1。按项目目标应选择哪个？',
      options: ['方案 A', '方案 B', '两者等价', '仅按生成顺序决定'],
      correctIndex: 0,
      explanation: '目标是字典序：先最大化分配数，只有分配数相同时才比较总旅行时间。'
    },
    evidence: {
      invariant: '解释容量、道路方向、截止时间和稳定排序如何共同约束字典序目标。',
      test: '运行本单元测试；增加“少分一单但成本极低”的对抗用例，并与小实例穷举对拍。',
      transfer: '加入服务优先级作为第三层目标，证明未经界定的加权和不能替代字典序。'
    }
  },
  'project-mini-search': {
    selfCheck: {
      question: '短语的两个词分别位于同一文档的不同字段，即使全局位置相邻，是否应匹配？',
      options: ['应匹配，因为文档编号相同', '不应匹配，位置链不能跨字段', '仅当 BM25 大于 0 时匹配', '按文档编号奇偶决定'],
      correctIndex: 1,
      explanation: '短语匹配要求同一字段内位置连续；跨字段拼接会产生原文中不存在的虚假短语。'
    },
    evidence: {
      invariant: '解释位置列表的字段边界与有序性，并说明完全同分时才用文档编号决胜。',
      test: '运行本单元测试；覆盖跨字段短语、重复词位置和完全同分的稳定排序。',
      transfer: '加入统一归一化层，构造索引与查询采用不同规则而导致漏检的反例。'
    }
  },
  'project-scheduling': {
    selfCheck: {
      question: '精确搜索因节点预算耗尽而停止时，本项目应如何处理？',
      options: ['把当前最好方案标为已证最优', '明确失败，绝不能让未完成搜索冒充最优证明', '把未搜索方案都标记为不可行', '忽略资源容量继续搜索'],
      correctIndex: 1,
      explanation: '预算耗尽只说明搜索不完整；本项目选择明确抛出异常，不能返回一个冒充已证最优的结果。'
    },
    evidence: {
      invariant: '解释依赖顺序、资源容量、失败回滚，以及“可行”和“已证最优”的区别。',
      test: '运行本单元测试；注入预留失败验证回滚，再用小实例穷举核对精确优化器。',
      transfer: '把单资源迁移为两种资源，构造只检查其中一种而产生不可行日程的反例。'
    }
  },
  'project-mini-storage': {
    selfCheck: {
      question: '一次写入要具备崩溃恢复能力，WAL 记录最迟应在何时持久化？',
      options: ['内存修改并确认之后', '应用内存修改和向调用方确认之前', '仅在缓存淘汰时', 'Bloom Filter 返回可能存在时'],
      correctIndex: 1,
      explanation: 'Write-ahead 要求日志先行；若先修改或确认再落 WAL，崩溃后可能无法恢复写入状态。'
    },
    evidence: {
      invariant: '解释 WAL 先行、B+ 树有序索引、Bloom 无假阴性，以及缓存为何不是事实来源。',
      test: '运行本单元测试；在日志和内存修改边界注入崩溃，并与朴素键值模型核对恢复。',
      transfer: '为 WAL 增加序号和校验和，构造尾部记录截断并安全忽略残缺尾记录。'
    }
  }
}
