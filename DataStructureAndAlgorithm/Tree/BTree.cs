namespace DataStructureAndAlgorithm.Tree;

public class BTree<TKey, TValue>
    where TKey : IComparable<TKey>
    where TValue : notnull
{
    public readonly int Degree;
    public BTreeNode<TKey, TValue> Root { get; private set; }
    public int Height { get; private set; } = 1;

    // 默认为度为2的B树（4阶B树）
    public BTree(int degree = 2)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(degree, 2);
        Degree = degree;
        Root = new BTreeNode<TKey, TValue>(degree);
    }

    public BTreeNodeEntry<TKey, TValue>? Search(TKey key) => SearchInternal(Root, key);

    public void Insert(TKey newKey, TValue newValue)
    {
        // 插入的节点未满
        if (!Root.HasReachedMaximumEntries)
        {
            InsertNotFull(Root, newKey, newValue);
            return;
        }

        // 插入的节点满了，插入会触发上溢
        // 保存旧根节点
        var oldRoot = Root;
        // 建立新的根节点
        Root = new BTreeNode<TKey, TValue>(Degree);
        // 将旧根节点作为新根节点的子节点
        Root.MutableChildren.Add(oldRoot);
        // 分离子节点
        SplitChild(Root, 0, oldRoot);
        // 将新键值对插入新的根节点
        InsertNotFull(Root, newKey, newValue);

        // 更新树高
        Height++;
    }

    public void Delete(TKey keyWantToDelete)
    {
        DeleteInternal(Root, keyWantToDelete);
        // 如果当前节点是叶子节点，或者当前节点中还保存有其他元素，就不做进一步处理。
        if (Root.Entries.Count != 0 || Root.IsLeaf) return;
        // 如果当前节点不是叶子节点，且当前节点中保存的元素全被移除了，就删除该节点。
        // 此时当前节点的子节点应该只有一个，将这一节点作为新的根结点
        Root = Root.Children.Single();
        // 更新树高
        Height--;
    }

    private void DeleteInternal(BTreeNode<TKey, TValue> node, TKey keyWantToBeDelete)
    {
        // 统计比keyWantToBeDelete小的元素个数，通过这种方式确定要删除位置的索引。
        var index = FindFirstGreaterThanOrEqual(node.Entries, keyWantToBeDelete);
        // 如果i没超出索引范围再判断索引i对应的位置是不是要删除的元素
        if (index < node.Entries.Count && node.Entries[index].Key.CompareTo(keyWantToBeDelete) == 0)
        {
            // 找到了该元素
            DeleteKeyFromNode(node, keyWantToBeDelete, index);
            return;
        }

        // 如果index对应的位置不是要删除的元素，且当前节点不是叶子节点，那么要删除的元素在索引i对应的子树上
        if (!node.IsLeaf)
        {
            DeleteKeyFromSubTree(node, keyWantToBeDelete, index);
        }
    }

    private void DeleteKeyFromSubTree(BTreeNode<TKey, TValue> parentNode, TKey keyWantToBeDelete, int index)
    {
        var childNode = PrepareChildForDeletion(parentNode, index);

        // 删除要删除的元素
        DeleteInternal(childNode, keyWantToBeDelete);
    }

    private BTreeNode<TKey, TValue> PrepareChildForDeletion(BTreeNode<TKey, TValue> parentNode, int index)
    {
        var childNode = parentNode.Children[index];
        // 如果childNode中的元素个数达到了最小，此时再移除元素会破坏树结构，需要调整
        if (childNode.HasReachedMinimumEntries)
        {
            // 调整要借用与childNode相邻的左右分支
            var leftIndex = index - 1;
            var rightIndex = index + 1;
            var leftSibling = index > 0 ? parentNode.Children[leftIndex] : null;
            var rightSibling = index < parentNode.Children.Count - 1 ? parentNode.Children[rightIndex] : null;

            // 左边分支中的元素可以借用
            if (leftSibling != null && leftSibling.Entries.Count > Degree - 1)
            {
                childNode.MutableEntries.Insert(0, parentNode.Entries[leftIndex]);
                parentNode.MutableEntries[leftIndex] = leftSibling.Entries.Last();
                leftSibling.MutableEntries.RemoveAt(leftSibling.Entries.Count - 1);

                if (!leftSibling.IsLeaf)
                {
                    childNode.MutableChildren.Insert(0, leftSibling.Children.Last());
                    leftSibling.MutableChildren.RemoveAt(leftSibling.Children.Count - 1);
                }
            }
            // 右边分支的元素可以借用
            else if (rightSibling != null && rightSibling.Entries.Count > Degree - 1)
            {
                childNode.MutableEntries.Add(parentNode.Entries[index]);
                parentNode.MutableEntries[index] = rightSibling.Entries.First();
                rightSibling.MutableEntries.RemoveAt(0);

                if (!rightSibling.IsLeaf)
                {
                    childNode.MutableChildren.Add(rightSibling.Children.First());
                    rightSibling.MutableChildren.RemoveAt(0);
                }
            }
            // 没有元素可以借用，当前分支吞并与其相邻左分支或右分支
            else
            {
                if (leftSibling != null)
                {
                    childNode.MutableEntries.Insert(0, parentNode.Entries[leftIndex]);
                    var oldEntries = childNode.Entries.ToArray();
                    childNode.MutableEntries.Clear();
                    childNode.MutableEntries.AddRange(leftSibling.Entries);
                    childNode.MutableEntries.AddRange(oldEntries);
                    if (!leftSibling.IsLeaf)
                    {
                        var oldChildren = childNode.Children.ToArray();
                        childNode.MutableChildren.Clear();
                        childNode.MutableChildren.AddRange(leftSibling.Children);
                        childNode.MutableChildren.AddRange(oldChildren);
                    }

                    parentNode.MutableChildren.RemoveAt(leftIndex);
                    parentNode.MutableEntries.RemoveAt(leftIndex);
                }
                else if (rightSibling != null)
                {
                    childNode.MutableEntries.Add(parentNode.Entries[index]);
                    childNode.MutableEntries.AddRange(rightSibling.Entries);
                    if (!rightSibling.IsLeaf)
                    {
                        childNode.MutableChildren.AddRange(rightSibling.Children);
                    }

                    parentNode.MutableChildren.RemoveAt(rightIndex);
                    parentNode.MutableEntries.RemoveAt(index);
                }
                else
                {
                    throw new NullReferenceException("Node should have at least one sibling");
                }
            }
        }

        return childNode;
    }

    private void DeleteKeyFromNode(BTreeNode<TKey, TValue> node, TKey keyWantToBeDelete, int index)
    {
        if (node.IsLeaf)
        {
            node.MutableEntries.RemoveAt(index);
            return;
        }

        var predecessorChild = node.Children[index];
        if (predecessorChild.Entries.Count >= this.Degree)
        {
            var predecessor = DeletePredecessor(predecessorChild);
            node.MutableEntries[index] = predecessor;
        }
        else
        {
            var successorChild = node.Children[index + 1];
            if (successorChild.Entries.Count >= this.Degree)
            {
                var successor = DeleteSuccessor(successorChild);
                node.MutableEntries[index] = successor;
            }
            else
            {
                predecessorChild.MutableEntries.Add(node.Entries[index]);
                predecessorChild.MutableEntries.AddRange(successorChild.Entries);
                predecessorChild.MutableChildren.AddRange(successorChild.Children);

                node.MutableEntries.RemoveAt(index);
                node.MutableChildren.RemoveAt(index + 1);

                DeleteInternal(predecessorChild, keyWantToBeDelete);
            }
        }
    }

    private BTreeNodeEntry<TKey, TValue> DeleteSuccessor(BTreeNode<TKey, TValue> node)
    {
        if (node.IsLeaf)
        {
            var result = node.Entries[0];
            node.MutableEntries.RemoveAt(0);
            return result;
        }

        // 删除极小元素前，沿整条下降路径保证下一子节点至少有 Degree 个键。
        var successorChild = PrepareChildForDeletion(node, 0);
        return DeleteSuccessor(successorChild);
    }

    private BTreeNodeEntry<TKey, TValue> DeletePredecessor(BTreeNode<TKey, TValue> node)
    {
        if (node.IsLeaf)
        {
            var result = node.Entries[^1];
            node.MutableEntries.RemoveAt(node.Entries.Count - 1);
            return result;
        }

        // 删除极大元素前，沿整条下降路径保证下一子节点至少有 Degree 个键。
        var predecessorChild = PrepareChildForDeletion(node, node.Children.Count - 1);
        return DeletePredecessor(predecessorChild);
    }

    private void SplitChild(BTreeNode<TKey, TValue> parentNode, int nodeToBeSplitIndex,
        BTreeNode<TKey, TValue> nodeToBeSplit)
    {
        var newNode = new BTreeNode<TKey, TValue>(Degree);

        parentNode.MutableEntries.Insert(nodeToBeSplitIndex, nodeToBeSplit.Entries[Degree - 1]);
        parentNode.MutableChildren.Insert(nodeToBeSplitIndex + 1, newNode);

        newNode.MutableEntries.AddRange(nodeToBeSplit.MutableEntries.GetRange(Degree, this.Degree - 1));

        nodeToBeSplit.MutableEntries.RemoveRange(Degree - 1, Degree);

        if (nodeToBeSplit.IsLeaf) return;
        newNode.MutableChildren.AddRange(nodeToBeSplit.MutableChildren.GetRange(Degree, Degree));
        nodeToBeSplit.MutableChildren.RemoveRange(Degree, Degree);
    }

    private void InsertNotFull(BTreeNode<TKey, TValue> node, TKey newKey, TValue newValue)
    {
        while (true)
        {
            var positionToInsert = FindFirstGreaterThan(node.Entries, newKey);
            // leaf node
            if (node.IsLeaf)
            {
                node.MutableEntries.Insert(positionToInsert,
                    new BTreeNodeEntry<TKey, TValue>(newKey, newValue));
                return;
            }

            // non-leaf
            var child = node.Children[positionToInsert];
            if (child.HasReachedMaximumEntries)
            {
                SplitChild(node, positionToInsert, child);
                if (newKey.CompareTo(node.Entries[positionToInsert].Key) > 0)
                {
                    positionToInsert++;
                }
            }

            node = node.Children[positionToInsert];
        }
    }

    private static BTreeNodeEntry<TKey, TValue>? SearchInternal(BTreeNode<TKey, TValue> node, TKey key)
    {
        while (true)
        {
            var i = FindFirstGreaterThanOrEqual(node.Entries, key);

            if (i < node.Entries.Count && node.Entries[i].Key.CompareTo(key) == 0)
            {
                return node.Entries[i];
            }

            if (node.IsLeaf) return null;

            node = node.Children[i];
        }
    }

    private static int FindFirstGreaterThanOrEqual(IReadOnlyList<BTreeNodeEntry<TKey, TValue>> entries, TKey key)
    {
        var low = 0;
        var high = entries.Count;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (entries[middle].Key.CompareTo(key) < 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }

    private static int FindFirstGreaterThan(IReadOnlyList<BTreeNodeEntry<TKey, TValue>> entries, TKey key)
    {
        var low = 0;
        var high = entries.Count;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (entries[middle].Key.CompareTo(key) <= 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }
}
