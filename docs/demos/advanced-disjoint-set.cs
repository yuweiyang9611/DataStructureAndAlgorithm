// 并查集与动态连通性：每连一条线就合并两个集合，连通块的数量只会减少。
// 改一改连接顺序，或者删掉一条线，看哪些城市还在同一个块里。
using DataStructureAndAlgorithm.Set;

public static class Demo
{
    public static void Run()
    {
        var network = new DisjointSet<string>();
        foreach (var city in new[] { "北京", "天津", "上海", "杭州", "广州", "深圳" }) network.Add(city);
        Console.WriteLine($"六座城市，起初 {network.SetCount} 个连通块");

        (string First, string Second)[] links = [("北京", "天津"), ("上海", "杭州"), ("广州", "深圳"), ("天津", "上海")];
        foreach (var (first, second) in links)
        {
            var merged = network.Union(first, second);
            Console.WriteLine($"  连接 {first}-{second}：{(merged ? "合并了两个块" : "本来就连通")}，现在 {network.SetCount} 块");
        }
        Console.WriteLine($"再连 北京-杭州：Union 返回 {network.Union("北京", "杭州")}，块数不变");

        Console.WriteLine($"北京 与 杭州 连通？{network.AreConnected("北京", "杭州")}；北京 与 深圳？{network.AreConnected("北京", "深圳")}");
        Console.WriteLine($"北京所在块有 {network.GetSetSize("北京")} 座城市，Find(杭州) == Find(北京)：{EqualityComparer<string>.Default.Equals(network.Find("杭州"), network.Find("北京"))}");
    }
}
