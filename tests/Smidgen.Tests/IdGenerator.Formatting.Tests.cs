namespace WarpCode.Smidgen.Tests;

/// <summary>
/// Tests for ID formatting and parsing functionality with 60-bit IDs.
/// </summary>
public class IdGeneratorFormattingTests
{
    [Fact]
    public void NextString_ThenParseRawString_RoundTripsCorrectly()
    {
        var timeProvider = new FakeTimeProvider(new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc));
        var entropyProvider = new FakeEntropyProvider(fixedEntropy: 12345);
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        // Generate first ID
        long id1 = generator.NextId();

        // Generate string - internally calls NextId() which will increment by 1
        string rawString = generator.NextString();

        // Parse the string back
        long parsedId = IdGenerator.ParseRawString(rawString);

        // The parsed ID should be exactly id1 + 1
        Assert.Equal(id1 + 1, parsedId);
    }

    [Fact]
    public void NextString_WithTemplate_ThenParseFormattedString_RoundTripsCorrectly()
    {
        var timeProvider = new FakeTimeProvider(new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc));
        var entropyProvider = new FakeEntropyProvider(fixedEntropy: 54321);
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        string template = "ORD-############";

        // Generate first ID
        long id1 = generator.NextId();

        // Generate formatted string - internally calls NextId() which increments by 1
        string formattedString = generator.NextString(template);

        // Parse it back
        long parsedId = IdGenerator.ParseFormattedString(formattedString, template);

        // The parsed ID should be exactly id1 + 1
        Assert.Equal(id1 + 1, parsedId);
    }

    [Fact]
    public void ParseRawString_WithLowerCase_ParsesSameAsUpperCase()
    {
        var timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var entropyProvider = new FakeEntropyProvider(fixedEntropy: 99999);
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = timeProvider,
            EntropyProvider = entropyProvider
        };

        string upperCase = generator.NextString();
        string lowerCase = upperCase.ToLowerInvariant();

        long parsedUpper = IdGenerator.ParseRawString(upperCase);
        long parsedLower = IdGenerator.ParseRawString(lowerCase);

        Assert.Equal(parsedUpper, parsedLower);
    }

    [Fact]
    public void NextString_UsesBase32Encoding()
    {
        var generator = new IdGenerator();
        string id = generator.NextString();

        // All characters should be valid Crockford Base32 or dash
        string validChars = "0123456789ABCDEFGHJKMNPQRSTVWXYZ-";
        foreach (char c in id)
        {
            Assert.Contains(c, validChars);
        }
    }

    [Fact]
    public void NextString_WithCustomPlaceholder_Works()
    {
        var generator = new IdGenerator();
        string formatted = generator.NextString("ID-************", '*');

        Assert.StartsWith("ID-", formatted);
        Assert.DoesNotContain("*", formatted);
    }

    [Fact]
    public void NextString_WithInsufficientPlaceholders_Throws()
    {
        var generator = new IdGenerator();

        // Only 4 placeholders, need 12 for 60-bit ID
        var ex = Assert.Throws<FormatException>(() =>
            generator.NextString("ID-####"));

        Assert.Contains("missing", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseRawString_WithInvalidCharacters_Throws()
    {
        // Invalid characters with correct length (14 chars with dashes)
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IdGenerator.ParseRawString("!@#$-!@#$-!@#$"));
    }

    [Fact]
    public void ParseFormattedString_WithInvalidId_Throws()
    {
        Assert.Throws<FormatException>(() =>
            IdGenerator.ParseFormattedString("INVALID", "ID-############"));
    }

    [Fact]
    public void TryParseRawString_WithInvalidId_Fails()
    {
        bool success = IdGenerator.TryParseRawString("INVALID!@#", out long parsed);

        Assert.False(success);
        Assert.Equal(0, parsed);
    }

    [Fact]
    public void TryParseFormattedString_WithInvalidId_Fails()
    {
        bool success = IdGenerator.TryParseFormattedString(
            "INVALID", "ID-############", '#', out long parsed);

        Assert.False(success);
        Assert.Equal(0, parsed);
    }

    [Fact]
    public void ParseFormattedString_WithEmptyTemplate_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            IdGenerator.ParseFormattedString("123", ""));
    }

    [Fact]
    public void NextString_WithEmptyTemplate_Throws()
    {
        var generator = new IdGenerator();

        Assert.Throws<ArgumentException>(() => generator.NextString(""));
    }

    [Fact]
    public void NextString_WithMorePlaceholdersThanNeeded_PadsWithLeadingZeros()
    {
        // At the epoch the time component is zero, so the ID is just its entropy
        var generator = new IdGenerator
        {
            Epoch = DateTime.UnixEpoch,
            TimeProvider = new FakeTimeProvider(DateTime.UnixEpoch),
            EntropyProvider = new FakeEntropyProvider(fixedEntropy: 5)
        };

        string formatted = generator.NextString("ID-##############");

        Assert.Equal("ID-00000000000005", formatted);
    }

    [Fact]
    public void ParseFormattedString_WithMoreThan12Placeholders_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            IdGenerator.ParseFormattedString("0000000000001", "#############"));

        Assert.Contains("Maximum 12", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseFormattedString_WithMismatchedLiteral_Throws()
    {
        var ex = Assert.Throws<FormatException>(() =>
            IdGenerator.ParseFormattedString("XX-000000000001", "ID-############"));

        Assert.Contains("Expected 'I' but got 'X'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParseFormattedString_WithValidId_Succeeds()
    {
        bool success = IdGenerator.TryParseFormattedString(
            "ID-0000000000ZZ", "ID-############", '#', out long parsed);

        Assert.True(success);
        Assert.Equal(1023, parsed);
    }

    [Fact]
    public void TryParseFormattedString_WithDefaultPlaceholder_Succeeds()
    {
        bool success = IdGenerator.TryParseFormattedString(
            "ID-0000000000ZZ", "ID-############", out long parsed);

        Assert.True(success);
        Assert.Equal(1023, parsed);
    }

    [Fact]
    public void TryParseFormattedString_WithDefaultPlaceholder_AndInvalidId_Fails()
    {
        bool success = IdGenerator.TryParseFormattedString(
            "INVALID", "ID-############", out long parsed);

        Assert.False(success);
        Assert.Equal(0, parsed);
    }

    [Theory]
    [InlineData("0000-0000-0001", "0000-0000-000I")] // 1 → I
    [InlineData("0000-0000-0001", "0000-0000-000i")] // 1 → i (lowercase)
    [InlineData("0000-0000-0001", "0000-0000-000L")] // 1 → L
    [InlineData("0000-0000-0001", "0000-0000-000l")] // 1 → l (lowercase)
    [InlineData("0000-0000-0010", "0000-0000-00I0")] // 1 → I, keep 0
    [InlineData("0000-0000-0010", "0000-0000-00IO")] // 1 → I, 0 → O
    [InlineData("0000-0000-1010", "000O-000O-IOIO")] // 0 → O, 1 → I (mixed)
    public void ParseRawString_WithConfusableCharacters_DecodesCorrectly(string canonicalForm, string confusableForm)
    {
        long expectedId = IdGenerator.ParseRawString(canonicalForm);
        long actualId = IdGenerator.ParseRawString(confusableForm);

        Assert.Equal(expectedId, actualId);
    }

    [Theory]
    [InlineData("ID-000000000001", "ID-00000000000I")] // 1 → I
    [InlineData("ID-000000000001", "ID-00000000000L")] // 1 → L
    [InlineData("ID-000000000010", "ID-0000000000I0")] // 1 → I
    [InlineData("ID-000000000010", "ID-0000000000IO")] // 1 → I, 0 → O
    public void ParseFormattedString_WithConfusableCharacters_DecodesCorrectly(string canonicalForm, string confusableForm)
    {
        string template = "ID-############";
        long expectedId = IdGenerator.ParseFormattedString(canonicalForm, template);
        long actualId = IdGenerator.ParseFormattedString(confusableForm, template);

        Assert.Equal(expectedId, actualId);
    }
}

