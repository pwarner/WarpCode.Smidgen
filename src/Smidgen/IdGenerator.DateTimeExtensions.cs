namespace WarpCode.Smidgen;

/// <summary>
/// Provides extension methods for extracting DateTime values from identifiers.
/// </summary>
public static class IdGeneratorDateTimeExtensions
{
    extension(IdGenerator self)
    {
        /// <summary>
        /// Extracts the DateTime from a 128-bit unsigned integer identifier.
        /// </summary>
        /// <param name="id">The identifier to extract the DateTime from.</param>
        /// <returns>The DateTime value encoded in the identifier's time component.</returns>
        public DateTime ExtractDateTime(UInt128 id)
        {
            var timeValue = (ulong)(id >> self.EntropyBits);

            return self.TimeAccuracy switch
            {
                TimeAccuracy.Seconds => self.Since.AddSeconds(timeValue),
                TimeAccuracy.Milliseconds => self.Since.AddMilliseconds(timeValue),
                TimeAccuracy.Microseconds => self.Since.AddTicks((long)timeValue * 10), // 1 microsecond = 10 ticks
                TimeAccuracy.Ticks => self.Since.AddTicks((long)timeValue),
                _ => throw new InvalidOperationException($"Unsupported time accuracy: {self.TimeAccuracy}")
            };
        }

        /// <summary>
        /// Extracts the DateTime from a formatted identifier string.
        /// </summary>
        /// <param name="formattedId">The formatted identifier string.</param>
        /// <param name="formatTemplate">The template string containing placeholder characters.</param>
        /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
        /// <returns>The DateTime value encoded in the identifier's time component.</returns>
        /// <exception cref="FormatException">Thrown when input doesn't match template or contains invalid characters.</exception>
        public DateTime ExtractDateTime(string formattedId, string formatTemplate, char placeholder = IdFormatter.DefaultPlaceholder)
        {
            UInt128 id = IdGenerator.ParseFormattedId(formattedId, formatTemplate, placeholder);
            return self.ExtractDateTime(id);
        }

        /// <summary>
        /// Extracts the DateTime from a raw Crockford Base32 identifier string.
        /// </summary>
        /// <param name="rawStringId">The raw Base32-encoded identifier string.</param>
        /// <returns>The DateTime value encoded in the identifier's time component.</returns>
        /// <exception cref="ArgumentException">Thrown when input is invalid or too large.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid character is encountered.</exception>
        public DateTime ExtractDateTime(string rawStringId)
        {
            UInt128 id = IdGenerator.ParseRawStringId(rawStringId);
            return self.ExtractDateTime(id);
        }
    }
}
