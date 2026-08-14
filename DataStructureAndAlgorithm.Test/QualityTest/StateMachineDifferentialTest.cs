using DataStructureAndAlgorithm.Hashing;
using DataStructureAndAlgorithm.Linear;
using DataStructureAndAlgorithm.Tree;

namespace DataStructureAndAlgorithm.Test.QualityTest;

/// <summary>
/// 同一串操作同时喂给教学实现和 .NET 参考实现；每一步都比较可观察状态。
/// 这比只验证最终结果更容易定位扩容、回绕、删除墓碑和重平衡发生在哪一步。
/// </summary>
public class StateMachineDifferentialTest
{
    [Fact]
    public void OpenAddressingHashTable_MatchesDictionaryAcrossMixedOperations()
    {
        var random = new Random(20260714);
        var actual = new OpenAddressingHashTable<int, int>(2);
        var expected = new Dictionary<int, int>();

        for (var step = 0; step < 2_000; step++)
        {
            var key = random.Next(0, 80);
            if (random.Next(3) == 0)
            {
                Assert.Equal(expected.Remove(key), actual.Remove(key));
            }
            else
            {
                var value = random.Next();
                expected[key] = value;
                actual[key] = value;
            }

            Assert.Equal(expected.Count, actual.Count);
            Assert.True(actual.OrderBy(pair => pair.Key).SequenceEqual(expected.OrderBy(pair => pair.Key)));
        }
    }

    [Fact]
    public void ArrayDeque_MatchesLinkedListAcrossBothEnds()
    {
        var random = new Random(20260714);
        var actual = new ArrayDeque<int>(1);
        var expected = new LinkedList<int>();

        for (var step = 0; step < 2_000; step++)
        {
            switch (random.Next(expected.Count == 0 ? 2 : 4))
            {
                case 0:
                    actual.AddFirst(step);
                    expected.AddFirst(step);
                    break;
                case 1:
                    actual.AddLast(step);
                    expected.AddLast(step);
                    break;
                case 2:
                    Assert.Equal(expected.First!.Value, actual.RemoveFirst());
                    expected.RemoveFirst();
                    break;
                default:
                    Assert.Equal(expected.Last!.Value, actual.RemoveLast());
                    expected.RemoveLast();
                    break;
            }

            Assert.Equal(expected.Count, actual.Count);
            Assert.True(actual.SequenceEqual(expected));
        }
    }

    [Fact]
    public void OrderedStructures_MatchSortedSetAfterEveryMutation()
    {
        var random = new Random(20260714);
        var redBlack = new RedBlackTree<int>();
        var skipList = new SkipList<int>(randomSeed: 42);
        var expected = new SortedSet<int>();

        for (var step = 0; step < 2_000; step++)
        {
            var value = random.Next(0, 250);
            if (random.Next(2) == 0)
            {
                var expectedResult = expected.Add(value);
                Assert.Equal(expectedResult, redBlack.Add(value));
                Assert.Equal(expectedResult, skipList.Add(value));
            }
            else
            {
                var expectedResult = expected.Remove(value);
                Assert.Equal(expectedResult, redBlack.Remove(value));
                Assert.Equal(expectedResult, skipList.Remove(value));
            }

            Assert.True(redBlack.HasValidInvariants());
            Assert.True(redBlack.SequenceEqual(expected));
            Assert.True(skipList.SequenceEqual(expected));
        }
    }
}
