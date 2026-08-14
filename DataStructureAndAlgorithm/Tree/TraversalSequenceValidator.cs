namespace DataStructureAndAlgorithm.Tree;

/// <summary>
/// 集中校验“中序 + 前序/后序”重建二叉树所需的公共前置条件。
/// </summary>
internal static class TraversalSequenceValidator
{
    /// <summary>
    /// 验证两条遍历序列，并建立“节点值到中序位置”的 O(1) 查找表。
    /// </summary>
    /// <remarks>
    /// 在节点值不唯一时，仅凭前序/后序与中序序列不能唯一确定一棵树，因此本项目明确拒绝重复值。
    /// 长度相同、元素集合相同仍不代表分区一定一致，递归或迭代构造过程还会继续验证结构关系。
    /// </remarks>
    public static Dictionary<TElementType, int> Validate<TElementType>(
        IReadOnlyList<TElementType> otherSequence,
        IReadOnlyList<TElementType> inOrderSequence)
        where TElementType : notnull
    {
        ArgumentNullException.ThrowIfNull(otherSequence);
        ArgumentNullException.ThrowIfNull(inOrderSequence);

        if (otherSequence.Count != inOrderSequence.Count)
        {
            throw new ArgumentException("两条遍历序列的长度必须相同。", nameof(otherSequence));
        }

        var positions = new Dictionary<TElementType, int>(inOrderSequence.Count);
        for (var index = 0; index < inOrderSequence.Count; index++)
        {
            if (!positions.TryAdd(inOrderSequence[index], index))
            {
                throw new ArgumentException("中序序列不能包含重复节点。", nameof(inOrderSequence));
            }
        }

        var seen = new HashSet<TElementType>();
        foreach (var value in otherSequence)
        {
            if (!seen.Add(value))
            {
                throw new ArgumentException("遍历序列不能包含重复节点。", nameof(otherSequence));
            }

            if (!positions.ContainsKey(value))
            {
                throw new ArgumentException("两条遍历序列必须包含相同的节点集合。", nameof(otherSequence));
            }
        }

        return positions;
    }
}
