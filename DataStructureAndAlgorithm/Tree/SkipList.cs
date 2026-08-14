using System.Collections;
using DataStructureAndAlgorithm.Collections;

namespace DataStructureAndAlgorithm.Tree;

/// <summary>
/// 通过随机多级索引实现的有序集合。
/// </summary>
/// <remarks>
/// 第 0 层包含所有节点，更高层按概率抽样形成“快速通道”。查找时从最高层向右移动，
/// 即将越过目标时下降一层；期望查找、插入和删除复杂度均为 O(log n)，最坏为 O(n)。
/// 跳表没有红黑树那样复杂的旋转，代价是平衡性由随机性提供且每个节点可能保存多个前向引用。
/// </remarks>
public sealed class SkipList<T> : IOrderedSet<T> where T : notnull
{
    private const int DefaultMaximumLevel = 16;
    private const int HighestSupportedLevel = 64;
    private const double DefaultProbability = 0.5;

    private sealed class Node(T value, int level)
    {
        public T Value { get; } = value;
        public Node?[] Forward { get; } = new Node?[level];
    }

    private readonly int _maximumLevel;
    private readonly double _probability;
    private readonly Random _random;
    private readonly IComparer<T> _comparer;
    private readonly Node _head;
    private int _currentLevel = 1;

    public SkipList(
        int maximumLevel = DefaultMaximumLevel,
        double probability = DefaultProbability,
        int? randomSeed = null,
        IComparer<T>? comparer = null)
    {
        if (maximumLevel is < 1 or > HighestSupportedLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumLevel), maximumLevel,
                $"最大层数必须在 1 到 {HighestSupportedLevel} 之间。");
        }

        if (probability is <= 0 or >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(probability), probability, "晋升概率必须在 0 和 1 之间。");
        }

        _maximumLevel = maximumLevel;
        _probability = probability;
        _random = randomSeed is null ? Random.Shared : new Random(randomSeed.Value);
        _comparer = comparer ?? Comparer<T>.Default;
        _head = new Node(default!, maximumLevel); // 头哨兵不代表真实值，只简化边界连接。
    }

    public int Count { get; private set; }

    public IComparer<T> Comparer => _comparer;

    /// <summary>当前非空索引层数；空表为 1。</summary>
    public int CurrentLevel => _currentLevel;

    public bool Contains(T value)
    {
        var predecessor = FindPredecessor(value);
        var candidate = predecessor.Forward[0];
        return candidate is not null && _comparer.Compare(candidate.Value, value) == 0;
    }

    /// <summary>插入不重复值；值已存在时返回 false。</summary>
    public bool Add(T value)
    {
        var update = new Node[_maximumLevel];
        var current = _head;

        for (var level = _currentLevel - 1; level >= 0; level--)
        {
            while (current.Forward[level] is { } next &&
                   _comparer.Compare(next.Value, value) < 0)
            {
                current = next;
            }

            update[level] = current;
        }

        var candidate = current.Forward[0];
        if (candidate is not null && _comparer.Compare(candidate.Value, value) == 0) return false;

        var newLevel = RandomLevel();
        if (newLevel > _currentLevel)
        {
            // 新节点首次打开的高层此前没有任何节点，其前驱只能是头哨兵。
            for (var level = _currentLevel; level < newLevel; level++) update[level] = _head;
            _currentLevel = newLevel;
        }

        var node = new Node(value, newLevel);
        for (var level = 0; level < newLevel; level++)
        {
            node.Forward[level] = update[level].Forward[level];
            update[level].Forward[level] = node;
        }

        Count++;
        return true;
    }

    /// <summary>删除指定值；不存在时返回 false。</summary>
    public bool Remove(T value)
    {
        var update = new Node[_maximumLevel];
        var current = _head;

        for (var level = _currentLevel - 1; level >= 0; level--)
        {
            while (current.Forward[level] is { } next &&
                   _comparer.Compare(next.Value, value) < 0)
            {
                current = next;
            }

            update[level] = current;
        }

        var target = current.Forward[0];
        if (target is null || _comparer.Compare(target.Value, value) != 0) return false;

        for (var level = 0; level < _currentLevel; level++)
        {
            if (!ReferenceEquals(update[level].Forward[level], target)) break;
            update[level].Forward[level] = target.Forward[level];
        }

        while (_currentLevel > 1 && _head.Forward[_currentLevel - 1] is null)
        {
            _currentLevel--;
        }

        Count--;
        return true;
    }

    public void Clear()
    {
        Array.Clear(_head.Forward);
        _currentLevel = 1;
        Count = 0;
    }

    /// <summary>
    /// 验证第 0 层包含全部节点、各层严格有序、高层节点都是第 0 层节点，并且层数与计数一致。
    /// </summary>
    public bool HasValidInvariants()
    {
        if (_currentLevel is < 1 || _currentLevel > _maximumLevel) return false;
        if (_currentLevel > 1 && _head.Forward[_currentLevel - 1] is null) return false;
        for (var level = _currentLevel; level < _maximumLevel; level++)
        {
            if (_head.Forward[level] is not null) return false;
        }

        var bottomNodes = new HashSet<Node>(ReferenceEqualityComparer.Instance);
        Node? previous = null;
        var current = _head.Forward[0];
        while (current is not null)
        {
            if (bottomNodes.Count > Count ||
                previous is not null && _comparer.Compare(previous.Value, current.Value) >= 0)
            {
                return false;
            }

            bottomNodes.Add(current);
            previous = current;
            current = current.Forward[0];
        }

        if (bottomNodes.Count != Count) return false;
        for (var level = 1; level < _currentLevel; level++)
        {
            previous = null;
            current = _head.Forward[level];
            var nodesOnLevel = 0;
            while (current is not null)
            {
                if (++nodesOnLevel > Count || current.Forward.Length <= level || !bottomNodes.Contains(current) ||
                    previous is not null && _comparer.Compare(previous.Value, current.Value) >= 0)
                {
                    return false;
                }

                previous = current;
                current = current.Forward[level];
            }
        }

        return true;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var current = _head.Forward[0]; current is not null; current = current.Forward[0])
        {
            yield return current.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private Node FindPredecessor(T value)
    {
        var current = _head;
        for (var level = _currentLevel - 1; level >= 0; level--)
        {
            while (current.Forward[level] is { } next &&
                   _comparer.Compare(next.Value, value) < 0)
            {
                current = next;
            }
        }

        return current;
    }

    private int RandomLevel()
    {
        var level = 1;
        while (level < _maximumLevel && _random.NextDouble() < _probability) level++;
        return level;
    }
}
