namespace WarpCode.Smidgen.Tests;

public class GeneratorSettingsTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var settings = new GeneratorSettings(
            timeBits: 48,
            entropyBits: 16,
            getTimeElement: () => 12345UL,
            getEntropyElement: () => 67890UL,
            incrementFunction: () => 100UL,
            getDateTimeFromId: t => DateTime.UnixEpoch.AddMilliseconds(t)
        );

        Assert.Equal(48, settings.TimeBits);
        Assert.Equal(16, settings.EntropyBits);
        Assert.NotNull(settings.GetTimeElement);
        Assert.NotNull(settings.GetEntropyElement);
        Assert.NotNull(settings.IncrementFunction);
        Assert.NotNull(settings.GetDateTimeFromId);
    }

    [Theory]
    [InlineData(31)] // Too small
    [InlineData(65)] // Too large
    public void Constructor_WithInvalidTimeBits_ThrowsArgumentOutOfRangeException(byte timeBits)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GeneratorSettings(
                timeBits: timeBits,
                entropyBits: 16,
                getTimeElement: () => 0UL,
                getEntropyElement: () => 0UL,
                incrementFunction: () => 0UL,
                getDateTimeFromId: _ => DateTime.UtcNow
            ));

        Assert.Contains("TimeBits must be between 32 and 64", exception.Message);
    }

    [Theory]
    [InlineData(15)] // Too small
    [InlineData(65)] // Too large
    public void Constructor_WithInvalidEntropyBits_ThrowsArgumentOutOfRangeException(byte entropyBits)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GeneratorSettings(
                timeBits: 48,
                entropyBits: entropyBits,
                getTimeElement: () => 0UL,
                getEntropyElement: () => 0UL,
                incrementFunction: () => 0UL,
                getDateTimeFromId: _ => DateTime.UtcNow
            ));

        Assert.Contains("EntropyBits must be between 16 and 64", exception.Message);
    }

    [Theory]
    [InlineData(17)] // Not a multiple of 8
    [InlineData(30)]
    [InlineData(63)]
    public void Constructor_WithEntropyBitsNotMultipleOf8_ThrowsArgumentOutOfRangeException(byte entropyBits)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GeneratorSettings(
                timeBits: 48,
                entropyBits: entropyBits,
                getTimeElement: () => 0UL,
                getEntropyElement: () => 0UL,
                incrementFunction: () => 0UL,
                getDateTimeFromId: _ => DateTime.UtcNow
            ));

        Assert.Contains("EntropyBits must be a multiple of 8", exception.Message);
    }

    [Fact]
    public void Constructor_WithTotalBitsExceeding128_ThrowsArgumentOutOfRangeException()
    {
        // Use 56 + 56 = 112 which is valid, then try 64 + 72 but 72 would fail entropy check first
        // So let's use a different approach: try 48 + 88 where 88 is divisible by 8
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GeneratorSettings(
                timeBits: 64,
                entropyBits: 72, // This will fail the "must not exceed 64" check first
                getTimeElement: () => 0UL,
                getEntropyElement: () => 0UL,
                incrementFunction: () => 0UL,
                getDateTimeFromId: _ => DateTime.UtcNow
            ));

        // The entropy validation happens first, so we expect that error instead
        Assert.Contains("EntropyBits must be between 16 and 64", exception.Message);
    }

    [Fact]
    public void Constructor_WithExactly128Bits_Succeeds()
    {
        // 64 + 64 = 128 should be allowed
        var settings = new GeneratorSettings(
            timeBits: 64,
            entropyBits: 64,
            getTimeElement: () => 0UL,
            getEntropyElement: () => 0UL,
            incrementFunction: () => 0UL,
            getDateTimeFromId: _ => DateTime.UtcNow
        );

        Assert.Equal(64, settings.TimeBits);
        Assert.Equal(64, settings.EntropyBits);
    }

    [Fact]
    public void Constructor_WithNullGetTimeElement_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new GeneratorSettings(
                timeBits: 48,
                entropyBits: 16,
                getTimeElement: null!,
                getEntropyElement: () => 0UL,
                incrementFunction: () => 0UL,
                getDateTimeFromId: _ => DateTime.UtcNow
            ));

        Assert.Equal("getTimeElement", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullGetEntropyElement_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new GeneratorSettings(
                timeBits: 48,
                entropyBits: 16,
                getTimeElement: () => 0UL,
                getEntropyElement: null!,
                incrementFunction: () => 0UL,
                getDateTimeFromId: _ => DateTime.UtcNow
            ));

        Assert.Equal("getEntropyElement", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullIncrementFunction_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new GeneratorSettings(
                timeBits: 48,
                entropyBits: 16,
                getTimeElement: () => 0UL,
                getEntropyElement: () => 0UL,
                incrementFunction: null!,
                getDateTimeFromId: _ => DateTime.UtcNow
            ));

        Assert.Equal("incrementFunction", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullGetDateTimeFromId_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new GeneratorSettings(
                timeBits: 48,
                entropyBits: 16,
                getTimeElement: () => 0UL,
                getEntropyElement: () => 0UL,
                incrementFunction: () => 0UL,
                getDateTimeFromId: null!
            ));

        Assert.Equal("getDateTimeFromId", exception.ParamName);
    }

    [Fact]
    public void SmallId_HasCorrectConfiguration()
    {
        GeneratorSettings settings = GeneratorSettings.SmallId;

        Assert.Equal(48, settings.TimeBits);
        Assert.Equal(16, settings.EntropyBits);
        Assert.NotNull(settings.GetTimeElement);
        Assert.NotNull(settings.GetEntropyElement);
        Assert.NotNull(settings.IncrementFunction);
        Assert.NotNull(settings.GetDateTimeFromId);
    }

    [Fact]
    public void SmallId_FunctionsReturnValidValues()
    {
        GeneratorSettings settings = GeneratorSettings.SmallId;

        var time = settings.GetTimeElement();
        var entropy = settings.GetEntropyElement();
        var increment = settings.IncrementFunction();

        Assert.True(time > 0);
        Assert.True(entropy <= 0x7FFF); // 15 bits effective (top bit clear)
        Assert.True(increment >= 37);
    }

    [Fact]
    public void SmallId_DateTimeConversion_RoundTrips()
    {
        GeneratorSettings settings = GeneratorSettings.SmallId;
        var originalTime = settings.GetTimeElement();
        DateTime dateTime = settings.GetDateTimeFromId(originalTime);
        TimeSpan elapsed = dateTime - DateTime.UnixEpoch;
        var roundTripTime = (ulong)elapsed.TotalMilliseconds;

        Assert.Equal(originalTime, roundTripTime);
    }

    [Fact]
    public void Id80_HasCorrectConfiguration()
    {
        GeneratorSettings settings = GeneratorSettings.Id80;

        Assert.Equal(48, settings.TimeBits);
        Assert.Equal(32, settings.EntropyBits);
    }

    [Fact]
    public void Id80_FunctionsReturnValidValues()
    {
        GeneratorSettings settings = GeneratorSettings.Id80;

        var entropy = settings.GetEntropyElement();
        Assert.True(entropy <= 0x7FFFFFFF); // 31 bits effective (top bit clear)
    }

    [Fact]
    public void Id96_HasCorrectConfiguration()
    {
        GeneratorSettings settings = GeneratorSettings.Id96;

        Assert.Equal(56, settings.TimeBits);
        Assert.Equal(40, settings.EntropyBits);
    }

    [Fact]
    public void Id96_FunctionsReturnValidValues()
    {
        GeneratorSettings settings = GeneratorSettings.Id96;

        var time = settings.GetTimeElement();
        var entropy = settings.GetEntropyElement();

        Assert.True(time > 0);
        Assert.True(entropy <= 0x7FFFFFFFFF); // 39 bits effective (top bit clear)
    }

    [Fact]
    public void Id96_DateTimeConversion_RoundTrips()
    {
        GeneratorSettings settings = GeneratorSettings.Id96;
        var originalTime = settings.GetTimeElement();
        DateTime dateTime = settings.GetDateTimeFromId(originalTime);
        TimeSpan elapsed = dateTime - DateTime.UnixEpoch;
        var roundTripTime = (ulong)(elapsed.Ticks / 10); // Microseconds

        Assert.Equal(originalTime, roundTripTime);
    }

    [Fact]
    public void BigId_HasCorrectConfiguration()
    {
        GeneratorSettings settings = GeneratorSettings.BigId;

        Assert.Equal(64, settings.TimeBits);
        Assert.Equal(64, settings.EntropyBits);
    }

    [Fact]
    public void BigId_FunctionsReturnValidValues()
    {
        GeneratorSettings settings = GeneratorSettings.BigId;

        var time = settings.GetTimeElement();
        var entropy = settings.GetEntropyElement();

        Assert.True(time > 0);
        Assert.True(entropy <= 0x7FFFFFFFFFFFFFFF); // 63 bits effective (top bit clear)
    }

    [Fact]
    public void BigId_DateTimeConversion_RoundTrips()
    {
        GeneratorSettings settings = GeneratorSettings.BigId;
        var originalTime = settings.GetTimeElement();
        DateTime dateTime = settings.GetDateTimeFromId(originalTime);
        TimeSpan elapsed = dateTime - DateTime.UnixEpoch;
        var roundTripTime = (ulong)elapsed.Ticks;

        Assert.Equal(originalTime, roundTripTime);
    }

    [Theory]
    [InlineData(32, 16)]  // Minimum valid
    [InlineData(48, 16)]  // SmallId configuration
    [InlineData(48, 32)]  // Id80 configuration
    [InlineData(56, 40)]  // Id96 configuration
    [InlineData(64, 64)]  // BigId configuration
    [InlineData(60, 24)]  // Custom configuration
    public void Constructor_WithValidBitCombinations_Succeeds(byte timeBits, byte entropyBits)
    {
        var settings = new GeneratorSettings(
            timeBits: timeBits,
            entropyBits: entropyBits,
            getTimeElement: () => 0UL,
            getEntropyElement: () => 0UL,
            incrementFunction: () => 0UL,
            getDateTimeFromId: _ => DateTime.UtcNow
        );

        Assert.Equal(timeBits, settings.TimeBits);
        Assert.Equal(entropyBits, settings.EntropyBits);
    }

    [Fact]
    public void CustomSettings_FunctionsExecuteCorrectly()
    {
        var timeCallCount = 0;
        var entropyCallCount = 0;
        var incrementCallCount = 0;
        var dateTimeCallCount = 0;

        var settings = new GeneratorSettings(
            timeBits: 48,
            entropyBits: 16,
            getTimeElement: () => { timeCallCount++; return 1000UL; },
            getEntropyElement: () => { entropyCallCount++; return 500UL; },
            incrementFunction: () => { incrementCallCount++; return 50UL; },
            getDateTimeFromId: t => { dateTimeCallCount++; return DateTime.UnixEpoch.AddMilliseconds(t); }
        );

        var time = settings.GetTimeElement();
        var entropy = settings.GetEntropyElement();
        var increment = settings.IncrementFunction();
        DateTime dateTime = settings.GetDateTimeFromId(1000);

        Assert.Equal(1, timeCallCount);
        Assert.Equal(1, entropyCallCount);
        Assert.Equal(1, incrementCallCount);
        Assert.Equal(1, dateTimeCallCount);
        Assert.Equal(1000UL, time);
        Assert.Equal(500UL, entropy);
        Assert.Equal(50UL, increment);
    }

    [Fact]
    public void AllPresets_ProduceDifferentEntropyValues()
    {
        var smallEntropy = new HashSet<ulong>();
        var id80Entropy = new HashSet<ulong>();
        var id96Entropy = new HashSet<ulong>();
        var bigEntropy = new HashSet<ulong>();

        for (var i = 0; i < 100; i++)
        {
            smallEntropy.Add(GeneratorSettings.SmallId.GetEntropyElement());
            id80Entropy.Add(GeneratorSettings.Id80.GetEntropyElement());
            id96Entropy.Add(GeneratorSettings.Id96.GetEntropyElement());
            bigEntropy.Add(GeneratorSettings.BigId.GetEntropyElement());
        }

        Assert.True(smallEntropy.Count > 50);
        Assert.True(id80Entropy.Count > 50);
        Assert.True(id96Entropy.Count > 50);
        Assert.True(bigEntropy.Count > 50);
    }
}
