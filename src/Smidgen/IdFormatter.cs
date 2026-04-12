using System.Buffers;
using System.Runtime.CompilerServices;

namespace WarpCode.Smidgen;

/// <summary>
/// Formats and parses 60-bit identifiers using Crockford Base32 encoding with customizable templates.
/// </summary>
internal static class IdFormatter
{
    public const char DefaultPlaceholder = '#';
    private const int MaxChars = 12; // 60 bits / 5 bits per char
    private const string RawFormat = "####-####-####"; // 4-4-4 pattern (12 chars + 2 dashes)

    // Lookup table for encoding: maps 0-31 to their ASCII character codes
    private static readonly byte[] EncodeTable = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"u8.ToArray();

    // Lookup table for decoding: maps ASCII character codes to their 5-bit values
    private static readonly byte[] DecodeTable = CreateDecodeTable();

    // SearchValues for efficient validation of valid Crockford Base32 characters
    private static readonly SearchValues<byte> ValidCharacters = SearchValues.Create(
        "0123456789ABCDEFGHIJKLMNOPQRSTVWXYZabcdefghijklmnopqrstvwxyz"u8);

    /// <summary>
    /// Encodes a 60-bit identifier to the raw string format (####-####-####).
    /// </summary>
    /// <param name="id">The 60-bit identifier to encode.</param>
    /// <returns>A string in the format ####-####-#### using Crockford Base32.</returns>
    public static string EncodeToRawString(long id)
    {
        return EncodeToFormattedString(id, RawFormat, DefaultPlaceholder);
    }

    /// <summary>
    /// Encodes a 60-bit identifier to a formatted string using a custom template.
    /// </summary>
    /// <param name="id">The 60-bit identifier to encode.</param>
    /// <param name="template">The template string containing placeholder characters.</param>
    /// <param name="placeholder">The character used as a placeholder in the template.</param>
    /// <returns>A formatted string representation of the identifier.</returns>
    /// <exception cref="ArgumentException">Thrown when the template is empty.</exception>
    /// <exception cref="FormatException">Thrown when the template has insufficient placeholders.</exception>
    public static string EncodeToFormattedString(long id, ReadOnlySpan<char> template, char placeholder = DefaultPlaceholder)
    {
        if (template.IsEmpty)
            throw new ArgumentException("Format template cannot be empty.", nameof(template));

        // From right to left, generate output by replacing placeholders with encoded characters
        Span<char> output = stackalloc char[template.Length];
        var value = (ulong)id; // Work with unsigned for bit operations

        for (var i = template.Length - 1; i >= 0; i--)
        {
            var templateChar = template[i];

            if (templateChar == placeholder)
            {
                if (value != 0)
                {
                    output[i] = (char)EncodeTable[value & 31u];
                    value >>= 5;
                }
                else
                {
                    output[i] = '0';
                }
            }
            else
            {
                output[i] = templateChar;
            }
        }

        // Check if we've consumed all input
        if (value != 0)
        {
            // Calculate how many characters are left to encode
            var remainingBits = 64 - (int)ulong.LeadingZeroCount(value);
            var missingPlaceholders = (remainingBits + 4) / 5;
            throw new FormatException($"Format template is missing {missingPlaceholders} placeholders causing truncation.");
        }

        return new string(output);
    }

    /// <summary>
    /// Parses a raw string (####-####-####) to a 60-bit identifier.
    /// </summary>
    /// <param name="rawString">The raw string to parse.</param>
    /// <returns>The 60-bit identifier represented by the raw string.</returns>
    /// <exception cref="FormatException">Thrown when the input is invalid.</exception>
    public static long ParseRawString(string rawString)
    {
        return ParseFormattedString(rawString, RawFormat, DefaultPlaceholder);
    }

    /// <summary>
    /// Attempts to parse a raw string (####-####-####) to a 60-bit identifier.
    /// </summary>
    /// <param name="rawString">The raw string to parse.</param>
    /// <param name="id">When successful, contains the parsed identifier; otherwise, 0.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParseRawString(string rawString, out long id)
    {
        return TryParseFormattedString(rawString, RawFormat, DefaultPlaceholder, out id);
    }

    /// <summary>
    /// Parses a formatted string to a 60-bit identifier using a custom template.
    /// </summary>
    /// <param name="formattedId">The formatted string to parse.</param>
    /// <param name="template">The template string containing placeholder characters.</param>
    /// <param name="placeholder">The character used as a placeholder in the template.</param>
    /// <returns>The 60-bit identifier represented by the formatted string.</returns>
    /// <exception cref="ArgumentException">Thrown when the template is empty.</exception>
    /// <exception cref="FormatException">Thrown when input doesn't match template or contains invalid characters.</exception>
    public static long ParseFormattedString(ReadOnlySpan<char> formattedId, ReadOnlySpan<char> template, char placeholder = DefaultPlaceholder)
    {
        if (template.IsEmpty)
            throw new ArgumentException("Format template cannot be empty.", nameof(template));

        if (formattedId.Length != template.Length)
            throw new FormatException("Input length does not match format template length.");

        // Count placeholders to check maximum length
        var placeholderCount = template.Count(placeholder);
        if (placeholderCount > MaxChars)
            ThrowInputTooLarge(placeholderCount);

        ulong value = 0;

        for (var i = 0; i < template.Length; i++)
        {
            var templateChar = template[i];
            var inputChar = formattedId[i];

            if (templateChar.Equals(placeholder))
            {
                value <<= 5;
                value |= DecodeChar(inputChar);
            }
            else if (!templateChar.Equals(inputChar))
            {
                throw new FormatException($"Input does not match format template. Expected '{templateChar}' but got '{inputChar}'.");
            }
        }

        return (long)value;
    }

    /// <summary>
    /// Attempts to parse a formatted string to a 60-bit identifier using a custom template.
    /// </summary>
    /// <param name="formattedId">The formatted string to parse.</param>
    /// <param name="template">The template string containing placeholder characters.</param>
    /// <param name="placeholder">The character used as a placeholder in the template.</param>
    /// <param name="id">When successful, contains the parsed identifier; otherwise, 0.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParseFormattedString(ReadOnlySpan<char> formattedId, ReadOnlySpan<char> template, char placeholder, out long id)
    {
        try
        {
            id = ParseFormattedString(formattedId, template, placeholder);
            return true;
        }
        catch
        {
            id = 0;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte DecodeChar(char value)
    {
        var byteValue = (byte)value;
        if (byteValue >= DecodeTable.Length || !ValidCharacters.Contains(byteValue))
            ThrowInvalidCharacter(byteValue);

        return DecodeTable[byteValue];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidCharacter(byte value) =>
        throw new ArgumentOutOfRangeException(nameof(value), $"Invalid character '{(char)value}' (0x{value:X2}) for Crockford's Base32");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInputTooLarge(int length) =>
        throw new ArgumentException($"Input too large. Maximum {MaxChars} characters allowed for decoding, but got {length} characters.");

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
