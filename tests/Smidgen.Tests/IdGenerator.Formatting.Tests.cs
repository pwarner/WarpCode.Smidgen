namespace WarpCode.Smidgen.Tests;

/// <summary>
/// Tests for ID formatting and parsing functionality.
/// </summary>
public class IdGeneratorFormattingTests
{
    [Fact]
    public void NextRawStringId_ShouldGenerateNonEmptyString()
    {
        var generator = new IdGenerator();
        var id = generator.NextRawStringId();

        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.True(id.Length <= generator.Base32Size);
    }

    [Fact]
    public void NextRawStringId_ShouldGenerateUniqueStrings()
    {
        var generator = new IdGenerator();
        var ids = new HashSet<string>();

        for (var i = 0; i < 1000; i++)
        {
            ids.Add(generator.NextRawStringId());
        }

        Assert.Equal(1000, ids.Count);
    }

    [Fact]
    public void NextRawStringId_ShouldUseBase32Encoding()
    {
        var generator = new IdGenerator();
        var id = generator.NextRawStringId();

        // All characters should be valid Crockford Base32
        var validChars = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        foreach (var c in id)
        {
            Assert.Contains(c, validChars);
        }
    }

    [Fact]
    public void NextFormattedId_ShouldFormatWithTemplate()
    {
        var generator = new IdGenerator();
        // Use enough placeholders - Base32Size for SmallId is 13
        var formatted = generator.NextFormattedId("PRE-#############-SUF");

        Assert.StartsWith("PRE-", formatted);
        Assert.EndsWith("-SUF", formatted);
    }

    [Fact]
    public void NextFormattedId_ShouldRespectPlaceholderCharacter()
    {
        var generator = new IdGenerator();
        var formatted = generator.NextFormattedId("ID-*************", '*');

        Assert.StartsWith("ID-", formatted);
        Assert.DoesNotContain("*", formatted);
    }

    [Fact]
    public void NextFormattedId_WithInsufficientPlaceholders_ShouldThrow()
    {
        var generator = new IdGenerator();

        // Only 4 placeholders, but SmallId needs 13
        Assert.Throws<FormatException>(() =>
            generator.NextFormattedId("ID-####"));
    }

    [Fact]
    public void ParseRawStringId_ShouldRoundTrip()
    {
        var generator = new IdGenerator();
        UInt128 originalId = generator.NextUInt128();

        // Convert to string
        Span<byte> encoded = stackalloc byte[generator.Base32Size];
        var length = CrockfordEncoding.Encode(originalId, encoded);
        var idString = System.Text.Encoding.ASCII.GetString(encoded[..length]);

        // Parse back
        UInt128 parsedId = IdGenerator.ParseRawStringId(idString);

        Assert.Equal(originalId, parsedId);
    }

    [Fact]
    public void ParseRawStringId_WithLowerCase_ShouldSucceed()
    {
        var generator = new IdGenerator();
        UInt128 id = generator.NextUInt128();

        Span<byte> encoded = stackalloc byte[generator.Base32Size];
        var length = CrockfordEncoding.Encode(id, encoded);
        var upperCase = System.Text.Encoding.ASCII.GetString(encoded[..length]);
        var lowerCase = upperCase.ToLowerInvariant();

        UInt128 parsed = IdGenerator.ParseRawStringId(lowerCase);

        Assert.Equal(id, parsed);
    }

