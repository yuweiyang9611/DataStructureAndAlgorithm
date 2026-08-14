using System.Runtime.CompilerServices;

// 精确搜索的节点预算属于内部安全阀，不应为了测试而扩大公共 API。
// 只向本仓库测试程序集开放内部优化器，使测试能注入极小预算并稳定证明“耗尽即失败”的契约。
[assembly: InternalsVisibleTo("DataStructureAndAlgorithm.Test")]
