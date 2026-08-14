using DataStructureAndAlgorithm.DynamicProgramming;

namespace DataStructureAndAlgorithm.Test.DynamicProgrammingTest;

public class P3DynamicProgrammingAlgorithmsTest
{
    [Fact]
    public void MatrixChain_ChoosesTheBestSplit() =>
        Assert.Equal(26_000, P3DynamicProgrammingAlgorithms.MinimumMatrixChainMultiplications([40, 20, 30, 10, 30]));

    [Fact]
    public void TreeIndependentSet_CombinesTakeAndSkipStates()
    {
        var weights = new Dictionary<int, long> { [1] = 10, [2] = 1, [3] = 2, [4] = 10, [5] = 10 };
        var result = P3DynamicProgrammingAlgorithms.MaximumWeightIndependentSetOnTree(
            weights, [(1, 2), (1, 3), (2, 4), (2, 5)]);

        Assert.Equal(30, result); // 选择 1、4、5。
    }

    [Fact]
    public void HeldKarp_FindsMinimumHamiltonianCycle()
    {
        double[,] distances =
        {
            { 0, 10, 15, 20 },
            { 10, 0, 35, 25 },
            { 15, 35, 0, 30 },
            { 20, 25, 30, 0 }
        };

        Assert.Equal(80, P3DynamicProgrammingAlgorithms.TravelingSalesperson(distances));
    }
}
