using System.Globalization;
using DataStructureAndAlgorithm.LeetCode.Models;

namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>LeetCode 100 题专题中的进阶二叉树题。</summary>
public static class AdditionalTreeProblems
{
    /// <summary>98 - Validate Binary Search Tree。向下传递开放上下界，O(n)。</summary>
    public static bool IsValidBinarySearchTree(TreeNode? root)
    {
        return Validate(root, long.MinValue, long.MaxValue);

        static bool Validate(TreeNode? node, long lower, long upper)
        {
            if (node is null) return true;
            if (node.Value <= lower || node.Value >= upper) return false;
            return Validate(node.Left, lower, node.Value) && Validate(node.Right, node.Value, upper);
        }
    }

    /// <summary>103 - Binary Tree Zigzag Level Order Traversal。按层填入正向或反向下标。</summary>
    public static IReadOnlyList<IReadOnlyList<int>> ZigzagLevelOrder(TreeNode? root)
    {
        if (root is null) return [];
        var result = new List<IReadOnlyList<int>>();
        var queue = new Queue<TreeNode>();
        queue.Enqueue(root);
        var leftToRight = true;

        while (queue.Count > 0)
        {
            var count = queue.Count;
            var level = new int[count];
            for (var index = 0; index < count; index++)
            {
                var node = queue.Dequeue();
                level[leftToRight ? index : count - index - 1] = node.Value;
                if (node.Left is not null) queue.Enqueue(node.Left);
                if (node.Right is not null) queue.Enqueue(node.Right);
            }

            result.Add(level);
            leftToRight = !leftToRight;
        }

        return result;
    }

    /// <summary>104 - Maximum Depth of Binary Tree。高度等于较高子树高度加一。</summary>
    public static int MaximumDepth(TreeNode? root) => root is null
        ? 0
        : Math.Max(MaximumDepth(root.Left), MaximumDepth(root.Right)) + 1;

    /// <summary>108 - Convert Sorted Array to BST。每次选中点，得到高度平衡树，O(n)。</summary>
    public static TreeNode? SortedArrayToBinarySearchTree(IReadOnlyList<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        return Build(0, numbers.Count);

        TreeNode? Build(int start, int end)
        {
            if (start >= end) return null;
            var middle = start + (end - start) / 2;
            return new TreeNode(numbers[middle], Build(start, middle), Build(middle + 1, end));
        }
    }

    /// <summary>114 - Flatten Binary Tree to Linked List。前序栈保持待访问顺序，O(n)。</summary>
    public static void FlattenToLinkedList(TreeNode? root)
    {
        if (root is null) return;
        var stack = new Stack<TreeNode>();
        stack.Push(root);
        TreeNode? previous = null;

        while (stack.TryPop(out var current))
        {
            // 先压右再压左，确保栈顶按前序访问左子树。
            if (current.Right is not null) stack.Push(current.Right);
            if (current.Left is not null) stack.Push(current.Left);

            if (previous is not null)
            {
                previous.Left = null;
                previous.Right = current;
            }

            previous = current;
        }

        previous!.Left = null;
        previous.Right = null;
    }

    /// <summary>124 - Binary Tree Maximum Path Sum。向父节点只返回可延伸的一条支路，O(n)。</summary>
    public static int MaximumPathSum(TreeNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var maximum = int.MinValue;
        Gain(root);
        return maximum;

        int Gain(TreeNode? node)
        {
            if (node is null) return 0;
            var left = Math.Max(0, Gain(node.Left));
            var right = Math.Max(0, Gain(node.Right));
            maximum = Math.Max(maximum, checked(node.Value + left + right));
            return checked(node.Value + Math.Max(left, right));
        }
    }

    /// <summary>199 - Binary Tree Right Side View。每层最后出队的节点可见。</summary>
    public static IReadOnlyList<int> RightSideView(TreeNode? root)
    {
        if (root is null) return [];
        var result = new List<int>();
        var queue = new Queue<TreeNode>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var count = queue.Count;
            for (var index = 0; index < count; index++)
            {
                var node = queue.Dequeue();
                if (node.Left is not null) queue.Enqueue(node.Left);
                if (node.Right is not null) queue.Enqueue(node.Right);
                if (index == count - 1) result.Add(node.Value);
            }
        }

