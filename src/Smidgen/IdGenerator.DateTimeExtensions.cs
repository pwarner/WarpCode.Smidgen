namespace WarpCode.Smidgen;

/// <summary>
/// Provides extension methods for extracting DateTime values from 60-bit identifiers.
/// </summary>
public static class IdGeneratorDateTimeExtensions
{
    extension(IdGenerator self)
    {
        /// <summary>
        /// Extracts the DateTime from a 60-bit identifier.
        /// </summary>
        /// <param name="id">The identifier to extract the DateTime from.</param>
        /// <returns>The DateTime value encoded in the identifier's time component.</returns>
        /// <remarks>
        /// The time component is stored in bits 20-59 and represents (elapsedTicks >> 16) to provide ~6.5ms precision.
        /// This method reverses the process:
        /// <code>
        /// reducedInterval = (id >> 20) &amp; 0xFFFFFFFFFF  // Extract 40-bit interval from bits 20-59
        /// elapsedTicks = reducedInterval << 16            // Restore to tick scale (loses lower 16 bits)
        /// dateTime = epoch + elapsedTicks
        /// </code>
        /// The precision loss of ~6.5ms (65536 ticks) is inherent to the ID format.
        /// </remarks>
        public DateTime ExtractDateTime(long id)
        {
            // Extract the 40-bit interval from bits 20-59
            // This represents (elapsedTicks >> 16), so we need to shift left by 16 to get back the approximate elapsed ticks
            long reducedInterval = (id >> 20) & 0xFFFFFFFFFF;

            // Restore precision: shift left by 16 to convert from intervals back to ticks
            // Note: This loses the precision of the lower 16 bits, which is expected (~6.5ms intervals)
            long elapsedTicks = reducedInterval << 16;

            // Add elapsed ticks to epoch
            return self.Epoch.AddTicks(elapsedTicks);
        }

        /// <summary>
        /// Extracts the DateTime from a formatted identifier string.
        /// </summary>
        /// <param name="formattedString">The formatted identifier string.</param>
        /// <param name="template">The template string containing placeholder characters.</param>
        /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
        /// <returns>The DateTime value encoded in the identifier's time component.</returns>
        /// <exception cref="FormatException">Thrown when input doesn't match template or contains invalid characters.</exception>
        public DateTime ExtractDateTime(string formattedString, string template, char placeholder = IdFormatter.DefaultPlaceholder)
        {
            long id = IdGenerator.ParseFormattedString(formattedString, template, placeholder);
            return self.ExtractDateTime(id);
        }

        /// <summary>
        /// Extracts the DateTime from a raw Crockford Base32 identifier string (####-####-####).
        /// </summary>
        /// <param name="rawString">The raw Base32-encoded identifier string.</param>
        /// <returns>The DateTime value encoded in the identifier's time component.</returns>
        /// <exception cref="FormatException">Thrown when input is invalid or doesn't match the raw format.</exception>
        public DateTime ExtractDateTime(string rawString)
        {
            long id = IdGenerator.ParseRawString(rawString);
            return self.ExtractDateTime(id);
        }
    }
}
