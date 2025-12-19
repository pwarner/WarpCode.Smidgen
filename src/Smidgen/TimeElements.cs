using System.Runtime.CompilerServices;

namespace WarpCode.Smidgen;

/// <summary>
/// Provides helper functions for getting time elements based on different epochs and resolutions.
/// </summary>
internal static class TimeElements
{
    private static readonly DateTime UnixEpoch = DateTime.UnixEpoch;

    /// <summary>
    /// Gets the number of milliseconds since the Unix epoch (January 1, 1970 00:00:00 UTC).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong MillisecondsSinceUnixEpoch()
    {
        TimeSpan elapsed = DateTime.UtcNow - UnixEpoch;
        return (ulong)elapsed.TotalMilliseconds;
    }

    /// <summary>
    /// Gets the number of microseconds since the Unix epoch (January 1, 1970 00:00:00 UTC).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong MicrosecondsSinceUnixEpoch()
    {
        TimeSpan elapsed = DateTime.UtcNow - UnixEpoch;
        return (ulong)(elapsed.Ticks / 10); // 1 microsecond = 10 ticks
    }

    /// <summary>
    /// Gets the number of ticks since the Unix epoch (January 1, 1970 00:00:00 UTC).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong TicksSinceUnixEpoch()
    {
        TimeSpan elapsed = DateTime.UtcNow - UnixEpoch;
        return (ulong)elapsed.Ticks;
    }

    /// <summary>
    /// Converts a time value in milliseconds since Unix epoch to a DateTime.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime DateTimeFromMillisecondsSinceUnixEpoch(ulong timeValue) => UnixEpoch.AddMilliseconds(timeValue);

    /// <summary>
    /// Converts a time value in microseconds since Unix epoch to a DateTime.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime DateTimeFromMicrosecondsSinceUnixEpoch(ulong timeValue) => UnixEpoch.AddTicks((long)timeValue * 10); // 1 microsecond = 10 ticks

    /// <summary>
    /// Converts a time value in ticks since Unix epoch to a DateTime.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime DateTimeFromTicksSinceUnixEpoch(ulong timeValue) => UnixEpoch.AddTicks((long)timeValue);
}
