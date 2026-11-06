// BST、AVL、红黑树与跳表：四种有序集合装同一批数，看谁会退化、谁会自己保持平衡。
// 把 1..15 顺序插进 BST 试试，再比较 AVL 的高度。
using DataStructureAndAlgorithm.Tree;

public static class Demo
{
    public static void Run()
    {
        int[] values = [50, 30, 70, 20, 40, 60, 80, 10];
        var bst = new BinarySearchTree<int>();
        foreach (var value in values) bst.Insert(value);
        Console.WriteLine($"BST 中序遍历：{string.Join(" ", InOrder(bst.ConvertToNormalBinaryTree()))}");
        bst.Delete(30);
        Console.WriteLine($"删除 30 后：{string.Join(" ", InOrder(bst.ConvertToNormalBinaryTree()))}，查找 40 {(bst.Find(40, out _) is null ? "失败" : "成功")}");

        var avl = new AvlTree<int>();
        foreach (var value in Enumerable.Range(1, 15)) avl.Insert(value);
        Console.WriteLine($"AVL 顺序插入 1..15：根是 {avl.RootNode!.Element}，树高 {avl.RootNode.TreeHeight}（BST 会退化成 15 层的链）");

        var redBlack = new RedBlackTree<int>();
        foreach (var value in Enumerable.Range(1, 1000)) redBlack.Add(value);
        redBlack.Remove(500);
        Console.WriteLine($"红黑树插入 1..1000 再删 500：Count = {redBlack.Count}，Height = {redBlack.Height}，不变量成立 {redBlack.HasValidInvariants()}，最小的五个 {string.Join(" ", redBlack.Take(5))}");

        var skipList = new SkipList<int>(randomSeed: 7);
        foreach (var value in values) skipList.Add(value);
        Console.WriteLine($"跳表（随机种子 7）：当前层数 {skipList.CurrentLevel}，Contains(60) = {skipList.Contains(60)}，有序遍历 {string.Join(" ", skipList)}，不变量成立 {skipList.HasValidInvariants()}");
    }

    static List<int> InOrder(NormalBinaryTree<int> tree)
    {
        var visited = new List<int>();
        tree.InOrderTraverse(node => visited.Add(node.Element));
        return visited;
    }
}
