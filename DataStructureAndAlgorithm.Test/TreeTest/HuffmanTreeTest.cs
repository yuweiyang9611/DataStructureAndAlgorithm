using DataStructureAndAlgorithm.Tree;
using ReflectionMagic;

namespace DataStructureAndAlgorithm.Test.TreeTest;

public class HuffmanTreeTest
{
    private static readonly Dictionary<char, uint> Data = new()
    {
        ['a'] = 5,
        ['b'] = 2,
        ['c'] = 1,
        ['d'] = 1,
        ['r'] = 2
    };

    [Fact]
    public void CreateHuffmanTree_PreservesLeavesAndTotalWeight()
    {
        var tree = new HuffmanTree<char>();
        dynamic leaves = tree.AsDynamic().CreateHuffmanTree(Data);

        foreach (HuffmanTreeNode<char> leaf in leaves)
        {
            Assert.Equal(Data[leaf.Value], leaf.Weight);
            Assert.NotNull(leaf.Parent);
        }
        Assert.NotNull(tree.RootNode);
        Assert.Equal(Data.Values.Sum(value => (double)value), tree.RootNode.Weight);
    }

    [Fact]
    public void CalculateHuffmanCode_ReturnsPrefixFreeBitStrings()
    {
        var tree = new HuffmanTree<char>();
        var codes = tree.CalculateHuffmanCode(Data);

        Assert.Equal(Data.Count, codes.Count);
        Assert.All(codes.Values, code =>
        {
            Assert.NotEmpty(code);
            Assert.All(code, bit => Assert.True(bit is '0' or '1'));
        });

        // 前缀码的关键不变量：任何符号的编码都不能是另一个编码的前缀。
        foreach (var first in codes)
            foreach (var second in codes)
            {
                if (first.Key != second.Key)
                {
                    Assert.False(second.Value.StartsWith(first.Value, StringComparison.Ordinal));
                }
            }

        // 权重越大的 a 应获得不长于最低权重符号的编码。
        Assert.True(codes['a'].Length <= codes['c'].Length);
        Assert.True(codes['a'].Length <= codes['d'].Length);
    }

    [Fact]
    public void EncodeAndDecode_RoundTripsAndPreservesLeadingZeroCodes()
    {
        var tree = new HuffmanTree<char>();
        var codes = tree.CalculateHuffmanCode(Data);
        const string source = "abracadabra";

        var encoded = tree.Encode(source);
        var decoded = tree.Decode(encoded);

        Assert.Equal(source, string.Concat(decoded));
        Assert.Contains(codes.Values, code => code.StartsWith('0'));
    }

    [Fact]
    public void InvalidInputs_AreRejectedExplicitly()
    {
        var tree = new HuffmanTree<char>();
        Assert.Throws<ArgumentException>(() =>
            tree.CalculateHuffmanCode(new Dictionary<char, uint> { ['a'] = 1 }));
        Assert.Throws<ArgumentException>(() =>
            tree.CalculateHuffmanCode(new Dictionary<char, uint> { ['a'] = 1, ['b'] = 0 }));
        Assert.Throws<InvalidOperationException>(() => tree.Encode("a"));

        tree.CalculateHuffmanCode(Data);
        Assert.Throws<ArgumentException>(() => tree.Decode("10x"));
        Assert.Throws<ArgumentException>(() => tree.Encode("z"));
    }
}
