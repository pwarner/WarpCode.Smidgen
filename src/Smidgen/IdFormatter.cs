namespace WarpCode.Smidgen;

/// <summary>
/// Formats and parses identifiers using customizable templates with Crockford Base32 encoding.
/// </summary>
internal static class IdFormatter
{
    private const char Zero = '0';
    private const int MaxPlaceholders = 26; // 26 * 5 bits = 130 bits > 128 bits of UInt128

    /// <summary>
    /// Converts the specified 128-bit unsigned integer to a formatted string representation.
    /// </summary>
    /// <param name="id">The 128-bit unsigned integer to convert.</param>
    /// <param name="base32Size">The expected Base32 size for efficient stackalloc.</param>
    /// <param name="formatTemplate">The template string containing placeholder characters that will be replaced with encoded identifier characters.</param>
    /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
    /// <returns>A formatted string representation of the specified value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatTemplate"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the template contains more than 26 placeholders.</exception>
    /// <exception cref="FormatException">Thrown when the template has insufficient placeholders.</exception>
    public static string Format(UInt128 id, int base32Size, string formatTemplate, char placeholder = '#')
    {
        ArgumentNullException.ThrowIfNull(formatTemplate);

        var placeholderCount = formatTemplate.Count(c => c == placeholder);
        if (placeholderCount > MaxPlaceholders)
        {
            throw new ArgumentException(
                $"Format template contains {placeholderCount} placeholders, but the maximum allowed is {MaxPlaceholders}.",
                nameof(formatTemplate));
        }

        Span<byte> encoded = stackalloc byte[base32Size];
        var length = CrockfordEncoding.Encode(id, encoded);

        // Validate that template has enough placeholders for the encoded value
        // Skip validation only if the encoded value is all zeros (which should never happen with real IDs)
        if (placeholderCount < length)
        {
            var hasNonZero = encoded[..length].IndexOfAnyExcept((byte)'0') != -1;
            if (hasNonZero)
            {
                throw new FormatException(
                    $"Format template has insufficient placeholders. Template has {placeholderCount} placeholders but needs at least {length} to encode the value without truncation.");
            }
        }

#if NET10_0_OR_GREATER
        // .NET 10+: Use zero-allocation string.Create with ReadOnlySpan<byte> state
        return string.Create(formatTemplate.Length, (ReadOnlySpan<byte>)encoded[..length],
            (output, input) => ResolveTemplate(output, input, formatTemplate, placeholder));
#else
        // .NET 8: Copy to array due to SpanAction limitation in this runtime
        var encodedArray = encoded[..length].ToArray();

        return string.Create(formatTemplate.Length, encodedArray,
            (output, input) => ResolveTemplate(output, input, formatTemplate, placeholder));
#endif
    }

    /// <summary>
    /// Converts the specified formatted string to a 128-bit unsigned integer.
    /// </summary>
    /// <param name="formattedId">The formatted string to convert.</param>
    /// <param name="formatTemplate">The template string containing placeholder characters.</param>
    /// <param name="placeholder">The character used as a placeholder in the template.</param>
    /// <returns>The 128-bit unsigned integer represented by the formatted string.</returns>
    /// <exception cref="FormatException">Thrown when input doesn't match template or contains invalid characters.</exception>
    public static UInt128 Parse(ReadOnlySpan<char> formattedId, string formatTemplate, char placeholder = '#')
    {
        if (formattedId.Length != formatTemplate.Length)
            throw new FormatException("Input length does not match format template length.");

        Span<byte> encodedBytes = stackalloc byte[MaxPlaceholders];
        var encodedCount = 0;

        ReadOnlySpan<char> template = formatTemplate;
        while (template.Length > 0)
        {
            var templateChar = template[0];
            var inputChar = formattedId[0];

            if (templateChar.Equals(placeholder))
                encodedBytes[encodedCount++] = (byte)inputChar;

            else if (!templateChar.Equals(inputChar))
                throw new FormatException($"Input does not match format template. Expected '{templateChar}' but got '{inputChar}'.");

            template = template[1..];
            formattedId = formattedId[1..];
        }

        return CrockfordEncoding.Decode(encodedBytes[..encodedCount]);
    }

    /// <summary>
    /// Attempts to convert the specified formatted string to a 128-bit unsigned integer.
    /// </summary>
    /// <param name="formattedId">The formatted string to convert.</param>
    /// <param name="formatTemplate">The template string containing placeholder characters.</param>
    /// <param name="placeholder">The character used as a placeholder in the template.</param>
    /// <param name="result">When this method returns, contains the 128-bit unsigned integer represented by the formatted string, if the conversion succeeded, or zero if the conversion failed.</param>
    /// <returns>true if the conversion was successful; otherwise, false.</returns>
    public static bool TryParse(ReadOnlySpan<char> formattedId, string formatTemplate, char placeholder, out UInt128 result)
    {
        try
        {
            result = Parse(formattedId, formatTemplate, placeholder);
            return true;
        }
        catch
        {
            result = UInt128.Zero;
            return false;
        }
    }

    private static void ResolveTemplate(Span<char> output, ReadOnlySpan<byte> input, string formatTemplate, char placeholder)
    {
        // From right to left, generate output by replacing placeholders with encoded input
        ReadOnlySpan<char> template = formatTemplate;
        var inputIndex = input.Length - 1;

        for (var i = template.Length - 1; i >= 0; i--)
        {
            output[i] = template[i].Equals(placeholder)
                ? inputIndex >= 0
                    ? (char)input[inputIndex--]
                    : Zero
                : template[i];
        }

        // Check if we've consumed all input or only have leading zeros
        if (inputIndex < 0 || input[..(inputIndex + 1)].IndexOfAnyExcept((byte)Zero) == -1)
            return;

        var missing = inputIndex + 1;
        throw new FormatException($"Format template is missing {missing} placeholders causing truncation.");
    }
}
