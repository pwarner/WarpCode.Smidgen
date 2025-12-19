namespace WarpCode.Smidgen;

/// <summary>
/// Formats and parses identifiers using customizable templates with Crockford Base32 encoding.
/// </summary>
public readonly struct IdFormatter
{
    private const char Zero = '0';
    private const int MaxPlaceholders = 26; // 26 * 5 bits = 130 bits > 128 bits of UInt128
    private readonly string _formatTemplate;
    private readonly char _placeholder;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdFormatter"/> struct with the specified format template and placeholder character.
    /// </summary>
    /// <param name="formatTemplate">The template string containing placeholder characters that will be replaced with encoded identifier characters.</param>
    /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatTemplate"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the template contains more than 26 placeholders.</exception>
    public IdFormatter(string formatTemplate, char placeholder = '#')
    {
        ArgumentNullException.ThrowIfNull(formatTemplate);

        var placeholderCount = formatTemplate.Count(c => c == placeholder);
        if (placeholderCount > MaxPlaceholders)
        {
            throw new ArgumentException(
                $"Format template contains {placeholderCount} placeholders, but the maximum allowed is {MaxPlaceholders}.",
                nameof(formatTemplate));
        }

        _formatTemplate = formatTemplate;
        _placeholder = placeholder;
    }

    /// <summary>
    /// Converts the specified 128-bit unsigned integer to a formatted string representation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method uses Crockford encoding to generate characters and replaces placeholders in
    /// the format template with the encoded characters.
    /// </para>
    /// <para>
    /// The process works from right (least significant) to left (most significant). Non-placeholder characters are simply copied.
    /// </para>
    /// <para>
    /// If there are fewer placeholders than 26, the process will complete without error if all remaining encoded characters are '0'.
    /// </para>
    /// <para>
    /// Otherwise a <see cref="FormatException"/> will be thrown.
    /// </para>
    /// </remarks>
    /// <param name="id">The 128-bit unsigned integer to convert.</param>
    /// <returns>A formatted string representation of the specified value.</returns>
    public string Format(UInt128 id)
    {
        Span<byte> encoded = stackalloc byte[26];
        var length = CrockfordEncoding.Encode(id, encoded);

#if NET10_0_OR_GREATER
        // .NET 10+: Use zero-allocation string.Create with ReadOnlySpan<byte> state
        return string.Create(_formatTemplate.Length, (ReadOnlySpan<byte>)encoded[..length], ResolveTemplate);
#else
        // .NET 8: Copy to array and capture struct fields to avoid 'this' capture
        var encodedArray = encoded[..length].ToArray();
        var template = _formatTemplate;
        var placeholder = _placeholder;
        
        return string.Create(_formatTemplate.Length, (template, placeholder, encodedArray), 
            static (output, state) =>
            {
                ReadOnlySpan<char> template = state.template;
                var placeholder = state.placeholder;
                ReadOnlySpan<byte> input = state.encodedArray;
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
            });
#endif
    }

#if !NET10_0_OR_GREATER
    private void ResolveTemplateFromArray(Span<char> output, byte[] input)
    {
        ReadOnlySpan<char> template = _formatTemplate;
        var inputIndex = input.Length - 1;

        for (var i = template.Length - 1; i >= 0; i--)
        {
            output[i] = template[i].Equals(_placeholder)
                ? inputIndex >= 0
                    ? (char)input[inputIndex--]
                    : Zero
                : template[i];
        }

        // Check if we've consumed all input or only have leading zeros
        if (inputIndex < 0 || input.AsSpan()[..(inputIndex + 1)].IndexOfAnyExcept((byte)Zero) == -1)
            return;

        var missing = inputIndex + 1;
        throw new FormatException($"Format template is missing {missing} placeholders causing truncation.");
    }
#endif

    /// <summary>
    /// Converts the specified formatted string to a 128-bit unsigned integer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method extracts Crockford-encoded characters from positions that correspond to
    /// placeholders in the format template and decodes them back to the original value.
    /// </para>
    /// <para>
    /// Non-placeholder characters in the input must match the template exactly.
    /// </para>
    /// <para>
    /// If the input length does not match the template length or if non-placeholder characters
    /// do not match, a <see cref="FormatException"/> will be thrown.
    /// </para>
    /// </remarks>
    /// <param name="formattedId">The formatted string to convert.</param>
    /// <returns>The 128-bit unsigned integer represented by the formatted string.</returns>
    public UInt128 Parse(ReadOnlySpan<char> formattedId)
    {
        if (formattedId.Length != _formatTemplate.Length)
            throw new FormatException("Input length does not match format template length.");

        Span<byte> encodedBytes = stackalloc byte[MaxPlaceholders];
        var encodedCount = 0;

        ReadOnlySpan<char> template = _formatTemplate;
        while (template.Length > 0)
        {
            var templateChar = template[0];
            var inputChar = formattedId[0];

            if (templateChar.Equals(_placeholder))
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
    /// <remarks>
    /// <para>
    /// The method extracts Crockford-encoded characters from positions that correspond to
    /// placeholders in the format template and decodes them back to the original value.
    /// </para>
    /// <para>
    /// Non-placeholder characters in the input must match the template exactly.
    /// </para>
    /// <para>
    /// If the input length does not match the template length, if non-placeholder characters
    /// do not match, or if the encoded characters are invalid, the method returns false.
    /// </para>
    /// </remarks>
    /// <param name="formattedId">The formatted string to convert.</param>
    /// <param name="result">When this method returns, contains the 128-bit unsigned integer represented by the formatted string, if the conversion succeeded, or zero if the conversion failed.</param>
    /// <returns>true if the conversion was successful; otherwise, false.</returns>
    public bool TryParse(ReadOnlySpan<char> formattedId, out UInt128 result)
    {
        try
        {
            result = Parse(formattedId);
            return true;
        }
        catch
        {
            result = UInt128.Zero;
            return false;
        }
    }

    private void ResolveTemplate(Span<char> output, ReadOnlySpan<byte> input)
    {
        // From right to left, generate output by replacing placeholders with encoded input
        ReadOnlySpan<char> template = _formatTemplate;
        var inputIndex = input.Length - 1;

        for (var i = template.Length - 1; i >= 0; i--)
        {
            output[i] = template[i].Equals(_placeholder)
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
