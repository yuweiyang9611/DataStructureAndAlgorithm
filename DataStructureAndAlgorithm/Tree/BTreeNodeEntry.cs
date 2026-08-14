namespace DataStructureAndAlgorithm.Tree;

public class BTreeNodeEntry<TKey, TValue>(TKey key, TValue value)
    where TKey : IComparable<TKey>
    where TValue : notnull
{
    public TKey Key { get; } = key;

    public TValue Value { get; } = value;
}
