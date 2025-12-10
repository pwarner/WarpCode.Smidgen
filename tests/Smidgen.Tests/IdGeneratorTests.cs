using System.Collections.Concurrent;

namespace WarpCode.Smidgen.Tests;

public class IdGeneratorTests
{
    [Fact]
    public void Generate_ShouldIncludeTimeInMostSignificantBits()
    {
        long currentTime = 1000;
        var generator = new IdGenerator(
            timeProvider: () => currentTime++,
            entropyProvider: () => 0);

        var id1 = generator.Generate();
        var id2 = generator.Generate();

        var time1 = id1 >> IdGenerator.RandomWidth;
        var time2 = id2 >> IdGenerator.RandomWidth;

        Assert.Equal(1000UL, time1);
        Assert.Equal(1001UL, time2);
    }

    [Fact]
    public void Generate_ShouldIncludeRandomInLeastSignificantBits()
    {
        var randomValue = 0;
        var generator = new IdGenerator(
            timeProvider: () => 0,
            entropyProvider: () => randomValue += 1000);

        var id1 = generator.Generate();
        var id2 = generator.Generate();

        Assert.Equal(1000UL, id1);
        Assert.Equal(2000UL, id2);
    }

    [Fact]
    public void Generate_ShouldProduceMonotonicallyIncreasingValues()
    {
        var generator = new IdGenerator();
        var id = generator.Generate();

        for (var i = 0; i < 100; i++)
        {
            var newId = generator.Generate();
            Assert.True(newId > id, $"Generated ID should be greater than previous. Previous: {id}, New: {newId}");
            id = newId;
        }
    }

    [Fact]
    public void Generate_ThreadSafety_ShouldProduceUniqueMonotonicIds()
    {
        const int threadCount = 10;
        const int idsPerThread = 100;

        var generator = new IdGenerator();
        ulong last = 0;
        var generatedIds = new ConcurrentBag<ulong>();

        Parallel.For(0, threadCount, _ =>
        {
            for (var i = 0; i < idsPerThread; i++)
            {
                var localLast = last;
                var id = generator.Generate();
                generatedIds.Add(id);
                Assert.True(id > localLast, $"Generated ID {id} is not greater than last ID {localLast}");
                Interlocked.CompareExchange(ref last, id, localLast);
            }
        });

        Assert.Equal(threadCount * idsPerThread, generatedIds.Distinct().Count());
    }

    [Fact]
    public void Generate_WithFixedInputs_ShouldIncrementByInterval()
    {
        var generator = new IdGenerator(
            timeProvider: () => 1000,
            entropyProvider: () => 1000);

        var previousId = generator.Generate();

        for (var i = 0; i < 10; i++)
        {
            var newId = generator.Generate();
            Assert.True(newId == previousId + IdGenerator.Interval, $"Generated ID should increase by {IdGenerator.Interval} from previous. Previous: {previousId}, New: {newId}");
            previousId = newId;
        }
    }

    [Fact]
    public void Generate_WithBackwardsTime_ShouldStillIncreaseMonotonically()
    {
        long currentTime = 2000;
        var generator = new IdGenerator(
            timeProvider: () => currentTime,
            entropyProvider: () => 0);

        var id1 = generator.Generate();

        // Time goes backwards
        currentTime = 1000;
        var id2 = generator.Generate();

        // Should still be greater despite backwards time
        Assert.True(id2 > id1);
    }

    [Fact]
    public void Generate_WithTimeExceedingMax_ShouldThrowArgumentOutOfRangeException()
    {
        var generator = new IdGenerator(timeProvider: () => IdGenerator.MaxTime + 1);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate());
        Assert.Contains("Time has exceeded max value", exception.Message);
    }

    [Fact]
    public void Generate_WithRandomExceedingMax_ShouldThrowArgumentOutOfRangeException()
    {
        var generator = new IdGenerator(entropyProvider: () => IdGenerator.MaxRandom + 1);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate());
        Assert.Contains("Random has exceeded max value", exception.Message);
    }

    [Fact]
    public void Generate_WithMaxValidTime_ShouldSucceed()
    {
        var generator = new IdGenerator(timeProvider: () => IdGenerator.MaxTime);

        var id = generator.Generate();
        var extractedTime = id >> IdGenerator.RandomWidth;

        Assert.Equal((ulong)IdGenerator.MaxTime, extractedTime);
    }

    [Fact]
    public void Generate_WithMaxValidRandom_ShouldSucceed()
    {
        var generator = new IdGenerator(entropyProvider: () => IdGenerator.MaxRandom);

        var id = generator.Generate();
        var extractedRandom = id & ((1UL << IdGenerator.RandomWidth) - 1);

        Assert.Equal((ulong)IdGenerator.MaxRandom, extractedRandom);
    }

    [Fact]
    public void DefaultConstructor_ShouldUseDefaultProviders()
    {
        var generator = new IdGenerator();

        var id1 = generator.Generate();
        var id2 = generator.Generate();

        Assert.True(id1 > 0);
        Assert.True(id2 > id1);
    }

    [Fact]
    public void Generate_WithDeterministicProviders_ShouldBeReproducible()
    {
        var generator1 = new IdGenerator(
            timeProvider: () => 12345,
            entropyProvider: () => 6789);

        var generator2 = new IdGenerator(
            timeProvider: () => 12345,
            entropyProvider: () => 6789);

        var id1 = generator1.Generate();
        var id2 = generator2.Generate();

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Generate_ShouldStayWithin64Bits()
    {
        var generator = new IdGenerator(
            timeProvider: () => IdGenerator.MaxTime,
            entropyProvider: () => IdGenerator.MaxRandom);

        var id = generator.Generate();

        Assert.True(id <= ulong.MaxValue);
    }
}
