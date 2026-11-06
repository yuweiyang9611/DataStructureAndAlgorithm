// 链地址与开放寻址哈希表：同样七个键放进两种表，看扩容、重复键、删除后再查找各有什么表现。
// 把初始容量改小，或者多插几个键，观察 Capacity 什么时候翻倍。
using DataStructureAndAlgorithm.Hashing;

public static class Demo
{
    public static void Run()
    {
        var chaining = new SeparateChainingHashTable<string, int>(capacity: 4);
        var open = new OpenAddressingHashTable<string, int>(capacity: 4);
        string[] words = ["heap", "trie", "graph", "stack", "queue", "deque", "hash"];
        for (var index = 0; index < words.Length; index++)
        {
            chaining.TryAdd(words[index], index);
            open.TryAdd(words[index], index);
        }
        Console.WriteLine($"插入 {words.Length} 个键：链地址 Count = {chaining.Count}，Capacity = {chaining.Capacity}；开放寻址 Count = {open.Count}，Capacity = {open.Capacity}");
        Console.WriteLine($"再插一次 trie：链地址 {chaining.TryAdd("trie", 99)}，开放寻址 {open.TryAdd("trie", 99)}（TryAdd 不覆盖）");

        chaining["trie"] = 42;
        open["trie"] = 42;
        Console.WriteLine($"索引器改写 trie：{chaining["trie"]} / {open["trie"]}");

        Console.WriteLine($"删除 graph：{chaining.Remove("graph")} / {open.Remove("graph")}，再查 graph：{chaining.ContainsKey("graph")} / {open.ContainsKey("graph")}");
        Console.WriteLine($"开放寻址删除后用墓碑占位，后面的键仍能找到：hash = {(open.TryGetValue("hash", out var hash) ? hash : -1)}");

        var pairs = chaining.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}");
        Console.WriteLine($"链地址表按键排序：{string.Join(", ", pairs)}");
    }
}
