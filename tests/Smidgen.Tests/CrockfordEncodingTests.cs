using System.Text;

namespace WarpCode.Smidgen.Tests;

public class CrockfordEncodingTests
{
    [Theory]
    [InlineData(0ul, "0")]
    [InlineData(1ul, "1")]
    [InlineData(31ul, "Z")]
    [InlineData(32ul, "10")]
    [InlineData(1024ul, "100")]
    [InlineData(0b11111_00000ul, "Z0")]
    [InlineData(0b11111_00000_00000_00000ul, "Z000")]
    [InlineData(0b10101ul, "N")]
    [InlineData(0b10101_00000ul, "N0")]
    [InlineData(0b10101_00000_00000_00000ul, "N000")]
    [InlineData(ulong.MaxValue, "FZZZZZZZZZZZZ")]
    public void Encode_ShouldProduceExpectedOutput(ulong value, string expected)
    {
        Span<byte> buffer = stackalloc byte[13];
        var length = CrockfordEncoding.Encode(value, buffer);
        var result = Encoding.UTF8.GetString(buffer[..length]);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("0", 0ul)]
    [InlineData("1", 1ul)]
    [InlineData("Z", 31ul)]
    [InlineData("10", 32ul)]
    [InlineData("FZZZZZZZZZZZZ", ulong.MaxValue)]
    public void Decode_ShouldProduceExpectedValue(string encoded, ulong expected)
    {
        Span<byte> buffer = stackalloc byte[encoded.Length];
        Encoding.UTF8.GetBytes(encoded, buffer);

        var result = CrockfordEncoding.Decode(buffer);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("a", 0b01010ul)]
    [InlineData("z", 0b11111ul)]
    [InlineData("A", 0b01010ul)]
    [InlineData("Z", 0b11111ul)]
    public void Decode_ShouldBeCaseInsensitive(string encoded, ulong expected)
    {
        Span<byte> buffer = stackalloc byte[encoded.Length];
        Encoding.UTF8.GetBytes(encoded, buffer);

        var result = CrockfordEncoding.Decode(buffer);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("O", 0ul)]
    [InlineData("o", 0ul)]
    [InlineData("I", 1ul)]
    [InlineData("i", 1ul)]
    [InlineData("L", 1ul)]
    [InlineData("l", 1ul)]
    public void Decode_ShouldHandleConfusableCharacters(string encoded, ulong expected)
    {
        Span<byte> buffer = stackalloc byte[encoded.Length];
        Encoding.UTF8.GetBytes(encoded, buffer);

        var result = CrockfordEncoding.Decode(buffer);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Encode_AndDecode_ShouldBeConsistent()
    {
        Span<byte> buffer = stackalloc byte[13];

        for (ulong i = 0; i < 10000; i++)
        {
            buffer.Clear();
            var length = CrockfordEncoding.Encode(i, buffer);
            var decoded = CrockfordEncoding.Decode(buffer[..length]);

            Assert.Equal(i, decoded);
        }
    }

    [Fact]
    public void Decode_WithEmptySpan_ShouldReturnZero()
    {
        var result = CrockfordEncoding.Decode(ReadOnlySpan<byte>.Empty);

        Assert.Equal(0ul, result);
    }

    [Fact]
    public void Decode_WithInvalidCharacter_ShouldThrowArgumentOutOfRangeException()
    {
        var buffer = new byte[4];
        Encoding.UTF8.GetBytes("ABC!", buffer);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CrockfordEncoding.Decode(buffer));
        Assert.Contains("Invalid character", ex.Message);
    }

    [Fact]
    public void Decode_WithExcludedUCharacter_ShouldThrowArgumentOutOfRangeException()
    {
        var buffer = new byte[4];
        Encoding.UTF8.GetBytes("ABCU", buffer);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CrockfordEncoding.Decode(buffer));
        Assert.Contains("Invalid character", ex.Message);
    }

    [Fact]
    public void Decode_WithTooLargeInput_ShouldThrowArgumentException()
    {
        var buffer = new byte[14];
        Encoding.UTF8.GetBytes("12345678901234", buffer);

        var ex = Assert.Throws<ArgumentException>(() => CrockfordEncoding.Decode(buffer));
        Assert.Contains("Input too large", ex.Message);
        Assert.Contains("Maximum 13 bytes", ex.Message);
    }

    [Fact]
    public void Decode_WithMaximumLength_ShouldSucceed()
    {
        Span<byte> buffer = stackalloc byte[13];
        Encoding.UTF8.GetBytes("FZZZZZZZZZZZZ", buffer);

        var result = CrockfordEncoding.Decode(buffer);

        Assert.Equal(ulong.MaxValue, result);
    }

    [Fact]
    public void Encode_WithMaxValue_ShouldRequire13Bytes()
    {
        Span<byte> buffer = stackalloc byte[13];

        var length = CrockfordEncoding.Encode(ulong.MaxValue, buffer);

        Assert.Equal(13, length);
    }

    [Fact]
    public void Encode_WithZero_ShouldRequire1Byte()
    {
        Span<byte> buffer = stackalloc byte[13];

        var length = CrockfordEncoding.Encode(0, buffer);

        Assert.Equal(1, length);
        Assert.Equal((byte)'0', buffer[0]);
    }
}
