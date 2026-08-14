namespace DataStructureAndAlgorithm.PatternMatching;

/// <summary>AC 自动机的一次模式命中。</summary>
public readonly record struct PatternMatch(string Pattern, int StartIndex)
{
    public int EndExclusive => StartIndex + Pattern.Length;
}

/// <summary>
/// Aho-Corasick 多模式串匹配器：Trie 负责共享前缀，失败指针负责在失配时复用最长可用后缀。
/// </summary>
/// <remarks>
/// 构建成本与模式总长度成正比；查找为 O(textLength + matchCount)。与逐个调用 KMP 相比，模式很多时只扫描文本一次。
/// 本教学实现按 UTF-16 <see cref="char"/> 匹配并采用序号大小写敏感语义；空模式会在每个边界命中，容易淹没结果，因此明确拒绝。
/// </remarks>
public sealed class AhoCorasickMatcher
{
    private sealed class Node
    {
        public Dictionary<char, Node> Children { get; } = [];
        public Node? Failure { get; set; }
        public List<string> Outputs { get; } = [];
    }

    private readonly Node _root = new();

    public AhoCorasickMatcher(IEnumerable<string> patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pattern in patterns)
        {
            ArgumentNullException.ThrowIfNull(pattern);
            if (pattern.Length == 0) throw new ArgumentException("Patterns cannot be empty.", nameof(patterns));
            if (!unique.Add(pattern)) continue;

            var current = _root;
            foreach (var character in pattern)
            {
                if (!current.Children.TryGetValue(character, out var child))
                {
                    child = new Node();
                    current.Children.Add(character, child);
                }

                current = child;
            }

            current.Outputs.Add(pattern);
        }

        if (unique.Count == 0) throw new ArgumentException("At least one pattern is required.", nameof(patterns));
        BuildFailureLinks();
    }

    public IReadOnlyList<PatternMatch> FindAll(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var result = new List<PatternMatch>();
        var current = _root;

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            // 失败指针相当于“已经比较过的最长后缀”，因此无需把文本下标退回去。
            while (!ReferenceEquals(current, _root) && !current.Children.ContainsKey(character))
            {
                current = current.Failure!;
            }

            if (current.Children.TryGetValue(character, out var next)) current = next;
            foreach (var pattern in current.Outputs)
            {
                result.Add(new PatternMatch(pattern, index - pattern.Length + 1));
            }
        }

        return result;
    }

    private void BuildFailureLinks()
    {
        _root.Failure = _root;
        var queue = new Queue<Node>();
        foreach (var child in _root.Children.Values)
        {
            child.Failure = _root;
            queue.Enqueue(child);
        }

        while (queue.TryDequeue(out var parent))
        {
            foreach (var (character, child) in parent.Children)
            {
                var fallback = parent.Failure!;
                while (!ReferenceEquals(fallback, _root) && !fallback.Children.ContainsKey(character))
                {
                    fallback = fallback.Failure!;
                }

                child.Failure = fallback.Children.TryGetValue(character, out var candidate) &&
                                !ReferenceEquals(candidate, child)
                    ? candidate
                    : _root;
                // 失败节点已经命中的短模式也是当前位置的后缀模式，必须一并报告。
                child.Outputs.AddRange(child.Failure.Outputs);
                queue.Enqueue(child);
            }
        }
    }
}
