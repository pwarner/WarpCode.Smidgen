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
        
        var formatted = formatter.Format(value);
        var parsed = formatter.Parse(formatted);
        
        Assert.Equal(value, parsed);
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
        
        var formatted = formatter.Format(value);
        var success = formatter.TryParse(formatted, out var parsed);
        
        Assert.True(success);
        Assert.Equal(value, parsed);
    }

    [Fact]
    public void Format_And_Parse_Should_Be_Consistent_ForRange()
    {
        var formatter = new IdFormatter("####-####-####");
        
        for (ulong i = 0; i < 1024; i++)
        {
            var formatted = formatter.Format(i);
            var parsed = formatter.Parse(formatted);
            Assert.Equal(i, parsed);
        }
    }

    [Fact]
    public void Format_And_TryParse_Should_Be_Consistent_ForRange()
    {
        var formatter = new IdFormatter("PRE-####-####-SUF");
        
        for (ulong i = 0; i < 1024; i++)
        {
            var formatted = formatter.Format(i);
            var success = formatter.TryParse(formatted, out var parsed);
            Assert.True(success);
            Assert.Equal(i, parsed);
        }
    }

    [Fact]
    public void Constructor_WithTooManyPlaceholders_ShouldThrowArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => new IdFormatter("##############"));
        Assert.Contains("14 placeholders", ex.Message);
        Assert.Contains("maximum allowed is 13", ex.Message);
    }

    [Fact]
    public void Constructor_WithExactly13Placeholders_ShouldSucceed()
    {
        var formatter = new IdFormatter("#############");
        var result = formatter.Format(0);
        Assert.Equal("0000000000000", result);
    }

    [Fact]
    public void Constructor_WithNullTemplate_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new IdFormatter(null!));
    }

    [Fact]
    public void Parse_WithIncorrectLength_ShouldThrowFormatException()
    {
        var formatter = new IdFormatter("####-####");
        
        var ex = Assert.Throws<FormatException>(() => formatter.Parse("ABC"));
        Assert.Contains("Input length does not match format template length", ex.Message);
    }

    [Fact]
    public void Parse_WithMismatchedNonPlaceholder_ShouldThrowFormatException()
    {
        var formatter = new IdFormatter("PRE-####");
        
        var ex = Assert.Throws<FormatException>(() => formatter.Parse("ABC-1234"));
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
        
        var success = formatter.TryParse("ABC", out var result);
        Assert.False(success);
        Assert.Equal(0ul, result);
    }

    [Fact]
    public void TryParse_WithMismatchedNonPlaceholder_ShouldReturnFalse()
    {
        var formatter = new IdFormatter("PRE-####");
        
        var success = formatter.TryParse("ABC-1234", out var result);
        Assert.False(success);
        Assert.Equal(0ul, result);
    }

    [Fact]
    public void TryParse_WithInvalidCrockfordCharacter_ShouldReturnFalse()
    {
        var formatter = new IdFormatter("####");
        
        var success = formatter.TryParse("ABC!", out var result);
        Assert.False(success);
        Assert.Equal(0ul, result);
    }

    [Fact]
    public void Parse_WithCaseInsensitiveInput_ShouldWork()
    {
        var formatter = new IdFormatter("####");
        
        var result1 = formatter.Parse("abcd");
        var result2 = formatter.Parse("ABCD");
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Parse_WithSpecialCrockfordCharacters_ShouldWork()
    {
        var formatter = new IdFormatter("####");
        
        // O -> 0, I -> 1, L -> 1
        var result1 = formatter.Parse("O123");
        var result2 = formatter.Parse("0123");
        Assert.Equal(result1, result2);
        
        var result3 = formatter.Parse("I000");
        var result4 = formatter.Parse("1000");
        Assert.Equal(result3, result4);
        
        var result5 = formatter.Parse("L000");
        Assert.Equal(result3, result5);
    }

    [Fact]
    public void Format_WithNoPlaceholders_ShouldReturnTemplateForZero()
    {
        var formatter = new IdFormatter("STATIC");
        
        var result = formatter.Format(0);
        Assert.Equal("STATIC", result);
    }

    [Fact]
    public void Format_WithNoPlaceholders_AndNonZeroValue_ShouldThrowFormatException()
    {
        var formatter = new IdFormatter("STATIC");
        
        var ex = Assert.Throws<FormatException>(() => formatter.Format(12345));
        Assert.Contains("Format template is missing", ex.Message);
        Assert.Contains("placeholders causing truncation", ex.Message);
    }

    [Fact]
    public void Parse_WithNoPlaceholders_ShouldReturnZero()
    {
        var formatter = new IdFormatter("STATIC");
        
        var result = formatter.Parse("STATIC");
        Assert.Equal(0ul, result);
    }

    [Fact]
    public void Format_WithMaxValue_ShouldWork()
    {
        var formatter = new IdFormatter("#############");
        
        var result = formatter.Format(ulong.MaxValue);
        Assert.Equal("FZZZZZZZZZZZZ", result);
    }

    [Fact]
    public void Parse_WithMaxValue_ShouldWork()
    {
        var formatter = new IdFormatter("#############");
        
        var result = formatter.Parse("FZZZZZZZZZZZZ");
        Assert.Equal(ulong.MaxValue, result);
    }

    [Fact]
    public void Format_WithZero_ShouldFillWithZeros()
    {
        var formatter = new IdFormatter("####-####");
        
        var result = formatter.Format(0);
        Assert.Equal("0000-0000", result);
    }

    [Fact]
    public void Parse_WithZeros_ShouldReturnZero()
    {
        var formatter = new IdFormatter("####-####");
        
        var result = formatter.Parse("0000-0000");
        Assert.Equal(0ul, result);
    }

    [Fact]
    public void Format_WithLeadingZeros_ShouldWork()
    {
        var formatter = new IdFormatter("########");
        
        var result = formatter.Format(1);
        Assert.Equal("00000001", result);
    }

    [Fact]
    public void Parse_WithLeadingZeros_ShouldWork()
    {
        var formatter = new IdFormatter("########");
        
        var result = formatter.Parse("00000001");
        Assert.Equal(1ul, result);
    }

    [Fact]
    public void Format_WithSpecificValues_ShouldProduceExpectedOutput()
    {
        var formatter = new IdFormatter("####");
        
        Assert.Equal("0000", formatter.Format(0));
        Assert.Equal("0001", formatter.Format(1));
        Assert.Equal("000Z", formatter.Format(31));
        Assert.Equal("0010", formatter.Format(32));
    }

    [Fact]
    public void Parse_WithSpecificFormattedStrings_ShouldProduceExpectedValues()
    {
        var formatter = new IdFormatter("####");
        
        Assert.Equal(0ul, formatter.Parse("0000"));
        Assert.Equal(1ul, formatter.Parse("0001"));
        Assert.Equal(31ul, formatter.Parse("000Z"));
        Assert.Equal(32ul, formatter.Parse("0010"));
    }
}
