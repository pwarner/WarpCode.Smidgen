namespace WarpCode.Smidgen.Tests;

/// <summary>
/// Tests for range query support (GetMinId/GetMaxId methods).
/// </summary>
public class IdGeneratorRangeQueryTests
{
    [Fact]
    public void GetMinId_ShouldReturnMinimumIdForDate()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);

        long minId = generator.GetMinId(testDate);

        Assert.True(minId > 0);

        // Min ID should have zero entropy (lower 20 bits)
        long entropy = minId & 0xFFFFF;
        Assert.Equal(0, entropy);
    }

    [Fact]
    public void GetMaxId_ShouldReturnMaximumIdForDate()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);

        long maxId = generator.GetMaxId(testDate);

        Assert.True(maxId > 0);

        // Max ID should have all-ones entropy (lower 20 bits)
        long entropy = maxId & 0xFFFFF;
        long expectedEntropy = 0xFFFFF; // 20 bits of 1s
        Assert.Equal(expectedEntropy, entropy);
    }

    [Fact]
    public void GetIdRange_ShouldReturnRange()
    {
        var generator = new IdGenerator();
        DateTime startDate = DateTime.UtcNow.AddHours(-2);
        DateTime endDate = DateTime.UtcNow.AddHours(-1);

        (long minId, long maxId) = generator.GetIdRange(startDate, endDate);

        Assert.True(maxId > minId);
    }

    [Fact]
    public void GetMinId_ExtractedDateTime_ShouldMatchInput()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);

        long minId = generator.GetMinId(testDate);
        DateTime extracted = generator.ExtractDateTime(minId);

        // Should match within the precision of the interval (~6.5ms)
        var difference = Math.Abs((testDate - extracted).TotalMilliseconds);
        Assert.True(difference < 7, $"Extracted time should match input within one interval. Difference: {difference}ms");
    }

    [Fact]
    public void GetMaxId_ExtractedDateTime_ShouldMatchInput()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);

        long maxId = generator.GetMaxId(testDate);
        DateTime extracted = generator.ExtractDateTime(maxId);

        // Should match within the precision of the interval (~6.5ms)
        var difference = Math.Abs((testDate - extracted).TotalMilliseconds);
        Assert.True(difference < 7, $"Extracted time should match input within one interval. Difference: {difference}ms");
    }

    [Fact]
    public void GetMinFormattedString_ShouldReturnFormattedString()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);
        var template = "ID-############";

        var minFormatted = generator.GetMinFormattedString(testDate, template);

        Assert.StartsWith("ID-", minFormatted);
    }

    [Fact]
    public void GetMaxFormattedString_ShouldReturnFormattedString()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);
        var template = "ID-############";

        var maxFormatted = generator.GetMaxFormattedString(testDate, template);

        Assert.StartsWith("ID-", maxFormatted);
    }

    [Fact]
    public void GetFormattedStringRange_ShouldReturnBothStrings()
    {
        var generator = new IdGenerator();
        DateTime startDate = DateTime.UtcNow.AddHours(-2);
        DateTime endDate = DateTime.UtcNow.AddHours(-1);
        var template = "ID-############";

        (var minFormatted, var maxFormatted) = generator.GetFormattedStringRange(startDate, endDate, template);

        Assert.StartsWith("ID-", minFormatted);
        Assert.StartsWith("ID-", maxFormatted);
        Assert.NotEqual(minFormatted, maxFormatted);
    }

    [Fact]
    public void GetMinRawString_ShouldReturnRawString()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);

        var minRaw = generator.GetMinRawString(testDate);

        Assert.NotNull(minRaw);
        Assert.NotEmpty(minRaw);
        Assert.Matches(@"^[0-9A-Z]{4}-[0-9A-Z]{4}-[0-9A-Z]{4}$", minRaw);
    }

    [Fact]
    public void GetMaxRawString_ShouldReturnRawString()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);

        var maxRaw = generator.GetMaxRawString(testDate);

        Assert.NotNull(maxRaw);
        Assert.NotEmpty(maxRaw);
        Assert.Matches(@"^[0-9A-Z]{4}-[0-9A-Z]{4}-[0-9A-Z]{4}$", maxRaw);
    }

    [Fact]
    public void GetRawStringRange_ShouldReturnBothStrings()
    {
        var generator = new IdGenerator();
        DateTime startDate = DateTime.UtcNow.AddHours(-2);
        DateTime endDate = DateTime.UtcNow.AddHours(-1);

        (var minRaw, var maxRaw) = generator.GetRawStringRange(startDate, endDate);

        Assert.NotNull(minRaw);
        Assert.NotNull(maxRaw);
        Assert.NotEqual(minRaw, maxRaw);
        Assert.Matches(@"^[0-9A-Z]{4}-[0-9A-Z]{4}-[0-9A-Z]{4}$", minRaw);
        Assert.Matches(@"^[0-9A-Z]{4}-[0-9A-Z]{4}-[0-9A-Z]{4}$", maxRaw);
    }

    [Fact]
    public void RangeQuery_GeneratedIdShouldFallWithinRange()
    {
        var generator = new IdGenerator();

        // Generate an ID
        long id = generator.NextId();
        DateTime idTime = generator.ExtractDateTime(id);

        // Get min/max for a range that includes this time
        DateTime startDate = idTime.AddMinutes(-1);
        DateTime endDate = idTime.AddMinutes(1);
        long minId = generator.GetMinId(startDate);
        long maxId = generator.GetMaxId(endDate);

        // The generated ID should fall within this range (inclusive)
        Assert.True(id >= minId, $"ID {id} should be >= min {minId}");
        Assert.True(id <= maxId, $"ID {id} should be <= max {maxId}");
    }

    [Fact]
    public void RangeQuery_MinMaxBoundsShouldBeInclusive()
    {
        var generator = new IdGenerator();
        DateTime testDate = DateTime.UtcNow.AddHours(-1);

        long minId = generator.GetMinId(testDate);
        long maxId = generator.GetMaxId(testDate);

        // Min should be less than max for the same timestamp
        Assert.True(minId < maxId);

        // Extract time from both - should be the same (within interval precision ~6.5ms)
        DateTime minTime = generator.ExtractDateTime(minId);
        DateTime maxTime = generator.ExtractDateTime(maxId);

        var difference = Math.Abs((minTime - maxTime).TotalMilliseconds);
        Assert.True(difference < 7, "Min and max should have the same time component (within one interval)");
    }

    [Fact]
    public void GetMinId_WithFutureDate_ShouldWork()
    {
        var generator = new IdGenerator();
        DateTime futureDate = DateTime.UtcNow.AddYears(1);

        long minId = generator.GetMinId(futureDate);

        Assert.True(minId > 0);
    }

    [Fact]
    public void GetMinId_WithDateBeforeEpoch_ClampsToEpoch()
    {
        var generator = new IdGenerator { Epoch = DateTime.UnixEpoch };
        var beforeEpoch = new DateTime(1960, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(0, generator.GetMinId(beforeEpoch));
    }

    [Fact]
    public void GetMaxId_WithDateBeforeEpoch_ClampsToEpoch()
    {
        var generator = new IdGenerator { Epoch = DateTime.UnixEpoch };
        var beforeEpoch = new DateTime(1960, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(0xFFFFF, generator.GetMaxId(beforeEpoch));
    }

    [Fact]
    public void GetMaxId_WithPastDate_ShouldWork()
    {
        var generator = new IdGenerator();
        DateTime pastDate = DateTime.UtcNow.AddYears(-1);

        long maxId = generator.GetMaxId(pastDate);

        Assert.True(maxId > 0);
    }

    [Fact]
    public void GetMinId_DifferentDatesShouldReturnDifferentIds()
    {
        var generator = new IdGenerator();
        DateTime date1 = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime date2 = new DateTime(2024, 1, 2, 12, 0, 0, DateTimeKind.Utc);

        long minId1 = generator.GetMinId(date1);
        long minId2 = generator.GetMinId(date2);

        Assert.NotEqual(minId1, minId2);
        Assert.True(minId2 > minId1, "Later date should produce larger minimum ID");
    }

    [Fact]
    public void GetMaxId_DifferentDatesShouldReturnDifferentIds()
    {
        var generator = new IdGenerator();
        DateTime date1 = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime date2 = new DateTime(2024, 1, 2, 12, 0, 0, DateTimeKind.Utc);

        long maxId1 = generator.GetMaxId(date1);
        long maxId2 = generator.GetMaxId(date2);

        Assert.NotEqual(maxId1, maxId2);
        Assert.True(maxId2 > maxId1, "Later date should produce larger maximum ID");
    }

    [Fact]
    public void RangeQuery_WithCustomEpoch_ShouldWork()
    {
        var customEpoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var generator = new IdGenerator { Epoch = customEpoch };

        DateTime testDate = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        long minId = generator.GetMinId(testDate);
        long maxId = generator.GetMaxId(testDate);

        Assert.True(minId > 0);
        Assert.True(maxId > minId);

        // Extracted times should match test date (within interval precision)
        DateTime extractedMin = generator.ExtractDateTime(minId);
        DateTime extractedMax = generator.ExtractDateTime(maxId);

        var minDiff = Math.Abs((extractedMin - testDate).TotalMilliseconds);
        var maxDiff = Math.Abs((extractedMax - testDate).TotalMilliseconds);

        Assert.True(minDiff < 7, "Min ID time should match test date within one interval");
        Assert.True(maxDiff < 7, "Max ID time should match test date within one interval");
    }
}
