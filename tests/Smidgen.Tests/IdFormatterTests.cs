namespace WarpCode.Smidgen.Tests;

public class IdFormatterTests
{
    [Theory]
    [InlineData("####", '#', 0ul)]
    [InlineData("####", '#', 1ul)]
    [InlineData("####", '#', 31ul)]
    [InlineData("####", '#', 32ul)]
    [InlineData("####", '#', 100ul)]
    [InlineData("####", '#', 1000ul)]
    [InlineData("####-####", '#', 0ul)]
    [InlineData("####-####", '#', 1ul)]
    [InlineData("####-####", '#', 1000ul)]
    [InlineData("PRE-####-SUF", '#', 0ul)]
    [InlineData("PRE-####-SUF", '#', 123ul)]
    [InlineData("PRE-####-SUF", '#', 999ul)]
    [InlineData("ID:####", '#', 42ul)]
    [InlineData("XXX-XXX", 'X', 0ul)]
    [InlineData("XXX-XXX", 'X', 100ul)]
    [InlineData("XXX-XXX", 'X', 1000ul)]
    [InlineData("#", '#', 0ul)]
    [InlineData("#", '#', 31ul)]
    [InlineData("A#B#C", '#', 0ul)]
    [InlineData("A#B#C", '#', 33ul)]
    public void Format_And_Parse_ShouldBeConsistent(string template, char placeholder, ulong value)
    {
        var formatter = new IdFormatter(template, placeholder);

        var formatted = formatter.Format((UInt128)value);
        UInt128 parsed = formatter.Parse(formatted);

        Assert.Equal((UInt128)value, parsed);
    }

    [Theory]
    [InlineData("####", '#', 0ul)]
    [InlineData("####", '#', 1ul)]
    [InlineData("####-####", '#', 1000ul)]
    [InlineData("PRE-####-SUF", '#', 123ul)]
    [InlineData("XXX-XXX", 'X', 1000ul)]
    public void Format_And_TryParse_ShouldBeConsistent(string template, char placeholder, ulong value)
    {
        var formatter = new IdFormatter(template, placeholder);

        var formatted = formatter.Format((UInt128)value);
        var success = formatter.TryParse(formatted, out UInt128 parsed);

        Assert.True(success);
        Assert.Equal((UInt128)value, parsed);
    }

    [Fact]
    public void Format_And_Parse_Should_Be_Consistent_ForRange()
    {
        var formatter = new IdFormatter("####-####-####");

        for (ulong i = 0; i < 1024; i++)
        {
            var formatted = formatter.Format((UInt128)i);
            UInt128 parsed = formatter.Parse(formatted);
            Assert.Equal((UInt128)i, parsed);
        }
    }

    [Fact]
    public void Format_And_TryParse_Should_Be_Consistent_ForRange()
    {
        var formatter = new IdFormatter("PRE-####-####-SUF");

        for (ulong i = 0; i < 1024; i++)
        {
            var formatted = formatter.Format((UInt128)i);
            var success = formatter.TryParse(formatted, out UInt128 parsed);
            Assert.True(success);
            Assert.Equal((UInt128)i, parsed);
        }
    }

