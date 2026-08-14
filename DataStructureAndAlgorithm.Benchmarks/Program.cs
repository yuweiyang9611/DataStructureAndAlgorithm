using BenchmarkDotNet.Running;

// BenchmarkSwitcher 允许通过 --filter 只运行一个实验，适合学习时逐项观察。
// 正式测量必须使用 Release，并让 BenchmarkDotNet 创建独立进程完成预热和多轮采样。
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
