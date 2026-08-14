using System.Text;

namespace DataStructureAndAlgorithm.Graph;

public class GraphWithAdjacencyMatrix<TElement> where TElement : notnull
{
    // 顶点列表
    private readonly List<TElement> _vertices;

    // 邻接矩阵
    private readonly List<List<double>> _adjacencyMatrix;

    // 是否为无向图
    public bool IsUndirectedGraph { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="vertices">顶点列表：值代表“顶点值”，索引代表“顶点索引”</param>
    /// <param name="edges">边：值代表“顶点索引”，共两列，索引0中元素为起始顶点在顶点列表中的索引，索引1中的元素为终止顶点在顶点列表中的索引</param>
    /// <param name="isUndirectedGraph">是否为无向图</param>
    public GraphWithAdjacencyMatrix(IEnumerable<TElement> vertices, IEnumerable<int[]> edges,
        bool isUndirectedGraph = true)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(edges);

        _vertices = [];
        _adjacencyMatrix = [];
        IsUndirectedGraph = isUndirectedGraph;

        // 添加顶点
        foreach (var val in vertices) AddVertex(val);

        // 添加边
        // 注意：edges中的元素代表顶点索引
        foreach (var edge in edges)
        {
            if (edge is not { Length: 2 })
            {
                throw new ArgumentException("Each edge must contain exactly two vertex indexes.", nameof(edges));
            }

            AddEdge(edge[0], edge[1]);
        }
    }

    // 顶点数量
    public int Size => _vertices.Count;

    public IReadOnlyList<TElement> Vertices => _vertices;

    /// <summary>
    /// 添加顶点
    /// </summary>
    /// <param name="val">顶点中元素的值</param>
    /// <exception cref="ArgumentNullException">顶点中元素的默认值不能为空</exception>
    public void AddVertex(TElement val)
    {
        // 向顶点列表中添加新顶点值
        _vertices.Add(val);

        /* 添加新顶点后要扩充邻接矩阵至Size+1 */
        // 在邻接矩阵中添加一列
        foreach (var row in _adjacencyMatrix) row.Add(0);
        // 在邻接矩阵中添加一行
        var newRow = new List<double>(Size);
        for (int i = 0; i < Size; i++) newRow.Add(0);
        _adjacencyMatrix.Add(newRow);
    }

    /// <summary>
    /// 删除顶点
    /// </summary>
    /// <param name="index">要删除的节点在顶点列表中的索引</param>
    public void RemoveVertex(int index)
    {
        ValidateVertexIndex(index);
        // 移除索引为index的顶点
        _vertices.RemoveAt(index);
        // 在邻接矩阵中删除索引为index的行
        _adjacencyMatrix.RemoveAt(index);
        // 在邻接矩阵中删除索引为index的列
        foreach (var row in _adjacencyMatrix) row.RemoveAt(index);
    }

    /// <summary>
    /// 添加边
    /// </summary>
    /// <param name="from">起始顶点在顶点列表中的索引</param>
    /// <param name="to">终止顶点在顶点列表中的索引</param>
    /// <exception cref="ArgumentOutOfRangeException">顶点索引越界</exception>
    /// <exception cref="ArgumentException">不支持自环边</exception>
    public void AddEdge(int from, int to)
    {
        ValidateEdge(from, to);
        _adjacencyMatrix[from][to] = 1;
        if (IsUndirectedGraph) _adjacencyMatrix[to][from] = 1;
    }

    /// <summary>
    /// 删除边
    /// </summary>
    /// <param name="from">起始顶点在顶点列表中的索引</param>
    /// <param name="to">终止顶点在顶点列表中的索引</param>
    /// <exception cref="ArgumentOutOfRangeException">顶点索引越界</exception>
    /// <exception cref="ArgumentException">不支持自环边</exception>
    public void RemoveEdge(int from, int to)
    {
        ValidateEdge(from, to);
        _adjacencyMatrix[from][to] = 0;
        if (IsUndirectedGraph) _adjacencyMatrix[to][from] = 0;
    }

    public bool HasEdge(int from, int to)
    {
        ValidateVertexIndex(from);
        ValidateVertexIndex(to);
        return _adjacencyMatrix[from][to] != 0;
    }

    public string Print()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("顶点列表 = ");
        foreach (var vertex in _vertices) stringBuilder.Append(vertex + ", ");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("邻接矩阵 = ");
        foreach (var row in _adjacencyMatrix)
        {
            foreach (var element in row) stringBuilder.Append(element + ", ");
            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }

    private void ValidateEdge(int from, int to)
    {
        ValidateVertexIndex(from);
        ValidateVertexIndex(to);
        if (from == to) throw new ArgumentException("Self-loops are not supported.");
    }

    private void ValidateVertexIndex(int index)
    {
        if (index < 0 || index >= Size)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Vertex index is outside the graph.");
        }
    }
}
