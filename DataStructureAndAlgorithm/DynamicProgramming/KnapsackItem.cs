namespace DataStructureAndAlgorithm.DynamicProgramming;

/// <summary>0/1 背包问题中的一个物品。</summary>
/// <param name="Weight">物品重量，必须大于 0。</param>
/// <param name="Value">物品价值，必须大于等于 0。</param>
public readonly record struct KnapsackItem(int Weight, int Value);

/// <summary>0/1 背包的最优值以及构成该最优解的原始物品下标。</summary>
public sealed record KnapsackResult(int MaximumValue, IReadOnlyList<int> SelectedIndexes);
