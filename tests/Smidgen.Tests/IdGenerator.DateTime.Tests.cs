namespace WarpCode.Smidgen.Tests;

/// <summary>
/// Tests for DateTime extraction functionality from 60-bit IDs.
/// </summary>
public class IdGeneratorDateTimeTests
{
    [Fact]
    public void ExtractDateTime_WithDeterministicTime_ShouldExtractCorrectly()
    {
        var knownTime = new DateTime(2020, 6, 15, 10, 30, 45, DateTimeKind.Utc);
        var timeProvider = new FakeTimeProvider(knownTime);
        var entropyProvider = new FakeEntropyProvider(fixedEntropy: 12345);

        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        var id = generator.NextId();
        DateTime extractedTime = generator.ExtractDateTime(id);

        // Due to interval precision (~6.5ms), extracted time should be very close
        var difference = (knownTime - extractedTime).TotalMilliseconds;

        // Should be within one interval (6.5536 ms)
        Assert.True(difference is >= 0 and < 7, $"Extracted time should be close to known time. Difference: {difference}ms");
    }

    [Fact]
    public void ExtractDateTime_WithCustomEpoch_ShouldUseCorrectBase()
    {
        var customEpoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var currentTime = new DateTime(2020, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        var timeProvider = new FakeTimeProvider(currentTime);
        var entropyProvider = new FakeEntropyProvider();

        var generator = new IdGenerator
        {
            Epoch = customEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        var id = generator.NextId();
        DateTime extracted = generator.ExtractDateTime(id);

        // Extracted time should be close to current time
        var differenceMs = (currentTime - extracted).TotalMilliseconds;
        Assert.True(differenceMs is >= 0 and < 7, $"Extracted time should match current time. Difference: {differenceMs}ms");
    }

    [Fact]
    public void ExtractDateTime_RoundTrip_ShouldBeConsistent()
    {
        var timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        byte[] entropyBytes = [0x12, 0x34, 0x56];
        var entropyProvider = new FakeEntropyProvider(entropyBytes);
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        var id1 = generator.NextId();
        DateTime time1 = generator.ExtractDateTime(id1);

        // Extract from raw string format (generate new ID to get the string)
        var rawString = generator.NextString();
        DateTime time2 = generator.ExtractDateTime(rawString);

        // Extract from formatted string
        var template = "PRE-############-SUF";
        var formatted = generator.NextString(template);
        DateTime time3 = generator.ExtractDateTime(formatted, template);

        // All extractions should produce the same time (from same deterministic time)
        Assert.Equal(time1, time2);
        Assert.Equal(time1, time3);
    }

    [Fact]
    public void ExtractDateTime_FromZeroId_ReturnsEpoch()
    {
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch
        };

        // ID of 0 should extract to the epoch
        DateTime extracted = generator.ExtractDateTime(0L);

        Assert.Equal(DateTime.UnixEpoch, extracted);
    }

    [Fact]
    public void ExtractDateTime_PreservesTimeOrdering()
    {
        var timeProvider = new FakeTimeProvider(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var entropyProvider = new FakeEntropyProvider();

        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        var id1 = generator.NextId();
        DateTime time1 = generator.ExtractDateTime(id1);

        // Advance time by 1 second
        timeProvider.Increment(TimeSpan.FromSeconds(1));
        var id2 = generator.NextId();
        DateTime time2 = generator.ExtractDateTime(id2);

        Assert.True(time2 > time1, $"Later ID should have later timestamp. time1={time1}, time2={time2}");
        Assert.True((time2 - time1).TotalSeconds is >= 0.9 and <= 1.1,
            $"Time difference should be ~1 second, got {(time2 - time1).TotalSeconds}s");
    }

    [Fact]
    public void ExtractDateTime_WithIntervalPrecision_IsAccurate()
    {
        // Test that extraction reverses the interval calculation correctly
        var knownTime = new DateTime(2020, 3, 15, 14, 25, 33, 500, DateTimeKind.Utc);
        var timeProvider = new FakeTimeProvider(knownTime);
        var entropyProvider = new FakeEntropyProvider();

        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        var id = generator.NextId();
        DateTime extracted = generator.ExtractDateTime(id);

        // Due to interval precision, we lose some precision
        // The interval is (ticks << 4), so precision is about 6.5ms
        var differenceMs = (knownTime - extracted).TotalMilliseconds;

        Assert.True(differenceMs is >= 0 and < 7, $"Should be within one interval (~6.5ms), got {differenceMs}ms");
    }
}
