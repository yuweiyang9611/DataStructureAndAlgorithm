# Scenario benchmark evidence

Commit: 7b588157a290d58979a92b4d3e89fa92be13aedc (working tree modified)

Each comparison requires identical method, parameters, job, SDK, runtime, OS and CPU. Dry is an execution check, not a performance conclusion.

| Method | Parameters | Job | Median (ns) | Allocated (B) | Previous median change |
| --- | --- | --- | ---: | ---: | ---: |
| DeliveryScalingScenarioBenchmarks.FirstPlan | LocationCount=20&OutDegree=2 | ShortRun | 534844.9 | 502120 | no comparable baseline |
| DeliveryScalingScenarioBenchmarks.CachedRoutes | LocationCount=20&OutDegree=2 | ShortRun | 103122.2 | 176496 | no comparable baseline |
| DeliveryScalingScenarioBenchmarks.FirstPlan | LocationCount=20&OutDegree=8 | ShortRun | 543554.3 | 710208 | no comparable baseline |
| DeliveryScalingScenarioBenchmarks.CachedRoutes | LocationCount=20&OutDegree=8 | ShortRun | 170342.2 | 270424 | no comparable baseline |
| DeliveryScalingScenarioBenchmarks.FirstPlan | LocationCount=100&OutDegree=2 | ShortRun | 1261735.9 | 2256432 | no comparable baseline |
| DeliveryScalingScenarioBenchmarks.CachedRoutes | LocationCount=100&OutDegree=2 | ShortRun | 247845.5 | 404488 | no comparable baseline |
| DeliveryScalingScenarioBenchmarks.FirstPlan | LocationCount=100&OutDegree=8 | ShortRun | 2934612.5 | 2940848 | no comparable baseline |
| DeliveryScalingScenarioBenchmarks.CachedRoutes | LocationCount=100&OutDegree=8 | ShortRun | 621538.0 | 938192 | no comparable baseline |
| ExactScalingScenarioBenchmarks.ExactComparison | TaskCount=4 | ShortRun | 3565.2 | 13040 | no comparable baseline |
| ExactScalingScenarioBenchmarks.ExactComparison | TaskCount=6 | ShortRun | 7114.5 | 20320 | no comparable baseline |
| ExactScalingScenarioBenchmarks.ExactComparison | TaskCount=8 | ShortRun | 10535.8 | 26992 | no comparable baseline |
| SchedulingScalingScenarioBenchmarks.Greedy | TaskCount=20 | ShortRun | 25557.6 | 60584 | no comparable baseline |
| SchedulingScalingScenarioBenchmarks.Greedy | TaskCount=100 | ShortRun | 167480.2 | 376792 | no comparable baseline |
| SearchScalingScenarioBenchmarks.BuildIndex | DocumentCount=100 | ShortRun | 264493.3 | 831912 | no comparable baseline |
| SearchScalingScenarioBenchmarks.CachedQuery | DocumentCount=100 | ShortRun | 172.2 | 904 | no comparable baseline |
| SearchScalingScenarioBenchmarks.Bm25Query | DocumentCount=100 | ShortRun | 59970.3 | 38936 | no comparable baseline |
| SearchScalingScenarioBenchmarks.ColdQuery | DocumentCount=100 | ShortRun | 314600.0 | 19832 | no comparable baseline |
| SearchScalingScenarioBenchmarks.BuildIndex | DocumentCount=1000 | ShortRun | 2858750.8 | 7973848 | no comparable baseline |
| SearchScalingScenarioBenchmarks.CachedQuery | DocumentCount=1000 | ShortRun | 172.6 | 904 | no comparable baseline |
| SearchScalingScenarioBenchmarks.Bm25Query | DocumentCount=1000 | ShortRun | 4378130.5 | 305280 | no comparable baseline |
| SearchScalingScenarioBenchmarks.ColdQuery | DocumentCount=1000 | ShortRun | 5293100.0 | 106232 | no comparable baseline |
| SearchScalingScenarioBenchmarks.BuildIndex | DocumentCount=10000 | ShortRun | 59012844.4 | 79163892 | no comparable baseline |
| SearchScalingScenarioBenchmarks.CachedQuery | DocumentCount=10000 | ShortRun | 161.5 | 904 | no comparable baseline |
| SearchScalingScenarioBenchmarks.Bm25Query | DocumentCount=10000 | ShortRun | 570487800.0 | 2910728 | no comparable baseline |
| SearchScalingScenarioBenchmarks.ColdQuery | DocumentCount=10000 | ShortRun | 522083400.0 | 946328 | no comparable baseline |
| StorageReadScalingScenarioBenchmarks.Read | LiveKeys=100&Hot=False | ShortRun | 62000.0 | 20992 | no comparable baseline |
| StorageReadScalingScenarioBenchmarks.Read | LiveKeys=100&Hot=True | ShortRun | 14100.0 | 8512 | no comparable baseline |
| StorageReadScalingScenarioBenchmarks.Read | LiveKeys=1000&Hot=False | ShortRun | 85700.0 | 20992 | no comparable baseline |
| StorageReadScalingScenarioBenchmarks.Read | LiveKeys=1000&Hot=True | ShortRun | 13600.0 | 8512 | no comparable baseline |
| StorageScalingScenarioBenchmarks.FullWalRecovery | LiveKeys=100&HistoryPerKey=1 | ShortRun | 3568700.0 | 232296 | no comparable baseline |
| StorageScalingScenarioBenchmarks.SnapshotRecovery | LiveKeys=100&HistoryPerKey=1 | ShortRun | 5395700.0 | 157768 | no comparable baseline |
| StorageScalingScenarioBenchmarks.Checkpoint | LiveKeys=100&HistoryPerKey=1 | ShortRun | 7686700.0 | 102616 | no comparable baseline |
| StorageScalingScenarioBenchmarks.FullWalRecovery | LiveKeys=100&HistoryPerKey=10 | ShortRun | 11180200.0 | 1794712 | no comparable baseline |
| StorageScalingScenarioBenchmarks.SnapshotRecovery | LiveKeys=100&HistoryPerKey=10 | ShortRun | 6778300.0 | 158456 | no comparable baseline |
| StorageScalingScenarioBenchmarks.Checkpoint | LiveKeys=100&HistoryPerKey=10 | ShortRun | 7146800.0 | 103448 | no comparable baseline |
| StorageScalingScenarioBenchmarks.FullWalRecovery | LiveKeys=1000&HistoryPerKey=1 | ShortRun | 10175700.0 | 2087664 | no comparable baseline |
| StorageScalingScenarioBenchmarks.SnapshotRecovery | LiveKeys=1000&HistoryPerKey=1 | ShortRun | 5645500.0 | 1235712 | no comparable baseline |
| StorageScalingScenarioBenchmarks.Checkpoint | LiveKeys=1000&HistoryPerKey=1 | ShortRun | 7331150.0 | 930272 | no comparable baseline |
| StorageScalingScenarioBenchmarks.FullWalRecovery | LiveKeys=1000&HistoryPerKey=10 | ShortRun | 54973900.0 | 17855888 | no comparable baseline |
| StorageScalingScenarioBenchmarks.SnapshotRecovery | LiveKeys=1000&HistoryPerKey=10 | ShortRun | 6033400.0 | 1242736 | no comparable baseline |
| StorageScalingScenarioBenchmarks.Checkpoint | LiveKeys=1000&HistoryPerKey=10 | ShortRun | 9231300.0 | 938760 | no comparable baseline |

## Recovery comparison

| Parameters | Job | Full WAL / snapshot median ratio |
| --- | --- | ---: |
| LiveKeys=100&HistoryPerKey=1 | ShortRun | 0.66 |
| LiveKeys=100&HistoryPerKey=10 | ShortRun | 1.65 |
| LiveKeys=1000&HistoryPerKey=1 | ShortRun | 1.80 |
| LiveKeys=1000&HistoryPerKey=10 | ShortRun | 9.11 |