        return result;
    }

    /// <summary>226 - Invert Binary Tree。交换每个节点的左右子树，O(n)。</summary>
    public static TreeNode? InvertTree(TreeNode? root)
    {
        if (root is null) return null;
        (root.Left, root.Right) = (InvertTree(root.Right), InvertTree(root.Left));
        return root;
    }

    /// <summary>230 - Kth Smallest in BST。中序遍历按升序产生节点，O(h+k)。</summary>
    public static int KthSmallest(TreeNode? root, int k)
    {
        if (k < 1) throw new ArgumentOutOfRangeException(nameof(k));
        var stack = new Stack<TreeNode>();

        while (root is not null || stack.Count > 0)
        {
            while (root is not null)
            {
                stack.Push(root);
                root = root.Left;
            }

            root = stack.Pop();
            if (--k == 0) return root.Value;
            root = root.Right;
        }

        throw new ArgumentOutOfRangeException(nameof(k), "k exceeds the number of nodes.");
    }

    /// <summary>235 - LCA of a BST。利用两个目标相对当前值的方向决定继续哪侧，O(h)。</summary>
    public static TreeNode? LowestCommonAncestorInBst(TreeNode? root, TreeNode first, TreeNode second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        var lower = Math.Min(first.Value, second.Value);
        var upper = Math.Max(first.Value, second.Value);

        while (root is not null)
        {
            if (root.Value < lower) root = root.Right;
            else if (root.Value > upper) root = root.Left;
            else return root;
        }

        return null;
    }

    /// <summary>437 - Path Sum III。前缀和计数把任意向下路径查询降为 O(n)。</summary>
    public static int CountPathsWithSum(TreeNode? root, long target)
    {
        var prefixCounts = new Dictionary<long, int> { [0] = 1 };
        return Visit(root, 0);

        int Visit(TreeNode? node, long prefix)
        {
            if (node is null) return 0;
            prefix += node.Value;
            var count = prefixCounts.GetValueOrDefault(prefix - target);
            prefixCounts[prefix] = prefixCounts.GetValueOrDefault(prefix) + 1;
            count += Visit(node.Left, prefix) + Visit(node.Right, prefix);

            // 离开当前递归路径时撤销，禁止把其他分支的前缀当祖先。
            if (--prefixCounts[prefix] == 0) prefixCounts.Remove(prefix);
            return count;
        }
    }

    /// <summary>543 - Diameter of Binary Tree。每个节点用左右高度更新经过它的最长边数。</summary>
    public static int Diameter(TreeNode? root)
    {
        var diameter = 0;
        Height(root);
        return diameter;

        int Height(TreeNode? node)
        {
            if (node is null) return 0;
            var left = Height(node.Left);
            var right = Height(node.Right);
            diameter = Math.Max(diameter, left + right);
            return Math.Max(left, right) + 1;
        }
    }

    /// <summary>572 - Subtree of Another Tree。先找候选根，再递归比较结构和值。</summary>
    public static bool IsSubtree(TreeNode? root, TreeNode? candidate)
    {
        if (candidate is null) return true;
        if (root is null) return false;
        return AreSame(root, candidate) || IsSubtree(root.Left, candidate) || IsSubtree(root.Right, candidate);

        static bool AreSame(TreeNode? first, TreeNode? second)
        {
            if (first is null || second is null) return first is null && second is null;
            return first.Value == second.Value && AreSame(first.Left, second.Left) && AreSame(first.Right, second.Right);
        }
    }

    /// <summary>1448 - Count Good Nodes。沿路径维护祖先最大值，O(n)。</summary>
    public static int CountGoodNodes(TreeNode? root)
    {
        return Visit(root, int.MinValue);

        static int Visit(TreeNode? node, int maximum)
        {
            if (node is null) return 0;
            var good = node.Value >= maximum ? 1 : 0;
            maximum = Math.Max(maximum, node.Value);
            return good + Visit(node.Left, maximum) + Visit(node.Right, maximum);
        }
    }

    /// <summary>297 - Serialize Binary Tree。前序序列带空标记可唯一恢复结构。</summary>
    public static string Serialize(TreeNode? root)
    {
        var tokens = new List<string>();
        Write(root);
        return string.Join(',', tokens);

        void Write(TreeNode? node)
        {
            if (node is null)
            {
                tokens.Add("#");
                return;
            }

            tokens.Add(node.Value.ToString(CultureInfo.InvariantCulture));
            Write(node.Left);
            Write(node.Right);
        }
    }

    /// <summary>LeetCode 297 的反序列化配套方法。按与序列化相同的前序契约消费 token。</summary>
    public static TreeNode? Deserialize(string data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var tokens = data.Split(',');
        var index = 0;
        var root = Read();
        if (index != tokens.Length) throw new FormatException("Extra tokens after tree.");
        return root;

        TreeNode? Read()
        {
            if (index >= tokens.Length) throw new FormatException("Incomplete tree data.");
            var token = tokens[index++];
            if (token == "#") return null;
            if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                throw new FormatException($"Invalid node value: {token}");

            return new TreeNode(value, Read(), Read());
        }
    }
}
