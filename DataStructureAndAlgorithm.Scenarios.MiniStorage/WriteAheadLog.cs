using System.Text;
using System.Text.Json;

namespace DataStructureAndAlgorithm.Scenarios.MiniStorage;

/// <summary>
/// 每行一个 JSON 记录的教学版预写日志。
/// </summary>
/// <remarks>
/// <para>
/// 业务修改必须先成功追加并刷新 WAL，再进入内存索引。若进程在两步之间退出，重启回放仍能恢复该操作；
/// 反过来先改内存再写日志，则可能返回成功却在重启后丢失数据。
/// </para>
/// <para>换行符是记录的提交标记：崩溃留下的最后一个无换行片段会被忽略并在重新追加前截断，已提交行损坏则明确失败。</para>
/// </remarks>
internal sealed class WriteAheadLog : IDisposable
{
    private readonly FileStream _stream;
    private readonly StreamWriter _writer;
    private bool _disposed;

    public WriteAheadLog(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _stream = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(_stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };
    }

    public static IEnumerable<WalRecord> ReadAll(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            yield break;
        }

        var finalRecordIsCommitted = EndsWithCommitMarker(path);
        using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var lineNumber = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            // StreamWriter.WriteLine 总把 LF 写在一条完整 JSON 之后，所以 LF 可以作为最小提交标记。
            // 只忽略文件末尾唯一的未提交片段；任何位于中间或已经 LF 结尾的损坏都必须报错。
            if (reader.EndOfStream && !finalRecordIsCommitted)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                throw new InvalidDataException($"WAL 第 {lineNumber} 行是已提交的空白记录。");
            }

            WalRecord? record;
            try
            {
                record = JsonSerializer.Deserialize<WalRecord>(line);
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"WAL 第 {lineNumber} 行不是合法 JSON。", exception);
            }

            if (record is null || record.Sequence < 1 || string.IsNullOrWhiteSpace(record.Key) ||
                record.Operation is not (WalOperation.Put or WalOperation.Delete) ||
                record.Operation == WalOperation.Put && record.Value is null)
            {
                throw new InvalidDataException($"WAL 第 {lineNumber} 行缺少有效操作字段。");
            }

            yield return record;
        }
    }

    /// <summary>删除最后一个没有 LF 提交标记的崩溃片段，使后续追加从完整记录边界开始。</summary>
    internal static void DiscardUncommittedTail(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            return;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
        if (stream.Length == 0)
        {
            return;
        }

        stream.Position = stream.Length - 1;
        if (stream.ReadByte() == '\n')
        {
            return;
        }

        // 从尾部按块寻找最近的已提交边界，避免为了修复几个尾部字节把整个 WAL 读入内存。
        var buffer = new byte[4_096];
        var searchEnd = stream.Length;
        while (searchEnd > 0)
        {
            var blockLength = checked((int)Math.Min(buffer.Length, searchEnd));
            var blockStart = searchEnd - blockLength;
            stream.Position = blockStart;
            stream.ReadExactly(buffer.AsSpan(0, blockLength));
            for (var index = blockLength - 1; index >= 0; index--)
            {
                if (buffer[index] != '\n')
                {
                    continue;
                }

                stream.SetLength(blockStart + index + 1);
                return;
            }

            searchEnd = blockStart;
        }

        // 文件没有任何提交标记，说明全部内容都是一次未完成的首条写入。
        stream.SetLength(0);
    }

    private static bool EndsWithCommitMarker(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        if (stream.Length == 0)
        {
            return true;
        }

        stream.Position = stream.Length - 1;
        return stream.ReadByte() == '\n';
    }

    public void Append(WalRecord record)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var line = JsonSerializer.Serialize(record);
        _writer.WriteLine(line);
        // AutoFlush 会刷新 StreamWriter 与底层 FileStream；教学实现以可靠语义优先，不做批量刷盘优化。
        _writer.Flush();
        _stream.Flush(flushToDisk: true);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _writer.Dispose();
        _stream.Dispose();
        _disposed = true;
    }
}

internal enum WalOperation
{
    Put,
    Delete
}

internal sealed record WalRecord(long Sequence, WalOperation Operation, string Key, string? Value);
