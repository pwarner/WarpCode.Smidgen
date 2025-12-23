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
        var generator = new IdGenerator(null, () => 12345, () => 67890, () => 0);
        UInt128 originalId = generator.NextUInt128();

        // Convert to string
        var idString = generator.NextRawStringId();

        // Parse back
        UInt128 parsedId = IdGenerator.ParseRawStringId(idString);

        Assert.Equal(originalId, parsedId);
    }

    [Fact]
    public void ParseRawStringId_WithLowerCase_ShouldSucceed()
    {
        var generator = new IdGenerator(null, () => 12345, () => 67890, () => 0);
        UInt128 id = generator.NextUInt128();

        // Generate raw string ID from the same ID and convert to lowercase
        var template = new string('#', generator.Base32Size);
        var upperCase = IdFormatter.Format(id, template);
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
        var generator = new IdGenerator(null, () => 12345, () => 67890, ()=> 0);
        UInt128 originalId = generator.NextUInt128();
        // Use enough placeholders for Base32Size (13 for SmallId)
        var template = "ID-#############";

        var formattedId = generator.NextFormattedId(template);
        UInt128 parsedId = IdGenerator.ParseFormattedId(formattedId, template);

        Assert.Equal(originalId, parsedId);
    }

    [Fact]
    public void ParseFormattedId_WithInvalidId_ShouldThrow()
    {
        Assert.Throws<FormatException>(() =>
            IdGenerator.ParseFormattedId("INVALID", "ID-#############"));
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
        var success = IdGenerator.TryParseFormattedId("INVALID", "ID-#############", out UInt128 parsed);

        Assert.False(success);
        Assert.Equal(UInt128.Zero, parsed);
    }

    [Fact]
    public void NextFormattedId_WithTooManyPlaceholders_ShouldNotThrow()
    {
        var generator = new IdGenerator();

        // More placeholders than needed - should work but pad with zeros
        var template = new string('#', generator.Base32Size + 5);
        
        var result = generator.NextFormattedId(template);
        
        // Result should have leading zeros
        Assert.NotNull(result);
        Assert.Equal(template.Length, result.Length);
    }

    [Fact]
    public void ParseRawStringId_WithEmpty_ShouldReturnZero()
    {
        // Empty span decodes to zero, which is valid
        UInt128 result = IdGenerator.ParseRawStringId("");
        Assert.Equal(UInt128.Zero, result);
    }

    [Fact]
    public void ParseFormattedId_WithEmptyTemplate_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            IdGenerator.ParseFormattedId("123", ""));
    }

    [Fact]
    public void TryParseRawStringId_WithEmpty_ShouldSucceedWithZero()
    {
        // Empty span decodes to UInt128.Zero, which is technically valid
        var success = IdGenerator.TryParseRawStringId("", out UInt128 result);

        Assert.True(success);
        Assert.Equal(UInt128.Zero, result);
    }
}
