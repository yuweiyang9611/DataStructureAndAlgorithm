using DataStructureAndAlgorithm.LeetCode.Models;

namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>LeetCode 中经典的二叉树题目。</summary>
public static class ClassicTreeProblems
{
    /// <summary>
    /// LeetCode 102 - Binary Tree Level Order Traversal。
    /// </summary>
    /// <remarks>队列按层处理，时间 O(n)，最坏空间 O(n)。</remarks>
    public static IReadOnlyList<IReadOnlyList<int>> LevelOrder(TreeNode? root)
    {
        if (root is null)
        {
            return [];
        }

        var result = new List<IReadOnlyList<int>>();
        var queue = new Queue<TreeNode>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var levelSize = queue.Count;
            var level = new int[levelSize];

            for (var index = 0; index < levelSize; index++)
            {
                var node = queue.Dequeue();
                level[index] = node.Value;

                if (node.Left is not null)
                {
                    queue.Enqueue(node.Left);
                }

                if (node.Right is not null)
                {
                    queue.Enqueue(node.Right);
                }
            }

            result.Add(level);
        }

        return result;
    }

    /// <summary>
    /// LeetCode 236 - Lowest Common Ancestor of a Binary Tree。
    /// </summary>
    /// <remarks>假定 first 和 second 均存在于树中。时间 O(n)，递归栈 O(h)。</remarks>
    public static TreeNode? LowestCommonAncestor(TreeNode? root, TreeNode first, TreeNode second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (root is null || ReferenceEquals(root, first) || ReferenceEquals(root, second))
        {
            return root;
        }

        var leftResult = LowestCommonAncestor(root.Left, first, second);
        var rightResult = LowestCommonAncestor(root.Right, first, second);

        // 两侧都找到目标时，当前节点是第一次汇合的位置。
        return leftResult is not null && rightResult is not null
            ? root
            : leftResult ?? rightResult;
    }
}
