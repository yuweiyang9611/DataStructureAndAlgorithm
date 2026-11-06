// 链表、栈、队列与双端队列：同一批数据放进五种线性结构，看每种结构从哪一头进、从哪一头出。
// 循环队列的底层数组会绕回，但出队顺序始终是入队顺序。
using DataStructureAndAlgorithm.Linear;

public static class Demo
{
    public static void Run()
    {
        var list = new SinglyLinkedList<string>();
        list.AddLast("B");
        list.AddLast("C");
        list.AddFirst("A");
        Console.WriteLine($"单链表：{string.Join(" -> ", list)}（Count = {list.Count}）");
        list.Remove("B");
        Console.WriteLine($"删除 B 后：{string.Join(" -> ", list)}，RemoveFirst 取出 {list.RemoveFirst()}");

        var doubly = new DoublyLinkedList<int>();
        foreach (var value in new[] { 1, 2, 3, 4 }) doubly.AddLast(value);
        Console.WriteLine($"双链表 {string.Join(" <-> ", doubly)}：两头各取一个得到 {doubly.RemoveFirst()} 和 {doubly.RemoveLast()}，剩下 {string.Join(" <-> ", doubly)}");

        var stack = new ArrayStack<char>(capacity: 4);
        foreach (var bracket in "([{") stack.Push(bracket);
        Console.WriteLine($"栈顶 {stack.Peek()}，弹出顺序 {stack.Pop()} {stack.Pop()} {stack.Pop()}（后进先出）");

        var queue = new CircularQueue<int>(capacity: 3);
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);
        var first = queue.Dequeue();
        queue.Enqueue(4);
        Console.WriteLine($"循环队列出队 {first}，再入队 4 绕回数组开头：队首 {queue.Peek()}，Count = {queue.Count}，Capacity = {queue.Capacity}");

        var deque = new ArrayDeque<int>();
        deque.AddLast(3);
        deque.AddFirst(2);
        deque.AddLast(4);
        deque.AddFirst(1);
        Console.WriteLine($"双端队列：{string.Join(", ", deque)}，首 {deque.PeekFirst()} 尾 {deque.PeekLast()}");
        Console.WriteLine($"RemoveFirst {deque.RemoveFirst()}，RemoveLast {deque.RemoveLast()}，剩下 {string.Join(", ", deque)}");
    }
}
