namespace DataStructureAndAlgorithm.Graph;

/// <summary>无向图中删除后会改变连通性的桥和割点。</summary>
public sealed record ConnectivityAnalysisResult<TVertex>(
    IReadOnlyList<ForestEdge<TVertex>> Bridges,
    IReadOnlyCollection<TVertex> ArticulationPoints)
    where TVertex : notnull;
