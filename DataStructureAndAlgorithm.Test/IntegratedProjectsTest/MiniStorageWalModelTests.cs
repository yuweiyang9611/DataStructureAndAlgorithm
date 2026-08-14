using System.IO;
using DataStructureAndAlgorithm.Scenarios.MiniStorage;

namespace DataStructureAndAlgorithm.Test.IntegratedProjectsTest;

/// <summary>
/// 用独立的 <see cref="SortedDictionary{TKey,TValue}"/> 参考模型持续校验 WAL 存储引擎。
/// </summary>
/// <remarks>
/// 操作由固定的线性同余生成器产生，看起来像随机状态机，却不依赖平台随机数实现；测试失败时可以用同一
/// 种子逐步重放。参考模型只表达“最后一次 Put 生效、Delete 移除、Get 读取”的业务语义，不复制 B+ 树、
/// Bloom Filter、LFU 或 WAL 代码，因此两边不容易共享同一个实现错误。
/// </remarks>
public sealed class MiniStorageWalModelTests
{
    [Fact]
    public void DeterministicRandomOperations_AreEquivalentToModelAcrossWalRestarts()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            $"data-structure-algorithm-mini-storage-wal-model-{Guid.NewGuid():N}");
        var walPath = Path.Combine(temporaryDirectory, "operations.wal");
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var options = new MiniStorageOptions(
                BPlusTreeOrder: 4,
                BloomBitCount: 4_096,
                BloomHashFunctionCount: 5,
                CacheCapacity: 5,
                WriteAheadLogPath: walPath);
            var model = new SortedDictionary<string, string>(StringComparer.Ordinal);
            uint generatorState = 0xC0FF_EE42;
            long persistedOperationCount = 0;

            // 第一阶段从空 WAL 开始，且每一步都核对业务状态与 B+ 树不变量。
            using (var first = new MiniStorageEngine(options))
            {
                ApplyOperations(
                    first,
                    model,
                    operationCount: 240,
                    ref generatorState,
                    ref persistedOperationCount);
                AssertMatchesModel(first, model, persistedOperationCount, expectedReplayCount: 0);
            }

            // 第一次重启证明 WAL 可以完整恢复模型；随后继续写入，验证恢复后的版本号还能单调递增。
            using (var restarted = new MiniStorageEngine(options))
            {
                AssertMatchesModel(restarted, model, persistedOperationCount, persistedOperationCount);
                ApplyOperations(
                    restarted,
                    model,
                    operationCount: 80,
                    ref generatorState,
                    ref persistedOperationCount);
                AssertMatchesModel(restarted, model, persistedOperationCount, expectedReplayCount: null);
            }

            // 第二次重启会从头回放所有持久化写操作，最终状态仍必须与 SortedDictionary 完全相同。
            using var recoveredAgain = new MiniStorageEngine(options);
            AssertMatchesModel(recoveredAgain, model, persistedOperationCount, persistedOperationCount);
        }
        finally
        {
            // WAL 流已在 using 作用域中关闭；无论断言成功或失败，都递归清理本测试创建的独立目录。
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }
    }

    private static void ApplyOperations(
        MiniStorageEngine engine,
        SortedDictionary<string, string> model,
        int operationCount,
        ref uint generatorState,
        ref long persistedOperationCount)
    {
        for (var step = 0; step < operationCount; step++)
        {
            var key = $"key:{Next(ref generatorState) % 24:D2}";
            switch (Next(ref generatorState) % 3)
            {
                case 0:
                    var value = $"value:{step:D3}:{Next(ref generatorState) % 10_000:D4}";
                    engine.Put(key, value);
                    model[key] = value;
                    persistedOperationCount++;
                    break;
                case 1:
                    var actualDeleted = engine.Delete(key);
                    var expectedDeleted = model.Remove(key);
                    Assert.Equal(expectedDeleted, actualDeleted);
                    if (actualDeleted)
                    {
                        persistedOperationCount++;
                    }

                    break;
                default:
                    var actualFound = engine.TryGet(key, out var actual);
                    var expectedFound = model.TryGetValue(key, out var expected);
                    Assert.Equal(expectedFound, actualFound);
                    Assert.Equal(expected, actual?.Value);
                    break;
            }

            AssertMatchesModel(engine, model, persistedOperationCount, expectedReplayCount: null);
        }
    }

    private static void AssertMatchesModel(
        MiniStorageEngine engine,
        SortedDictionary<string, string> model,
        long persistedOperationCount,
        long? expectedReplayCount)
    {
        var actual = engine.RangeScan("key:00", "key:99")
            .Select(entry => new KeyValuePair<string, string>(entry.Key, entry.Value));

        Assert.Equal(model.Count, engine.Count);
        Assert.True(actual.SequenceEqual(model));
        Assert.True(engine.HasValidIndexInvariants());
        Assert.Equal(persistedOperationCount, engine.GetStatistics().Version);
        if (expectedReplayCount is { } replayCount)
        {
            Assert.Equal(replayCount, engine.GetStatistics().WalRecordsReplayed);
        }
    }

    private static uint Next(ref uint state)
    {
        // 32 位 LCG 的溢出就是定义的一部分；unchecked 使 Debug/Release 都得到相同序列。
        state = unchecked(state * 1_664_525U + 1_013_904_223U);
        return state;
    }
}
