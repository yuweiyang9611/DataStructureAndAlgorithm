namespace DataStructureAndAlgorithm.Greedy;

/// <summary>经典贪心算法。</summary>
public static class GreedyAlgorithms
{
    /// <summary>
    /// 选择数量最多的互不重叠区间。
    /// </summary>
    /// <remarks>
    /// 每次选择结束最早的可行区间，为后续区间留下最大空间。排序成本 O(n log n)。
    /// </remarks>
    public static IReadOnlyList<Interval> SelectMaximumNonOverlappingIntervals(
        IEnumerable<Interval> intervals)
    {
        ArgumentNullException.ThrowIfNull(intervals);
        var ordered = intervals.OrderBy(interval => interval.End)
            .ThenBy(interval => interval.Start)
            .ToList();

        if (ordered.Any(interval => interval.Start > interval.End))
        {
            throw new ArgumentException("Every interval must satisfy Start <= End.", nameof(intervals));
        }

        var result = new List<Interval>();
        var hasSelection = false;
        var lastEnd = 0;

        foreach (var interval in ordered)
        {
            if (hasSelection && interval.Start < lastEnd)
            {
                continue;
            }

            result.Add(interval);
            lastEnd = interval.End;
            hasSelection = true;
        }

        return result;
    }
}

/// <summary>半开时间区间 [Start, End)。</summary>
public readonly record struct Interval(int Start, int End);
