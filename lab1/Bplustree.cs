namespace lab1;

public record Person(string Name, string Phone, string Address);

public class BPlusNode
{
    public bool IsLeaf;
    public List<long> Keys = new();
    public List<BPlusNode?> Children = new();
    public List<List<Person>> Records = new();
    public BPlusNode? Next;

    public BPlusNode(bool isLeaf) { IsLeaf = isLeaf; }
}

public class BPlusTree
{
    public const int Order = 4;
    public const int MaxKeys = 2 * Order - 1;
    public const int MinKeys = Order - 1;
    public const int MaxDepth = 3;

    private BPlusNode _root;
    private int _depth = 1;

    public BPlusTree()
    {
        _root = new BPlusNode(isLeaf: true);
    }

    private static long Hash(string name) => NameHashFunction.Compute(name);

    public void Insert(Person person)
    {
        long key = Hash(person.Name);
        var result = InsertRecursive(_root, key, person, 1);

        if (result != null)
        {
            if (_depth >= MaxDepth) throw new InvalidOperationException($"Досягнуто максимальну глибину дерева ({MaxDepth}) ");

            var newRoot = new BPlusNode(isLeaf: false);
            newRoot.Keys.Add(result.Value.promotedKey);
            newRoot.Children.Add(_root);
            newRoot.Children.Add(result.Value.newNode);
            _root = newRoot;
            _depth++;
        }
    }

    private (long promotedKey, BPlusNode newNode)? InsertRecursive(BPlusNode node, long key, Person person, int currentDepth)
    {
        if (node.IsLeaf)
        {
            InsertIntoLeaf(node, key, person);
            if (node.Keys.Count <= MaxKeys) return null;
            return SplitLeaf(node);
        }

        int idx = FindChildIndex(node, key);
        var splitResult = InsertRecursive(node.Children[idx]!, key, person, currentDepth + 1);

        if (splitResult == null) return null;

        var (pk, newChild) = splitResult.Value;
        node.Keys.Insert(idx, pk);
        node.Children.Insert(idx + 1, newChild);

        if (node.Keys.Count <= MaxKeys) return null;
        return SplitInternal(node);
    }

    private void InsertIntoLeaf(BPlusNode leaf, long key, Person person)
    {
        int idx = leaf.Keys.BinarySearch(key);
        if (idx >= 0) leaf.Records[idx].Add(person);
        else
        {
            idx = ~idx;
            leaf.Keys.Insert(idx, key);
            leaf.Records.Insert(idx, new List<Person> { person });
        }
    }

    private (long, BPlusNode) SplitLeaf(BPlusNode leaf)
    {
        int mid = leaf.Keys.Count / 2;
        var newLeaf = new BPlusNode(isLeaf: true);

        newLeaf.Keys.AddRange(leaf.Keys[mid..]);
        newLeaf.Records.AddRange(leaf.Records[mid..]);
        leaf.Keys.RemoveRange(mid, leaf.Keys.Count - mid);
        leaf.Records.RemoveRange(mid, leaf.Records.Count - mid);

        newLeaf.Next = leaf.Next;
        leaf.Next = newLeaf;

        return (newLeaf.Keys[0], newLeaf);
    }

    private (long, BPlusNode) SplitInternal(BPlusNode node)
    {
        int mid = node.Keys.Count / 2;
        long promoted = node.Keys[mid];

        var newNode = new BPlusNode(isLeaf: false);
        newNode.Keys.AddRange(node.Keys[(mid + 1)..]);
        newNode.Children.AddRange(node.Children[(mid + 1)..]);

        node.Keys.RemoveRange(mid, node.Keys.Count - mid);
        node.Children.RemoveRange(mid + 1, node.Children.Count - mid - 1);

        return (promoted, newNode);
    }

