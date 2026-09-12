using System.Security.Cryptography;
using System.Text.Json;

namespace DataStructureAndAlgorithm.Scenarios.MiniStorage;

/// <summary>校验实际负载字节，不依赖反序列化后的属性顺序。</summary>
internal static class PersistenceCodec
{
    public static string Encode<T>(T value)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(value);
        return JsonSerializer.Serialize(new Envelope(2, Convert.ToBase64String(payload), Convert.ToHexString(SHA256.HashData(payload))));
    }

    public static T Decode<T>(string text, bool allowLegacy = false)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            byte[] payload;
            if (document.RootElement.TryGetProperty("FormatVersion", out _))
            {
                var envelope = JsonSerializer.Deserialize<Envelope>(text);
                if (envelope is null || envelope.FormatVersion != 2 || envelope.Payload is null || envelope.Sha256 is null)
                    throw new InvalidDataException("不支持的持久化格式版本或缺少校验字段。");
                payload = Convert.FromBase64String(envelope.Payload);
                if (!StringComparer.Ordinal.Equals(Convert.ToHexString(SHA256.HashData(payload)), envelope.Sha256))
                    throw new InvalidDataException("持久化记录校验失败。");
            }
            else
            {
                if (!allowLegacy) throw new InvalidDataException("快照必须包含格式版本和校验和。");
                payload = System.Text.Encoding.UTF8.GetBytes(text);
            }

            return JsonSerializer.Deserialize<T>(payload) ?? throw new InvalidDataException("持久化记录不能为空。");
        }
        catch (Exception exception) when (exception is JsonException or FormatException or InvalidOperationException)
        {
            throw new InvalidDataException("持久化记录格式无效。", exception);
        }
    }

    private sealed record Envelope(int FormatVersion, string Payload, string Sha256);
}
