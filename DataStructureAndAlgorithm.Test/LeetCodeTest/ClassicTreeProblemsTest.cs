using DataStructureAndAlgorithm.LeetCode;
using DataStructureAndAlgorithm.LeetCode.Models;

namespace DataStructureAndAlgorithm.Test.LeetCodeTest;

public class ClassicTreeProblemsTest
{
    [Fact]
    public void LevelOrder_ShouldGroupNodesByDepth()
    {
        var root = new TreeNode(
            3,
            new TreeNode(9),
            new TreeNode(20, new TreeNode(15), new TreeNode(7)));

        var levels = ClassicTreeProblems.LevelOrder(root);

        Assert.Collection(
            levels,
            level => Assert.Equal([3], level),
            level => Assert.Equal([9, 20], level),
            level => Assert.Equal([15, 7], level));
    }

    [Fact]
    public void LowestCommonAncestor_ShouldReturnFirstSplitNode()
    {
        var node5 = new TreeNode(5);
        var node1 = new TreeNode(1);
        var root = new TreeNode(3, node5, node1);
        var node6 = new TreeNode(6);
        var node2 = new TreeNode(2);
        node5.Left = node6;
        node5.Right = node2;

        Assert.Same(root, ClassicTreeProblems.LowestCommonAncestor(root, node5, node1));
        Assert.Same(node5, ClassicTreeProblems.LowestCommonAncestor(root, node6, node2));
    }
}
