using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace DataStructureAndAlgorithm.Diagnostics;

/// <summary>算法执行过程中的一个可序列化步骤。</summary>
public sealed record AlgorithmTraceEvent(
    [property: JsonPropertyName("step")] int Step,
    [property: JsonPropertyName("algorithm")] string Algorithm,
    [property: JsonPropertyName("operation")] string Operation,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("state")] IReadOnlyDictionary<string, string> State)
{
    /// <summary>
    /// 当前追踪事件 JSON 契约的版本。只要字段语义保持向后兼容，就不应随实现重构随意递增。
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// 标识序列化后的事件遵循哪个契约版本。它是计算属性而不是主构造函数参数，
    /// 因而既不会破坏现有的五参数构造调用，也不会改变 record 的值相等性与解构形状。
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion => CurrentSchemaVersion;
}

/// <summary>
/// 接收算法步骤的最小接口。算法只依赖该抽象，不依赖控制台、JSON 或 UI。
/// </summary>
public interface IAlgorithmTraceSink
{
    void Record(
        string algorithm,
        string operation,
        string description,
        IReadOnlyDictionary<string, string>? state = null);
}

/// <summary>
/// 把任意追踪接收器转换为不会让普通观察器故障破坏领域操作的 best-effort 接收器。
/// </summary>
/// <remarks>
/// <para>
/// 追踪是可选观察层。若业务状态已经写入索引或 WAL，随后因为 UI、日志或测试接收器抛异常而向调用方报告失败，
/// 调用方可能重试并造成重复操作。因此四个综合场景在构造时都通过本装饰器隔离接收器异常。
/// </para>
/// <para>
/// 内存耗尽、栈溢出和访问冲突仍然传播；这些进程级故障不能被伪装成一次可安全忽略的日志丢失。
/// 调用方若需要观察丢弃数量，也可以显式构造本类型并读取 <see cref="DroppedEventCount"/>。
/// </para>
/// </remarks>
public sealed class BestEffortAlgorithmTraceSink : IAlgorithmTraceSink
{
    private readonly IAlgorithmTraceSink _inner;
    private int _droppedEventCount;

    public BestEffortAlgorithmTraceSink(IAlgorithmTraceSink inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <summary>因接收器抛出普通异常而丢弃的事件数量。</summary>
    public int DroppedEventCount => Volatile.Read(ref _droppedEventCount);

    /// <summary>避免重复套装饰器；<see langword="null"/> 仍表示完全关闭追踪。</summary>
    public static IAlgorithmTraceSink? Wrap(IAlgorithmTraceSink? trace) => trace switch
    {
        null => null,
        BestEffortAlgorithmTraceSink => trace,
        _ => new BestEffortAlgorithmTraceSink(trace)
    };

    public void Record(
        string algorithm,
        string operation,
        string description,
        IReadOnlyDictionary<string, string>? state = null)
    {
        try
        {
            _inner.Record(algorithm, operation, description, state);
        }
        catch (Exception exception) when (exception is not (
            OutOfMemoryException or StackOverflowException or AccessViolationException))
        {
            Interlocked.Increment(ref _droppedEventCount);
        }
    }
}

/// <summary>
/// 将步骤保存在内存中的教学收集器，适合 Demo 和测试；生产热路径默认不注入追踪器。
/// </summary>
public sealed class CollectingAlgorithmTraceSink : IAlgorithmTraceSink
{
    private readonly List<AlgorithmTraceEvent> _events = [];
    private readonly ReadOnlyCollection<AlgorithmTraceEvent> _readOnlyEvents;

    public CollectingAlgorithmTraceSink()
    {
        // 缓存只读包装器，既不在每次读取时分配，也不把内部 List 暴露给调用者强制转换后修改。
        _readOnlyEvents = _events.AsReadOnly();
    }

    public IReadOnlyList<AlgorithmTraceEvent> Events => _readOnlyEvents;

    public void Record(
        string algorithm,
        string operation,
        string description,
        IReadOnlyDictionary<string, string>? state = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(algorithm);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(description);

        // 事件是历史快照，必须复制调用方字典；否则调用方稍后修改原字典会篡改已经记录的步骤。
        var stateSnapshot = new ReadOnlyDictionary<string, string>(
            state is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(state, StringComparer.Ordinal));
        _events.Add(new AlgorithmTraceEvent(
            _events.Count + 1,
            algorithm,
            operation,
            description,
            stateSnapshot));
    }

    public void Clear() => _events.Clear();
}
