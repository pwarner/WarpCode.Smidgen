namespace WarpCode.Smidgen.Tests;

/// <summary>
/// Tests for core 60-bit ID generation functionality.
/// Uses both deterministic (custom providers) and non-deterministic (default providers) approaches.
/// </summary>
public class IdGeneratorGenerationTests
{
    #region Deterministic Tests (Custom Providers)

    [Fact]
    public void NextId_WithFixedTimeAndEntropy_ProducesDeterministicResult()
    {
        var timeProvider1 = new FakeTimeProvider(new DateTime(2020, 1, 1, 12, 34, 56, DateTimeKind.Utc));
        var entropyProvider1 = new FakeEntropyProvider(fixedEntropy: 12345);
        var generator1 = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider1,
            EntropyProvider = entropyProvider1
        };

        var timeProvider2 = new FakeTimeProvider(new DateTime(2020, 1, 1, 12, 34, 56, DateTimeKind.Utc));
        var entropyProvider2 = new FakeEntropyProvider(fixedEntropy: 12345);
        var generator2 = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider2,
            EntropyProvider = entropyProvider2
        };

        long id1 = generator1.NextId();
        long id2 = generator2.NextId();

        Assert.Equal(id1, id2);
    }

    [Theory]
    [InlineData(0x12345, 0x12345)]        // Below MaxEntropy (793,575) - no adjustment
    [InlineData(0xFFFFF, 0xC1BE7)]        // Max 20-bit (1,048,575) - adjusted by Headroom (255,000) = 793,575
    [InlineData(0xC1BE8, 0x837D0)]        // Just above MaxEntropy (793,576) - adjusted to 538,576
    [InlineData(0x00000, 0x00000)]        // Zero entropy - no adjustment
    [InlineData(0xC1BE7, 0xC1BE7)]        // Exactly at MaxEntropy (793,575) - no adjustment
    public void NextId_WithKnownInputs_ProducesExpectedId(int inputEntropy, int expectedEntropyInId)
    {
        // Known inputs
        var knownTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var epoch = DateTime.UnixEpoch; // 1970-01-01

        var timeProvider = new FakeTimeProvider(knownTime);
        var entropyProvider = new FakeEntropyProvider(fixedEntropy: inputEntropy);
        var generator = new IdGenerator
        {
            Epoch = epoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        // Calculate expected ID based on the documented formula:
        // elapsedTicks = knownTime.Ticks - epoch.Ticks
        long elapsedTicks = knownTime.Ticks - epoch.Ticks;

        // interval = (elapsedTicks << 4) & 0x0FFFFFFFFFF00000L
        long expectedInterval = (elapsedTicks << 4) & 0x0FFFFFFFFFF00000L;

        // expectedId = interval | expectedEntropy
        long expectedId = expectedInterval | (long)expectedEntropyInId;

        // Generate actual ID
        long actualId = generator.NextId();

        // Assert exact match
        Assert.Equal(expectedId, actualId);
    }

    [Fact]
    public void NextId_WithDifferentTimes_ProducesDifferentIds()
    {
        var timeProvider = new FakeTimeProvider(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var entropyProvider = new FakeEntropyProvider(fixedEntropy: 0);
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        long id1 = generator.NextId();

        timeProvider.Increment(TimeSpan.FromMilliseconds(10));
        long id2 = generator.NextId();

        Assert.True(id2 > id1, $"ID should increase with time. id1={id1}, id2={id2}");
    }

    [Fact]
    public void NextId_WithBackwardsTime_StillIncreases()
    {
        var timeProvider = new FakeTimeProvider(new DateTime(2020, 1, 1, 0, 0, 1, DateTimeKind.Utc));
        var entropyProvider = new FakeEntropyProvider(fixedEntropy: 0);
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        long id1 = generator.NextId();

        // Time goes backwards
        timeProvider.SetTime(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        long id2 = generator.NextId();

        // Should still be greater despite backwards time (monotonicity guarantee)
        // With deterministic increment of 1, id2 should be exactly id1 + 1
        Assert.Equal(id1 + 1, id2);
    }

    #endregion

    #region Time Validation Tests

    [Fact]
    public void NextId_BeforeEpoch_ThrowsInvalidOperationException()
    {
        var timeProvider = new FakeTimeProvider(new DateTime(1960, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var entropyProvider = new FakeEntropyProvider();
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        var ex = Assert.Throws<InvalidOperationException>(() => generator.NextId());
        Assert.Contains("before epoch", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NextId_FarInFuture_ThrowsInvalidOperationException()
    {
        // More than 228 years from Unix epoch
        var timeProvider = new FakeTimeProvider(new DateTime(2300, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var entropyProvider = new FakeEntropyProvider();
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        var ex = Assert.Throws<InvalidOperationException>(() => generator.NextId());
        Assert.Contains("exceeds representable range", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Monotonicity Tests

    [Fact]
    public void NextId_SameTime_DifferentEntropy_StillIncreases()
    {
        var timeProvider = new FakeTimeProvider(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var entropyProvider = new FakeEntropyProvider(fixedEntropy: 1000);
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        long id1 = generator.NextId();
        long id2 = generator.NextId(); // Same time, should increment by 1

        // With deterministic increment of 1, id2 should be exactly id1 + 1
        Assert.Equal(id1 + 1, id2);
    }

    [Fact]
    public void NextId_SameInterval_IncrementsInsteadOfDrawingFreshEntropy()
    {
        var timeProvider = new FakeTimeProvider(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var entropyProvider = new FakeEntropyProvider(fixedEntropy: 1000) { Increments = [7, 3] };
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        long id1 = generator.NextId();
        entropyProvider.SetEntropy(500_000); // larger, but must not be used within the same interval
        long id2 = generator.NextId();
        long id3 = generator.NextId();

        Assert.Equal(1000, id1 & 0xFFFFF);
        Assert.Equal(id1 + 7, id2);
        Assert.Equal(id2 + 3, id3);
        Assert.Equal(1, entropyProvider.GetBytesCalls);
    }

    [Fact]
    public void NextId_NewInterval_StartsFromFreshEntropy()
    {
        var timeProvider = new FakeTimeProvider(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var entropyProvider = new FakeEntropyProvider(fixedEntropy: 500_000);
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        long id1 = generator.NextId();
        timeProvider.Increment(TimeSpan.FromMilliseconds(10)); // past the ~6.5 ms interval
        entropyProvider.SetEntropy(1000); // smaller than id1's entropy, but a new interval starts afresh
        long id2 = generator.NextId();

        Assert.True(id2 > id1);
        Assert.Equal(1000, id2 & 0xFFFFF);
        Assert.Equal(2, entropyProvider.GetBytesCalls);
    }

    [Fact]
    public void NextId_ZeroIncrement_IsSkipped()
    {
        var timeProvider = new FakeTimeProvider(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var entropyProvider = new FakeEntropyProvider(fixedEntropy: 1000) { Increments = [0, 0, 5] };
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        long id1 = generator.NextId();
        long id2 = generator.NextId();

        Assert.Equal(id1 + 5, id2);
    }

    [Fact]
    public void NextId_AtEpoch_FirstIdUsesFreshEntropy()
    {
        // The time component is zero at the epoch, so the first ID must still start from entropy rather than 0 + increment
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = new FakeTimeProvider(DateTime.UnixEpoch),
            EntropyProvider = new FakeEntropyProvider(fixedEntropy: 0)
        };

        Assert.Equal(0, generator.NextId());
        Assert.Equal(1, generator.NextId());
    }

    #endregion

    #region Non-Deterministic Tests (Public API)

    [Fact]
    public void NextId_GeneratesMonotonicallyIncreasingValues()
    {
        var generator = new IdGenerator();
        long previousId = generator.NextId();

        for (int i = 0; i < 1000; i++)
        {
            long currentId = generator.NextId();
            Assert.True(currentId > previousId, $"ID should be monotonically increasing. previous={previousId}, current={currentId}");
            previousId = currentId;
        }
    }

    [Fact]
    public void NextString_ReturnsValidBase32String()
    {
        var generator = new IdGenerator();

        string idString = generator.NextString();

        // Raw format is ####-####-####  (12 chars + 2 dashes = 14 total)
        Assert.Equal(14, idString.Length);
        Assert.Equal('-', idString[4]);
        Assert.Equal('-', idString[9]);
    }

    [Fact]
    public void NextString_GeneratesAlphanumericallyIncreasingValues()
    {
        var generator = new IdGenerator();
        string previousId = generator.NextString();

        for (int i = 0; i < 100; i++)
        {
            string currentId = generator.NextString();
            Assert.True(string.CompareOrdinal(currentId, previousId) > 0,
                $"String ID should be alphanumerically increasing. previous={previousId}, current={currentId}");
            previousId = currentId;
        }
    }

    [Fact]
    public void NextString_WithTemplate_UsesCorrectFormat()
    {
        var generator = new IdGenerator();

        string id = generator.NextString("ID-############");

        Assert.StartsWith("ID-", id);
        Assert.Equal(15, id.Length); // "ID-" + 12 chars
    }

    #endregion
}
