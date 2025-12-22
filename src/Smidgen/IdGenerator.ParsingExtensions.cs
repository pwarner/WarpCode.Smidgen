namespace WarpCode.Smidgen;

/// <summary>
/// Provides extension methods for parsing identifiers from various string formats.
/// </summary>
public static class IdGeneratorParsingExtensions
{
    extension(IdGenerator)
    {
        /// <summary>
        /// Parses a formatted identifier string back to its 128-bit unsigned integer representation.
        /// </summary>
        /// <param name="formattedId">The formatted string to parse.</param>
        /// <param name="formatTemplate">The template string containing placeholder characters.</param>
        /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
        /// <returns>The 128-bit unsigned integer represented by the formatted string.</returns>
        /// <exception cref="FormatException">Thrown when input doesn't match template or contains invalid characters.</exception>
        public static UInt128 ParseFormattedId(ReadOnlySpan<char> formattedId, ReadOnlySpan<char> formatTemplate, char placeholder = IdFormatter.DefaultPlaceholder)
        {
            var formatter = new IdFormatter(formatTemplate, placeholder);
            return formatter.Parse(formattedId);
        }

        /// <summary>
        /// Parses a raw Crockford Base32 string back to its 128-bit unsigned integer representation.
        /// </summary>
        /// <param name="rawStringId">The raw Base32-encoded string to parse.</param>
        /// <returns>The 128-bit unsigned integer represented by the string.</returns>
        /// <exception cref="ArgumentException">Thrown when input is invalid or too large.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid character is encountered.</exception>
        public static UInt128 ParseRawStringId(ReadOnlySpan<char> rawStringId) => CrockfordEncoding.Decode(System.Text.Encoding.ASCII.GetBytes(rawStringId.ToArray()));

        /// <summary>
        /// Attempts to parse a formatted identifier string to its 128-bit unsigned integer representation.
        /// </summary>
        /// <param name="formattedId">The formatted identifier string to parse.</param>
        /// <param name="formatTemplate">The template string containing placeholder characters.</param>
        /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
        /// <param name="result">When this method returns, contains the 128-bit unsigned integer represented by the formatted string, if the conversion succeeded, or zero if the conversion failed.</param>
        /// <returns>true if the conversion was successful; otherwise, false.</returns>
        public static bool TryParseFormattedId(
            ReadOnlySpan<char> formattedId,
            ReadOnlySpan<char> formatTemplate,
            char placeholder,
            out UInt128 result)
        {
            var formatter = new IdFormatter(formatTemplate, placeholder);
            return formatter.TryParse(formattedId, out result);
        }

        /// <summary>
        /// Attempts to parse a formatted identifier string to its 128-bit unsigned integer representation using the default placeholder character.
        /// </summary>
        /// <param name="formattedId">The formatted identifier string to parse.</param>
        /// <param name="formatTemplate">The template string containing placeholder characters.</param>
        /// <param name="result">When this method returns, contains the 128-bit unsigned integer represented by the formatted string, if the conversion succeeded, or zero if the conversion failed.</param>
        /// <returns>true if the conversion was successful; otherwise, false.</returns>
        public static bool TryParseFormattedId(
            ReadOnlySpan<char> formattedId,
            ReadOnlySpan<char> formatTemplate,
            out UInt128 result) =>
            TryParseFormattedId(formattedId, formatTemplate, IdFormatter.DefaultPlaceholder, out result);

        /// <summary>
        /// Attempts to parse a raw Crockford Base32 string to its 128-bit unsigned integer representation.
        /// </summary>
        /// <param name="rawStringId">The raw Base32-encoded identifier string to parse.</param>
        /// <param name="result">When this method returns, contains the 128-bit unsigned integer represented by the string, if the conversion succeeded, or zero if the conversion failed.</param>
        /// <returns>true if the conversion was successful; otherwise, false.</returns>
        public static bool TryParseRawStringId(
            ReadOnlySpan<char> rawStringId,
            out UInt128 result)
        {
            try
            {
                result = ParseRawStringId(rawStringId);
                return true;
            }
            catch
            {
                result = UInt128.Zero;
                return false;
            }
        }
    }
}
