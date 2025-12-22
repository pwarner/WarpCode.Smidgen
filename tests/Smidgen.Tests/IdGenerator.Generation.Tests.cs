namespace WarpCode.Smidgen.Tests;

/// <summary>
/// Tests for core ID generation functionality.
/// Uses both deterministic (internal constructor) and non-deterministic (public API) approaches.
/// </summary>
public class IdGeneratorGenerationTests
{
    #region Deterministic Tests (Internal Constructor)

    [Fact]
    public void Next_ShouldIncludeTimeInMostSignificantBits()
    {
        ulong currentTime = 1000;
        var generator = new IdGenerator(
            options => options.WithTimeAccuracy(TimeAccuracy.Milliseconds).WithEntropySize(EntropySize.Bits16),
            getTimeElement: () => currentTime++,
            getEntropyElement: () => 0,
            increment: () => 0);

        UInt128 id1 = generator.NextUInt128();
        UInt128 id2 = generator.NextUInt128();

        // Extract time component by shifting right to remove entropy bits
        UInt128 time1 = id1 >> generator.EntropyBits;
        UInt128 time2 = id2 >> generator.EntropyBits;

        Assert.Equal((UInt128)1000, time1);
        Assert.Equal((UInt128)1001, time2);
    }

    [Fact]
    public void Next_ShouldIncludeEntropyInLeastSignificantBits()
    {
        ulong randomValue = 0;
        var generator = new IdGenerator(
            options => options.WithTimeAccuracy(TimeAccuracy.Milliseconds).WithEntropySize(EntropySize.Bits16),
            getTimeElement: () => 0,
            getEntropyElement: () => randomValue += 1000,
            increment: () => 0);

        UInt128 id1 = generator.NextUInt128();
        UInt128 id2 = generator.NextUInt128();

        // With time=0, the ID is just the entropy component
        Assert.Equal((UInt128)1000, id1);
        Assert.Equal((UInt128)2000, id2);
    }

    [Fact]
    public void Next_WithBackwardsTime_ShouldStillIncreaseMonotonically()
    {
        ulong currentTime = 2000;
        var generator = new IdGenerator(
            options => options.WithTimeAccuracy(TimeAccuracy.Milliseconds).WithEntropySize(EntropySize.Bits16),
            getTimeElement: () => currentTime,
            getEntropyElement: null, increment: null);

        UInt128 id1 = generator.NextUInt128();

        // Time goes backwards
        currentTime = 1000;
        UInt128 id2 = generator.NextUInt128();

        // Should still be greater despite backwards time
        Assert.True(id2 > id1, $"ID should be monotonic even with backwards time. Previous: {id1}, New: {id2}");
    }

