namespace DataStructureAndAlgorithm.Graph;

/// <summary>最小生成森林中的一条无向边。</summary>
public readonly record struct ForestEdge<TVertex>(TVertex First, TVertex Second, double Weight)
    where TVertex : notnull;

/// <summary>Kruskal 算法返回的最小生成森林。</summary>
public sealed record MinimumSpanningForestResult<TVertex>(
    IReadOnlyList<ForestEdge<TVertex>> Edges,
    double TotalWeight,
    int ComponentCount)
    where TVertex : notnull;
