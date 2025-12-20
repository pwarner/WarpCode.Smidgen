namespace WarpCode.Smidgen.Tests;

/// <summary>
/// Tests for range query support (GetMinId/GetMaxId methods).
/// </summary>
public class IdGeneratorRangeQueryTests
{
    [Fact]
    public void GetMinId_ShouldReturnMinimumForDateTime()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);

        UInt128 minId = generator.GetMinUInt128Id(testDate);

        // Min ID should have zero entropy
        UInt128 entropy = minId & ((UInt128.One << generator.EntropyBits) - 1);
        Assert.Equal(UInt128.Zero, entropy);
    }

    [Fact]
    public void GetMaxId_ShouldReturnMaximumForDateTime()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);

        UInt128 maxId = generator.GetMaxUInt128Id(testDate);

        // Max ID should have all-ones entropy
        UInt128 entropy = maxId & ((UInt128.One << generator.EntropyBits) - 1);
        UInt128 expectedEntropy = (UInt128.One << generator.EntropyBits) - 1;
        Assert.Equal(expectedEntropy, entropy);
    }

    [Fact]
    public void GetMinMaxId_ShouldReturnRange()
    {
        var generator = new IdGenerator();
        DateTime startDate = DateTime.UtcNow.AddHours(-2);
        DateTime endDate = DateTime.UtcNow.AddHours(-1);

        (UInt128 minId, UInt128 maxId) = generator.GetUInt128IdRange(startDate, endDate);

        Assert.True(maxId > minId);
    }

    [Fact]
    public void GetMinId_ExtractedDateTime_ShouldMatchInput()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);

        UInt128 minId = generator.GetMinUInt128Id(testDate);
        DateTime extracted = generator.ExtractDateTime(minId);

        // Should match within the precision of the time accuracy (milliseconds for SmallId)
        var difference = Math.Abs((testDate - extracted).TotalMilliseconds);
        Assert.True(difference < 1, $"Extracted time should match input. Difference: {difference}ms");
    }

    [Fact]
    public void GetMaxId_ExtractedDateTime_ShouldMatchInput()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);

        UInt128 maxId = generator.GetMaxUInt128Id(testDate);
        DateTime extracted = generator.ExtractDateTime(maxId);

        // Should match within the precision of the time accuracy
        var difference = Math.Abs((testDate - extracted).TotalMilliseconds);
        Assert.True(difference < 1, $"Extracted time should match input. Difference: {difference}ms");
    }

    [Fact]
    public void GetMinFormattedId_ShouldReturnFormattedString()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);
        // Use enough placeholders for Base32Size (13 for SmallId)
        var template = "ID-#############";

        var minFormatted = generator.GetMinFormattedId(testDate, template);

        Assert.StartsWith("ID-", minFormatted);
    }

    [Fact]
    public void GetMaxFormattedId_ShouldReturnFormattedString()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);
        var template = "ID-#############";

        var maxFormatted = generator.GetMaxFormattedId(testDate, template);

        Assert.StartsWith("ID-", maxFormatted);
    }

    [Fact]
    public void GetMinMaxFormattedId_ShouldReturnBothStrings()
    {
        var generator = new IdGenerator();
        DateTime startDate = DateTime.UtcNow.AddHours(-2);
        DateTime endDate = DateTime.UtcNow.AddHours(-1);
        var template = "ID-#############";

        (var minFormatted, var maxFormatted) = generator.GetFormattedIdRange(startDate, endDate, template);

        Assert.StartsWith("ID-", minFormatted);
        Assert.StartsWith("ID-", maxFormatted);
        Assert.NotEqual(minFormatted, maxFormatted);
    }

    [Fact]
    public void GetMinRawStringId_ShouldReturnRawString()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);

        var minRaw = generator.GetMinRawStringId(testDate);

        Assert.NotNull(minRaw);
        Assert.NotEmpty(minRaw);
    }

    [Fact]
    public void GetMaxRawStringId_ShouldReturnRawString()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);

        var maxRaw = generator.GetMaxRawStringId(testDate);

        Assert.NotNull(maxRaw);
        Assert.NotEmpty(maxRaw);
    }

    [Fact]
    public void GetMinMaxRawStringId_ShouldReturnBothStrings()
    {
        var generator = new IdGenerator();
        DateTime startDate = DateTime.UtcNow.AddHours(-2);
        DateTime endDate = DateTime.UtcNow.AddHours(-1);

        (var minRaw, var maxRaw) = generator.GetRawStringIdRange(startDate, endDate);

        Assert.NotNull(minRaw);
        Assert.NotNull(maxRaw);
        Assert.NotEqual(minRaw, maxRaw);
    }

    [Fact]
    public void RangeQuery_GeneratedIdShouldFallWithinRange()
    {
        var generator = new IdGenerator();

        // Generate an ID
        UInt128 id = generator.NextUInt128();
        DateTime idTime = generator.ExtractDateTime(id);

        // Get min/max for a range that includes this time
        DateTime startDate = idTime.AddMinutes(-1);
        DateTime endDate = idTime.AddMinutes(1);
        UInt128 minId = generator.GetMinUInt128Id(startDate);
        UInt128 maxId = generator.GetMaxUInt128Id(endDate);

        // The generated ID should fall within this range (inclusive)
        Assert.True(id >= minId, $"ID {id} should be >= min {minId}");
        Assert.True(id <= maxId, $"ID {id} should be <= max {maxId}");
    }

    [Fact]
    public void RangeQuery_MinMaxBoundsShouldBeInclusive()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);

        UInt128 minId = generator.GetMinUInt128Id(testDate);
        UInt128 maxId = generator.GetMaxUInt128Id(testDate);

        // Min should be less than max for the same timestamp
        Assert.True(minId < maxId);

        // Extract time from both - should be the same (within precision)
        DateTime minTime = generator.ExtractDateTime(minId);
        DateTime maxTime = generator.ExtractDateTime(maxId);

        var difference = Math.Abs((minTime - maxTime).TotalMilliseconds);
        Assert.True(difference < 1, "Min and max should have the same time component");
    }

    [Fact]
    public void GetMinId_WithFutureDate_ShouldWork()
    {
        var generator = new IdGenerator();
        DateTime futureDate = DateTime.UtcNow.AddYears(1);

        UInt128 minId = generator.GetMinUInt128Id(futureDate);

        Assert.True(minId > 0);
    }

    [Fact]
    public void GetMaxId_WithPastDate_ShouldWork()
    {
        var generator = new IdGenerator();
        DateTime pastDate = DateTime.UtcNow.AddYears(-1);

        UInt128 maxId = generator.GetMaxUInt128Id(pastDate);

        Assert.True(maxId > 0);
    }
}
