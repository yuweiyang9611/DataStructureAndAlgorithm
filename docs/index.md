---
layout: home

hero:
  name: "DataStructure · Algorithm"
  text: "把算法学成可解释、可验证的工程能力"
  tagline: "基于 C# 14 与 .NET 10，从不变量和复杂度出发，沿着源码、测试、追踪与四个综合项目建立完整知识体系。"
  actions:
    - theme: brand
      text: 开始学习
      link: /开始学习
    - theme: alt
      text: 打开学习单元
      link: /学习单元
---

<div class="learning-shell">
  <section class="learning-stats" aria-label="项目学习规模">
    <div class="learning-stat"><strong>100 + 14</strong><span>100 题主线 + 14 道 P3</span></div>
    <div class="learning-stat"><strong>CI</strong><span>xUnit · FsCheck · 变异测试</span></div>
    <div class="learning-stat"><strong>4</strong><span>综合实战项目</span></div>
    <div class="learning-stat"><strong>22</strong><span>源码学习主题</span></div>
  </section>

  <section class="learning-intro">
    <div>
      <p class="learning-kicker">Learn by evidence</p>
      <h2>不止记住答案，<br>还要知道为什么正确。</h2>
    </div>
    <div class="learning-intro-copy">
      每个主题都沿着同一条证据链展开：先明确输入与契约，找到循环或结构不变量，手算最小样例，再推导复杂度，最后用 <code>xUnit</code>、性质测试和可选追踪固定理解。
    </div>
  </section>

  <section class="learning-section" aria-labelledby="path-title">
    <div class="learning-section-heading">
      <div>
        <p class="learning-kicker">A guided path</p>
        <h2 id="path-title">一条从基础到系统的学习路径</h2>
      </div>
      <p>先建立正确性思维，再扩大算法工具箱，最后把多个结构组合进真实约束。</p>
    </div>

  <ol class="learning-path">
      <li>
        <span class="learning-path-index">01</span>
        <div><strong>契约与复杂度</strong><small>半开区间、比较器、所有权、Big-O</small></div>
      </li>
      <li>
        <span class="learning-path-index">02</span>
        <div><strong>线性结构</strong><small>链表、栈、队列、双端队列</small></div>
      </li>
      <li>
        <span class="learning-path-index">03</span>
        <div><strong>查找、排序与树</strong><small>从二分边界走到平衡结构</small></div>
      </li>
      <li>
        <span class="learning-path-index">04</span>
        <div><strong>图、字符串与 DP</strong><small>选择算法，解释状态与不变量</small></div>
      </li>
      <li>
        <span class="learning-path-index">05</span>
        <div><strong>100 题分类主线</strong><small>把方法迁移到经典题型</small></div>
      </li>
      <li>
        <span class="learning-path-index">06</span>
        <div><strong>四个综合项目</strong><small>用端到端证据验证系统行为</small></div>
      </li>
    </ol>

  <a class="learning-text-link" href="./开始学习.html">选择适合你的第一步 <span aria-hidden="true">→</span></a>
  </section>

  <section class="learning-section" aria-labelledby="topics-title">
    <div class="learning-section-heading">
      <div>
        <p class="learning-kicker">Choose a track</p>
        <h2 id="topics-title">按目标进入专题</h2>
      </div>
      <p>每条路线都连接概念、实现、测试与练习，不需要在仓库目录里猜下一步。</p>
    </div>

  <div class="learning-topic-grid">
      <a class="learning-topic-card learning-topic-card--primary" href="./CSharp数据结构与算法学习指导.html">
        <span class="learning-card-label">FOUNDATION</span>
        <h3>数据结构与算法主线</h3>
        <p>从线性结构、排序和树出发，掌握字符串、图、动态规划与回溯。</p>
        <span class="learning-card-meta">8 个阶段 · 源码导读 · 验收清单</span>
      </a>
      <a class="learning-topic-card" href="./CSharp数据结构与算法进阶学习指导.html">
        <span class="learning-card-label">ADVANCED</span>
        <h3>进阶结构与工程方法</h3>
        <p>哈希、并查集、缓存、区间查询、高级图算法以及测试与性能证据。</p>
        <span class="learning-card-meta">比较器 · 不变量 · 性质测试</span>
      </a>
      <a class="learning-topic-card" href="./LeetCode100题完整学习指导.html">
        <span class="learning-card-label">PRACTICE</span>
        <h3>LeetCode 100 题</h3>
        <p>按题型组织的 C# 14 主线，逐题解释选择理由、核心不变量和常见错误。</p>
        <span class="learning-card-meta">16 周路线 · 100 道不重复题</span>
      </a>
      <a class="learning-topic-card" href="./P3数论位运算与高级算法学习指导.html">
        <span class="learning-card-label">P3</span>
        <h3>高级算法专题</h3>
        <p>数论、位运算、网络流、高级字符串与 DP，把工具箱扩展到复杂题型。</p>
        <span class="learning-card-meta">14 道专题题 · JSON / Mermaid 追踪</span>
      </a>
    </div>
  </section>

  <section class="learning-section" aria-labelledby="projects-title">
    <div class="learning-section-heading">
      <div>
        <p class="learning-kicker">From algorithms to systems</p>
        <h2 id="projects-title">四个项目，观察结构如何协作</h2>
      </div>
      <p>同一个结构在孤立练习里容易理解，在目标函数、容量和恢复约束中才真正学会取舍。</p>
    </div>

  <div class="learning-projects">
      <article class="learning-project">
        <span class="learning-project-no">01</span>
        <div>
          <h3>CityDelivery</h3>
          <p>在尽量多派单的前提下，用最小费用最大流最小化总旅行时间。</p>
          <small>图 · A* · 并查集 · 缓存 · 网络流</small>
        </div>
      </article>
      <article class="learning-project">
        <span class="learning-project-no">02</span>
        <div>
          <h3>MiniSearch</h3>
          <p>从位置倒排索引走到 BM25、短语检索、邻近查询与纠错。</p>
          <small>Trie · Aho-Corasick · BK-tree · Top-K</small>
        </div>
      </article>
      <article class="learning-project">
        <span class="learning-project-no">03</span>
        <div>
          <h3>ProjectScheduling</h3>
          <p>在 DAG 与资源约束下，对照确定性贪心与小规模精确解。</p>
          <small>关键路径 DP · 堆 · 懒标记线段树</small>
        </div>
      </article>
      <article class="learning-project">
        <span class="learning-project-no">04</span>
        <div>
          <h3>MiniStorage</h3>
          <p>沿真实读写路径组合主索引、概率结构、缓存与恢复日志。</p>
          <small>B+ 树 · Bloom Filter · LFU · WAL</small>
        </div>
      </article>
    </div>

  <a class="learning-text-link" href="./综合项目实战学习指导.html">进入综合项目路线 <span aria-hidden="true">→</span></a>
  </section>

  <section class="learning-evidence" aria-labelledby="evidence-title">
    <div class="learning-evidence-copy">
      <p class="learning-kicker">Evidence over intuition</p>
      <h2 id="evidence-title">让“我觉得正确”变成可复查的证据。</h2>
      <p>示例测试固定边界，不变量覆盖结构规则，性质与状态机测试覆盖输入族，追踪解释过程，覆盖率、变异测试与 Benchmark 分别回答不同的问题。</p>
      <a class="learning-text-link" href="./源码导航.html">打开文档与源码索引 <span aria-hidden="true">→</span></a>
    </div>
    <div class="learning-evidence-panel" aria-label="项目质量基线">
      <div><span>tests</span><strong>465 / 465</strong></div>
      <div><span>line coverage</span><strong>91.94%</strong></div>
      <div><span>branch coverage</span><strong>84.80%</strong></div>
      <div><span>mutation score</span><strong>81.07%</strong></div>
      <small>README 记录的 2026-08-15 / 2026-07-18 基线；以最新 CI 为准。</small>
    </div>
  </section>

  <section class="learning-cta" aria-labelledby="cta-title">
    <div>
      <p class="learning-kicker">Start with one invariant</p>
      <h2 id="cta-title">今天就从第一阶段开始。</h2>
      <p>读一段实现，手算一个最小例子，再用测试证明你的理解。</p>
    </div>
    <div class="learning-cta-actions">
      <a class="learning-cta-primary" href="./CSharp数据结构与算法学习指导.html">开始主线学习</a>
      <a class="learning-cta-secondary" href="https://github.com/yuweiyang9611/DataStructureAndAlgorithm">查看 GitHub 源码</a>
    </div>
  </section>
</div>
