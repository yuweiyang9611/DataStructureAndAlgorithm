using DataStructureAndAlgorithm.LeetCode;
using DataStructureAndAlgorithm.LeetCode.Models;

namespace DataStructureAndAlgorithm.Test.LeetCodeTest;

public class AdditionalTreeProblemsTest
{
    [Fact]
    public void P98_IsValidBinarySearchTree()
    {
        Assert.True(AdditionalTreeProblems.IsValidBinarySearchTree(new TreeNode(2, new TreeNode(1), new TreeNode(3))));
        Assert.False(AdditionalTreeProblems.IsValidBinarySearchTree(new TreeNode(5, new TreeNode(1), new TreeNode(4, new TreeNode(3), new TreeNode(6)))));
    }

    [Fact]
    public void P103_ZigzagLevelOrder()
    {
        var root = new TreeNode(3, new TreeNode(9), new TreeNode(20, new TreeNode(15), new TreeNode(7)));
        var result = AdditionalTreeProblems.ZigzagLevelOrder(root);
        Assert.True(result[0].SequenceEqual([3]));
        Assert.True(result[1].SequenceEqual([20, 9]));
        Assert.True(result[2].SequenceEqual([15, 7]));
    }

    [Fact]
    public void P104_MaximumDepth() => Assert.Equal(3,
        AdditionalTreeProblems.MaximumDepth(new TreeNode(3, new TreeNode(9), new TreeNode(20, new TreeNode(15), null))));

    [Fact]
    public void P108_SortedArrayToBinarySearchTree()
    {
        var root = AdditionalTreeProblems.SortedArrayToBinarySearchTree([-10, -3, 0, 5, 9]);
        Assert.True(InOrder(root).SequenceEqual([-10, -3, 0, 5, 9]));
        Assert.True(AdditionalTreeProblems.IsValidBinarySearchTree(root));
    }

    [Fact]
    public void P114_FlattenToLinkedList()
    {
        var root = new TreeNode(1, new TreeNode(2, new TreeNode(3), new TreeNode(4)), new TreeNode(5, null, new TreeNode(6)));
        AdditionalTreeProblems.FlattenToLinkedList(root);
        var values = new List<int>();
        for (var node = root; node is not null; node = node.Right)
        {
            Assert.Null(node.Left);
            values.Add(node.Value);
        }
        Assert.Equal([1, 2, 3, 4, 5, 6], values);
    }

    [Fact]
    public void P124_MaximumPathSum() => Assert.Equal(42,
        AdditionalTreeProblems.MaximumPathSum(new TreeNode(-10, new TreeNode(9), new TreeNode(20, new TreeNode(15), new TreeNode(7)))));

    [Fact]
    public void P199_RightSideView() => Assert.Equal([1, 3, 4],
        AdditionalTreeProblems.RightSideView(new TreeNode(1, new TreeNode(2, null, new TreeNode(5)), new TreeNode(3, null, new TreeNode(4)))));

    [Fact]
    public void P226_InvertTree()
    {
        var root = AdditionalTreeProblems.InvertTree(new TreeNode(2, new TreeNode(1), new TreeNode(3)))!;
        Assert.Equal(3, root.Left!.Value);
        Assert.Equal(1, root.Right!.Value);
    }

    [Fact]
    public void P230_KthSmallest()
    {
        var root = new TreeNode(3, new TreeNode(1, null, new TreeNode(2)), new TreeNode(4));
        Assert.Equal(1, AdditionalTreeProblems.KthSmallest(root, 1));
    }

    [Fact]
    public void P235_LowestCommonAncestorInBst()
    {
        var node2 = new TreeNode(2);
        var node8 = new TreeNode(8);
        var root = new TreeNode(6, node2, node8);
        Assert.Same(root, AdditionalTreeProblems.LowestCommonAncestorInBst(root, node2, node8));
    }

    [Fact]
    public void P437_CountPathsWithSum()
    {
        var root = new TreeNode(10,
            new TreeNode(5, new TreeNode(3, new TreeNode(3), new TreeNode(-2)), new TreeNode(2, null, new TreeNode(1))),
            new TreeNode(-3, null, new TreeNode(11)));
        Assert.Equal(3, AdditionalTreeProblems.CountPathsWithSum(root, 8));
    }

    [Fact]
    public void P543_Diameter() => Assert.Equal(3,
        AdditionalTreeProblems.Diameter(new TreeNode(1, new TreeNode(2, new TreeNode(4), new TreeNode(5)), new TreeNode(3))));

    [Fact]
    public void P572_IsSubtree()
    {
        var candidate = new TreeNode(4, new TreeNode(1), new TreeNode(2));
        var root = new TreeNode(3, candidate, new TreeNode(5));
        Assert.True(AdditionalTreeProblems.IsSubtree(root, new TreeNode(4, new TreeNode(1), new TreeNode(2))));
    }

    [Fact]
    public void P1448_CountGoodNodes()
    {
        var root = new TreeNode(3, new TreeNode(1, new TreeNode(3), null), new TreeNode(4, new TreeNode(1), new TreeNode(5)));
        Assert.Equal(4, AdditionalTreeProblems.CountGoodNodes(root));
    }

    [Fact]
    public void P297_SerializeAndDeserialize()
    {
        var root = new TreeNode(1, new TreeNode(2), new TreeNode(3, new TreeNode(4), new TreeNode(5)));
        var data = AdditionalTreeProblems.Serialize(root);
        Assert.Equal(data, AdditionalTreeProblems.Serialize(AdditionalTreeProblems.Deserialize(data)));
    }

    private static IEnumerable<int> InOrder(TreeNode? node)
    {
        if (node is null) yield break;
        foreach (var value in InOrder(node.Left)) yield return value;
        yield return node.Value;
        foreach (var value in InOrder(node.Right)) yield return value;
    }
}
