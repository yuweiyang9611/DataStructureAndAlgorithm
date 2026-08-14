using System.Globalization;

namespace DataStructureAndAlgorithm.Diagnostics;

/// <summary>为数论和区间 DP 提供可选的结构化学习步骤。</summary>
public static class P3TraceScenarios
{
    /// <summary>追踪欧几里得算法中的每次除法；余数严格变小是算法终止的关键不变量。</summary>
    public static ulong GreatestCommonDivisor(long left, long right, IAlgorithmTraceSink? trace = null)
    {
        var dividend = Magnitude(left);
        var divisor = Magnitude(right);
        while (divisor != 0)
        {
            var remainder = dividend % divisor;
            trace?.Record("EuclideanGcd", "Remainder", "gcd(a,b) = gcd(b,a mod b)，且非零余数严格变小。",
                new Dictionary<string, string>
                {
                    ["dividend"] = dividend.ToString(CultureInfo.InvariantCulture),
                    ["divisor"] = divisor.ToString(CultureInfo.InvariantCulture),
                    ["remainder"] = remainder.ToString(CultureInfo.InvariantCulture)
                });
            (dividend, divisor) = (divisor, remainder);
        }
        return dividend;
    }

    /// <summary>追踪矩阵链区间 DP 每次找到更优分割点的过程。</summary>
    public static long MinimumMatrixChainMultiplications(IReadOnlyList<int> dimensions, IAlgorithmTraceSink? trace = null)
    {
        ArgumentNullException.ThrowIfNull(dimensions);
        if (dimensions.Count < 2) throw new ArgumentException("At least two dimensions are required.", nameof(dimensions));
        if (dimensions.Any(value => value <= 0)) throw new ArgumentException("Dimensions must be positive.", nameof(dimensions));
        var count = dimensions.Count - 1;
        var costs = new long[count, count];
        for (var length = 2; length <= count; length++)
            for (var left = 0; left + length <= count; left++)
            {
                var right = left + length - 1;
                costs[left, right] = long.MaxValue;
                for (var split = left; split < right; split++)
                {
                    var candidate = checked(costs[left, split] + costs[split + 1, right] +
                                            (long)dimensions[left] * dimensions[split + 1] * dimensions[right + 1]);
                    if (candidate >= costs[left, right]) continue;
                    costs[left, right] = candidate;
                    trace?.Record("MatrixChain", "ChooseSplit", "左右子链先各自最优，再支付最后一次矩阵乘法成本。",
                        new Dictionary<string, string>
                        {
                            ["interval"] = $"[{left},{right}]",
                            ["split"] = split.ToString(CultureInfo.InvariantCulture),
                            ["cost"] = candidate.ToString(CultureInfo.InvariantCulture)
                        });
                }
            }
        return count == 1 ? 0 : costs[0, count - 1];
    }

    private static ulong Magnitude(long value) => value >= 0 ? (ulong)value : (ulong)(-(value + 1)) + 1;
}
