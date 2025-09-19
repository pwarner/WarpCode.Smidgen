namespace WarpCode.Smidgen;

public readonly struct IdFormatter(string formatTemplate, char empty = '*', char placeholder = '*')
{
    public static readonly IdFormatter Fixed = new("*************", '0'); // fixed length with leading zeros.
    public static readonly IdFormatter Short = new("*************", '\0'); // no leading zeros, will be variable length.
    public string ToFormattedString(ulong smallId)
    {
        Span<byte> input = stackalloc byte[13];
        input.Clear(); // zero out the buffer.
        Bit32Encoding.Encode(smallId, input);
        return string.Create(formatTemplate.Length, (ReadOnlySpan<byte>)input, ResolveTemplate);
    }

    private void ResolveTemplate(Span<char> output, ReadOnlySpan<byte> input)
    {
        // From right to left, generate an output by replacing placeholders with input bytes.
        ReadOnlySpan<char> template = formatTemplate;
        while (template.Length > 0)
        {
            output[^1] = ResolveTemplatePosition(template[^1], ref input);
            output = output[..^1];
            template = template[..^1];
        }

        if (input is [] or [.., 0]) return;

        var missing = input.Length - input.Count((byte)0);
        throw new Exception($"Format template is missing {missing} placeholders causing truncation.");
    }

    private char ResolveTemplatePosition(char templateChar, ref ReadOnlySpan<byte> input)
    {
        if (input is [] || !templateChar.Equals(placeholder)) return templateChar;

        var next = input[^1];
        input = input[..^1];

        return next is 0 ? empty : (char)next;
    }
}