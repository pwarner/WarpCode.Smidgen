using System.Runtime.CompilerServices;

namespace WarpCode.Smidgen;

/// <summary>
/// Formats and parses identifiers using customizable templates with Crockford Base32 encoding.
/// </summary>
internal static class IdFormatter
{
    public const char DefaultPlaceholder = '#';
    private const byte MaxPlaceholders = 26; // 26 * 5 = 130 bits > 128 bits

    /// <summary>
    /// Converts the specified 128-bit unsigned integer to a formatted string representation.
    /// </summary>
    /// <param name="id">The 128-bit unsigned integer to convert.</param>
    /// <param name="formatTemplate">The template string containing placeholder characters.</param>
    /// <param name="placeholder">The character used as a placeholder in the template.</param>
    /// <returns>A formatted string representation of the specified value.</returns>
    /// <exception cref="ArgumentException">Thrown when the template is empty.</exception>
    /// <exception cref="FormatException">Thrown when the template has insufficient placeholders.</exception>
    public static string Format(UInt128 id, ReadOnlySpan<char> formatTemplate, char placeholder = DefaultPlaceholder)
    {
        if (formatTemplate.IsEmpty)
            throw new ArgumentException("Format template cannot be empty.", nameof(formatTemplate));

        // From right to left, generate output by replacing placeholders with encoded characters
        Span<char> output = stackalloc char[formatTemplate.Length];

        for (var i = formatTemplate.Length - 1; i >= 0; i--)
        {
            var templateChar = formatTemplate[i];
            
            output[i] = (templateChar == placeholder, id != UInt128.Zero) switch
            {
                (true, true) => ExtractAndShift(ref id),
                (true, false) => '0',
                (false, _) => templateChar
            };
        }

        // Check if we've consumed all input
        if (id != UInt128.Zero)
        {
            // Calculate how many characters are left to encode
            var remainingBits = 128 - (int)UInt128.LeadingZeroCount(id);
            var missingPlaceholders = (remainingBits + 4) / 5;
            throw new FormatException($"Format template is missing {missingPlaceholders} placeholders causing truncation.");
        }

        return new string(output);
    }

    /// <summary>
    /// Converts the specified formatted string to a 128-bit unsigned integer.
    /// </summary>
    /// <param name="formattedId">The formatted string to convert.</param>
    /// <param name="formatTemplate">The template string containing placeholder characters.</param>
    /// <param name="placeholder">The character used as a placeholder in the template.</param>
    /// <returns>The 128-bit unsigned integer represented by the formatted string.</returns>
    /// <exception cref="ArgumentException">Thrown when the template is empty or input exceeds maximum length.</exception>
    /// <exception cref="FormatException">Thrown when input doesn't match template or contains invalid characters.</exception>
    public static UInt128 Parse(ReadOnlySpan<char> formattedId, ReadOnlySpan<char> formatTemplate, char placeholder = DefaultPlaceholder)
    {
        if (formatTemplate.IsEmpty)
            throw new ArgumentException("Format template cannot be empty.", nameof(formatTemplate));

        if (formattedId.Length != formatTemplate.Length)
            throw new FormatException("Input length does not match format template length.");

        // Count placeholders to check maximum length
        var placeholderCount = formatTemplate.Count(placeholder);
        if (placeholderCount > MaxPlaceholders)
            ThrowInputTooLarge(placeholderCount);

        UInt128 value = UInt128.Zero;

        for (var i = 0; i < formatTemplate.Length; i++)
        {
            var templateChar = formatTemplate[i];
            var inputChar = formattedId[i];

            if (templateChar.Equals(placeholder))
            {
                value <<= 5;
                value |= DecodeChar(inputChar);
            }
            else if (!templateChar.Equals(inputChar))
                throw new FormatException($"Input does not match format template. Expected '{templateChar}' but got '{inputChar}'.");
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char ExtractAndShift(ref UInt128 value)
    {
        var result = (char)CrockfordEncoding.EncodeTable[(int)(value & 31u)];
        value >>= 5;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte DecodeChar(char value)
    {
        var byteValue = (byte)value;
        if (byteValue >= CrockfordEncoding.DecodeTable.Length || !CrockfordEncoding.ValidCharacters.Contains(byteValue))
            ThrowInvalidCharacter(byteValue);

        return CrockfordEncoding.DecodeTable[byteValue];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidCharacter(byte value) =>
        throw new ArgumentOutOfRangeException(nameof(value), $"Invalid character '{(char)value}' (0x{value:X2}) for Crockford's Base32");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInputTooLarge(int length) =>
        throw new ArgumentException($"Input too large. Maximum {MaxPlaceholders} characters allowed for decoding, but got {length} characters.");
}