    [Fact]
    public void Constructor_WithTooManyPlaceholders_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => new IdFormatter("###########################")); // 27 placeholders
        Assert.Contains("27 placeholders", ex.Message);
        Assert.Contains("maximum allowed is 26", ex.Message);
    }

    [Fact]
    public void Constructor_WithExactly26Placeholders_ShouldSucceed()
    {
        var formatter = new IdFormatter("##########################"); // 26 placeholders
        var result = formatter.Format(UInt128.Zero);
        Assert.Equal("00000000000000000000000000", result);
    }

    [Fact]
    public void Constructor_WithNullTemplate_ShouldThrowArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new IdFormatter(null!));

    [Fact]
    public void Parse_WithIncorrectLength_ShouldThrowFormatException()
    {
        var formatter = new IdFormatter("####-####");

        FormatException ex = Assert.Throws<FormatException>(() => formatter.Parse("ABC"));
        Assert.Contains("Input length does not match format template length", ex.Message);
    }

    [Fact]
    public void Parse_WithMismatchedNonPlaceholder_ShouldThrowFormatException()
    {
        var formatter = new IdFormatter("PRE-####");

        FormatException ex = Assert.Throws<FormatException>(() => formatter.Parse("ABC-1234"));
        Assert.Contains("Input does not match format template", ex.Message);
    }

    [Fact]
    public void Parse_WithInvalidCrockfordCharacter_ShouldThrowArgumentOutOfRangeException()
    {
        var formatter = new IdFormatter("####");

        Assert.Throws<ArgumentOutOfRangeException>(() => formatter.Parse("ABC!"));
    }

    [Fact]
    public void TryParse_WithIncorrectLength_ShouldReturnFalse()
    {
        var formatter = new IdFormatter("####-####");

        var success = formatter.TryParse("ABC", out UInt128 result);
        Assert.False(success);
        Assert.Equal(UInt128.Zero, result);
    }

    [Fact]
    public void TryParse_WithMismatchedNonPlaceholder_ShouldReturnFalse()
    {
        var formatter = new IdFormatter("PRE-####");

        var success = formatter.TryParse("ABC-1234", out UInt128 result);
        Assert.False(success);
        Assert.Equal(UInt128.Zero, result);
    }

    [Fact]
    public void TryParse_WithInvalidCrockfordCharacter_ShouldReturnFalse()
    {
        var formatter = new IdFormatter("####");

        var success = formatter.TryParse("ABC!", out UInt128 result);
        Assert.False(success);
        Assert.Equal(UInt128.Zero, result);
    }

    [Fact]
    public void Parse_WithCaseInsensitiveInput_ShouldWork()
    {
        var formatter = new IdFormatter("####");

        UInt128 result1 = formatter.Parse("abcd");
        UInt128 result2 = formatter.Parse("ABCD");
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Parse_WithSpecialCrockfordCharacters_ShouldWork()
    {
        var formatter = new IdFormatter("####");

        // O -> 0, I -> 1, L -> 1
        UInt128 result1 = formatter.Parse("O123");
        UInt128 result2 = formatter.Parse("0123");
        Assert.Equal(result1, result2);

        UInt128 result3 = formatter.Parse("I000");
        UInt128 result4 = formatter.Parse("1000");
        Assert.Equal(result3, result4);

        UInt128 result5 = formatter.Parse("L000");
        Assert.Equal(result3, result5);
    }

    [Fact]
    public void Format_WithNoPlaceholders_ShouldReturnTemplateForZero()
    {
        var formatter = new IdFormatter("STATIC");

        var result = formatter.Format(UInt128.Zero);
        Assert.Equal("STATIC", result);
    }

    [Fact]
    public void Format_WithNoPlaceholders_AndNonZeroValue_ShouldThrowFormatException()
    {
        var formatter = new IdFormatter("STATIC");

        FormatException ex = Assert.Throws<FormatException>(() => formatter.Format((UInt128)12345));
        Assert.Contains("Format template is missing", ex.Message);
        Assert.Contains("placeholders causing truncation", ex.Message);
    }

    [Fact]
    public void Parse_WithNoPlaceholders_ShouldReturnZero()
    {
        var formatter = new IdFormatter("STATIC");

        UInt128 result = formatter.Parse("STATIC");
        Assert.Equal(UInt128.Zero, result);
    }

    [Fact]
    public void Format_WithUInt128MaxValue_ShouldWork()
    {
        var formatter = new IdFormatter("##########################"); // 26 placeholders

        var formatted = formatter.Format(UInt128.MaxValue);
        UInt128 parsed = formatter.Parse(formatted);

        Assert.Equal(UInt128.MaxValue, parsed);
    }

    [Fact]
    public void Format_WithLargeValue_ShouldWork()
    {
        var formatter = new IdFormatter("##########################"); // 26 placeholders
        UInt128 value = (UInt128)ulong.MaxValue * 2;

        var formatted = formatter.Format(value);
        UInt128 parsed = formatter.Parse(formatted);

        Assert.Equal(value, parsed);
    }

    [Fact]
    public void Format_WithZero_ShouldFillWithZeros()
    {
        var formatter = new IdFormatter("####-####");

        var result = formatter.Format(UInt128.Zero);
        Assert.Equal("0000-0000", result);
    }

    [Fact]
    public void Parse_WithZeros_ShouldReturnZero()
    {
        var formatter = new IdFormatter("####-####");

        UInt128 result = formatter.Parse("0000-0000");
        Assert.Equal(UInt128.Zero, result);
    }

    [Fact]
    public void Format_WithLeadingZeros_ShouldWork()
    {
        var formatter = new IdFormatter("########");

        var result = formatter.Format((UInt128)1);
        Assert.Equal("00000001", result);
    }

    [Fact]
    public void Parse_WithLeadingZeros_ShouldWork()
    {
        var formatter = new IdFormatter("########");

        UInt128 result = formatter.Parse("00000001");
        Assert.Equal((UInt128)1, result);
    }

    [Fact]
    public void Format_WithSpecificValues_ShouldProduceExpectedOutput()
    {
        var formatter = new IdFormatter("####");

        Assert.Equal("0000", formatter.Format(UInt128.Zero));
        Assert.Equal("0001", formatter.Format((UInt128)1));
        Assert.Equal("000Z", formatter.Format((UInt128)31));
        Assert.Equal("0010", formatter.Format((UInt128)32));
    }

    [Fact]
    public void Parse_WithSpecificFormattedStrings_ShouldProduceExpectedValues()
    {
        var formatter = new IdFormatter("####");

        Assert.Equal(UInt128.Zero, formatter.Parse("0000"));
        Assert.Equal((UInt128)1, formatter.Parse("0001"));
        Assert.Equal((UInt128)31, formatter.Parse("000Z"));
        Assert.Equal((UInt128)32, formatter.Parse("0010"));
    }

    [Fact]
    public void Format_And_Parse_WithVeryLargeValues_ShouldBeConsistent()
    {
        var formatter = new IdFormatter("##########################"); // 26 placeholders

        UInt128[] testValues = new[]
        {
            (UInt128)ulong.MaxValue,
            (UInt128)ulong.MaxValue + 1,
            (UInt128)ulong.MaxValue * 2,
            UInt128.MaxValue / 2,
            UInt128.MaxValue - 1,
            UInt128.MaxValue
        };

        foreach (UInt128 value in testValues)
        {
            var formatted = formatter.Format(value);
            UInt128 parsed = formatter.Parse(formatted);
            Assert.Equal(value, parsed);
        }
    }
}
