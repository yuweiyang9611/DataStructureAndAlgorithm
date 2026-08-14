using System.Text;
using DataStructureAndAlgorithm.Diagnostics;

namespace DataStructureAndAlgorithm.Tree;

/// <summary>
/// 使用 Huffman 贪心策略构造最优前缀编码树。
/// </summary>
/// <typeparam name="TElementType">待编码符号的类型。</typeparam>
/// <remarks>
/// 每次合并权重最小的两棵树，可以最小化带权路径长度。
/// 编码必须使用字符串而不是整数保存：例如 <c>0</c>、<c>00</c> 和 <c>000</c>
/// 转成整数后都会变成 0，前导零和编码长度会永久丢失。
/// </remarks>
public class HuffmanTree<TElementType> where TElementType : notnull
{
    private Dictionary<TElementType, string>? _codeTable;

    /// <summary>最近一次构造得到的 Huffman 树根节点。</summary>
    public HuffmanTreeNode<TElementType>? RootNode { get; private set; }

    /// <summary>
    /// 构造 Huffman 树，并返回所有叶子节点。
    /// </summary>
    /// <remarks>
    /// 元组优先级中的 <c>Order</c> 是稳定的入队序号。相同权重可能对应多棵同样最优的树，
    /// 明确的次级顺序能让示例和测试具有可重复性，但算法正确性不依赖某一种具体形状。
    /// </remarks>
    private List<HuffmanTreeNode<TElementType>> CreateHuffmanTree(
        IReadOnlyDictionary<TElementType, uint> values) => CreateHuffmanTree(values, trace: null);

    private List<HuffmanTreeNode<TElementType>> CreateHuffmanTree(
        IReadOnlyDictionary<TElementType, uint> values,
        IAlgorithmTraceSink? trace)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count < 2)
        {
            throw new ArgumentException("Huffman 编码至少需要两个不同符号。", nameof(values));
        }

        if (values.Any(pair => pair.Value == 0))
        {
            throw new ArgumentException("符号权重必须大于 0。", nameof(values));
        }

        var leafNodes = values
            .Select(pair => new HuffmanTreeNode<TElementType>(pair.Key, pair.Value))
            .ToList();
        var queue = new PriorityQueue<HuffmanTreeNode<TElementType>, (double Weight, long Order)>();
        long order = 0;

        foreach (var leaf in leafNodes)
        {
            queue.Enqueue(leaf, (leaf.Weight, order++));
        }

        while (queue.Count > 1)
        {
            var left = queue.Dequeue();
            var right = queue.Dequeue();
            var parent = new HuffmanTreeNode<TElementType>(
                default!, left.Weight + right.Weight, left, right);
            left.Parent = parent;
            right.Parent = parent;
            queue.Enqueue(parent, (parent.Weight, order++));
            trace?.Record(
                "Huffman",
                "Merge",
                "取出当前权重最小的两棵树并合并；贪心选择保证带权路径长度最小。",
                new Dictionary<string, string>
                {
                    ["leftWeight"] = left.Weight.ToString(),
                    ["rightWeight"] = right.Weight.ToString(),
                    ["parentWeight"] = parent.Weight.ToString(),
                    ["remainingTrees"] = queue.Count.ToString()
                });
        }

        RootNode = queue.Dequeue();
        return leafNodes;
    }

    /// <summary>
    /// 根据符号权重计算 Huffman 编码表。
    /// </summary>
    /// <param name="values">符号及其出现频次或正权重。</param>
    /// <returns>以仅包含 <c>0</c> 和 <c>1</c> 的字符串表示的前缀编码。</returns>
    public Dictionary<TElementType, string> CalculateHuffmanCode(
        IReadOnlyDictionary<TElementType, uint> values,
        IAlgorithmTraceSink? trace = null)
    {
        var leaves = CreateHuffmanTree(values, trace);
        var result = new Dictionary<TElementType, string>(leaves.Count);

        foreach (var leaf in leaves)
        {
            // 从叶子回溯得到的是逆序位串，因此先收集、最后再反转。
            var reversedBits = new StringBuilder();
            var current = leaf;
            while (!ReferenceEquals(current, RootNode))
            {
                var parent = current.Parent
                             ?? throw new InvalidOperationException("Huffman 节点缺少父节点。 ");
                if (ReferenceEquals(parent.LeftChild, current))
                {
                    reversedBits.Append('0');
                }
                else if (ReferenceEquals(parent.RightChild, current))
                {
                    reversedBits.Append('1');
                }
                else
                {
                    throw new InvalidOperationException("Huffman 树的父子关系不一致。 ");
                }

                current = parent;
            }

            var characters = reversedBits.ToString().ToCharArray();
            Array.Reverse(characters);
            var code = new string(characters);
            result.Add(leaf.Value, code);
            trace?.Record(
                "Huffman",
                "AssignCode",
                "从叶子回溯到根并反转路径，得到不会成为其他编码前缀的位串。",
                new Dictionary<string, string>
                {
                    ["symbol"] = leaf.Value.ToString() ?? "<null>",
                    ["weight"] = leaf.Weight.ToString(),
                    ["code"] = code
                });
        }

        _codeTable = result;
        return new Dictionary<TElementType, string>(result);
    }

    /// <summary>
    /// 使用最近一次生成的编码表把符号序列编码成位串。
    /// </summary>
    public string Encode(IEnumerable<TElementType> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var codes = _codeTable
                    ?? throw new InvalidOperationException("请先调用 CalculateHuffmanCode 生成编码表。 ");
        var result = new StringBuilder();

        foreach (var value in source)
        {
            if (!codes.TryGetValue(value, out var code))
            {
                throw new ArgumentException($"符号 {value} 不在当前 Huffman 编码表中。", nameof(source));
            }

            result.Append(code);
        }

        return result.ToString();
    }

    /// <summary>
    /// 使用最近一次生成的树解码位串。
    /// </summary>
    public IReadOnlyList<TElementType> Decode(string bits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        var root = RootNode
                   ?? throw new InvalidOperationException("请先调用 CalculateHuffmanCode 生成 Huffman 树。 ");
        var result = new List<TElementType>();
        var current = root;

        foreach (var bit in bits)
        {
            current = bit switch
            {
                '0' => current.LeftChild,
                '1' => current.RightChild,
                _ => throw new ArgumentException("编码只能包含字符 0 和 1。", nameof(bits))
            } ?? throw new ArgumentException("位串不是当前 Huffman 树中的有效编码。", nameof(bits));

            if (current.LeftChild is null && current.RightChild is null)
            {
                result.Add(current.Value);
                current = root;
            }
        }

        if (!ReferenceEquals(current, root))
        {
            throw new ArgumentException("位串在一个完整符号结束前提前终止。", nameof(bits));
        }

        return result;
    }
}
