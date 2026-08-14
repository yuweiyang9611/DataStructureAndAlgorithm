namespace DataStructureAndAlgorithm.LeetCode;

/// <summary>155 - Min Stack。每层同时保存当前最小值，所以所有操作 O(1)。</summary>
public sealed class MinStack
{
    private readonly Stack<(int Value, int Minimum)> _values = [];

    public int Count => _values.Count;

    public void Push(int value)
    {
        var minimum = _values.TryPeek(out var top) ? Math.Min(top.Minimum, value) : value;
        _values.Push((value, minimum));
    }

    public int Pop() => _values.TryPop(out var item)
        ? item.Value
        : throw new InvalidOperationException("Stack is empty.");

    public int Top() => _values.TryPeek(out var item)
        ? item.Value
        : throw new InvalidOperationException("Stack is empty.");

    public int GetMinimum() => _values.TryPeek(out var item)
        ? item.Minimum
        : throw new InvalidOperationException("Stack is empty.");
}
