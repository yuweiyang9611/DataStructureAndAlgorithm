using DataStructureAndAlgorithm.Scenarios.MiniStorage;

if (args.Length != 2 || !Enum.TryParse<PersistenceStage>(args[1], out var target)) return 2;
using var engine = new MiniStorageEngine(new MiniStorageOptions(WriteAheadLogPath: args[0]), null, new CrashBarrier(target));
if (target is PersistenceStage.WalPartialWrite or PersistenceStage.WalFlushed) engine.Put("incremental", "value");
else engine.Checkpoint();
return 0;

internal sealed class CrashBarrier(PersistenceStage target) : IPersistenceFaults
{
    public void At(PersistenceStage stage)
    {
        if (stage != target) return;
        Console.WriteLine("READY");
        Console.Out.Flush();
        Thread.Sleep(Timeout.Infinite);
    }
}
