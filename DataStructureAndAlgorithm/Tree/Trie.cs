namespace DataStructureAndAlgorithm.Tree;

/// <summary>
/// Trie（前缀树），按字符逐层存储字符串集合。
/// </summary>
/// <remarks>
/// 查找复杂度取决于字符串长度 O(L)，而不是已存单词数量。
/// 公共前缀共享节点，因此适合自动补全、词典和前缀统计。
/// 当前实现按 UTF-16 <see cref="char"/> 存储；需要完整 Unicode 文本单元时可改用 Rune。
/// </remarks>
public sealed class Trie
{
    private sealed class Node
    {
        public Dictionary<char, Node> Children { get; } = [];

        public bool IsWord { get; set; }
    }

    private readonly Node _root = new();

    public int Count { get; private set; }

    /// <summary>添加单词；单词已存在时返回 false。</summary>
    public bool Add(string word)
    {
        ArgumentNullException.ThrowIfNull(word);
        var current = _root;

        foreach (var character in word)
        {
            if (!current.Children.TryGetValue(character, out var child))
            {
                child = new Node();
                current.Children.Add(character, child);
            }

            current = child;
        }

        if (current.IsWord)
        {
            return false;
        }

        // 空字符串也可作为单词，它使用根节点的 IsWord 标记。
        current.IsWord = true;
        Count++;
        return true;
    }

    public bool Contains(string word)
    {
        ArgumentNullException.ThrowIfNull(word);
        return FindNode(word) is { IsWord: true };
    }

    public bool StartsWith(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        return FindNode(prefix) is not null;
    }

    /// <summary>删除单词，并清理不再被其他单词共享的尾部节点。</summary>
    public bool Remove(string word)
    {
        ArgumentNullException.ThrowIfNull(word);

        if (!Contains(word))
        {
            return false;
        }

        Remove(_root, word, 0);
        Count--;
        return true;
    }

    /// <summary>
    /// 按字典序返回最多 <paramref name="maximumCount"/> 个具有指定前缀的单词。
    /// </summary>
    public IReadOnlyList<string> GetWordsWithPrefix(string prefix, int maximumCount = 10)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        if (maximumCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        var prefixNode = FindNode(prefix);
        if (prefixNode is null || maximumCount == 0)
        {
            return [];
        }

        var result = new List<string>(Math.Min(maximumCount, Count));
        var buffer = new List<char>(prefix);
        Collect(prefixNode, buffer, result, maximumCount);
        return result;
    }

    private Node? FindNode(string text)
    {
        var current = _root;

        foreach (var character in text)
        {
            if (!current.Children.TryGetValue(character, out current))
            {
                return null;
            }
        }

        return current;
    }

    private static bool Remove(Node node, string word, int depth)
    {
        if (depth == word.Length)
        {
            node.IsWord = false;
            return node.Children.Count == 0;
        }

        var character = word[depth];
        var child = node.Children[character];

        if (Remove(child, word, depth + 1))
        {
            node.Children.Remove(character);
        }

        // 当前节点仍代表其他完整单词时不能被父节点清理。
        return !node.IsWord && node.Children.Count == 0;
    }

    private static void Collect(
        Node node,
        List<char> buffer,
        List<string> result,
        int maximumCount)
    {
        if (node.IsWord)
        {
            result.Add(new string([.. buffer]));
            if (result.Count == maximumCount)
            {
                return;
            }
        }

        foreach (var (character, child) in node.Children.OrderBy(pair => pair.Key))
        {
            buffer.Add(character);
            Collect(child, buffer, result, maximumCount);
            buffer.RemoveAt(buffer.Count - 1);

            if (result.Count == maximumCount)
            {
                return;
            }
        }
    }
}
