namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>703 - Kth Largest Element in a Stream。大小为 k 的最小堆保存当前最大的 k 个值。</summary>
public sealed class KthLargestStream
{
    private readonly PriorityQueue<int, int> _largest = new();

    public KthLargestStream(int k, IEnumerable<int> initialValues)
    {
        if (k < 1) throw new ArgumentOutOfRangeException(nameof(k));
        ArgumentNullException.ThrowIfNull(initialValues);
        K = k;
        foreach (var value in initialValues)
        {
            _largest.Enqueue(value, value);
            if (_largest.Count > K) _largest.Dequeue();
        }
    }

    public int K { get; }

    public int Add(int value)
    {
        _largest.Enqueue(value, value);
        if (_largest.Count > K) _largest.Dequeue();

        return _largest.Count == K
            ? _largest.Peek()
            : throw new InvalidOperationException("Fewer than k values have been observed.");
    }
}