    public List<Person> Search(string name)
    {
        long key = Hash(name);
        var leaf = FindLeaf(_root, key);
        int idx = leaf.Keys.BinarySearch(key);
        if (idx < 0) return new List<Person>();

        return leaf.Records[idx]
            .Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
    
    public List<Person> SearchGreaterOrEqual(string name)
    {
        long key = Hash(name);
        var leaf = FindLeaf(_root, key);
        var result = new List<Person>();

        var cur = leaf;
        while (cur != null)
        {
            for (int i = 0; i < cur.Keys.Count; i++) if (cur.Keys[i] >= key) result.AddRange(cur.Records[i]);
            cur = cur.Next;
        }
        return result;
    }

    public List<Person> SearchLess(string name)
    {
        long key = Hash(name);
        var result = new List<Person>();
        CollectLess(_root, key, result);
        return result;
    }

    private void CollectLess(BPlusNode node, long key, List<Person> result)
    {
        if (node.IsLeaf)
        {
            for (int i = 0; i < node.Keys.Count; i++) if (node.Keys[i] < key) result.AddRange(node.Records[i]);
            return;
        }
        foreach (var child in node.Children) if (child != null) CollectLess(child, key, result);
    }

    public bool Delete(string name)
    {
        long key = Hash(name);
        bool deleted = DeleteRecursive(_root, key, name, null, -1);

        if (!_root.IsLeaf && _root.Keys.Count == 0 && _root.Children.Count > 0)
        {
            _root = _root.Children[0]!;
            _depth--;
        }
        return deleted;
    }

    private bool DeleteRecursive(BPlusNode node, long key, string name, BPlusNode? parent, int parentIdx)
    {
        if (node.IsLeaf)
        {
            int idx = node.Keys.BinarySearch(key);
            if (idx < 0) return false;

            node.Records[idx].RemoveAll(p =>
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

            if (node.Records[idx].Count == 0)
            {
                node.Keys.RemoveAt(idx);
                node.Records.RemoveAt(idx);
            }

            if (parent != null && node.Keys.Count < MinKeys) FixUnderflow(node, parent, parentIdx);

            return true;
        }

        int childIdx = FindChildIndex(node, key);
        bool result = DeleteRecursive(node.Children[childIdx]!, key, name, node, childIdx);

        return result;
    }

    private void FixUnderflow(BPlusNode node, BPlusNode parent, int idx)
    {
        if (idx > 0)
        {
            var leftSibling = parent.Children[idx - 1]!;
            if (leftSibling.Keys.Count > MinKeys)
            {
                BorrowFromLeft(node, leftSibling, parent, idx);
                return;
            }
        }
        if (idx < parent.Children.Count - 1)
        {
            var rightSibling = parent.Children[idx + 1]!;
            if (rightSibling.Keys.Count > MinKeys)
            {
                BorrowFromRight(node, rightSibling, parent, idx);
                return;
            }
        }
        if (idx > 0) Merge(parent.Children[idx - 1]!, node, parent, idx - 1);
        else Merge(node, parent.Children[idx + 1]!, parent, idx);
    }

    private void BorrowFromLeft(BPlusNode node, BPlusNode left, BPlusNode parent, int idx)
    {
        if (node.IsLeaf)
        {
            node.Keys.Insert(0, left.Keys[^1]);
            node.Records.Insert(0, left.Records[^1]);
            left.Keys.RemoveAt(left.Keys.Count - 1);
            left.Records.RemoveAt(left.Records.Count - 1);
            parent.Keys[idx - 1] = node.Keys[0];
        }
        else
        {
            node.Keys.Insert(0, parent.Keys[idx - 1]);
            node.Children.Insert(0, left.Children[^1]);
            parent.Keys[idx - 1] = left.Keys[^1];
            left.Keys.RemoveAt(left.Keys.Count - 1);
            left.Children.RemoveAt(left.Children.Count - 1);
        }
    }

    private void BorrowFromRight(BPlusNode node, BPlusNode right, BPlusNode parent, int idx)
    {
        if (node.IsLeaf)
        {
            node.Keys.Add(right.Keys[0]);
            node.Records.Add(right.Records[0]);
            right.Keys.RemoveAt(0);
            right.Records.RemoveAt(0);
            parent.Keys[idx] = right.Keys[0];
        }
        else
        {
            node.Keys.Add(parent.Keys[idx]);
            node.Children.Add(right.Children[0]);
            parent.Keys[idx] = right.Keys[0];
            right.Keys.RemoveAt(0);
            right.Children.RemoveAt(0);
        }
    }

    private void Merge(BPlusNode left, BPlusNode right, BPlusNode parent, int leftIdx)
    {
        if (left.IsLeaf)
        {
            left.Keys.AddRange(right.Keys);
            left.Records.AddRange(right.Records);
            left.Next = right.Next;
        }
        else
        {
            left.Keys.Add(parent.Keys[leftIdx]);
            left.Keys.AddRange(right.Keys);
            left.Children.AddRange(right.Children);
        }
        parent.Keys.RemoveAt(leftIdx);
        parent.Children.RemoveAt(leftIdx + 1);
    }

    private static BPlusNode FindLeaf(BPlusNode node, long key)
    {
        while (!node.IsLeaf) node = node.Children[FindChildIndex(node, key)]!;
        return node;
    }

    private static int FindChildIndex(BPlusNode node, long key)
    {
        int i = 0;
        while (i < node.Keys.Count && key >= node.Keys[i]) i++;
        return i;
    }

    public void PrintTree()
    {
        Console.WriteLine($"\n{'═',60}");
        Console.WriteLine($"  B+ Дерево (Order={Order}, MaxDepth={MaxDepth}, Depth={_depth})");
        Console.WriteLine($"{'═',60}");
        PrintNode(_root, "", true, 1);
        Console.WriteLine();
    }

    private void PrintNode(BPlusNode node, string indent, bool isLast, int depth)
    {
        string branch = isLast ? "└── " : "├── ";
        string type = node.IsLeaf ? "[Листок]" : "[Вузол] ";
        string keys = string.Join(", ", node.Keys);
        Console.WriteLine($"{indent}{branch}{type} Keys: [{keys}]  (глибина {depth})");

        if (node.IsLeaf)
        {
            string childIndent = indent + (isLast ? "    " : "│   ");
            for (int i = 0; i < node.Keys.Count; i++)
            {
                var persons = node.Records[i];
                foreach (var p in persons) Console.WriteLine($"{childIndent}    - {p.Name} | {p.Phone}");
            }
            return;
        }

        string newIndent = indent + (isLast ? "    " : "│   ");
        for (int i = 0; i < node.Children.Count; i++)
        {
            bool last = i == node.Children.Count - 1;
            PrintNode(node.Children[i]!, newIndent, last, depth + 1);
        }
    }

    public int Depth => _depth;
}