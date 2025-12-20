namespace WarpCode.Smidgen;

/// <summary>
/// Defines the time resolution for the time component of generated identifiers.
/// </summary>
public enum TimeAccuracy
{
    /// <summary>
    /// Second-precision time elements.
    /// </summary>
    Seconds,

    /// <summary>
    /// Millisecond-precision time elements (1/1,000 of a second).
    /// </summary>
    Milliseconds,

    /// <summary>
    /// Microsecond-precision time elements (1/1,000,000 of a second).
    /// </summary>
    Microseconds,

    /// <summary>
    /// Tick-precision time elements (100-nanosecond units, 1/10,000,000 of a second).
    /// </summary>
    Ticks
}
