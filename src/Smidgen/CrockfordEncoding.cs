using System.Buffers;
using System.Runtime.CompilerServices;

namespace WarpCode.Smidgen;

/// <summary>
/// Provides lookup tables for Crockford's Base32 encoding.
/// </summary>
internal static class CrockfordEncoding
{
    // Lookup table for encoding: maps 0-31 to their ASCII character codes
    public static readonly byte[] EncodeTable = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"u8.ToArray();

    // Lookup table for decoding: maps ASCII character codes to their 5-bit values
    // Array sized to cover all ASCII printable characters (0-122 for 'z')
    public static readonly byte[] DecodeTable = CreateDecodeTable();

    // SearchValues for efficient validation of valid Crockford Base32 characters
    // Note: U is intentionally excluded as per Crockford Base32 specification
    public static readonly SearchValues<byte> ValidCharacters = SearchValues.Create(
        "0123456789ABCDEFGHIJKLMNOPQRSTVWXYZabcdefghijklmnopqrstvwxyz"u8);

    private static byte[] CreateDecodeTable()
    {
        var table = new byte[123]; // Cover 0-122 ('z')
        Array.Fill(table, byte.MaxValue); // Invalid marker

        // 0-9 -> 0-9
        for (byte i = 0; i < 10; i++)
            table[i + 48] = i;

        // A-H -> 10-17
        for (byte i = 0; i < 8; i++)
            table[i + 65] = (byte)(i + 10);

        // I -> 1 (confusion with 1)
        table[73] = 1;

        // J-K -> 18-19
        table[74] = 18;
        table[75] = 19;

        // L -> 1 (confusion with 1)
        table[76] = 1;

        // M-N -> 20-21
        table[77] = 20;
        table[78] = 21;

        // O -> 0 (confusion with 0)
        table[79] = 0;

        // P-T -> 22-26
        for (byte i = 0; i < 5; i++)
            table[i + 80] = (byte)(i + 22);

        // U is skipped (85)

        // V-Z -> 27-31
        for (byte i = 0; i < 5; i++)
            table[i + 86] = (byte)(i + 27);

        // Lowercase a-z -> same as uppercase
        for (var i = 97; i <= 122; i++)
            table[i] = table[i - 32];

        return table;
    }
}
