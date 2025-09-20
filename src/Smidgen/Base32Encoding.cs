namespace WarpCode.Smidgen;

/// <summary>
/// Implements Crockford's Base32 encoding
/// </summary>
public static class Base32Encoding
{
    public static void Encode(ulong value, Span<byte> bytes)
    {
        bytes.Fill((byte)'0');
        while (value > 0)
        {
            bytes[^1] = EncodeMap(value & 31);
            bytes = bytes[..^1];
            value >>= 5;
        }
    }

    public static ulong Decode(ReadOnlySpan<byte> bytes)
    {
        ulong value = 0;
        foreach (var b in bytes)
        {
            value <<= 5;
            value |= DecodeMap(b);
        }
        return value;
    }

    private static byte DecodeMap(byte value) => value switch
    {
        //0 => (byte)0,
        >= 48 and <= 57 => (byte)(value - 48), // 0-9
        >= 65 and <= 72 => (byte)(value - 55), // A-H
        73 => 1, // I -> 1
        74 or 75 => (byte)(value - 56), // J-K,
        76 => 1, // L -> 1
        >= 77 and <= 78 => (byte)(value - 57), // M-N
        79 => 0, // O -> 0
        >= 80 and <= 84 => (byte)(value - 58), // P-T
        >= 86 and <= 90 => (byte)(value - 59), // V-Z
        >= 97 and <= 122 => DecodeMap((byte)(value - 32)), // a-z -> A-Z
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Invalid character for Crockford's Base32")
    };

    private static byte EncodeMap(ulong value) => value switch
    {
        >= 0 and <= 9 => (byte)(value + 48), // 0-9
        >= 10 and <= 17 => (byte)(value + 55), // A-H
        >= 18 and <= 19 => (byte)(value + 56), // J-K
        >= 20 and <= 21 => (byte)(value + 57), // M-N
        >= 22 and <= 26 => (byte)(value + 58), // P-T
        >= 27 and <= 31 => (byte)(value + 59), // V-Z
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Invalid ordinal for Crockford's Base32")
    };
}
