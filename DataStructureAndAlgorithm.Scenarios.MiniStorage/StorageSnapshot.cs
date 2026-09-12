namespace DataStructureAndAlgorithm.Scenarios.MiniStorage;

internal sealed record StorageSnapshot(
    [property: System.Text.Json.Serialization.JsonRequired] long Version,
    [property: System.Text.Json.Serialization.JsonRequired] StorageEntry[] Entries)
{
    public static StorageSnapshot? Read(string path)
    {
        if (!File.Exists(path)) return null;
        var snapshot = PersistenceCodec.Decode<StorageSnapshot>(File.ReadAllText(path));
        if (snapshot.Version < 0 || snapshot.Entries is null) throw new InvalidDataException("快照版本或条目无效。");
        string? previous = null;
        foreach (var entry in snapshot.Entries)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Key) || entry.Value is null ||
                entry.Version <= 0 || entry.Version > snapshot.Version ||
                previous is not null && StringComparer.Ordinal.Compare(previous, entry.Key) >= 0)
                throw new InvalidDataException("快照条目必须有序、唯一且具有有效版本。");
            previous = entry.Key;
        }

        return snapshot;
    }

    public void Publish(string path, IPersistenceFaults? faults)
    {
        var temporary = path + ".tmp";
        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(PersistenceCodec.Encode(this));
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var half = bytes.Length / 2;
                stream.Write(bytes.AsSpan(0, half));
                stream.Flush(flushToDisk: true);
                faults?.At(PersistenceStage.SnapshotPartialWrite);
                stream.Write(bytes.AsSpan(half));
                stream.Flush(flushToDisk: true);
            }

            faults?.At(PersistenceStage.SnapshotFlushed);
            File.Move(temporary, path, overwrite: true);
            faults?.At(PersistenceStage.SnapshotPublished);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
