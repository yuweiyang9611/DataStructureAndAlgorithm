namespace DataStructureAndAlgorithm.Collections;

/// <summary>
/// 教学项目中“按比较器去重并有序枚举”的统一最小契约。
/// </summary>
/// <remarks>
/// 接口只暴露红黑树与跳表共同拥有且语义一致的能力，避免为了形式统一而泄漏旋转、层高等实现细节。
/// <see cref="IComparer{T}"/> 的 Compare(x,y)==0 同时定义顺序和元素等价性，这与 <see cref="SortedSet{T}"/> 的约定一致。
/// </remarks>
public interface IOrderedSet<T> : IReadOnlyCollection<T> where T : notnull
{
    IComparer<T> Comparer { get; }
    bool Add(T value);
    bool Remove(T value);
    bool Contains(T value);
    void Clear();

    /// <summary>执行实现特有的不变量检查；主要用于学习、测试和诊断，不建议放在生产热路径。</summary>
    bool HasValidInvariants();
}
