using BenchmarkDotNet.Attributes;
using DataStructureAndAlgorithm.Hashing;
using DataStructureAndAlgorithm.Memory;
using DataStructureAndAlgorithm.Sorting;
using DataStructureAndAlgorithm.Tree;

namespace DataStructureAndAlgorithm.Benchmarks;

[MemoryDiagnoser]
public class SortingBenchmarks
{
    private int[] _source = [];
    private int[] _working = [];

    [Params(1_000, 10_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var random = new Random(42);
        _source = Enumerable.Range(0, Count).Select(_ => random.Next(int.MinValue, int.MaxValue)).ToArray();
    }

    [IterationSetup]
    public void IterationSetup() => _working = (int[])_source.Clone();

    [Benchmark(Baseline = true)]
    public void ArraySort() => Array.Sort(_working);

    [Benchmark]
    public void RadixSort() => NonComparisonSortAlgorithms.RadixSort(_working);

    [Benchmark]
    public void PooledSpanRadixSort() => SpanAlgorithms.PooledRadixSort(_working);
}

[MemoryDiagnoser]
public class OrderedSetBenchmarks
{
    private int[] _values = [];

    [Params(1_000, 10_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup() => _values = Enumerable.Range(0, Count).Reverse().ToArray();

    [Benchmark(Baseline = true)]
    public int SortedSet()
    {
        var set = new SortedSet<int>();
        foreach (var value in _values) set.Add(value);
        return set.Count;
    }

    [Benchmark]
    public int RedBlackTree()
    {
        var set = new RedBlackTree<int>();
        foreach (var value in _values) set.Add(value);
        return set.Count;
    }

    [Benchmark]
    public int SkipList()
    {
        var set = new SkipList<int>(randomSeed: 42);
        foreach (var value in _values) set.Add(value);
        return set.Count;
    }
}

[MemoryDiagnoser]
public class HashTableBenchmarks
{
    private int[] _values = [];

    [Params(1_000, 10_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup() => _values = Enumerable.Range(0, Count).ToArray();

    [Benchmark(Baseline = true)]
    public int Dictionary()
    {
        var dictionary = new Dictionary<int, int>();
        foreach (var value in _values) dictionary[value] = value;
        return dictionary.Count;
    }

    [Benchmark]
    public int OpenAddressing()
    {
        var table = new OpenAddressingHashTable<int, int>();
        foreach (var value in _values) table[value] = value;
        return table.Count;
    }
}
