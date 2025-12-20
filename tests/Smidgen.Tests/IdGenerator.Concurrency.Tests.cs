using System.Collections.Concurrent;

namespace WarpCode.Smidgen.Tests;

/// <summary>
/// Tests for thread safety and concurrent ID generation.
/// </summary>
public class IdGeneratorConcurrencyTests
{
    [Fact]
    public void ThreadSafety_ShouldGenerateUniqueMonotonicIds()
    {
        const int threadCount = 10;
        const int idsPerThread = 100;

        var generator = new IdGenerator();
        var generatedIds = new ConcurrentBag<UInt128>();

        Parallel.For(0, threadCount, _ =>
        {
            UInt128 lastId = UInt128.Zero;
            for (var i = 0; i < idsPerThread; i++)
            {
                var id = generator.NextUInt128();
                Assert.True(id > lastId, "IDs should be monotonically increasing per thread.");
                generatedIds.Add(generator.NextUInt128());
            }
        });

        // All IDs should be unique
        Assert.Equal(threadCount * idsPerThread, generatedIds.Distinct().Count());
    }

    [Fact]
    public void ThreadSafety_RawStringGeneration()
    {
        const int threadCount = 10;
        const int idsPerThread = 100;

        var generator = new IdGenerator();
        var generatedIds = new ConcurrentBag<string>();

        Parallel.For(0, threadCount, _ =>
        {
            string lastId = "0000000000000";
            for (var i = 0; i < idsPerThread; i++)
            {
                var id = generator.NextRawStringId();
                Assert.True(string.Compare(id, lastId, StringComparison.Ordinal) > 0, "Raw string IDs should be monotonically increasing per thread.");
                generatedIds.Add(id);
            }
        });

        // All IDs should be unique
        Assert.Equal(threadCount * idsPerThread, generatedIds.Distinct().Count());
    }

    [Fact]
    public void ThreadSafety_FormattedIdGeneration()
    {
        const int threadCount = 10;
        const int idsPerThread = 50;

        var generator = new IdGenerator();
        var template = $"ID-#############";
        var generatedIds = new ConcurrentBag<string>();

        Parallel.For(0, threadCount, _ =>
        {
            var lastId = "ID-0000000000000";
            for (var i = 0; i < idsPerThread; i++)
            {
                var id = generator.NextFormattedId(template);
                Assert.True(string.Compare(id, lastId, StringComparison.Ordinal) > 0, "Formatted IDs should be monotonically increasing per thread.");
                generatedIds.Add(id);
            }
        });

        // All IDs should be unique
        Assert.Equal(threadCount * idsPerThread, generatedIds.Distinct().Count());
    }

    [Fact]
    public void ThreadSafety_HighContentionScenario()
    {
        const int threadCount = 50;
        const int idsPerThread = 20;

        var generator = new IdGenerator();
        var generatedIds = new ConcurrentBag<UInt128>();
        var startBarrier = new Barrier(threadCount);

        var threads = Enumerable.Range(0, threadCount).Select(_ => new Thread(() =>
        {
            // All threads start at the same time to maximize contention
            startBarrier.SignalAndWait();
            var lastId = UInt128.Zero;
            for (var i = 0; i < idsPerThread; i++)
            {
                var id = generator.NextUInt128();
                Assert.True(id > lastId, "IDs should be monotonically increasing per thread.");
                generatedIds.Add(id);
            }
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        // All IDs should be unique despite high contention
        Assert.Equal(threadCount * idsPerThread, generatedIds.Distinct().Count());
    }
}
