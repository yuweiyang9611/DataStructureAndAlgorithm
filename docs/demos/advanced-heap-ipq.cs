// 二叉堆与索引优先队列：堆只认大小，索引优先队列还能按键找到元素并把优先级降下来。
// 试着把“打包”的优先级改成 25，看出队顺序怎么变。
using DataStructureAndAlgorithm.Heap;

public static class Demo
{
    public static void Run()
    {
        var heap = new BinaryMinHeap<int>(new[] { 9, 4, 7, 1, 8, 2 });
        heap.Enqueue(0);
        Console.WriteLine($"建堆后 Count = {heap.Count}，堆顶 {heap.Peek()}");
        var drained = new List<int>();
        while (heap.Count > 0) drained.Add(heap.Dequeue());
        Console.WriteLine($"二叉最小堆依次出堆：{string.Join(" ", drained)}");

        var tasks = new IndexedPriorityQueue<string, int>();
        tasks.EnqueueOrDecrease("编译", 30);
        tasks.EnqueueOrDecrease("测试", 20);
        tasks.EnqueueOrDecrease("打包", 40);
        Console.WriteLine($"把“打包”的优先级从 40 降到 10：{tasks.EnqueueOrDecrease("打包", 10)}；再想升到 50：{tasks.EnqueueOrDecrease("打包", 50)}（只允许降低）");
        Console.WriteLine($"含“测试”{tasks.ContainsKey("测试")}，不变量成立 {tasks.HasValidInvariants()}");
        while (tasks.TryDequeue(out var key, out var priority)) Console.WriteLine($"  出队 {key}（优先级 {priority}）");
        Console.WriteLine($"清空后 Count = {tasks.Count}");
    }
}
