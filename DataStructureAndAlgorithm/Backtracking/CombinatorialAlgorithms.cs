namespace DataStructureAndAlgorithm.Backtracking;

/// <summary>排列、子集和括号生成等组合搜索示例。</summary>
public static class CombinatorialAlgorithms
{
    /// <summary>生成 1 到 pairCount 对括号的全部合法组合。</summary>
    public static IReadOnlyList<string> GenerateParentheses(int pairCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pairCount);
        var result = new List<string>();
        var buffer = new char[pairCount * 2];

        Search(0, 0, 0);
        return result;

        void Search(int position, int openCount, int closeCount)
        {
            if (position == buffer.Length)
            {
                result.Add(new string(buffer));
                return;
            }

            if (openCount < pairCount)
            {
                buffer[position] = '(';
                Search(position + 1, openCount + 1, closeCount);
            }

            // 任意前缀中右括号数量不能超过左括号，这是最关键的剪枝条件。
            if (closeCount < openCount)
            {
                buffer[position] = ')';
                Search(position + 1, openCount, closeCount + 1);
            }
        }
    }

    /// <summary>生成 values 的全部子集，输入位置不同即视为不同元素。</summary>
    public static IReadOnlyList<IReadOnlyList<T>> GenerateSubsets<T>(IReadOnlyList<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var result = new List<IReadOnlyList<T>>();
        var selection = new List<T>();

        Search(0);
        return result;

        void Search(int index)
        {
            if (index == values.Count)
            {
                result.Add(selection.ToArray());
                return;
            }

            // 不选择当前元素。
            Search(index + 1);

            // 选择当前元素，递归结束后撤销。
            selection.Add(values[index]);
            Search(index + 1);
            selection.RemoveAt(selection.Count - 1);
        }
    }
}
