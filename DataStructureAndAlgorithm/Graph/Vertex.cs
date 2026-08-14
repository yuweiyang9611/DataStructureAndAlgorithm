namespace DataStructureAndAlgorithm.Graph;

/// <summary>
/// 将顶点包装成一个结构体
/// </summary>
/// <typeparam name="TValue">顶点所存储的元素的类型</typeparam>
public readonly record struct Vertex<TValue>(TValue Value)
    where TValue : notnull
{
    public TValue Value { get; } = Value;
}
