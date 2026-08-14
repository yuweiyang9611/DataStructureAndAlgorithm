namespace DataStructureAndAlgorithm.LeetCode.Models;

/// <summary>LeetCode 133 使用的无向图节点；邻接关系通过节点引用表达。</summary>
public sealed class GraphNode(int value)
{
    public int Value { get; set; } = value;

    public IList<GraphNode> Neighbors { get; } = new List<GraphNode>();
}
