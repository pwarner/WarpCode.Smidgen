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
/// <para>
/// ID Structure (60 bits in a 64-bit long):
/// <code>
/// ┌─────────┬──────────────────────┬────────────────────┐
/// │ 4 bits  │      40 bits         │      20 bits       │
/// │ Unused  │  Time (Interval)     │  Entropy           │
/// │ (0000)  │  Shifted Ticks &lt;&lt; 4  │  Random            │
/// └─────────┴──────────────────────┴────────────────────┘
/// </code>
/// </para>
/// <para>
/// Thread Safety: All methods are thread-safe and use lock-free atomic operations for optimal performance.
/// The generator ensures that IDs always increase, even when called concurrently or when the system clock moves backwards.
/// </para>
/// <para>
/// Time Precision: The time component uses ~6.5536ms intervals (ticks << 4), providing approximately 
/// 228 years of representable time from the configured epoch.
/// </para>
/// </remarks>
public sealed class IdGenerator
{
    // Constants for entropy headroom (not user-configurable)
    private const int MaxIdsPerInterval = 1000;
    private const int Headroom = MaxIdsPerInterval * 255; // 255,000
    private const long MaxEntropy = (1L << 20) - 1 - Headroom; // 793,575
    private const long MaxElapsedTicks = (1L << 56) - 1; // Maximum ticks that fit in 56 bits

    private long _lastId;

    /// <summary>
    /// Gets or initializes the epoch (start date) for ID generation.
    /// </summary>
    /// <remarks>
    /// The epoch determines the zero point for the time component. Defaults to <see cref="DateTime.UnixEpoch"/> (1970-01-01).
    /// The representable time range extends approximately 228 years from the epoch.
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
    /// Note: The constructor does NOT capture init property values. Use the public properties directly,
    /// or initialize fields in a different pattern if init properties don't suit your needs.
    /// </remarks>
    public IdGenerator()
    {
        // Note: Init properties are set AFTER the constructor runs,
        // so we cannot copy them to fields here. We must use the properties directly.
    }

    /// <summary>
    /// Generates the next monotonically increasing identifier as a 60-bit signed integer.
    /// </summary>
    /// <returns>A monotonically increasing 60-bit identifier stored in a <see cref="long"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current time is before the epoch or exceeds the representable time range (228 years).
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method combines the current time and cryptographic entropy to create a unique identifier.
    /// The identifier is guaranteed to be greater than any previously generated identifier from this instance,
    /// even in concurrent scenarios.
    /// </para>
    /// <para>
    /// If the generated raw identifier is not greater than the last generated identifier (e.g., due to 
    /// clock drift or high throughput), the method increments the last identifier by a random amount 
    /// to maintain monotonicity.
    /// </para>
    /// <para>
    /// Thread Safety: This method is thread-safe and uses lock-free atomic operations (<see cref="Interlocked.CompareExchange"/>).
    /// </para>
    /// <para>
    /// Performance: No allocations occur in the hot path. All operations use stack-allocated buffers.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long NextId()
    {
        var newId = GenerateRawId();

        while (true)
        {
            var lastId = Volatile.Read(ref _lastId);

            if (newId > lastId)
            {
                // Happy path: new ID is greater
                var previous = Interlocked.CompareExchange(ref _lastId, newId, lastId);
                if (previous == lastId)
                    return newId; // Successfully stored

                lastId = previous; // CAS failed, retry with updated lastId
            }
            else
            {
                // Need to increment: add random byte to maintain monotonicity
                var incrementByte = EntropyProvider.GetByte();
                newId = lastId + incrementByte;
                // Note: Overflow to next interval is acceptable
            }
        }
    }

    /// <summary>
    /// Generates the next monotonically increasing identifier as a raw Crockford Base32 string.
    /// </summary>
    /// <returns>A string in the format ####-####-#### using Crockford Base32 encoding.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current time is before the epoch or exceeds the representable time range (228 years).
    /// </exception>
    /// <remarks>
    /// This method generates a 60-bit ID and formats it as a 14-character string (12 Base32 digits with 2 dashes).
    /// The raw format provides a consistent, readable representation suitable for display and database storage.
    /// </remarks>
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
    /// Thrown when the current time is before the epoch or exceeds the representable time range (228 years).
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
    /// Each '#' placeholder is replaced with a Crockford Base32 character (0-9, A-Z excluding I, L, O, U).
    /// Non-placeholder characters are preserved in the output.
    /// </para>
    /// </remarks>
    public string NextString(string template, char placeholder = IdFormatter.DefaultPlaceholder)
    {
        var id = NextId();
        return IdFormatter.EncodeToFormattedString(id, template, placeholder);
    }

    /// <summary>
    /// Generates a raw identifier by combining the time interval and entropy components.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Time Calculation:
    /// <code>
    /// elapsedTicks = currentUtcTime.Ticks - epoch.Ticks
    /// interval = (elapsedTicks &lt;&lt; 4) &amp; 0xFFFFFFFFFFF00000L
    /// </code>
    /// The shift-left by 4 bits provides ~6.5536ms precision without division.
    /// </para>
    /// <para>
    /// Entropy Calculation:
    /// <code>
    /// Read 3 random bytes (24 bits)
    /// entropy = (bytes combined) &amp; 0x0FFFFF  // Keep lower 20 bits
    /// if (entropy > MaxEntropy) entropy -= Headroom
    /// </code>
    /// Headroom capping prevents overflow during monotonic increments.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GenerateRawId()
    {
        // Get elapsed time since epoch (use properties directly since init properties set after constructor)
        var elapsedTicks = TimeProvider.GetUtcNow().UtcDateTime.Ticks - Epoch.Ticks;

        // Validation: Elapsed ticks must be non-negative
        if (elapsedTicks < 0)
            ThrowCurrentTimeBeforeEpoch();

        // Validation: Elapsed ticks must not exceed 56 bits
        if (elapsedTicks > MaxElapsedTicks)
            ThrowTimeExceedsRange();

        // Generate interval: shift left 4 bits, clear lower 20 bits
        // Mask keeps bits 20-59 (40 bits for interval)
        const long IntervalMask = 0x0FFFFFFFFFF00000L;
        var interval = (elapsedTicks << 4) & IntervalMask;

        // Generate entropy: get 3 random bytes, keep lower 20 bits
        Span<byte> buffer = stackalloc byte[3];
        EntropyProvider.GetBytes(buffer);
        long entropy = ((buffer[0] << 16) | (buffer[1] << 8) | buffer[2]) & 0x0FFFFF;

        // Apply headroom cap to prevent overflow during monotonic increments
        if (entropy > MaxEntropy)
            entropy -= Headroom;

        // Combine interval and entropy
        return interval | entropy;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowCurrentTimeBeforeEpoch() =>
        throw new InvalidOperationException("Current time is before epoch. Check system clock or adjust epoch.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowTimeExceedsRange() =>
        throw new InvalidOperationException("Time exceeds representable range (228 years from epoch). Adjust epoch to extend usable range.");
}

