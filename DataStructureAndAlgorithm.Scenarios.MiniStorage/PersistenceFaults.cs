namespace DataStructureAndAlgorithm.Scenarios.MiniStorage;

internal enum PersistenceStage
{
    WalPartialWrite,
    WalFlushed,
    SnapshotPartialWrite,
    SnapshotFlushed,
    SnapshotPublished,
    WalTruncated,
    CheckpointCompleted
}

internal interface IPersistenceFaults
{
    void At(PersistenceStage stage);
}
