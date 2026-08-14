using DataStructureAndAlgorithm.LeetCode;

namespace DataStructureAndAlgorithm.Test.LeetCodeTest;

public class AdditionalArrayProblemsTest
{
    [Fact]
    public void P11_MaximumContainerArea() =>
        Assert.Equal(49, AdditionalArrayProblems.MaximumContainerArea([1, 8, 6, 2, 5, 4, 8, 3, 7]));

    [Fact]
    public void P26_RemoveDuplicatesFromSortedArray()
    {
        var values = new List<int> { 0, 0, 1, 1, 1, 2, 2, 3, 3, 4 };
        var count = AdditionalArrayProblems.RemoveDuplicatesFromSortedArray(values);
        Assert.Equal(5, count);
        Assert.True(values.Take(count).SequenceEqual([0, 1, 2, 3, 4]));
    }

    [Fact]
    public void P27_RemoveElement()
    {
        var values = new List<int> { 3, 2, 2, 3 };
        var count = AdditionalArrayProblems.RemoveElement(values, 3);
        Assert.Equal(2, count);
        Assert.True(values.Take(count).SequenceEqual([2, 2]));
    }

    [Fact]
    public void P31_NextPermutation()
    {
        var values = new List<int> { 1, 2, 3 };
        AdditionalArrayProblems.NextPermutation(values);
        Assert.Equal([1, 3, 2], values);
    }

    [Fact]
    public void P33_SearchRotatedSortedArray() =>
        Assert.Equal(4, AdditionalArrayProblems.SearchRotatedSortedArray([4, 5, 6, 7, 0, 1, 2], 0));

    [Fact]
    public void P34_SearchRange() =>
        Assert.True(AdditionalArrayProblems.SearchRange([5, 7, 7, 8, 8, 10], 8).SequenceEqual([3, 4]));

    [Fact]
    public void P36_IsValidSudoku()
    {
        char[][] board =
        [
            ['5','3','.','.','7','.','.','.','.'], ['6','.','.','1','9','5','.','.','.'],
            ['.','9','8','.','.','.','.','6','.'], ['8','.','.','.','6','.','.','.','3'],
            ['4','.','.','8','.','3','.','.','1'], ['7','.','.','.','2','.','.','.','6'],
            ['.','6','.','.','.','.','2','8','.'], ['.','.','.','4','1','9','.','.','5'],
            ['.','.','.','.','8','.','.','7','9']
        ];
        Assert.True(AdditionalArrayProblems.IsValidSudoku(board));
        board[0][1] = '5';
        Assert.False(AdditionalArrayProblems.IsValidSudoku(board));
    }

    [Fact]
    public void P48_RotateImage()
    {
        int[][] matrix = [[1, 2, 3], [4, 5, 6], [7, 8, 9]];
        AdditionalArrayProblems.RotateImage(matrix);
        Assert.True(matrix[0].SequenceEqual([7, 4, 1]));
        Assert.True(matrix[1].SequenceEqual([8, 5, 2]));
        Assert.True(matrix[2].SequenceEqual([9, 6, 3]));
    }

    [Fact]
    public void P54_SpiralOrder() =>
        Assert.Equal([1, 2, 3, 6, 9, 8, 7, 4, 5], AdditionalArrayProblems.SpiralOrder([[1, 2, 3], [4, 5, 6], [7, 8, 9]]));

    [Fact]
    public void P56_MergeIntervals()
    {
        var result = AdditionalArrayProblems.MergeIntervals([[1, 3], [2, 6], [8, 10], [15, 18]]);
        Assert.Equal(3, result.Length);
        Assert.True(result[0].SequenceEqual([1, 6]));
    }

    [Fact]
    public void P66_PlusOne() =>
        Assert.True(AdditionalArrayProblems.PlusOne([9, 9]).SequenceEqual([1, 0, 0]));

    [Fact]
    public void P73_SetMatrixZeroes()
    {
        int[][] matrix = [[1, 1, 1], [1, 0, 1], [1, 1, 1]];
        AdditionalArrayProblems.SetMatrixZeroes(matrix);
        Assert.True(matrix[0].SequenceEqual([1, 0, 1]));
        Assert.True(matrix[1].SequenceEqual([0, 0, 0]));
        Assert.True(matrix[2].SequenceEqual([1, 0, 1]));
    }

    [Fact]
    public void P75_SortColors()
    {
        var colors = new List<int> { 2, 0, 2, 1, 1, 0 };
        AdditionalArrayProblems.SortColors(colors);
        Assert.Equal([0, 0, 1, 1, 2, 2], colors);
    }

    [Fact]
    public void P88_MergeSortedArrays()
    {
        int[] first = [1, 2, 3, 0, 0, 0];
        AdditionalArrayProblems.MergeSortedArrays(first, 3, [2, 5, 6], 3);
        Assert.True(first.SequenceEqual([1, 2, 2, 3, 5, 6]));
    }

    [Fact]
    public void P128_LongestConsecutiveSequence() =>
        Assert.Equal(4, AdditionalArrayProblems.LongestConsecutiveSequence([100, 4, 200, 1, 3, 2]));

    [Fact]
    public void P136_SingleNumber() => Assert.Equal(4, AdditionalArrayProblems.SingleNumber([4, 1, 2, 1, 2]));

    [Fact]
    public void P169_MajorityElement() => Assert.Equal(2, AdditionalArrayProblems.MajorityElement([2, 2, 1, 1, 1, 2, 2]));

    [Fact]
    public void P189_RotateArray()
    {
        var values = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
        AdditionalArrayProblems.RotateArray(values, 3);
        Assert.Equal([5, 6, 7, 1, 2, 3, 4], values);
    }

    [Fact]
    public void P217_ContainsDuplicate() => Assert.True(AdditionalArrayProblems.ContainsDuplicate([1, 2, 3, 1]));

    [Fact]
    public void P238_ProductExceptSelf() =>
        Assert.True(AdditionalArrayProblems.ProductExceptSelf([1, 2, 3, 4]).SequenceEqual([24L, 12L, 8L, 6L]));
}
