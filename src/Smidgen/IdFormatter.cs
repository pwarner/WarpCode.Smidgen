namespace WarpCode.Smidgen;

public readonly struct IdFormatter(string formatTemplate, char placeholder = '#')
{
    private const char Zero = '0';

    /// <summary>
    /// Converts the specified 64-bit unsigned integer to a formatted string representation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method uses Crockford encoding to generate characters and replaces placeholders in
    /// the <see cref="formatTemplate"/> with the encoded characters.
    /// </para>
    /// <para>
    /// The process works from right (least significant) to left (most significant). Non-placeholder characters are simply copied.
    /// </para>
    /// <para>
    /// If there are more placeholders than can be filled (e.g greater than 13) the placeholders will be replaced by '0'.
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
    public string ToFormattedString(ulong smallId)
    {
        Span<byte> input = stackalloc byte[13];
        var encoded = CrockfordEncoding.Encode(smallId, input);
        return string.Create(formatTemplate.Length, (ReadOnlySpan<byte>)input[^encoded..], ResolveTemplate);
    }

    private void ResolveTemplate(Span<char> output, ReadOnlySpan<byte> input)
    {
        // From right to left, generate an output by replacing placeholders with inputs.
        ReadOnlySpan<char> template = formatTemplate;
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
        if (!templateChar.Equals(placeholder)) return templateChar;

        if (input is []) return Zero;

        var next = input[^1];
        input = input[..^1];

        return (char)next;
    }
}
