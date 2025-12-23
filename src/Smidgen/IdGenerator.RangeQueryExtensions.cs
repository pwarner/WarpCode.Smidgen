namespace WarpCode.Smidgen;

/// <summary>
/// Provides extension methods for generating minimum and maximum identifiers for range queries.
/// All min/max bounds are inclusive.
/// </summary>
public static class IdGeneratorRangeQueryExtensions
{
    extension(IdGenerator self)
    {
        /// <summary>
        /// Gets the minimum possible identifier for a given DateTime.
        /// The entropy component is set to zero.
        /// </summary>
        /// <param name="dateTime">The date/time to create the minimum ID for.</param>
        /// <returns>The minimum 128-bit unsigned integer identifier for the given DateTime (inclusive).</returns>
        public UInt128 GetMinUInt128Id(DateTime dateTime)
        {
            var timeValue = self.GetTimeValueFromDateTime(dateTime);
            return (UInt128)timeValue << self.EntropyBits;
        }

        /// <summary>
        /// Gets the maximum possible identifier for a given DateTime.
        /// The entropy component is set to all ones.
        /// </summary>
        /// <param name="dateTime">The date/time to create the maximum ID for.</param>
        /// <returns>The maximum 128-bit unsigned integer identifier for the given DateTime (inclusive).</returns>
        public UInt128 GetMaxUInt128Id(DateTime dateTime)
        {
            var timeValue = self.GetTimeValueFromDateTime(dateTime);
            UInt128 maxEntropy = (UInt128.One << self.EntropyBits) - 1;
            return ((UInt128)timeValue << self.EntropyBits) | maxEntropy;
        }

        /// <summary>
        /// Gets the minimum and maximum possible identifiers for a given DateTime range.
        /// Both bounds are inclusive.
        /// </summary>
        /// <param name="startDateTime">The start date/time of the range.</param>
        /// <param name="endDateTime">The end date/time of the range.</param>
        /// <returns>A tuple containing the minimum and maximum identifiers for the given range (both inclusive).</returns>
        public (UInt128 MinId, UInt128 MaxId) GetUInt128IdRange(DateTime startDateTime, DateTime endDateTime) =>
            (self.GetMinUInt128Id(startDateTime), self.GetMaxUInt128Id(endDateTime));

        /// <summary>
        /// Gets the minimum possible formatted identifier string for a given DateTime.
        /// The entropy component is set to zero.
        /// </summary>
        /// <param name="dateTime">The date/time to create the minimum ID for.</param>
        /// <param name="formatTemplate">The template string containing placeholder characters.</param>
        /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
        /// <returns>A formatted string representation of the minimum identifier for the given DateTime (inclusive).</returns>
        public string GetMinFormattedId(DateTime dateTime, string formatTemplate, char placeholder = IdFormatter.DefaultPlaceholder)
        {
            UInt128 minId = self.GetMinUInt128Id(dateTime);
            return IdFormatter.Format(minId, formatTemplate, placeholder);
        }

        /// <summary>
        /// Gets the maximum possible formatted identifier string for a given DateTime.
        /// The entropy component is set to all ones.
        /// </summary>
        /// <param name="dateTime">The date/time to create the maximum ID for.</param>
        /// <param name="formatTemplate">The template string containing placeholder characters.</param>
        /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
        /// <returns>A formatted string representation of the maximum identifier for the given DateTime (inclusive).</returns>
        public string GetMaxFormattedId(DateTime dateTime, string formatTemplate, char placeholder = IdFormatter.DefaultPlaceholder)
        {
            UInt128 maxId = self.GetMaxUInt128Id(dateTime);
            return IdFormatter.Format(maxId, formatTemplate, placeholder);
        }

        /// <summary>
        /// Gets the minimum and maximum possible formatted identifier strings for a given DateTime range.
        /// Both bounds are inclusive.
        /// </summary>
        /// <param name="startDateTime">The start date/time of the range.</param>
        /// <param name="endDateTime">The end date/time of the range.</param>
        /// <param name="formatTemplate">The template string containing placeholder characters.</param>
        /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
        /// <returns>A tuple containing the minimum and maximum formatted identifiers for the given range (both inclusive).</returns>
        public (string MinFormattedId, string MaxFormattedId) GetFormattedIdRange(
            DateTime startDateTime,
            DateTime endDateTime,
            string formatTemplate,
            char placeholder = '#') => (
                self.GetMinFormattedId(startDateTime, formatTemplate, placeholder),
                self.GetMaxFormattedId(endDateTime, formatTemplate, placeholder)
            );

        /// <summary>
        /// Gets the minimum possible raw string identifier for a given DateTime.
        /// The entropy component is set to zero.
        /// </summary>
        /// <param name="dateTime">The date/time to create the minimum ID for.</param>
        /// <returns>A raw Crockford Base32 string representation of the minimum identifier for the given DateTime (inclusive).</returns>
        public string GetMinRawStringId(DateTime dateTime)
        {
            UInt128 minId = self.GetMinUInt128Id(dateTime);
            var template = new string('#', self.Base32Size);
            return IdFormatter.Format(minId, template);
        }

        /// <summary>
        /// Gets the maximum possible raw string identifier for a given DateTime.
        /// The entropy component is set to all ones.
        /// </summary>
        /// <param name="dateTime">The date/time to create the maximum ID for.</param>
        /// <returns>A raw Crockford Base32 string representation of the maximum identifier for the given DateTime (inclusive).</returns>
        public string GetMaxRawStringId(DateTime dateTime)
        {
            UInt128 maxId = self.GetMaxUInt128Id(dateTime);
            var template = new string('#', self.Base32Size);
            return IdFormatter.Format(maxId, template);
        }

        /// <summary>
        /// Gets the minimum and maximum possible raw string identifiers for a given DateTime range.
        /// Both bounds are inclusive.
        /// </summary>
        /// <param name="startDateTime">The start date/time of the range.</param>
        /// <param name="endDateTime">The end date/time of the range.</param>
        /// <returns>A tuple containing the minimum and maximum raw string identifiers for the given range (both inclusive).</returns>
        public (string MinRawStringId, string MaxRawStringId) GetRawStringIdRange(
            DateTime startDateTime,
            DateTime endDateTime) => (
                self.GetMinRawStringId(startDateTime),
                self.GetMaxRawStringId(endDateTime)
            );

        private ulong GetTimeValueFromDateTime(DateTime dateTime)
        {
            TimeSpan elapsed = dateTime - self.Since;

            return self.TimeAccuracy switch
            {
                TimeAccuracy.Seconds => (ulong)elapsed.TotalSeconds,
                TimeAccuracy.Milliseconds => (ulong)elapsed.TotalMilliseconds,
                TimeAccuracy.Microseconds => (ulong)(elapsed.Ticks / 10), // 1 microsecond = 10 ticks
                TimeAccuracy.Ticks => (ulong)elapsed.Ticks,
                _ => throw new InvalidOperationException($"Unsupported time accuracy: {self.TimeAccuracy}")
            };
        }
    }
}
