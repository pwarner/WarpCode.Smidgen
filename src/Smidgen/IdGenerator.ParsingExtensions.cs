namespace WarpCode.Smidgen;

/// <summary>
/// Provides static extension methods for parsing 60-bit identifiers from various string formats.
/// </summary>
public static class IdGeneratorParsingExtensions
{
    extension(IdGenerator)
    {
        /// <summary>
        /// Parses a raw Crockford Base32 string (####-####-####) back to its 60-bit signed integer representation.
        /// </summary>
        /// <param name="rawString">The raw Base32-encoded string to parse (format: ####-####-####).</param>
        /// <returns>The 60-bit identifier represented by the string.</returns>
        /// <exception cref="FormatException">Thrown when input is invalid or doesn't match the raw format.</exception>
        /// <remarks>
        /// Example:
        /// <code>
        /// long id = IdGenerator.ParseRawString("A1B2-C3D4-E5F6");
        /// </code>
        /// </remarks>
        public static long ParseRawString(string rawString) =>
            IdFormatter.ParseRawString(rawString);

        /// <summary>
        /// Attempts to parse a raw Crockford Base32 string to its 60-bit signed integer representation.
        /// </summary>
        /// <param name="rawString">The raw Base32-encoded identifier string to parse (format: ####-####-####).</param>
        /// <param name="id">When this method returns, contains the 60-bit identifier represented by the string, if the conversion succeeded, or zero if the conversion failed.</param>
        /// <returns><c>true</c> if the conversion was successful; otherwise, <c>false</c>.</returns>
        public static bool TryParseRawString(string rawString, out long id) =>
            IdFormatter.TryParseRawString(rawString, out id);

        /// <summary>
        /// Parses a formatted identifier string back to its 60-bit signed integer representation.
        /// </summary>
        /// <param name="formattedId">The formatted string to parse.</param>
        /// <param name="template">The template string containing placeholder characters.</param>
        /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
        /// <returns>The 60-bit identifier represented by the formatted string.</returns>
        /// <exception cref="ArgumentException">Thrown when the template is empty.</exception>
        /// <exception cref="FormatException">Thrown when input doesn't match template or contains invalid characters.</exception>
        /// <remarks>
        /// Example:
        /// <code>
        /// long id = IdGenerator.ParseFormattedString("ID-A1B2C3D4E5F6", "ID-############");
        /// </code>
        /// </remarks>
        public static long ParseFormattedString(string formattedId, string template, char placeholder = IdFormatter.DefaultPlaceholder) =>
            IdFormatter.ParseFormattedString(formattedId, template, placeholder);

        /// <summary>
        /// Attempts to parse a formatted identifier string to its 60-bit signed integer representation.
        /// </summary>
        /// <param name="formattedId">The formatted identifier string to parse.</param>
        /// <param name="template">The template string containing placeholder characters.</param>
        /// <param name="placeholder">The character used as a placeholder in the template.</param>
        /// <param name="id">When this method returns, contains the 60-bit identifier represented by the formatted string, if the conversion succeeded, or zero if the conversion failed.</param>
        /// <returns><c>true</c> if the conversion was successful; otherwise, <c>false</c>.</returns>
        public static bool TryParseFormattedString(
            string formattedId,
            string template,
            char placeholder,
            out long id) =>
            IdFormatter.TryParseFormattedString(formattedId, template, placeholder, out id);

        /// <summary>
        /// Attempts to parse a formatted identifier string to its 60-bit signed integer representation using the default placeholder character.
        /// </summary>
        /// <param name="formattedId">The formatted identifier string to parse.</param>
        /// <param name="template">The template string containing placeholder characters.</param>
        /// <param name="id">When this method returns, contains the 60-bit identifier represented by the formatted string, if the conversion succeeded, or zero if the conversion failed.</param>
        /// <returns><c>true</c> if the conversion was successful; otherwise, <c>false</c>.</returns>
        public static bool TryParseFormattedString(
            string formattedId,
            string template,
            out long id) =>
            TryParseFormattedString(formattedId, template, IdFormatter.DefaultPlaceholder, out id);
    }
}
