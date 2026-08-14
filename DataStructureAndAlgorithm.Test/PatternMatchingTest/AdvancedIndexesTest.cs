using DataStructureAndAlgorithm.PatternMatching;

namespace DataStructureAndAlgorithm.Test.PatternMatchingTest;

public class AdvancedIndexesTest
{
    [Fact]
    public void AhoCorasick_FindsOverlappingAndSuffixPatternsInOneScan()
    {
        var matcher = new AhoCorasickMatcher(["he", "she", "his", "hers", "he"]);

        var matches = matcher.FindAll("ushers")
            .Select(match => $"{match.Pattern}@{match.StartIndex}")
            .Order()
            .ToArray();

        Assert.True(matches.SequenceEqual(["he@2", "hers@2", "she@1"]));
    }

    [Fact]
    public void AhoCorasick_RejectsEmptyPatternSetsAndEmptyPatterns()
    {
        Assert.Throws<ArgumentException>(() => new AhoCorasickMatcher([]));
        Assert.Throws<ArgumentException>(() => new AhoCorasickMatcher(["valid", ""]));
    }

    [Fact]
    public void SuffixArray_BuildsClassicBananaOrderAndLcp()
    {
        var result = SuffixArrayAlgorithms.Build("banana");

        Assert.True(result.Suffixes.SequenceEqual([5, 3, 1, 0, 4, 2]));
        Assert.True(result.LongestCommonPrefixes.SequenceEqual([0, 1, 3, 0, 0, 2]));
        Assert.True(result.FindAll("banana", "ana").SequenceEqual([3, 1]));
        Assert.Empty(result.FindAll("banana", "xyz"));
        Assert.Throws<ArgumentException>(() => result.FindAll("banana", ""));
        Assert.False(result.Suffixes is int[]);
    }

    [Fact]
    public void SuffixArray_AgreesWithOrdinalSuffixSortingForRandomTexts()
    {
        var random = new Random(42);
        for (var sample = 0; sample < 100; sample++)
        {
            var text = new string(Enumerable.Range(0, random.Next(0, 40))
                .Select(_ => (char)('a' + random.Next(4))).ToArray());
            var expected = Enumerable.Range(0, text.Length)
                .OrderBy(index => text[index..], StringComparer.Ordinal)
                .ToArray();

            var actual = SuffixArrayAlgorithms.Build(text);

            Assert.True(actual.Suffixes.SequenceEqual(expected));
            for (var order = 1; order < expected.Length; order++)
            {
                var first = text.AsSpan(expected[order - 1]);
                var second = text.AsSpan(expected[order]);
                var common = 0;
                while (common < first.Length && common < second.Length && first[common] == second[common]) common++;
                Assert.Equal(common, actual.LongestCommonPrefixes[order]);
            }
        }
    }
}
