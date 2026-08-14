namespace DataStructureAndAlgorithm.Tree;

public class BTreeNode<TKey, TValue>
    where TKey : IComparable<TKey>
    where TValue : notnull
{
    private readonly int _degree;

    public BTreeNode(int degree)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(degree, 2);
        _degree = degree;
        MutableChildren = new List<BTreeNode<TKey, TValue>>(degree);
        MutableEntries = new List<BTreeNodeEntry<TKey, TValue>>(degree);
        Children = MutableChildren.AsReadOnly();
        Entries = MutableEntries.AsReadOnly();
    }

    internal List<BTreeNode<TKey, TValue>> MutableChildren { get; }

    internal List<BTreeNodeEntry<TKey, TValue>> MutableEntries { get; }

    public IReadOnlyList<BTreeNode<TKey, TValue>> Children { get; }

    public IReadOnlyList<BTreeNodeEntry<TKey, TValue>> Entries { get; }

    public bool IsLeaf => Children.Count == 0;
    public bool HasReachedMaximumEntries => Entries.Count >= 2 * _degree - 1;
    public bool HasReachedMinimumEntries => Entries.Count <= _degree - 1;
}
