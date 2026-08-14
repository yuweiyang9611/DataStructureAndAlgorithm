namespace DataStructureAndAlgorithm.LeetCode.Models;

/// <summary>LeetCode 138 使用的链表节点，Random 可以指向链表中的任意节点。</summary>
public sealed class RandomListNode(int value)
{
    public int Value { get; set; } = value;

    public RandomListNode? Next { get; set; }

    public RandomListNode? Random { get; set; }
}
