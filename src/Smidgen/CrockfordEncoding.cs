using System.Buffers;
using System.Runtime.CompilerServices;

namespace WarpCode.Smidgen;

/// <summary>
/// Implements Crockford's 5-bit (Base32) encoding for UInt128 values.
/// </summary>
internal static class CrockfordEncoding
{
    private const int MaxLength = 26; // 26 * 5 = 130 bits > 128 bits of UInt128

    // Lookup table for encoding: maps 0-31 to their ASCII character codes
    private static readonly byte[] EncodeTable = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"u8.ToArray();

    // Lookup table for decoding: maps ASCII character codes to their 5-bit values
    // Array sized to cover all ASCII printable characters (0-122 for 'z')
    private static readonly byte[] DecodeTable = CreateDecodeTable();

    // SearchValues for efficient validation of valid Crockford Base32 characters
    // Note: U is intentionally excluded as per Crockford Base32 specification
    private static readonly SearchValues<byte> ValidCharacters = SearchValues.Create(
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

    /// <summary>
    /// Encodes the specified unsigned 128-bit integer into a Crockford Base-32 representation and writes the result into
    /// the provided byte span.
    /// </summary>
    /// <remarks>
    /// This method assumes the caller provides a destination buffer of at least 26 bytes.
    /// The encoded digits are written from left to right starting at index 0.
    /// </remarks>
    /// <param name="value">The unsigned 128-bit integer value to encode.</param>
    /// <param name="destination">The span of bytes that receives the encoded representation. Must be at least 26 bytes.</param>
    /// <returns>The number of bytes written to the span.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encode(UInt128 value, Span<byte> destination)
    {
        if (value == UInt128.Zero)
        {
            destination[0] = (byte)'0';
            return 1;
        }

        // Calculate required length: ceil((128 - leadingZeros) / 5)
        var leadingZeros = (int)UInt128.LeadingZeroCount(value);
        var requiredLength = (128 - leadingZeros + 4) / 5;

        // Encode from right to left into the buffer
        var index = requiredLength - 1;
        while (value > UInt128.Zero)
        {
            destination[index--] = EncodeTable[(int)(value & 31u)];
            value >>= 5;
        }

        return requiredLength;
    }

    /// <summary>
    /// Decodes a sequence of bytes encoded in Crockford 32 bit encoding format into an unsigned 128-bit integer.
    /// </summary>
    /// <remarks>The method processes each byte in the input as a 5-bit value and combines them into a single
    /// integer. The input must not contain more than 26 bytes, as only the lowest 130 bits are used; excess bytes may
    /// result in data loss or overflow.</remarks>
    /// <param name="bytes">A read-only span of bytes representing the encoded value to decode. Each byte must be a valid encoded value as
    /// defined by the decoding map.</param>
    /// <returns>The unsigned 128-bit integer obtained by decoding the input byte sequence.</returns>
    /// <exception cref="ArgumentException">Thrown when the input span exceeds 26 bytes.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid character is encountered.</exception>
    public static UInt128 Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
            return UInt128.Zero;

        // Maximum 26 characters can encode 130 bits (26 * 5 = 130)
        // Since we're decoding to UInt128 (128 bits), 26 is the safe maximum
        if (bytes.Length > MaxLength)
            ThrowInputTooLarge(bytes.Length);

        UInt128 value = UInt128.Zero;
        foreach (var b in bytes)
        {
            value <<= 5;
            value |= DecodeMap(b);
        }
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte DecodeMap(byte value)
    {
        if (value >= DecodeTable.Length || !ValidCharacters.Contains(value))
            ThrowInvalidCharacter(value);

        return DecodeTable[value];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidCharacter(byte value) =>
        throw new ArgumentOutOfRangeException(nameof(value), $"Invalid character '{(char)value}' (0x{value:X2}) for Crockford's Base32");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInputTooLarge(int length) =>
        throw new ArgumentException($"Input too large. Maximum {MaxLength} bytes allowed for decoding, but got {length} bytes.");
}
