using System.Text;

namespace DataStructureAndAlgorithm.Scenarios.MiniSearch;

/// <summary>
/// MiniSearch 的词元字符规则，以及 UTF-16 索引到 Unicode 标量边界的适配。
/// </summary>
/// <remarks>
/// .NET 字符串和模式匹配结果使用 UTF-16 索引，而分词器按 <see cref="Rune"/> 遍历 Unicode 标量。
/// 把规则集中在这里，可以避免分词阶段认为某个辅助平面字母属于词元，高亮阶段却只看到一个代理项并误判为边界。
/// </remarks>
internal static class SearchTextRules
{
    /// <summary>判断一个完整 Unicode 标量是否属于 MiniSearch 词元。</summary>
    public static bool IsTokenCharacter(Rune value) =>
        Rune.IsLetterOrDigit(value) || value.Value is '_' or '#' or '+';

    /// <summary>
    /// 判断 UTF-16 索引之前的完整 Unicode 标量是否属于词元。
    /// </summary>
    public static bool IsTokenCharacterBefore(string text, int utf16Index)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(utf16Index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(utf16Index, text.Length);
        if (utf16Index == 0)
        {
            return false;
        }

        // DecodeLastFromUtf16 从索引左侧读取一个完整 Rune；若它是代理对，会一次消费两个 char，
        // 因而不会把低代理项误当成“非字母分隔符”。无效 UTF-16 会解码为非词元的替换字符，
        // 与 EnumerateRunes 在 Tokenize 中的容错语义保持一致。
        Rune.DecodeLastFromUtf16(text.AsSpan(0, utf16Index), out var previous, out _);
        return IsTokenCharacter(previous);
    }

    /// <summary>
    /// 判断 UTF-16 索引处开始的完整 Unicode 标量是否属于词元。
    /// </summary>
    public static bool IsTokenCharacterAt(string text, int utf16Index)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(utf16Index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(utf16Index, text.Length);
        if (utf16Index == text.Length)
        {
            return false;
        }

        // DecodeFromUtf16 从索引右侧读取完整 Rune，避免把高代理项误判为分隔符。
        Rune.DecodeFromUtf16(text.AsSpan(utf16Index), out var next, out _);
        return IsTokenCharacter(next);
    }
}
