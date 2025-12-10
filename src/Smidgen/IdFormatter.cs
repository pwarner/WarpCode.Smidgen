namespace WarpCode.Smidgen;

public readonly struct IdFormatter
{
    private const char Zero = '0';
    private const int MaxPlaceholders = 13; // 13 * 5 bits = 65 bits > 64 bits of ulong
    private readonly string _formatTemplate;
    private readonly char _placeholder;

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
    /// Converts the specified 64-bit unsigned integer to a formatted string representation.
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
    /// If there are fewer placeholders than 13, the process will complete without error if all remaining encoded characters are '0'.
    /// </para>
    /// <para>
    /// Otherwise a <see cref="FormatException"/> will be thrown.
    /// </para>
    /// </remarks>
    /// <param name="smallId">The 64-bit unsigned integer to convert.</param>
    /// <returns>A formatted string representation of the specified value.</returns>
    public string Format(ulong smallId)
    {
        Span<byte> input = stackalloc byte[13];
        var encoded = CrockfordEncoding.Encode(smallId, input);
        return string.Create(_formatTemplate.Length, (ReadOnlySpan<byte>)input[^encoded..], ResolveTemplate);
    }

    /// <summary>
    /// Converts the specified formatted string to a 64-bit unsigned integer.
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
    /// <returns>The 64-bit unsigned integer represented by the formatted string.</returns>
    public ulong Parse(ReadOnlySpan<char> formattedId)
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
    /// Attempts to convert the specified formatted string to a 64-bit unsigned integer.
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
    /// <param name="result">When this method returns, contains the 64-bit unsigned integer represented by the formatted string, if the conversion succeeded, or zero if the conversion failed.</param>
    /// <returns>true if the conversion was successful; otherwise, false.</returns>
    public bool TryParse(ReadOnlySpan<char> formattedId, out ulong result)
    {
        try
        {
            result = Parse(formattedId);
            return true;
        }
        catch
        {
            result = 0;
            return false;
        }
    }

    private void ResolveTemplate(Span<char> output, ReadOnlySpan<byte> input)
    {
        // From right to left, generate an output by replacing placeholders with inputs.
        ReadOnlySpan<char> template = _formatTemplate;
        while (template.Length > 0)
        {
            output[^1] = CopyOrReplace(template[^1], ref input);
            output = output[..^1];
            template = template[..^1];
        }

        if (input is [] || !input.ContainsAnyExcept((byte)Zero)) return;

        var missing = input.Length - input.Count((byte)Zero);
        throw new FormatException($"Format template is missing {missing} placeholders causing truncation.");
    }

    private char CopyOrReplace(char templateChar, ref ReadOnlySpan<byte> input)
    {
        if (!templateChar.Equals(_placeholder)) return templateChar;

        if (input is []) return Zero;

        var next = input[^1];
        input = input[..^1];

        return (char)next;
    }
}
