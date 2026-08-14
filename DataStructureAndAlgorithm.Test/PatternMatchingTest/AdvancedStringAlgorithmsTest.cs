using DataStructureAndAlgorithm.PatternMatching;

namespace DataStructureAndAlgorithm.Test.PatternMatchingTest;

public class AdvancedStringAlgorithmsTest
{
    [Fact]
    public void RabinKarp_ShouldFindOverlappingMatchesAndEmptyPatternBoundaries()
    {
        Assert.Equal([0, 1, 2], AdvancedStringAlgorithms.RabinKarpFindAll("aaaa", "aa"));
        Assert.Equal([0, 1, 2, 3], AdvancedStringAlgorithms.RabinKarpFindAll("abc", ""));
    }

    [Fact]
    public void ZFunction_ShouldReturnLongestPrefixMatches()
    {
        var z = AdvancedStringAlgorithms.CalculateZFunction("aabcaabxaaaz");

        Assert.Equal(12, z[0]);
        Assert.Equal(1, z[1]);
        Assert.Equal(3, z[4]);
        Assert.Equal(2, z[9]);
    }
}
