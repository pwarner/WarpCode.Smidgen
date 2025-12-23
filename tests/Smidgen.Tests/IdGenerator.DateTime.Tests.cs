namespace WarpCode.Smidgen.Tests;

/// <summary>
/// Tests for DateTime extraction functionality.
/// </summary>
public class IdGeneratorDateTimeTests
{
    [Fact]
    public void ExtractDateTime_FromUInt128_ShouldExtractTimeComponent()
    {
        var generator = new IdGenerator();
        UInt128 id = generator.NextUInt128();
        DateTime extractedTime = generator.ExtractDateTime(id);

        DateTime now = DateTime.UtcNow;
        var difference = Math.Abs((now - extractedTime).TotalSeconds);

        Assert.True(difference < 1, $"Extracted time should be close to now. Difference: {difference}s");
    }

    [Fact]
    public void ExtractDateTime_FromRawString_ShouldWork()
    {
        var generator = new IdGenerator();
        var rawId = generator.NextRawStringId();
        DateTime extractedTime = generator.ExtractDateTime(rawId);

        DateTime now = DateTime.UtcNow;
        var difference = Math.Abs((now - extractedTime).TotalSeconds);

        Assert.True(difference < 1);
    }

    [Fact]
    public void ExtractDateTime_FromFormattedString_ShouldWork()
    {
        var generator = new IdGenerator();
        // Use enough placeholders for Base32Size (13 for SmallId)
        var template = "ID-#############";
        var formattedId = generator.NextFormattedId(template);
        DateTime extractedTime = generator.ExtractDateTime(formattedId, template);

        DateTime now = DateTime.UtcNow;
        var difference = Math.Abs((now - extractedTime).TotalSeconds);

        Assert.True(difference < 1);
    }

    [Fact]
    public void ExtractDateTime_WithDeterministicGenerator_ShouldExtractCorrectTime()
    {
        var knownTime = 123456UL;
        DateTime since = DateTime.UnixEpoch;

        var generator = new IdGenerator(
            options => options
                .WithTimeAccuracy(TimeAccuracy.Milliseconds)
                .WithEntropySize(EntropySize.Bits16)
                .Since(since),
            getTimeElement: () => knownTime,
            getEntropyElement: () => 999,
            increment: ()=> 0);

        UInt128 id = generator.NextUInt128();
        DateTime extractedTime = generator.ExtractDateTime(id);

        Assert.Equal(since.AddMilliseconds(knownTime), extractedTime);
    }

    [Theory]
    [InlineData(GeneratorPreset.SmallId)]
    [InlineData(GeneratorPreset.Id80)]
    [InlineData(GeneratorPreset.Id96)]
    [InlineData(GeneratorPreset.BigId)]
    public void ExtractDateTime_WithDifferentPresets_ShouldRoundTripCorrectly(GeneratorPreset preset)
    {
        var generator = new IdGenerator(options => options.UsePreset(preset));
        UInt128 id = generator.NextUInt128();
        DateTime dateTime = generator.ExtractDateTime(id);

        // Should be close to current time
        DateTime now = DateTime.UtcNow;
        var difference = Math.Abs((now - dateTime).TotalSeconds);

        Assert.True(difference < 1, $"Extracted DateTime should be close to current time. Difference: {difference}s");
    }

    [Fact]
    public void ExtractDateTime_WithCustomEpoch_ShouldUseCorrectBase()
    {
        var customEpoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var generator = new IdGenerator(options => options.Since(customEpoch));

        UInt128 id = generator.NextUInt128();
        DateTime extracted = generator.ExtractDateTime(id);

        // Extracted time should be after custom epoch
        Assert.True(extracted >= customEpoch);
        Assert.True(extracted <= DateTime.UtcNow);
    }

    [Fact]
    public void ExtractDateTime_MultipleFormats_ShouldProduceSameResult()
    {
        var generator = new IdGenerator();
        UInt128 id = generator.NextUInt128();

        // Extract from UInt128
        DateTime fromUInt128 = generator.ExtractDateTime(id);

        // Convert to raw string and extract
        var rawString = generator.NextRawStringId();
        DateTime fromRawString = generator.ExtractDateTime(rawString);

        // Convert to formatted string and extract
        var template = "ID-#############";
        var formatted = IdFormatter.Format(id, template);
        UInt128 parsedId = IdGenerator.ParseFormattedId(formatted, template);
        DateTime fromFormatted = generator.ExtractDateTime(parsedId);

        // All should be within 1 millisecond of each other
        Assert.True(Math.Abs((fromUInt128 - fromRawString).TotalMilliseconds) < 1);
        Assert.True(Math.Abs((fromUInt128 - fromFormatted).TotalMilliseconds) < 1);
    }

    [Fact]
    public void ExtractDateTime_FromInvalidRawString_ShouldThrow()
    {
        var generator = new IdGenerator();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            generator.ExtractDateTime("INVALID!@#"));
    }
}
