namespace DataStructureAndAlgorithm.PatternMatching;

/// <summary>
/// 字符串模式匹配：传统方法(<see cref="SimpleStringPatternMatching"/>)和Kmp算法(<see cref="KmpAlgorithm"/>)
/// </summary>
public class StringPatternMatching
{
    private readonly string _originalString;
    private readonly string _patternString;
    private readonly int _originalStringLength;
    private readonly int _patternStringLength;
    private readonly IReadOnlyList<int> _partiallyMatchedValues;

    public StringPatternMatching(string originalString, string patternString)
    {
        ArgumentNullException.ThrowIfNull(originalString);
        ArgumentNullException.ThrowIfNull(patternString);

        _originalString = originalString;
        _patternString = patternString;
        _originalStringLength = _originalString.Length;
        _patternStringLength = _patternString.Length;
        _partiallyMatchedValues = Array.AsReadOnly(BuildPrefixTable(_patternString));
    }

    /// <summary>
    /// 暴力算法，匹配失败后模式串完全回退，原始串从下一个位置开始匹配。(无法利用匹配失败后获得的信息)
    /// </summary>
    /// <param name="indexesOfMatching">输出完全匹配的字符串在原始字符串中的起始位置</param>
    /// <returns>第一个完全匹配的字符串在原始字符串中的起始位置</returns>
    public int SimpleStringPatternMatching(out List<int> indexesOfMatching)
    {
        if (_patternStringLength == 0)
        {
            indexesOfMatching = GetAllMatchesOfEmptyPattern();
            return 0;
        }

        indexesOfMatching = new List<int>();
        for (var startIndex = 0; startIndex <= _originalStringLength - _patternStringLength; startIndex++)
        {
            var indexOfPatternString = 0;
            while (indexOfPatternString < _patternStringLength &&
                   _originalString[startIndex + indexOfPatternString] == _patternString[indexOfPatternString])
            {
                indexOfPatternString++;
            }

            if (indexOfPatternString == _patternStringLength) indexesOfMatching.Add(startIndex);
        }

        if (indexesOfMatching.Count == 0) return -1;
        return indexesOfMatching[0];
    }

    /// <summary>
    /// 1. Kmp算法用到了部分匹配值，这个匹配值只与模式串自身有关，所以可以提前计算
    /// 2. 部分匹配值: 字符串的后缀子串与字符串的前缀子串中公共前后缀的最长长度
    /// </summary>
    /// <returns>计算得到的部分匹配值结果(用数组表示)</returns>
    public IEnumerable<int> CalculateNext()
        => _partiallyMatchedValues;

    private static int[] BuildPrefixTable(string pattern)
    {
        var partiallyMatchedValues = new int[pattern.Length];
        for (int i = 1, j = 0; i < pattern.Length;)
        {
            if (pattern[i] == pattern[j])
            {
                // 当前字符匹配，部分匹配值计数+1
                partiallyMatchedValues[i] = j + 1;
                i++;
                j++;
            }
            else if (j != 0)
            {
                // 如果当前字符不匹配，且j没回溯到0，就继续向前回溯
                j = partiallyMatchedValues[j - 1];
            }
            else
            {
                // j回溯到0，但当前字符还不匹配
                partiallyMatchedValues[i] = 0;
                i++;
            }
        }

        return partiallyMatchedValues;
    }

    /// <summary>
    /// 1. Kmp算法：最大程度利用匹配失败后获得的信息，确定模式串回退的位置。
    /// 2. Kmp算法与生成模式串的部分匹配值所用的动态规划方法相同。
    /// </summary>
    /// <param name="indexesOfMatching">输出完全匹配的字符串在原始字符串中的起始位置</param>
    /// <returns>第一个完全匹配的字符串在原始字符串中的起始位置</returns>
    public int KmpAlgorithm(out List<int> indexesOfMatching)
    {
        if (_patternStringLength == 0)
        {
            indexesOfMatching = GetAllMatchesOfEmptyPattern();
            return 0;
        }

        indexesOfMatching = new List<int>();

        // KMP算法
        for (int i = 0, j = 0; i < _originalStringLength;)
        {
            if (_originalString[i] == _patternString[j])
            {
                // 如果当前字符匹配成功,就继续向下匹配
                i++;
                j++;
                // 如果与模式串完全匹配就记录下主串中与模式串完全匹配的子串起始位置的索引，失败就继续执行循环
                if (j != _patternStringLength) continue;
                indexesOfMatching.Add(i - _patternStringLength);
                // 继续查找重叠匹配
                j = _partiallyMatchedValues[j - 1];
            }
            else if (j != 0)
            {
                // 如果当前字符匹配失败且模式串没回退到0就回退
                j = _partiallyMatchedValues[j - 1];
            }
            else
            {
                // 当前字符匹配失败，且模式串已经回退到0
                i++;
            }
        }

        if (indexesOfMatching.Count == 0) return -1;
        return indexesOfMatching[0];
    }

    private List<int> GetAllMatchesOfEmptyPattern()
    {
        var indexesOfMatching = new List<int>(_originalStringLength + 1);
        for (var i = 0; i <= _originalStringLength; i++)
        {
            indexesOfMatching.Add(i);
        }

        return indexesOfMatching;
    }
}
