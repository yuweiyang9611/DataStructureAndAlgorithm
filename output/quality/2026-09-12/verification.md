# 五项完善验收记录

日期：2026-09-12。结果来自当前工作区的本地验收；Windows 原生运行和独立 Linux .NET 10 容器均已验证。GitHub Actions 工作流已配置；本报告保留本地验收快照，远端运行状态以 PR 与 Actions 为准。

## 实现范围

- 学习进度：纯静态站点采用 IndexedDB 事务保存、跨标签页通知及焦点刷新；删除标记与存储代次阻止旧页面复活进度。旧 localStorage 只读迁移，写入失败保留页面内操作和导出。
- 备份与复习：v3 JSON 导出、v2/v3 严格导入、预览后按更新时间合并，保留本地同时间记录；复习资格与历史掌握状态分离。
- MiniStorage：校验信封、v1 WAL 兼容、独占写锁、原子快照发布、手动检查点、墓碑回收、增量恢复，以及失败后必须重开。默认 JSON/Trace 示例包含检查点与增量写入。
- 性能实验：保留原有小样本，新增搜索、配送、贪心调度、精确调度和存储独立参数组；固定输入、隔离准备与测量、恢复路径使用等价数据。
- 质量门禁：六项可靠性规则阻断构建、场景纳入覆盖率、五组独立变异测试、三浏览器 CI、NuGet 每周维护。README、学习页面、综合指导和 82 页 PDF 已更新。

## 实测结果

| 验收项 | 结果 |
| --- | --- |
| Windows Release 测试 | 508 通过，0 失败、0 跳过 |
| Linux Release 测试 | 508 通过，0 失败、0 跳过 |
| Release 构建 | 0 警告、0 错误 |
| dotnet format --verify-no-changes | 通过 |
| 行 / 分支覆盖率 | 95.38% / 86.86%（门槛 80% / 70%） |
| Node 状态、备份与报告门禁测试 | 9 通过 |
| TypeScript / Vue 类型检查 | 通过 |
| GitHub Actions 语法与表达式 | actionlint v1.7.11 通过 |
| Chromium / Firefox / WebKit | 每引擎 10 项，共 30 项通过 |
| 静态站点检查 | 36 个页面、导出数据和 6 个 Trace 资源通过 |
| 参数组合 | Dry 41 / 41，Short 41 / 41 |

浏览器测试包括双标签页修改、过期页面与重置冲突、学习与复习、前置依赖、迁移不重复、导入合并及非法备份、实际存储写入失败、移动视口、键盘路径、Trace 竞态和失败重试。持久化测试包括随机版本模型、首次及替换检查点各 7 个 I/O 故障边界、各 7 个子进程强制退出边界，以及正式快照损坏、非法字段、缺失序列、旧格式和重复检查点。

| Stryker Basic 作业 | 实际得分 | break 门槛 |
| --- | ---: | ---: |
| core | 80.79% | 60% |
| delivery | 82.65% | 60% |
| search | 80.28% | 60% |
| scheduling | 75.88% | 60% |
| storage | 76.03% | 60% |

配送组初次未通过门槛，补充数字边界、引用合法性、截止时间和优先级排序断言后通过；未降低门槛。

## 性能证据

测量基于提交 7b588157a290d58979a92b4d3e89fa92be13aedc 的已修改工作区。环境：Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)；Intel Core i7-14650HX；.NET 10.0.12 (10.0.12, 10.0.1226.42308)；X64；10.0.401；0.15.8。每组 Short 已完成一次；当前没有同参数、同环境、同作业的历史基线，趋势摘要明确标注不可比较。

| 参数 | 完整 WAL 中位耗时 (ms) | 快照增量中位耗时 (ms) | 完整 WAL / 快照 |
| --- | ---: | ---: | ---: |
| LiveKeys=100&HistoryPerKey=1 | 3.569 | 5.396 | 0.66 |
| LiveKeys=100&HistoryPerKey=10 | 11.180 | 6.778 | 1.65 |
| LiveKeys=1000&HistoryPerKey=1 | 10.176 | 5.646 | 1.80 |
| LiveKeys=1000&HistoryPerKey=10 | 54.974 | 6.033 | 9.11 |

以上只描述当前输入与本机测量，不设共享 CI 毫秒硬阈值。完整报告保留所有参数、耗时和分配量：

- [Dry 摘要](benchmarks-dry.md)
- [Short 摘要](benchmarks-short.md)
- [机器可读环境和测量数据](benchmark-evidence.json)

CI 在 PR 运行 Dry，周期任务按场景运行 Short，手动任务支持 Default 完整测量；报告保留 90 天。相同参数、SDK、运行时、操作系统、CPU 和作业才生成历史变化百分比。

## 维护与复验

    dotnet format DataStructureAndAlgorithm.slnx --verify-no-changes
    dotnet build DataStructureAndAlgorithm.slnx -c Release
    dotnet test DataStructureAndAlgorithm.slnx -c Release --no-build --collect "XPlat Code Coverage" --settings coverage.runsettings --results-directory TestResults/check
    ./tools/check_coverage.ps1 -Report TestResults/check
    npm ci
    npm run docs:typecheck
    npm run docs:test
    npm run docs:traces
    npm run docs:build
    npx playwright install chromium firefox webkit
    npm run docs:test:e2e

覆盖率结果目录须为空或仅含本轮的一份 coverage.cobertura.xml。变异测试在 DataStructureAndAlgorithm.Test 目录按 stryker-core.json、stryker-delivery.json、stryker-search.json、stryker-scheduling.json、stryker-storage.json 分别运行，每次仅选择一个被测项目。配置遵循 [Stryker 文档](https://stryker-mutator.io/docs/stryker-net/configuration/)；浏览器环境遵循 [Playwright 文档](https://playwright.dev/docs/intro)。

原始本地日志位于 TestResults，覆盖率位于 TestResults/audit-final，浏览器报告位于 playwright-report，完整变异报告位于 DataStructureAndAlgorithm.Test/StrykerOutput，原始 BenchmarkDotNet 报告位于 BenchmarkDotNet.Artifacts。上述原始目录由 Git 忽略；本目录保留便于代码评审的验收快照，CI 会上传对应工件。

持久化保证范围为单写者、同步调用和进程中断恢复；断电及文件系统元数据持久性不属于教学版保证。

## 完成审计补充

- 报告门禁逐项校验方法与参数，而非仅比较数量；重复、缺失、未知组合和不完整环境元数据均拒绝。报告反例与跨运行时不可比测试通过，已有 Dry / Short 各 41 条测量重新通过门禁。
- Trace 测试使用受控时钟验证播放推进与暂停静止，释放延迟旧响应后核对实际事件 JSON，确保旧响应不能覆盖新场景。
- 两轮检查点故障测试逐字节比较发布前的旧快照和 WAL，并在发布、截断边界重开验证业务版本。首次与替换快照两种情况均在 Windows / Linux 通过。
