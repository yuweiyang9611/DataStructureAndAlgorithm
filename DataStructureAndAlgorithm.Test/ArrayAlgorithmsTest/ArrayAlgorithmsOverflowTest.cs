using Algorithms = DataStructureAndAlgorithm.ArrayAlgorithms.ArrayAlgorithms;

namespace DataStructureAndAlgorithm.Test.ArrayAlgorithmsTest;

public class ArrayAlgorithmsOverflowTest
{
    [Fact]
    public void TwoSum_ShouldNotTreatOverflowedAdditionAsAValidMatch()
    {
        // int.MaxValue + 1 在 unchecked int 中会回绕成 int.MinValue，
        // 但数学意义上的和并不等于目标值。
        Assert.Null(Algorithms.TwoSum([int.MaxValue, 1], int.MinValue));
    }
}
