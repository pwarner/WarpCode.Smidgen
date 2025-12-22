namespace WarpCode.Smidgen;

/// <summary>
/// Formats and parses identifiers using customizable templates with Crockford Base32 encoding.
/// </summary>
internal readonly ref struct IdFormatter
{
    public const char DefaultPlaceholder = '#';
    private const char Zero = '0';
    private readonly int _size;
    private readonly ReadOnlySpan<char> _formatTemplate;
    private readonly char _placeholder;

    public IdFormatter(ReadOnlySpan<char> formatTemplate, char placeholder)
    {
        if (formatTemplate.IsEmpty)
            throw new ArgumentException("Format template cannot be empty.", nameof(formatTemplate));

        _size = formatTemplate.Count(placeholder);
        _formatTemplate = formatTemplate;
        _placeholder = placeholder;
    }

    public IdFormatter(int size, ReadOnlySpan<char> formatTemplate, char placeholder)
    {
        if (formatTemplate.IsEmpty)
            throw new ArgumentException("Format template cannot be empty.", nameof(formatTemplate));

        var placeholderCount = formatTemplate.Count(placeholder);
        if (placeholderCount > size)
        {
            throw new ArgumentException(
                $"Format template contains {placeholderCount} placeholders, but the maximum allowed is {size}.",
                nameof(formatTemplate));
        }

        _size = size;
        _formatTemplate = formatTemplate;
        _placeholder = placeholder;
    }

    /// <summary>
    /// Converts the specified 128-bit unsigned integer to a formatted string representation.
    /// </summary>
    /// <param name="id">The 128-bit unsigned integer to convert.</param>
    /// <returns>A formatted string representation of the specified value.</returns>
    /// <exception cref="ArgumentException">Thrown when the template is empty.</exception>
    /// <exception cref="FormatException">Thrown when the template has insufficient placeholders.</exception>
    public string Format(UInt128 id)
    {
        Span<byte> encodedBytes = stackalloc byte[_size];
        var length = CrockfordEncoding.Encode(id, encodedBytes);
        encodedBytes = encodedBytes[..length];

        // From right to left, generate output by replacing placeholders with encoded input
        Span<char> output = stackalloc char[_formatTemplate.Length];

        for (var i = _formatTemplate.Length - 1; i >= 0; i--)
        {
            output[i] = (_formatTemplate[i], length) switch
            {
                (var c, > 0) when c.Equals(_placeholder) => (char)encodedBytes[--length],
                (var c, 0) when c.Equals(_placeholder) => Zero,
                _ => _formatTemplate[i]
            };
        }

        // Check if we've consumed all input or only have leading zeros
        if (length is 0)
            return new string(output);

        throw new FormatException($"Format template is missing {length} placeholders causing truncation.");
    }

    /// <summary>
    /// Converts the specified formatted string to a 128-bit unsigned integer.
    /// </summary>
    /// <param name="formattedId">The formatted string to convert.</param>
    /// <returns>The 128-bit unsigned integer represented by the formatted string.</returns>
    /// <exception cref="FormatException">Thrown when input doesn't match template or contains invalid characters.</exception>
    public UInt128 Parse(ReadOnlySpan<char> formattedId)
    {
        if (formattedId.Length != _formatTemplate.Length)
            throw new FormatException("Input length does not match format template length.");

        Span<byte> encodedBytes = stackalloc byte[_size];
        var encodedCount = 0;

        for (var i = 0; i < _formatTemplate.Length; i++)
        {
            var templateChar = _formatTemplate[i];
            var inputChar = formattedId[i];

            if (templateChar.Equals(_placeholder))
                encodedBytes[encodedCount++] = (byte)inputChar;

            else if (!templateChar.Equals(inputChar))
                throw new FormatException($"Input does not match format template. Expected '{templateChar}' but got '{inputChar}'.");
        }

        return CrockfordEncoding.Decode(encodedBytes[..encodedCount]);
    }

    /// <summary>
    /// Attempts to convert the specified formatted string to a 128-bit unsigned integer.
    /// </summary>
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
}
