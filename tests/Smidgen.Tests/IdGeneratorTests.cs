using System.Collections.Concurrent;

namespace WarpCode.Smidgen.Tests;

public class IdGeneratorTests
{
    [Fact]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new IdGenerator(null!));
        Assert.Equal("settings", exception.ParamName);
    }

    [Fact]
    public void Next_ShouldIncludeTimeInMostSignificantBits()
    {
        ulong currentTime = 1000;
        var settings = new GeneratorSettings(
            timeBits: 48,
            entropyBits: 16,
            getTimeElement: () => currentTime++,
            getEntropyElement: () => 0,
            incrementFunction: () => 1,
            getDateTimeFromId: t => DateTime.UnixEpoch.AddMilliseconds(t));

        var generator = new IdGenerator(settings);
        UInt128 id1 = generator.Next();
        UInt128 id2 = generator.Next();

        UInt128 time1 = id1 >> 16; // Shift right to remove entropy bits
        UInt128 time2 = id2 >> 16;

        Assert.Equal((UInt128)1000, time1);
        Assert.Equal((UInt128)1001, time2);
    }

    [Fact]
    public void Next_ShouldIncludeEntropyInLeastSignificantBits()
    {
        ulong randomValue = 0;
        var settings = new GeneratorSettings(
            timeBits: 48,
            entropyBits: 16,
            getTimeElement: () => 0,
            getEntropyElement: () => randomValue += 1000,
            incrementFunction: () => 1,
            getDateTimeFromId: t => DateTime.UnixEpoch.AddMilliseconds(t));

        var generator = new IdGenerator(settings);
        UInt128 id1 = generator.Next();
        UInt128 id2 = generator.Next();

        Assert.Equal((UInt128)1000, id1);
        Assert.Equal((UInt128)2000, id2);
    }

    [Fact]
    public void Next_ShouldProduceMonotonicallyIncreasingValues()
    {
        var generator = new IdGenerator(GeneratorSettings.SmallId);
        UInt128 id = generator.Next();

        for (var i = 0; i < 100; i++)
        {
            UInt128 newId = generator.Next();
            Assert.True(newId > id, $"Generated ID should be greater than previous. Previous: {id}, New: {newId}");
            id = newId;
        }
    }

    [Fact]
    public void Next_ThreadSafety_ShouldProduceUniqueMonotonicIds()
    {
        const int threadCount = 10;
        const int idsPerThread = 100;

        var generator = new IdGenerator(GeneratorSettings.SmallId);
        var generatedIds = new ConcurrentBag<UInt128>();

        Parallel.For(0, threadCount, _ =>
        {
            for (var i = 0; i < idsPerThread; i++)
            {
                UInt128 id = generator.Next();
                generatedIds.Add(id);
            }
        });

        // All IDs should be unique
        Assert.Equal(threadCount * idsPerThread, generatedIds.Distinct().Count());

        // All IDs should be in monotonic order when sorted
        var sortedIds = generatedIds.OrderBy(x => x).ToList();
        for (var i = 1; i < sortedIds.Count; i++)
            Assert.True(sortedIds[i] > sortedIds[i - 1]);
    }

    [Fact]
    public void Next_WithFixedInputs_ShouldIncrementWhenNotMonotonic()
    {
        var settings = new GeneratorSettings(
            timeBits: 48,
            entropyBits: 16,
            getTimeElement: () => 1000,
            getEntropyElement: () => 1000,
            incrementFunction: () => 50, // Fixed increment
            getDateTimeFromId: t => DateTime.UnixEpoch.AddMilliseconds(t));

        var generator = new IdGenerator(settings);
        UInt128 previousId = generator.Next();

        for (var i = 0; i < 10; i++)
        {
            UInt128 newId = generator.Next();
            Assert.True(newId == previousId + 50, $"Generated ID should increase by 50 from previous. Previous: {previousId}, New: {newId}");
            previousId = newId;
        }
    }

    [Fact]
    public void Next_WithBackwardsTime_ShouldStillIncreaseMonotonically()
    {
        ulong currentTime = 2000;
        var settings = new GeneratorSettings(
            timeBits: 48,
            entropyBits: 16,
            getTimeElement: () => currentTime,
            getEntropyElement: () => 0,
            incrementFunction: () => 100,
            getDateTimeFromId: t => DateTime.UnixEpoch.AddMilliseconds(t));

        var generator = new IdGenerator(settings);
        UInt128 id1 = generator.Next();

        // Time goes backwards
        currentTime = 1000;
        UInt128 id2 = generator.Next();

        // Should still be greater despite backwards time
        Assert.True(id2 > id1);
    }

    [Fact]
    public void Next_WithSmallIdSettings_ShouldGenerateValidIds()
    {
        var generator = new IdGenerator(GeneratorSettings.SmallId);

        for (var i = 0; i < 1000; i++)
        {
            UInt128 id = generator.Next();
            Assert.True(id > 0);
        }
    }

    [Fact]
    public void Next_WithId80Settings_ShouldGenerateValidIds()
    {
        var generator = new IdGenerator(GeneratorSettings.Id80);

        for (var i = 0; i < 1000; i++)
        {
            UInt128 id = generator.Next();
            Assert.True(id > 0);
        }
    }

    [Fact]
    public void Next_WithId96Settings_ShouldGenerateValidIds()
    {
        var generator = new IdGenerator(GeneratorSettings.Id96);

        for (var i = 0; i < 1000; i++)
        {
            UInt128 id = generator.Next();
            Assert.True(id > 0);
        }
    }

    [Fact]
    public void Next_WithBigIdSettings_ShouldGenerateValidIds()
    {
        var generator = new IdGenerator(GeneratorSettings.BigId);

        for (var i = 0; i < 1000; i++)
        {
            UInt128 id = generator.Next();
            Assert.True(id > 0);
        }
    }

    [Fact]
    public void GetDateTime_ShouldExtractCorrectTime()
    {
        var knownTime = 123456UL;
        var settings = new GeneratorSettings(
            timeBits: 48,
            entropyBits: 16,
            getTimeElement: () => knownTime,
            getEntropyElement: () => 999,
            incrementFunction: () => 1,
            getDateTimeFromId: t => DateTime.UnixEpoch.AddMilliseconds(t));

        var generator = new IdGenerator(settings);
        UInt128 id = generator.Next();
        DateTime extractedTime = generator.GetDateTime(id);

        Assert.Equal(DateTime.UnixEpoch.AddMilliseconds(knownTime), extractedTime);
    }

    [Fact]
    public void Next_WithDeterministicSettings_ShouldBeReproducible()
    {
        var settings1 = new GeneratorSettings(
            timeBits: 48,
            entropyBits: 16,
            getTimeElement: () => 12345,
            getEntropyElement: () => 6789,
            incrementFunction: () => 1,
            getDateTimeFromId: t => DateTime.UnixEpoch.AddMilliseconds(t));

        var settings2 = new GeneratorSettings(
            timeBits: 48,
            entropyBits: 16,
            getTimeElement: () => 12345,
            getEntropyElement: () => 6789,
            incrementFunction: () => 1,
            getDateTimeFromId: t => DateTime.UnixEpoch.AddMilliseconds(t));

        var generator1 = new IdGenerator(settings1);
        var generator2 = new IdGenerator(settings2);

        UInt128 id1 = generator1.Next();
        UInt128 id2 = generator2.Next();

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Next_WithLargeValues_ShouldHandleUInt128Properly()
    {
        var settings = new GeneratorSettings(
            timeBits: 64,
            entropyBits: 64,
            getTimeElement: () => ulong.MaxValue,
            getEntropyElement: () => ulong.MaxValue >> 1, // Top bit clear
            incrementFunction: () => 1,
            getDateTimeFromId: t => DateTime.UnixEpoch.AddTicks((long)t));

        var generator = new IdGenerator(settings);
        UInt128 id = generator.Next();

        Assert.True(id > (UInt128)ulong.MaxValue);
    }

    [Fact]
    public void Next_HighThroughput_ShouldMaintainMonotonicity()
    {
        var generator = new IdGenerator(GeneratorSettings.SmallId);
        var ids = new UInt128[10000];

        for (var i = 0; i < ids.Length; i++)
            ids[i] = generator.Next();

        for (var i = 1; i < ids.Length; i++)
            Assert.True(ids[i] > ids[i - 1], $"ID at index {i} ({ids[i]}) should be greater than previous ({ids[i - 1]})");
    }

    [Fact]
    public void Next_ConcurrentHighThroughput_ShouldMaintainUniqueness()
    {
        var generator = new IdGenerator(GeneratorSettings.SmallId);
        var ids = new ConcurrentBag<UInt128>();

        Parallel.For(0, 100, _ =>
        {
            for (var i = 0; i < 100; i++)
                ids.Add(generator.Next());
        });

        Assert.Equal(10000, ids.Distinct().Count());
    }

    [Fact]
    public void GetDateTime_WithDifferentPresets_ShouldRoundTripCorrectly()
    {
        GeneratorSettings[] presets = new[]
        {
            GeneratorSettings.SmallId,
            GeneratorSettings.Id80,
            GeneratorSettings.Id96,
            GeneratorSettings.BigId
        };

        foreach (GeneratorSettings? preset in presets)
        {
            var generator = new IdGenerator(preset);
            UInt128 id = generator.Next();
            DateTime dateTime = generator.GetDateTime(id);

            // Should be close to current time
            DateTime now = DateTime.UtcNow;
            var difference = Math.Abs((now - dateTime).TotalSeconds);

            Assert.True(difference < 1, $"Extracted DateTime should be close to current time. Difference: {difference}s");
        }
    }
}