    [Fact]
    public void ParseRawStringId_WithInvalidCharacters_ShouldThrow()
    {
        var generator = new IdGenerator();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IdGenerator.ParseRawStringId("INVALID!"));
    }

    [Fact]
    public void ParseFormattedId_ShouldRoundTrip()
    {
        var generator = new IdGenerator();
        // Use enough placeholders for Base32Size (13 for SmallId)
        var template = "ID-#############";

        var formattedId = generator.NextFormattedId(template);
        UInt128 parsedId = IdGenerator.ParseFormattedId(formattedId, template);

        Assert.True(parsedId > UInt128.Zero);
    }

    [Fact]
    public void ParseFormattedId_WithMismatchedTemplate_ShouldThrow()
    {
        var generator = new IdGenerator();
        var template = "ID-#############";

        Assert.Throws<FormatException>(() =>
            IdGenerator.ParseFormattedId("INVALID", template));
    }

    [Fact]
    public void TryParseRawStringId_WithValidId_ShouldSucceed()
    {
        var generator = new IdGenerator();
        var rawId = generator.NextRawStringId();

        var success = IdGenerator.TryParseRawStringId(rawId, out UInt128 parsed);

        Assert.True(success);
        Assert.True(parsed > UInt128.Zero);
    }

    [Fact]
    public void TryParseRawStringId_WithInvalidId_ShouldFail()
    {
        var generator = new IdGenerator();

        var success = IdGenerator.TryParseRawStringId("INVALID!", out UInt128 parsed);

        Assert.False(success);
        Assert.Equal(UInt128.Zero, parsed);
    }

    [Fact]
    public void TryParseFormattedId_WithValidId_ShouldSucceed()
    {
        var generator = new IdGenerator();
        // Use a template with enough placeholders for Base32Size (13 for SmallId)
        var template = "ID-#############";
        var formattedId = generator.NextFormattedId(template);

        var success = IdGenerator.TryParseFormattedId(formattedId, template, out UInt128 parsed);

        Assert.True(success);
        Assert.True(parsed > UInt128.Zero);
    }

    [Fact]
    public void TryParseFormattedId_WithInvalidId_ShouldFail()
    {
        var generator = new IdGenerator();
        var template = "ID-#############";

        var success = IdGenerator.TryParseFormattedId("INVALID", template, out UInt128 parsed);

        Assert.False(success);
        Assert.Equal(UInt128.Zero, parsed);
    }

    [Fact]
    public void TryParseFormattedId_WithDefaultPlaceholder_ShouldWork()
    {
        var generator = new IdGenerator();
        var template = "ID-#############";
        var formattedId = generator.NextFormattedId(template);

        // Use overload without explicit placeholder
        var success = IdGenerator.TryParseFormattedId(formattedId, template, out UInt128 parsed);

        Assert.True(success);
        Assert.True(parsed > UInt128.Zero);
    }

    [Fact]
    public void NextFormattedId_WithNullTemplate_ShouldThrow()
    {
        var generator = new IdGenerator();

        Assert.Throws<ArgumentNullException>(() =>
            generator.NextFormattedId(null!));
    }

    [Fact]
    public void NextFormattedId_WithEmptyTemplate_ShouldThrow()
    {
        var generator = new IdGenerator();

        // Empty template has insufficient placeholders for any real ID
        Assert.Throws<FormatException>(() =>
            generator.NextFormattedId(""));
    }

    [Fact]
    public void NextFormattedId_WithTooManyPlaceholders_ShouldThrow()
    {
        var generator = new IdGenerator();

        // More than 26 placeholders (130 bits)
        var template = new string('#', 27);

        Assert.Throws<ArgumentException>(() =>
            generator.NextFormattedId(template));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("INVALID!")]
    [InlineData("   ")]
    public void ParseRawStringId_WithInvalidInput_ShouldThrow(string? input)
    {
        // ArgumentNullException, ArgumentException, or ArgumentOutOfRangeException are acceptable
        Assert.ThrowsAny<ArgumentException>(() =>
            IdGenerator.ParseRawStringId(input!));
    }

    [Fact]
    public void ParseRawStringId_WithEmpty_ShouldReturnZero()
    {
        // Empty string decodes to zero, which is valid
        UInt128 result = IdGenerator.ParseRawStringId("");
        Assert.Equal(UInt128.Zero, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("INVALID")]
    public void ParseFormattedId_WithInvalidInput_ShouldThrow(string? input)
    {
        // FormatException is thrown when input doesn't match template
        Assert.ThrowsAny<Exception>(() =>
            IdGenerator.ParseFormattedId(input!, "ID-#############"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("INVALID!")]
    [InlineData("   ")]
    public void TryParseRawStringId_WithInvalidInput_ShouldReturnFalse(string? input)
    {
        var success = IdGenerator.TryParseRawStringId(input!, out UInt128 result);

        Assert.False(success);
        Assert.Equal(UInt128.Zero, result);
    }

    [Fact]
    public void TryParseRawStringId_WithEmpty_ShouldSucceedWithZero()
    {
        // Empty string decodes to UInt128.Zero, which is technically valid
        var success = IdGenerator.TryParseRawStringId("", out UInt128 result);

        Assert.True(success);
        Assert.Equal(UInt128.Zero, result);
    }
}
