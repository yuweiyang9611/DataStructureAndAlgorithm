// LeetCode 100 题分型训练：数组、字符串、链表、树、栈和堆各抽一两题，用题库里的实现直接算。
// 换一组输入再跑，看每题的答案；每个方法都在 DataStructureAndAlgorithm/LeetCode 里。
using DataStructureAndAlgorithm.LeetCode;
using DataStructureAndAlgorithm.LeetCode.Models;

public static class Demo
{
    public static void Run()
    {
        Console.WriteLine($"Two Sum [2,7,11,15] 目标 9：下标 {string.Join(",", ClassicArrayProblems.TwoSum(new[] { 2, 7, 11, 15 }, 9))}");
        var triples = ClassicArrayProblems.ThreeSum(new[] { -1, 0, 1, 2, -1, -4 }).Select(triple => "[" + string.Join(",", triple) + "]");
        Console.WriteLine($"Three Sum = 0：{string.Join(" ", triples)}");
        Console.WriteLine($"接雨水 [0,1,0,2,1,0,1,3,2,1,2,1] = {ClassicArrayProblems.TrapRainWater(new[] { 0, 1, 0, 2, 1, 0, 1, 3, 2, 1, 2, 1 })}");
        Console.WriteLine($"股票最大利润 [7,1,5,3,6,4] = {ClassicArrayProblems.MaximumStockProfit(new[] { 7, 1, 5, 3, 6, 4 })}，[3,2,1,5,6,4] 的第 2 大 = {ClassicArrayProblems.FindKthLargest(new[] { 3, 2, 1, 5, 6, 4 }, 2)}");

        var brackets = "([{}])";
        Console.WriteLine($"最长无重复子串 abcabcbb = {ClassicStringProblems.LongestSubstringWithoutRepeatingCharacters("abcabcbb")}，括号 {brackets} 有效？{ClassicStringProblems.HasValidParentheses(brackets)}");
        Console.WriteLine($"最小覆盖子串 ADOBECODEBANC / ABC = {ClassicStringProblems.MinimumWindowSubstring("ADOBECODEBANC", "ABC")}");

        var reversed = ClassicLinkedListProblems.ReverseList(ListNode.FromValues(1, 2, 3, 4, 5));
        Console.WriteLine($"反转链表 1->2->3->4->5：{string.Join("->", ListNode.ToArray(reversed))}");
        var merged = ClassicLinkedListProblems.MergeTwoSortedLists(ListNode.FromValues(1, 2, 4), ListNode.FromValues(1, 3, 4));
        Console.WriteLine($"合并两个有序链表：{string.Join("->", ListNode.ToArray(merged))}");

        var root = new TreeNode(3, new TreeNode(9), new TreeNode(20, new TreeNode(15), new TreeNode(7)));
        var levels = ClassicTreeProblems.LevelOrder(root).Select(level => "[" + string.Join(",", level) + "]");
        Console.WriteLine($"二叉树层序遍历：{string.Join(" ", levels)}");

        var stack = new MinStack();
        stack.Push(-2);
        stack.Push(0);
        stack.Push(-3);
        Console.WriteLine($"MinStack 当前最小 {stack.GetMinimum()}，弹出 {stack.Pop()} 后最小 {stack.GetMinimum()}");

        var stream = new KthLargestStream(3, new[] { 4, 5, 8, 2 });
        Console.WriteLine($"数据流第 3 大：加 3 → {stream.Add(3)}，加 5 → {stream.Add(5)}，加 10 → {stream.Add(10)}，加 9 → {stream.Add(9)}");
    }
}
