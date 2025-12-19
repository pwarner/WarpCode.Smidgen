using System.Text;

namespace WarpCode.Smidgen.Tests;

public class CrockfordEncodingTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(31, "Z")]
    [InlineData(32, "10")]
    [InlineData(1024, "100")]
    public void Encode_WithSmallValues_ShouldProduceExpectedOutput(ulong value, string expected)
    {
        Span<byte> buffer = stackalloc byte[26];
        var length = CrockfordEncoding.Encode((UInt128)value, buffer);
        var result = Encoding.UTF8.GetString(buffer[..length]);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("Z", 31)]
    [InlineData("10", 32)]
    public void Decode_WithSmallValues_ShouldProduceExpectedValue(string encoded, ulong expected)
    {
        Span<byte> buffer = stackalloc byte[encoded.Length];
        Encoding.UTF8.GetBytes(encoded, buffer);

        UInt128 result = CrockfordEncoding.Decode(buffer);

        Assert.Equal((UInt128)expected, result);
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

        UInt128 result = CrockfordEncoding.Decode(buffer);

        Assert.Equal((UInt128)expected, result);
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

        UInt128 result = CrockfordEncoding.Decode(buffer);

        Assert.Equal((UInt128)expected, result);
    }

    [Fact]
    public void Encode_AndDecode_ShouldBeConsistent()
    {
        Span<byte> buffer = stackalloc byte[26];

        for (ulong i = 0; i < 10000; i++)
        {
            buffer.Clear();
            var length = CrockfordEncoding.Encode((UInt128)i, buffer);
            UInt128 decoded = CrockfordEncoding.Decode(buffer[..length]);

            Assert.Equal((UInt128)i, decoded);
        }
    }

    [Fact]
    public void Decode_WithEmptySpan_ShouldReturnZero()
    {
        UInt128 result = CrockfordEncoding.Decode(ReadOnlySpan<byte>.Empty);

        Assert.Equal(UInt128.Zero, result);
    }

    [Fact]
    public void Decode_WithInvalidCharacter_ShouldThrowArgumentOutOfRangeException()
    {
        var buffer = new byte[4];
        Encoding.UTF8.GetBytes("ABC!", buffer);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => CrockfordEncoding.Decode(buffer));
        Assert.Contains("Invalid character", ex.Message);
    }

    [Fact]
    public void Decode_WithExcludedUCharacter_ShouldThrowArgumentOutOfRangeException()
    {
        var buffer = new byte[4];
        Encoding.UTF8.GetBytes("ABCU", buffer);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => CrockfordEncoding.Decode(buffer));
        Assert.Contains("Invalid character", ex.Message);
    }

    [Fact]
    public void Decode_WithTooLargeInput_ShouldThrowArgumentException()
    {
        var buffer = new byte[27];
        Encoding.UTF8.GetBytes("123456789012345678901234567", buffer);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => CrockfordEncoding.Decode(buffer));
        Assert.Contains("Input too large", ex.Message);
        Assert.Contains("Maximum 26 bytes", ex.Message);
    }

    [Fact]
    public void Decode_WithMaximumLength_ShouldSucceed()
    {
        Span<byte> buffer = stackalloc byte[26];
        Encoding.UTF8.GetBytes("ZZZZZZZZZZZZZZZZZZZZZZZZZZ", buffer);

        UInt128 result = CrockfordEncoding.Decode(buffer);

        Assert.True(result > UInt128.Zero);
    }

    [Fact]
    public void Encode_WithMaxValue_ShouldProduceExpectedOutput()
    {
        Span<byte> buffer = stackalloc byte[26];
        var length = CrockfordEncoding.Encode(UInt128.MaxValue, buffer);
        var result = Encoding.UTF8.GetString(buffer[..length]);

        Assert.Equal(26, length);
        Assert.StartsWith("7ZZZZZZZZZZZZZZZZZZZZZZZZ", result);
    }

    [Fact]
    public void Encode_WithZero_ShouldRequire1Byte()
    {
        Span<byte> buffer = stackalloc byte[26];
        var length = CrockfordEncoding.Encode(UInt128.Zero, buffer);

        Assert.Equal(1, length);
        Assert.Equal((byte)'0', buffer[0]);
    }

    [Fact]
    public void Encode_With64BitBoundary_ShouldHandleCorrectly()
    {
        UInt128 value = (UInt128)ulong.MaxValue + 1; // 2^64

        Span<byte> buffer = stackalloc byte[26];
        var length = CrockfordEncoding.Encode(value, buffer);

        Assert.Equal(13, length);
    }

    [Fact]
    public void Decode_WithMaxValue_ShouldProduceExpectedValue()
    {
        Span<byte> encodeBuffer = stackalloc byte[26];
        var length = CrockfordEncoding.Encode(UInt128.MaxValue, encodeBuffer);

        UInt128 result = CrockfordEncoding.Decode(encodeBuffer[..length]);

        Assert.Equal(UInt128.MaxValue, result);
    }

    [Fact]
    public void EncodeAndDecode_WithLargeValues_ShouldBeConsistent()
    {
        Span<byte> buffer = stackalloc byte[26];

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
            buffer.Clear();
            var length = CrockfordEncoding.Encode(value, buffer);
            UInt128 decoded = CrockfordEncoding.Decode(buffer[..length]);

            Assert.Equal(value, decoded);
        }
    }
}
