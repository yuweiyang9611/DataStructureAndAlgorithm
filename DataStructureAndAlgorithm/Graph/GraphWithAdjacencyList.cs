using System.Collections.ObjectModel;
using System.Text;

namespace DataStructureAndAlgorithm.Graph;

public class GraphWithAdjacencyList<TValue> where TValue : notnull
{
    private readonly Dictionary<Vertex<TValue>, List<Vertex<TValue>>> _adjacencyList;
    private readonly Dictionary<Vertex<TValue>, IReadOnlyList<Vertex<TValue>>> _readOnlyAdjacencyList;

    public IReadOnlyDictionary<Vertex<TValue>, IReadOnlyList<Vertex<TValue>>> AdjacencyList { get; }
    public bool IsUndirectedGraph { get; }

    public GraphWithAdjacencyList(IEnumerable<Vertex<TValue>[]> edges, bool isUndirectedGraph = true)
    {
        ArgumentNullException.ThrowIfNull(edges);

        IsUndirectedGraph = isUndirectedGraph;
        _adjacencyList = [];
        _readOnlyAdjacencyList = [];
        AdjacencyList = new ReadOnlyDictionary<Vertex<TValue>, IReadOnlyList<Vertex<TValue>>>(
            _readOnlyAdjacencyList);
        foreach (var edge in edges)
        {
            if (edge is not { Length: 2 })
            {
                throw new ArgumentException("Each edge must contain exactly two vertices.", nameof(edges));
            }

            AddVertex(edge[0]);
            AddVertex(edge[1]);
            AddEdge(edge[0], edge[1]);
        }
    }

    public int Size => _adjacencyList.Count;

    /// <summary>
    /// 添加边
    /// </summary>
    /// <param name="vet1">起始顶点</param>
    /// <param name="vet2">终止顶点</param>
    /// <exception cref="InvalidOperationException">如果指定的顶点不存在，则抛出该异常</exception>
    public void AddEdge(Vertex<TValue> vet1, Vertex<TValue> vet2)
    {
        if (!_adjacencyList.ContainsKey(vet1) || !_adjacencyList.ContainsKey(vet2) || vet1.Equals(vet2))
            throw new InvalidOperationException();

        AddNeighborIfMissing(vet1, vet2);
        if (IsUndirectedGraph) AddNeighborIfMissing(vet2, vet1);
    }

    /// <summary>
    /// 移除边
    /// </summary>
    /// <param name="vet1">起始顶点</param>
    /// <param name="vet2">终止顶点</param>
    /// <exception cref="InvalidOperationException">如果指定的顶点不存在，则抛出该异常</exception>
    public void RemoveEdge(Vertex<TValue> vet1, Vertex<TValue> vet2)
    {
        if (!_adjacencyList.ContainsKey(vet1) || !_adjacencyList.ContainsKey(vet2) || vet1.Equals(vet2))
            throw new InvalidOperationException();
        _adjacencyList[vet1].Remove(vet2);
        if (IsUndirectedGraph) _adjacencyList[vet2].Remove(vet1);
    }

    /// <summary>
    /// 添加顶点
    /// </summary>
    /// <param name="vertex">等待添加的顶点</param>
    public void AddVertex(Vertex<TValue> vertex)
    {
        if (_adjacencyList.ContainsKey(vertex)) return;
        var neighbors = new List<Vertex<TValue>>();
        _adjacencyList.Add(vertex, neighbors);
        _readOnlyAdjacencyList.Add(vertex, neighbors.AsReadOnly());
    }

    public bool ContainsVertex(Vertex<TValue> vertex) => _adjacencyList.ContainsKey(vertex);

    public IReadOnlyList<Vertex<TValue>> GetNeighbors(Vertex<TValue> vertex)
    {
        if (!_readOnlyAdjacencyList.TryGetValue(vertex, out var neighbors))
        {
            throw new ArgumentException("The vertex does not exist in the graph.", nameof(vertex));
        }

        return neighbors;
    }

    /// <summary>
    /// 移除顶点
    /// </summary>
    /// <param name="vertex">等待删除的顶点</param>
    /// <exception cref="InvalidOperationException">如果要删除的顶点不存在就抛出该异常</exception>
    public void RemoveVertex(Vertex<TValue> vertex)
    {
        if (!_adjacencyList.ContainsKey(vertex)) throw new InvalidOperationException();
        // 从邻接表中删除该顶点
        _adjacencyList.Remove(vertex);
        _readOnlyAdjacencyList.Remove(vertex);
        // 遍历其他顶点的列表，删除所有包含vertex的边
        foreach (var list in _adjacencyList.Values) list.Remove(vertex);
    }

    /// <summary>
    /// 打印邻接表
    /// </summary>
    /// <returns>用string表示的邻接表</returns>
    public string Print()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("邻接表 = ");

        foreach (var (key, value) in _adjacencyList)
        {
            var temp = value.Select(vertex => vertex.Value);
            stringBuilder.AppendLine($"{key.Value}: [{string.Join(", ", temp)}],");
        }

        return stringBuilder.ToString();
    }

    private void AddNeighborIfMissing(Vertex<TValue> from, Vertex<TValue> to)
    {
        var neighbors = _adjacencyList[from];
        if (!neighbors.Contains(to)) neighbors.Add(to);
    }
}
