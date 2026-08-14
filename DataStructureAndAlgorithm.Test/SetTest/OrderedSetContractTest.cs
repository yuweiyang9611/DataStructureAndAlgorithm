using DataStructureAndAlgorithm.Collections;
using DataStructureAndAlgorithm.Tree;

namespace DataStructureAndAlgorithm.Test.SetTest;

public class OrderedSetContractTest
{
    public static TheoryData<string, Func<IOrderedSet<string>>> Implementations => new()
    {
        { "RedBlackTree", () => new RedBlackTree<string>(StringComparer.OrdinalIgnoreCase) },
        { "SkipList", () => new SkipList<string>(randomSeed: 42, comparer: StringComparer.OrdinalIgnoreCase) }
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public void EveryOrderedSet_UsesItsComparerForOrderingAndUniqueness(
        string implementation,
        Func<IOrderedSet<string>> create)
    {
        ArgumentNullException.ThrowIfNull(create);

        _ = implementation; // 让失败用例在测试资源管理器中显示具体实现名称。
        var set = create();

        Assert.True(set.Add("beta"));
        Assert.True(set.Add("Alpha"));
        Assert.False(set.Add("ALPHA"));
        Assert.True(set.Contains("alpha"));
        Assert.True(set.SequenceEqual(["Alpha", "beta"]));
        Assert.True(set.HasValidInvariants());
        Assert.True(set.Remove("BETA"));
        Assert.True(set.HasValidInvariants());
        set.Clear();
        Assert.Empty(set);
        Assert.True(set.HasValidInvariants());
    }
}
