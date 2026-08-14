using DataStructureAndAlgorithm.PatternMatching;

namespace DataStructureAndAlgorithm.Test.PatternMatchingTest;

public class StringPatternMatchingTest
{
    private const string OriginalString = "abababaababacbababacbde";
    private const string PatternString = "ababacb";

    private readonly StringPatternMatching _stringPatternMatching =
        new(OriginalString, PatternString);

    private readonly StringBuilder _result = new();

    private string ShowResult(IEnumerable<int> items)
    {
        _result.Clear();
        foreach (var item in items) _result.Append($"{item},");
        return _result.ToString();
    }

    [Fact]
    public void SimpleStringPatternMatching_Test1()
    {
        var returnValue = _stringPatternMatching.SimpleStringPatternMatching(out var indexOfMatching);
        Assert.Equal("7,14,", ShowResult(indexOfMatching));
        Assert.Equal(7, returnValue);
    }

    [Fact]
    public void SimpleStringPatternMatching_Test2() // 字符串无匹配时
    {
        var temp = new StringPatternMatching("afasfwetrwaef", "bvbbbc");
        var returnValue = temp.SimpleStringPatternMatching(out var indexOfMatching);
        Assert.Equal("", ShowResult(indexOfMatching));
        Assert.Equal(-1, returnValue);
    }

    [Fact]
    public void CalculatePartiallyMatchedValues_Test1()
    {
        var list = _stringPatternMatching.CalculateNext();
        Assert.Equal("0,0,1,2,3,0,0,", ShowResult(list));
    }

    [Fact]
    public void KmpAlgorithm_Test1()
    {
        var returnValue = _stringPatternMatching.KmpAlgorithm(out var indexOfMatching);
        Assert.Equal("7,14,", ShowResult(indexOfMatching));
        Assert.Equal(7, returnValue);
    }

    [Fact]
    public void KmpAlgorithm_Test2() // 字符串无匹配时
    {
        var temp = new StringPatternMatching("afasfwetrwaef", "bvbbbc");
        var returnValue = temp.KmpAlgorithm(out var indexOfMatching);
        Assert.Equal("", ShowResult(indexOfMatching));
        Assert.Equal(-1, returnValue);
    }
    [Fact]
    public void StringPatternMatching_ShouldSupportOverlappingMatches()
    {
        var temp = new StringPatternMatching("aaaaa", "aaa");

        var simpleReturnValue = temp.SimpleStringPatternMatching(out var simpleMatches);
        Assert.Equal("0,1,2,", ShowResult(simpleMatches));
        Assert.Equal(0, simpleReturnValue);

        var kmpReturnValue = temp.KmpAlgorithm(out var kmpMatches);
        Assert.Equal("0,1,2,", ShowResult(kmpMatches));
        Assert.Equal(0, kmpReturnValue);
    }

    [Fact]
    public void StringPatternMatching_EmptyPattern_ShouldMatchEveryBoundary()
    {
        var temp = new StringPatternMatching("abc", "");

        var simpleReturnValue = temp.SimpleStringPatternMatching(out var simpleMatches);
        Assert.Equal("0,1,2,3,", ShowResult(simpleMatches));
        Assert.Equal(0, simpleReturnValue);

        var kmpReturnValue = temp.KmpAlgorithm(out var kmpMatches);
        Assert.Equal("0,1,2,3,", ShowResult(kmpMatches));
        Assert.Equal(0, kmpReturnValue);
    }

    [Fact]
    public void Constructor_ShouldRejectNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new StringPatternMatching(null!, "pattern"));
        Assert.Throws<ArgumentNullException>(() => new StringPatternMatching("text", null!));
    }

    [Fact]
    public void CalculateNext_ShouldReturnStableCachedValues()
    {
        var first = _stringPatternMatching.CalculateNext().ToArray();
        var second = _stringPatternMatching.CalculateNext().ToArray();

        Assert.Equal(first, second);
        Assert.Equal(new[] { 0, 0, 1, 2, 3, 0, 0 }, second);
    }
}