    [Fact]
    public void Next_WithDeterministicSettings_ShouldBeReproducible()
    {
        var generator1 = new IdGenerator(
            options => options.WithTimeAccuracy(TimeAccuracy.Milliseconds).WithEntropySize(EntropySize.Bits16),
            getTimeElement: () => 12345,
            getEntropyElement: () => 6789,
            increment: null);

        var generator2 = new IdGenerator(
            options => options.WithTimeAccuracy(TimeAccuracy.Milliseconds).WithEntropySize(EntropySize.Bits16),
            getTimeElement: () => 12345,
            getEntropyElement: () => 6789,
            increment: null);

        UInt128 id1 = generator1.NextUInt128();
        UInt128 id2 = generator2.NextUInt128();

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Next_WithLargeValues_ShouldHandleUInt128Properly()
    {
        var generator = new IdGenerator(
            options => options.WithTimeAccuracy(TimeAccuracy.Ticks).WithEntropySize(EntropySize.Bits64),
            getTimeElement: () => ulong.MaxValue,
            getEntropyElement: () => ulong.MaxValue >> 1, // Top bit clear
            null);

        UInt128 id = generator.NextUInt128();

        Assert.True(id > (UInt128)ulong.MaxValue);
    }

    #endregion

    #region Non-Deterministic Tests (Public API)

    [Fact]
    public void NextUInt128_ShouldGenerateMonotonicallyIncreasingValues()
    {
        var generator = new IdGenerator();
        UInt128 id1 = generator.NextUInt128();
        UInt128 id2 = generator.NextUInt128();
        UInt128 id3 = generator.NextUInt128();

        Assert.True(id2 > id1);
        Assert.True(id3 > id2);
    }

    [Fact]
    public void NextUInt128_ShouldProduceMonotonicallyIncreasingValues()
    {
        var generator = new IdGenerator();
        UInt128 id = generator.NextUInt128();

        for (var i = 0; i < 1000; i++)
        {
            UInt128 newId = generator.NextUInt128();
            Assert.True(newId > id, $"Generated ID should be greater than previous. Previous: {id}, New: {newId}");
            id = newId;
        }
    }

    [Fact]
    public void NextUInt128_ShouldGenerateNonZeroValues()
    {
        var generator = new IdGenerator();

        for (var i = 0; i < 100; i++)
        {
            UInt128 id = generator.NextUInt128();
            Assert.True(id > 0);
        }
    }

    [Theory]
    [InlineData(GeneratorPreset.SmallId)]
    [InlineData(GeneratorPreset.Id80)]
    [InlineData(GeneratorPreset.Id96)]
    [InlineData(GeneratorPreset.BigId)]
    public void NextUInt128_WithPreset_ShouldGenerateValidIds(GeneratorPreset preset)
    {
        var generator = new IdGenerator(options => options.UsePreset(preset));

        for (var i = 0; i < 100; i++)
        {
            UInt128 id = generator.NextUInt128();
            Assert.True(id > 0);
        }
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Constructor_WithDefaultOptions_ShouldUseSmallIdPreset()
    {
        var generator = new IdGenerator();

        Assert.Equal(64, generator.TotalBits);
        Assert.Equal(48, generator.TimeBits);
        Assert.Equal(16, generator.EntropyBits);
        Assert.Equal(13, generator.Base32Size); // (64 + 4) / 5 = 13
    }

    [Fact]
    public void Constructor_WithSmallIdPreset_ShouldConfigureCorrectly()
    {
        var generator = new IdGenerator(options => options.UsePreset(GeneratorPreset.SmallId));

        Assert.Equal(64, generator.TotalBits);
        Assert.Equal(48, generator.TimeBits);
        Assert.Equal(16, generator.EntropyBits);
    }

    [Fact]
    public void Constructor_WithId80Preset_ShouldConfigureCorrectly()
    {
        var generator = new IdGenerator(options => options.UsePreset(GeneratorPreset.Id80));

        Assert.Equal(80, generator.TotalBits);
        Assert.Equal(48, generator.TimeBits);
        Assert.Equal(32, generator.EntropyBits);
    }

    [Fact]
    public void Constructor_WithId96Preset_ShouldConfigureCorrectly()
    {
        var generator = new IdGenerator(options => options.UsePreset(GeneratorPreset.Id96));

        // Id96 uses Microseconds accuracy and 40-bit entropy
        // TimeBits will be calculated based on the date range
        Assert.Equal(40, generator.EntropyBits);
        Assert.True(generator.TimeBits >= 32, $"TimeBits should be at least 32, but was {generator.TimeBits}");
        Assert.True(generator.TotalBits is >= 72 and <= 128, $"TotalBits should be reasonable, but was {generator.TotalBits}");
    }

    [Fact]
    public void Constructor_WithBigIdPreset_ShouldConfigureCorrectly()
    {
        var generator = new IdGenerator(options => options.UsePreset(GeneratorPreset.BigId));

        // BigId uses Ticks accuracy and 64-bit entropy
        // TimeBits will be calculated based on the date range (Unix epoch to DateTime.MaxValue)
        // The actual time bits will be less than 64 because we don't need the full range
        Assert.Equal(64, generator.EntropyBits);
        Assert.True(generator.TimeBits >= 32, $"TimeBits should be at least 32, but was {generator.TimeBits}");
        Assert.True(generator.TotalBits <= 128, $"TotalBits should not exceed 128, but was {generator.TotalBits}");
    }

    [Fact]
    public void Constructor_WithCustomConfiguration_ShouldRespectSettings()
    {
        var generator = new IdGenerator(options => options
            .WithTimeAccuracy(TimeAccuracy.Microseconds)
            .WithEntropySize(EntropySize.Bits24));

        Assert.Equal(24, generator.EntropyBits);
        // TimeBits will be calculated based on Unix epoch to DateTime.MaxValue range
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldUseDefaults()
    {
        var generator = new IdGenerator(null);

        // Should use SmallId preset defaults
        Assert.Equal(64, generator.TotalBits);
        Assert.Equal(16, generator.EntropyBits);
    }

    [Fact]
    public void Constructor_WithCustomEpochInFuture_ShouldThrow() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        new IdGenerator(options => options.Since(DateTime.UtcNow.AddYears(1))));

    [Fact]
    public void Constructor_WithUntilInPast_ShouldThrow() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        new IdGenerator(options => options.Until(DateTime.UtcNow.AddYears(-1))));

    [Fact]
    public void NextUInt128_WithMinimalEntropyBits_ShouldWork()
    {
        var generator = new IdGenerator(options => options.WithEntropySize(EntropySize.Bits16));

        var ids = new HashSet<UInt128>();
        for (var i = 0; i < 100; i++)
        {
            ids.Add(generator.NextUInt128());
        }

        Assert.Equal(100, ids.Count);
    }

    [Fact]
    public void NextUInt128_WithMaximalEntropyBits_ShouldWork()
    {
        var generator = new IdGenerator(options => options.WithEntropySize(EntropySize.Bits64));

        var ids = new HashSet<UInt128>();
        for (var i = 0; i < 100; i++)
        {
            ids.Add(generator.NextUInt128());
        }

        Assert.Equal(100, ids.Count);
    }

    [Fact]
    public void Base32Size_ShouldBeCorrectForAllPresets()
    {
        (GeneratorPreset, int)[] presets = new[]
        {
            (GeneratorPreset.SmallId, 13),  // (64 + 4) / 5 = 13
            (GeneratorPreset.Id80, 16),     // (80 + 4) / 5 = 16
            (GeneratorPreset.Id96, 20),     // (96 + 4) / 5 = 20
            (GeneratorPreset.BigId, 26)     // (128 + 4) / 5 = 26 (approximate)
        };

        foreach ((GeneratorPreset preset, var expectedMinSize) in presets)
        {
            var generator = new IdGenerator(options => options.UsePreset(preset));

            // Base32Size should be at least the expected size
            Assert.True(generator.Base32Size >= expectedMinSize - 1 && generator.Base32Size <= expectedMinSize + 1,
                $"Base32Size for {preset} was {generator.Base32Size}, expected around {expectedMinSize}");
        }
    }

    #endregion
}
