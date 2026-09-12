# Scenario benchmark evidence

Commit: 7b588157a290d58979a92b4d3e89fa92be13aedc (working tree modified)

Each comparison requires identical method, parameters, job, SDK, runtime, OS and CPU. Dry is an execution check, not a performance conclusion.

| Method | Parameters | Job | Median (ns) | Allocated (B) | Previous median change |
| --- | --- | --- | ---: | ---: | ---: |
| DeliveryScalingScenarioBenchmarks.FirstPlan | LocationCount=20&OutDegree=2 | Dry | 2044900.0 | 531368 | no comparable baseline |
| DeliveryScalingScenarioBenchmarks.CachedRoutes | LocationCount=20&OutDegree=2 | Dry | 1728700.0 | 203184 | no comparable baseline |
| DeliveryScalingScenarioBenchmarks.FirstPlan | LocationCount=20&OutDegree=8 | Dry | 3319400.0 | 747136 | no comparable baseline |
| DeliveryScalingScenarioBenchmarks.CachedRoutes | LocationCount=20&OutDegree=8 | Dry | 1393900.0 | 304792 | no comparable baseline |
| DeliveryScalingScenarioBenchmarks.FirstPlan | LocationCount=100&OutDegree=2 | Dry | 9118800.0 | 2295920 | no comparable baseline |
| DeliveryScalingScenarioBenchmarks.CachedRoutes | LocationCount=100&OutDegree=2 | Dry | 2741700.0 | 441416 | no comparable baseline |
| DeliveryScalingScenarioBenchmarks.FirstPlan | LocationCount=100&OutDegree=8 | Dry | 16117600.0 | 3018736 | no comparable baseline |
| DeliveryScalingScenarioBenchmarks.CachedRoutes | LocationCount=100&OutDegree=8 | Dry | 3698900.0 | 1013520 | no comparable baseline |
| ExactScalingScenarioBenchmarks.ExactComparison | TaskCount=4 | Dry | 22289200.0 | 13448 | no comparable baseline |
| ExactScalingScenarioBenchmarks.ExactComparison | TaskCount=6 | Dry | 21292200.0 | 20608 | no comparable baseline |
| ExactScalingScenarioBenchmarks.ExactComparison | TaskCount=8 | Dry | 22020000.0 | 27240 | no comparable baseline |
| SchedulingScalingScenarioBenchmarks.Greedy | TaskCount=20 | Dry | 17337800.0 | 60688 | no comparable baseline |
| SchedulingScalingScenarioBenchmarks.Greedy | TaskCount=100 | Dry | 19697700.0 | 376896 | no comparable baseline |
| SearchScalingScenarioBenchmarks.BuildIndex | DocumentCount=100 | Dry | 1662000.0 | 843112 | no comparable baseline |
| SearchScalingScenarioBenchmarks.ColdQuery | DocumentCount=100 | Dry | 631500.0 | 19832 | no comparable baseline |
| SearchScalingScenarioBenchmarks.CachedQuery | DocumentCount=100 | Dry | 460300.0 | 944 | no comparable baseline |
| SearchScalingScenarioBenchmarks.Bm25Query | DocumentCount=100 | Dry | 3692600.0 | 38984 | no comparable baseline |
| SearchScalingScenarioBenchmarks.BuildIndex | DocumentCount=1000 | Dry | 11605700.0 | 8085848 | no comparable baseline |
| SearchScalingScenarioBenchmarks.ColdQuery | DocumentCount=1000 | Dry | 25863200.0 | 106232 | no comparable baseline |
| SearchScalingScenarioBenchmarks.CachedQuery | DocumentCount=1000 | Dry | 505200.0 | 944 | no comparable baseline |
| SearchScalingScenarioBenchmarks.Bm25Query | DocumentCount=1000 | Dry | 35792300.0 | 305328 | no comparable baseline |
| SearchScalingScenarioBenchmarks.BuildIndex | DocumentCount=10000 | Dry | 171706700.0 | 80283688 | no comparable baseline |
| SearchScalingScenarioBenchmarks.ColdQuery | DocumentCount=10000 | Dry | 580244800.0 | 970232 | no comparable baseline |
| SearchScalingScenarioBenchmarks.CachedQuery | DocumentCount=10000 | Dry | 496900.0 | 944 | no comparable baseline |
| SearchScalingScenarioBenchmarks.Bm25Query | DocumentCount=10000 | Dry | 668434000.0 | 2910728 | no comparable baseline |
| StorageReadScalingScenarioBenchmarks.Read | LiveKeys=100&Hot=False | Dry | 776200.0 | 20992 | no comparable baseline |
| StorageReadScalingScenarioBenchmarks.Read | LiveKeys=100&Hot=True | Dry | 532100.0 | 8512 | no comparable baseline |
| StorageReadScalingScenarioBenchmarks.Read | LiveKeys=1000&Hot=False | Dry | 719300.0 | 20992 | no comparable baseline |
| StorageReadScalingScenarioBenchmarks.Read | LiveKeys=1000&Hot=True | Dry | 516500.0 | 8512 | no comparable baseline |
| StorageScalingScenarioBenchmarks.FullWalRecovery | LiveKeys=100&HistoryPerKey=1 | Dry | 4670500.0 | 232296 | no comparable baseline |
| StorageScalingScenarioBenchmarks.SnapshotRecovery | LiveKeys=100&HistoryPerKey=1 | Dry | 5377200.0 | 157768 | no comparable baseline |
| StorageScalingScenarioBenchmarks.Checkpoint | LiveKeys=100&HistoryPerKey=1 | Dry | 8744500.0 | 102616 | no comparable baseline |
| StorageScalingScenarioBenchmarks.FullWalRecovery | LiveKeys=100&HistoryPerKey=10 | Dry | 11350600.0 | 1794712 | no comparable baseline |
| StorageScalingScenarioBenchmarks.SnapshotRecovery | LiveKeys=100&HistoryPerKey=10 | Dry | 6248000.0 | 158456 | no comparable baseline |
| StorageScalingScenarioBenchmarks.Checkpoint | LiveKeys=100&HistoryPerKey=10 | Dry | 6844500.0 | 103448 | no comparable baseline |
| StorageScalingScenarioBenchmarks.FullWalRecovery | LiveKeys=1000&HistoryPerKey=1 | Dry | 12082000.0 | 2087664 | no comparable baseline |
| StorageScalingScenarioBenchmarks.SnapshotRecovery | LiveKeys=1000&HistoryPerKey=1 | Dry | 9196900.0 | 1235712 | no comparable baseline |
| StorageScalingScenarioBenchmarks.Checkpoint | LiveKeys=1000&HistoryPerKey=1 | Dry | 9604700.0 | 930272 | no comparable baseline |
| StorageScalingScenarioBenchmarks.FullWalRecovery | LiveKeys=1000&HistoryPerKey=10 | Dry | 80241100.0 | 17855888 | no comparable baseline |
| StorageScalingScenarioBenchmarks.SnapshotRecovery | LiveKeys=1000&HistoryPerKey=10 | Dry | 9418100.0 | 1242736 | no comparable baseline |
| StorageScalingScenarioBenchmarks.Checkpoint | LiveKeys=1000&HistoryPerKey=10 | Dry | 8991500.0 | 938760 | no comparable baseline |

## Recovery comparison

| Parameters | Job | Full WAL / snapshot median ratio |
| --- | --- | ---: |
| LiveKeys=100&HistoryPerKey=1 | Dry | 0.87 |
| LiveKeys=100&HistoryPerKey=10 | Dry | 1.82 |
| LiveKeys=1000&HistoryPerKey=1 | Dry | 1.31 |
| LiveKeys=1000&HistoryPerKey=10 | Dry | 8.52 |
