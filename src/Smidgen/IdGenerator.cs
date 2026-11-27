using System.Runtime.CompilerServices;

namespace WarpCode.Smidgen;

/// <summary>
/// Generates monotonically increasing 60-bit identifiers with time-based ordering and cryptographic entropy.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IdGenerator"/> produces 60-bit monotonic IDs that combine a 40-bit time component 
/// with a 20-bit entropy component. IDs are stored as <see cref="long"/> (64-bit) with the top 4 bits unused.
/// </para>
/// </remarks>
public sealed class IdGenerator
{
    // Constants for entropy headroom (not user-configurable)
    private const int MaxIdsPerInterval = 1000;
    private const int Headroom = MaxIdsPerInterval * 255; // 255,000
    private const long MaxEntropy = (1L << 20) - 1 - Headroom; // 793,575
    private const long MaxElapsedTicks = (1L << 56) - 1; // Maximum ticks that fit in 56 bits
    private const long IntervalMask = 0x0FFFFFFFFFF00000L; // bits 20-59 (40 bits for interval)

    // -1 until the first ID: real IDs are never negative, so the first call always starts a fresh interval
    private long _lastId = -1;

    /// <summary>
    /// Gets or initializes the epoch (start date) for ID generation.
    /// </summary>
    /// <remarks>
    /// The epoch determines the zero point for the time component. Defaults to <see cref="DateTime.UnixEpoch"/> (1970-01-01).
    /// </remarks>
    public DateTime Epoch { get; init; } = DateTime.UnixEpoch;

    /// <summary>
    /// Gets or initializes the <see cref="TimeProvider"/> used for time operations.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="TimeProvider.System"/>. Can be overridden for testing or time zone adjustments.
    /// </remarks>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>
    /// Gets or initializes the <see cref="EntropyProvider"/> used for random number generation.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="EntropyProvider.Shared"/>. Can be overridden for testing or custom entropy sources.
    /// </remarks>
    public EntropyProvider EntropyProvider { get; init; } = EntropyProvider.Shared;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdGenerator"/> class.
    /// </summary>
    /// <remarks>
    /// Use object initializer syntax to configure the generator:
    /// <code>
    /// var generator = new IdGenerator
    /// {
    ///     Epoch = DateTime.UnixEpoch,
    ///     TimeProvider = TimeProvider.System,
    ///     EntropyProvider = EntropyProvider.Shared
    /// };
    /// </code>
    /// </remarks>
    public IdGenerator()
    {
    }

    /// <summary>
    /// Generates the next monotonically increasing identifier as a 60-bit signed integer.
    /// </summary>
    /// <returns>A monotonically increasing 60-bit identifier stored in a <see cref="long"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current time is before the epoch or exceeds the representable time range (83400 days from the epoch).
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method combines the current time and cryptographic entropy to create a unique identifier.
    /// The identifier is guaranteed to be greater than any previously generated identifier from this instance,
    /// even in concurrent scenarios.
    /// </para>
    /// <para>
    /// The first identifier in each time interval starts from fresh random entropy. Later identifiers in the
    /// same interval add a random increment of 1-255 to the previous one, so separate instances spread their
    /// identifiers across the whole entropy range rather than converging on the same values.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long NextId()
    {
        var interval = GetCurrentInterval();

        while (true)
        {
            var lastId = Volatile.Read(ref _lastId);
            long newId;

            if (lastId < 0 || interval > (lastId & IntervalMask))
            {
                // First ID in a new interval: start from fresh entropy
                newId = interval | NextEntropy();
            }
            else
            {
                // Same (or an earlier) interval: step on from the last ID.
                // Overflowing into the next interval is acceptable.
                var increment = EntropyProvider.GetByte();
                if (increment == 0)
                    continue;

                newId = lastId + increment;
            }

            if (Interlocked.CompareExchange(ref _lastId, newId, lastId) == lastId)
                return newId;

            // Another thread stored an ID first: retry against it
        }
    }

    /// <summary>
    /// Generates the next monotonically increasing identifier as a Crockford Base32 string.
    /// </summary>
    /// <returns>A string in the format ####-####-#### using Crockford Base32 encoding.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current time is before the epoch or exceeds the representable time range (83400 days from the epoch).
    /// </exception>
    public string NextString()
    {
        var id = NextId();
        return IdFormatter.EncodeToRawString(id);
    }

    /// <summary>
    /// Generates the next monotonically increasing identifier and formats it according to the provided template.
    /// </summary>
    /// <param name="template">The template string containing placeholder characters that will be replaced with encoded identifier characters.</param>
    /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
    /// <returns>A formatted string representation of the generated identifier.</returns>
    /// <exception cref="ArgumentException">Thrown when the template is empty.</exception>
    /// <exception cref="FormatException">Thrown when the template has insufficient placeholders.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current time is before the epoch or exceeds the representable time range (83400 days from the epoch).
    /// </exception>
    /// <remarks>
    /// <para>
    /// Custom templates allow flexible formatting for different display contexts:
    /// <code>
    /// var id1 = generator.NextString("ID-############");    // ID-XXXXXXXXXXXX
    /// var id2 = generator.NextString("USR_####_####_####"); // USR_XXXX_XXXX_XXXX
    /// </code>
    /// </para>
    /// <para>
    /// Each '#' placeholder is replaced with a an encoded character.
    /// Non-placeholder characters are preserved in the output.
    /// </para>
    /// </remarks>
    public string NextString(string template, char placeholder = IdFormatter.DefaultPlaceholder)
    {
        var id = NextId();
        return IdFormatter.EncodeToFormattedString(id, template, placeholder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetCurrentInterval()
    {
        // Get elapsed time since epoch (use properties directly since init properties set after constructor)
        var elapsedTicks = TimeProvider.GetUtcNow().UtcDateTime.Ticks - Epoch.Ticks;

        // Validation: Elapsed ticks must be non-negative
        if (elapsedTicks < 0)
            ThrowCurrentTimeBeforeEpoch();

        // Validation: Elapsed ticks must not exceed 56 bits
        if (elapsedTicks > MaxElapsedTicks)
            ThrowTimeExceedsRange();

        // Interval: shift left 4 bits, clear lower 20 bits
        return (elapsedTicks << 4) & IntervalMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long NextEntropy()
    {
        // Get 3 random bytes, keep lower 20 bits
        Span<byte> buffer = stackalloc byte[3];
        EntropyProvider.GetBytes(buffer);
        long entropy = ((buffer[0] << 16) | (buffer[1] << 8) | buffer[2]) & 0x0FFFFF;

        // Apply headroom cap to prevent overflow during monotonic increments
        if (entropy > MaxEntropy)
            entropy -= Headroom;

        return entropy;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowCurrentTimeBeforeEpoch() =>
        throw new InvalidOperationException("Current time is before epoch. Check system clock or adjust epoch.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowTimeExceedsRange() =>
        throw new InvalidOperationException("Time exceeds representable range (228 years from epoch). Adjust epoch to extend usable range.");
}

