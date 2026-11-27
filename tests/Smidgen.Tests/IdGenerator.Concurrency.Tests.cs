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
        var generatedIds = new ConcurrentBag<long>();

        Parallel.For(0, threadCount, _ =>
        {
            long lastId = 0;
            for (var i = 0; i < idsPerThread; i++)
            {
                var id = generator.NextId();
                Assert.True(id > lastId, "IDs should be monotonically increasing per thread.");
                lastId = id;
                generatedIds.Add(id);
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
            string lastId = "0000-0000-0000";
            for (var i = 0; i < idsPerThread; i++)
            {
                var id = generator.NextString();
                Assert.True(string.Compare(id, lastId, StringComparison.Ordinal) > 0,
                    "Raw string IDs should be monotonically increasing per thread.");
                lastId = id;
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
        var template = "ID-############";
        var generatedIds = new ConcurrentBag<string>();

        Parallel.For(0, threadCount, _ =>
        {
            var lastId = "ID-000000000000";
            for (var i = 0; i < idsPerThread; i++)
            {
                var id = generator.NextString(template);
                Assert.True(string.Compare(id, lastId, StringComparison.Ordinal) > 0,
                    "Formatted IDs should be monotonically increasing per thread.");
                lastId = id;
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
        var generatedIds = new ConcurrentBag<long>();
        var startBarrier = new Barrier(threadCount);

        var threads = Enumerable.Range(0, threadCount).Select(_ => new Thread(() =>
        {
            // All threads start at the same time to maximize contention
            startBarrier.SignalAndWait();
            long lastId = 0;
            for (var i = 0; i < idsPerThread; i++)
            {
                var id = generator.NextId();
                Assert.True(id > lastId, "IDs should be monotonically increasing per thread.");
                lastId = id;
                generatedIds.Add(id);
            }
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        // All IDs should be unique despite high contention
        Assert.Equal(threadCount * idsPerThread, generatedIds.Distinct().Count());
    }

    [Fact]
    public void ThreadSafety_NoDuplicatesAcrossThreads()
    {
        const int threadCount = 20;
        const int idsPerThread = 100;

        var generator = new IdGenerator();
        var allIds = new ConcurrentBag<long>();

        Parallel.For(0, threadCount, _ =>
        {
            for (var i = 0; i < idsPerThread; i++)
            {
                allIds.Add(generator.NextId());
            }
        });

        // Verify no duplicates and correct total count
        var distinctIds = allIds.Distinct().ToList();
        Assert.Equal(threadCount * idsPerThread, distinctIds.Count);

        // Verify all IDs are positive
        Assert.All(distinctIds, id => Assert.True(id > 0));
    }

    [Fact]
    public void ThreadSafety_MonotonicityAcrossMultipleGenerators()
    {
        // Generators running concurrently on the shared EntropyProvider each produce
        // their own strictly increasing sequence. Uniqueness across generators is not
        // asserted: it is not guaranteed, and in a burst within one interval every
        // generator climbs to the top of the entropy range and increments from there,
        // so their sequences can collide.
        const int generatorCount = 5;
        const int idsPerGenerator = 100;

        var sequences = Enumerable.Range(0, generatorCount)
            .Select(_ => (Generator: new IdGenerator(), Ids: new long[idsPerGenerator]))
            .ToArray();

        Parallel.ForEach(sequences, static sequence =>
        {
            for (var i = 0; i < sequence.Ids.Length; i++)
                sequence.Ids[i] = sequence.Generator.NextId();
        });

        Assert.All(sequences, static sequence =>
        {
            for (var i = 1; i < sequence.Ids.Length; i++)
                Assert.True(sequence.Ids[i] > sequence.Ids[i - 1], $"ID {i} is not greater than ID {i - 1}");
        });
    }
}
