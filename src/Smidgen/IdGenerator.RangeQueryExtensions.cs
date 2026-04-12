namespace WarpCode.Smidgen;

/// <summary>
/// Provides extension methods for generating minimum and maximum identifiers for range queries.
/// All min/max bounds are inclusive and suitable for database WHERE clauses.
/// </summary>
/// <remarks>
/// Example usage for database queries:
/// <code>
/// var (minId, maxId) = generator.GetIdRange(startDate, endDate);
/// // SQL: WHERE id >= minId AND id &lt;= maxId
/// </code>
/// </remarks>
public static class IdGeneratorRangeQueryExtensions
{
    extension(IdGenerator self)
    {
        /// <summary>
        /// Gets the minimum possible identifier for a given DateTime.
        /// The entropy component is set to zero.
        /// </summary>
        /// <param name="dateTime">The date/time to create the minimum ID for.</param>
        /// <returns>The minimum 60-bit identifier for the given DateTime (inclusive).</returns>
        /// <remarks>
        /// This method creates an ID with the time component set to the specified DateTime
        /// and the entropy component set to zero, representing the smallest possible ID
        /// for that time interval.
        /// </remarks>
        public long GetMinId(DateTime dateTime)
        {
            long elapsedTicks = dateTime.Ticks - self.Epoch.Ticks;

            // Ensure non-negative
            if (elapsedTicks < 0)
                elapsedTicks = 0;

            // Generate interval: shift left 4 bits, clear lower 20 bits (same as ID generation)
            // Mask keeps bits 20-59 (40 bits for interval)
            const long IntervalMask = 0x0FFFFFFFFFF00000L;
            long interval = (elapsedTicks << 4) & IntervalMask;

            // Entropy is zero for minimum ID
            return interval;
        }

        /// <summary>
        /// Gets the maximum possible identifier for a given DateTime.
        /// The entropy component is set to all ones.
        /// </summary>
        /// <param name="dateTime">The date/time to create the maximum ID for.</param>
        /// <returns>The maximum 60-bit identifier for the given DateTime (inclusive).</returns>
        /// <remarks>
        /// This method creates an ID with the time component set to the specified DateTime
        /// and the entropy component set to the maximum value (0xFFFFF), representing the 
        /// largest possible ID for that time interval.
        /// </remarks>
        public long GetMaxId(DateTime dateTime)
        {
            long elapsedTicks = dateTime.Ticks - self.Epoch.Ticks;

            // Ensure non-negative
            if (elapsedTicks < 0)
                elapsedTicks = 0;

            // Generate interval: shift left 4 bits, clear lower 20 bits
            // Mask keeps bits 20-59 (40 bits for interval)
            const long IntervalMask = 0x0FFFFFFFFFF00000L;
            long interval = (elapsedTicks << 4) & IntervalMask;

            // Entropy is maximum (20 bits of 1s)
            const long MaxEntropy = 0xFFFFF;
            return interval | MaxEntropy;
        }

        /// <summary>
        /// Gets the minimum and maximum possible identifiers for a given DateTime range.
        /// Both bounds are inclusive.
        /// </summary>
        /// <param name="startDateTime">The start date/time of the range.</param>
        /// <param name="endDateTime">The end date/time of the range.</param>
        /// <returns>A tuple containing the minimum and maximum identifiers for the given range (both inclusive).</returns>
        /// <remarks>
        /// Use this method to generate IDs for database range queries:
        /// <code>
        /// var (minId, maxId) = generator.GetIdRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        /// // SQL: SELECT * FROM table WHERE id >= @minId AND id &lt;= @maxId
        /// </code>
        /// </remarks>
        public (long minId, long maxId) GetIdRange(DateTime startDateTime, DateTime endDateTime) =>
            (self.GetMinId(startDateTime), self.GetMaxId(endDateTime));

        /// <summary>
        /// Gets the minimum possible raw string identifier (####-####-####) for a given DateTime.
        /// The entropy component is set to zero.
        /// </summary>
        /// <param name="dateTime">The date/time to create the minimum ID for.</param>
        /// <returns>A raw Crockford Base32 string representation of the minimum identifier for the given DateTime (inclusive).</returns>
        public string GetMinRawString(DateTime dateTime)
        {
            long minId = self.GetMinId(dateTime);
            return IdFormatter.EncodeToRawString(minId);
        }

        /// <summary>
        /// Gets the maximum possible raw string identifier (####-####-####) for a given DateTime.
        /// The entropy component is set to all ones.
        /// </summary>
        /// <param name="dateTime">The date/time to create the maximum ID for.</param>
        /// <returns>A raw Crockford Base32 string representation of the maximum identifier for the given DateTime (inclusive).</returns>
        public string GetMaxRawString(DateTime dateTime)
        {
            long maxId = self.GetMaxId(dateTime);
            return IdFormatter.EncodeToRawString(maxId);
        }

        /// <summary>
        /// Gets the minimum and maximum possible raw string identifiers for a given DateTime range.
        /// Both bounds are inclusive.
        /// </summary>
        /// <param name="startDateTime">The start date/time of the range.</param>
        /// <param name="endDateTime">The end date/time of the range.</param>
        /// <returns>A tuple containing the minimum and maximum raw string identifiers for the given range (both inclusive).</returns>
        public (string minId, string maxId) GetRawStringRange(DateTime startDateTime, DateTime endDateTime) =>
            (self.GetMinRawString(startDateTime), self.GetMaxRawString(endDateTime));

        /// <summary>
        /// Gets the minimum possible formatted identifier string for a given DateTime.
        /// The entropy component is set to zero.
        /// </summary>
        /// <param name="dateTime">The date/time to create the minimum ID for.</param>
        /// <param name="template">The template string containing placeholder characters.</param>
        /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
        /// <returns>A formatted string representation of the minimum identifier for the given DateTime (inclusive).</returns>
        public string GetMinFormattedString(DateTime dateTime, string template, char placeholder = IdFormatter.DefaultPlaceholder)
        {
            long minId = self.GetMinId(dateTime);
            return IdFormatter.EncodeToFormattedString(minId, template, placeholder);
        }

        /// <summary>
        /// Gets the maximum possible formatted identifier string for a given DateTime.
        /// The entropy component is set to all ones.
        /// </summary>
        /// <param name="dateTime">The date/time to create the maximum ID for.</param>
        /// <param name="template">The template string containing placeholder characters.</param>
        /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
        /// <returns>A formatted string representation of the maximum identifier for the given DateTime (inclusive).</returns>
        public string GetMaxFormattedString(DateTime dateTime, string template, char placeholder = IdFormatter.DefaultPlaceholder)
        {
            long maxId = self.GetMaxId(dateTime);
            return IdFormatter.EncodeToFormattedString(maxId, template, placeholder);
        }

        /// <summary>
        /// Gets the minimum and maximum possible formatted identifier strings for a given DateTime range.
        /// Both bounds are inclusive.
        /// </summary>
        /// <param name="startDateTime">The start date/time of the range.</param>
        /// <param name="endDateTime">The end date/time of the range.</param>
        /// <param name="template">The template string containing placeholder characters.</param>
        /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
        /// <returns>A tuple containing the minimum and maximum formatted identifiers for the given range (both inclusive).</returns>
        public (string minId, string maxId) GetFormattedStringRange(
            DateTime startDateTime,
            DateTime endDateTime,
            string template,
            char placeholder = IdFormatter.DefaultPlaceholder) =>
            (self.GetMinFormattedString(startDateTime, template, placeholder),
             self.GetMaxFormattedString(endDateTime, template, placeholder));
    }
}
