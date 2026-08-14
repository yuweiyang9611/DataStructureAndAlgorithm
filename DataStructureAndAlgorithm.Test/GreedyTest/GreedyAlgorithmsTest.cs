using DataStructureAndAlgorithm.Greedy;

namespace DataStructureAndAlgorithm.Test.GreedyTest;

public class GreedyAlgorithmsTest
{
    [Fact]
    public void IntervalScheduling_ShouldSelectMaximumNumberOfCompatibleIntervals()
    {
        Interval[] intervals =
        [
            new(1, 4),
            new(3, 5),
            new(0, 6),
            new(5, 7),
            new(5, 9),
            new(8, 9)
        ];

        var selected = GreedyAlgorithms.SelectMaximumNonOverlappingIntervals(intervals);

        Assert.Equal([new Interval(1, 4), new Interval(5, 7), new Interval(8, 9)], selected);
    }
}
