using DataStructureAndAlgorithm.Sorting;
using DataStructureAndAlgorithm.Tree;
using FsCheck.Xunit;

namespace DataStructureAndAlgorithm.Test.QualityTest;

/// <summary>
/// 性质测试不绑定某一组手写样例，而是持续生成输入验证“对所有输入都应成立”的规则。
/// 失败时 FsCheck 会自动缩减输入，给出尽量小的反例，特别适合排序和树不变量。
/// </summary>
public class PropertyBasedAlgorithmsTest
{
    [Property(MaxTest = 150)]
    public bool RadixSort_AlwaysMatchesTheFrameworkSort(int[]? source)
    {
        source ??= [];
        var expected = (int[])source.Clone();
        Array.Sort(expected);
        var actual = (int[])source.Clone();

        NonComparisonSortAlgorithms.RadixSort(actual);

        return actual.SequenceEqual(expected);
    }

    [Property(MaxTest = 150)]
    public bool RedBlackTree_IsASortedSetAndPreservesInvariants(int[]? source)
    {
        source ??= [];
        var tree = new RedBlackTree<int>();
        foreach (var value in source) tree.Add(value);

        return tree.HasValidInvariants() &&
               tree.SequenceEqual(source.Distinct().Order()) &&
               tree.Count == source.Distinct().Count();
    }
}
