using System.Text;

namespace WarpCode.Smidgen.Tests;

public class CrockfordEncodingTests
{
    [Fact]
    public void Encode_And_Decode_Should_Be_Consistent()
    {
        Span<byte> buffer = stackalloc byte[13];
        for (ulong i = 0; i < 1024; i++)
        {
            buffer.Clear();
            var length = CrockfordEncoding.Encode(i, buffer);
            var decoded = CrockfordEncoding.Decode(buffer[^length..]);
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
    [MemberData(nameof(DecodeData))]
    public void Decode_ShouldReturnExpectedValue(ulong expected, string encoded)
    {
        Assert.Equal(expected, DecodeFromString(encoded));
    }

    public static TheoryData<ulong, string> EncodeData() => new()
    {
            { ulong.MaxValue, "FZZZZZZZZZZZZ" },
            { 0, "0" },
            { 0b11111ul, "Z" },
            { 0b11111_00000ul, "Z0" },
            { 0b11111_00000_00000_00000ul, "Z000" },
            { 0b10101ul, "N" },
            { 0b10101_00000ul, "N0" },
            { 0b10101_00000_00000_00000ul, "N000" }
    };

    public static TheoryData<ulong, string> DecodeData() => new()
    {
            { 0b01010, "a" }, // case insentivity
            { 0b11111, "z" },
            { 0, "o" },
            { 0, "O" }, // special handling for similar chars
            { 1, "I" },
            { 1, "i" },
            { 1, "L" },
            { 1, "l" }
    };

    private string EncodeToString(ulong value)
    {
        Span<byte> buffer = stackalloc byte[13];
        var encoded = CrockfordEncoding.Encode(value, buffer);
        return Encoding.UTF8.GetString(buffer[^encoded..]);
    }

    private ulong DecodeFromString(string value)
    {
        Span<byte> buffer = stackalloc byte[value.Length];
        var bytes = Encoding.UTF8.GetBytes(value, buffer);
        Assert.Equal(value.Length, bytes);
        return CrockfordEncoding.Decode(buffer);
    }
}
