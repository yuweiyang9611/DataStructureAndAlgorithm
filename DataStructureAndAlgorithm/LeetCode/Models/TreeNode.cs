namespace DataStructureAndAlgorithm.LeetCode.Models;

/// <summary>LeetCode 二叉树题目常用的节点模型。</summary>
public sealed class TreeNode(int value = 0, TreeNode? left = null, TreeNode? right = null)
{
    public int Value { get; set; } = value;

    public TreeNode? Left { get; set; } = left;

    public TreeNode? Right { get; set; } = right;
}
