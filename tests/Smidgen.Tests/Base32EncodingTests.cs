using System.Text;

namespace WarpCode.Smidgen.Tests;

public class Base32EncodingTests
{
    [Fact]
    public void Encode_And_Decode_Should_Be_Consistent()
    {
        Span<byte> buffer = stackalloc byte[13];
        for (ulong i = 0; i < 1024; i++)
        {
            buffer.Clear();
            Base32Encoding.Encode(i, buffer);
            var decoded = Base32Encoding.Decode(buffer);
            Assert.Equal(i, decoded);
        }
    }

    [Theory]
    [MemberData(nameof(EncodeData))]
    public void Encode_ShouldReturnExpectedValue(ulong value, string expected)
    {
        Assert.Equal(expected, EncodeToString(value));
    }

    [Theory]
    [MemberData(nameof(EncodeData))]
    public void Decode_ShouldReturnExpectedValue(ulong expected, string encoded)
    {
        Assert.Equal(expected, DecodeFromString(encoded));
    }

    public static TheoryData<ulong, string> EncodeData() => new()
    {
            { ulong.MaxValue, "FZZZZZZZZZZZZ" },
            { 0, "0000000000000" },
            { 0b11111ul, "000000000000Z" },
            { 0b11111_00000ul, "00000000000Z0" },
            { 0b11111_00000_00000_00000ul, "000000000Z000" },
            { 0b10101ul, "000000000000N" },
            { 0b10101_00000ul, "00000000000N0" },
            { 0b10101_00000_00000_00000ul, "000000000N000" }
    };

    private string EncodeToString(ulong value)
    {
        Span<byte> buffer = stackalloc byte[13];
        Base32Encoding.Encode(value, buffer);
        return Encoding.UTF8.GetString(buffer);
    }

    private ulong DecodeFromString(string value)
    {
        Span<byte> buffer = stackalloc byte[13];
        var bytes = Encoding.UTF8.GetBytes(value, buffer);
        Assert.Equal(value.Length, bytes);
        return Base32Encoding.Decode(buffer);
    }
}
