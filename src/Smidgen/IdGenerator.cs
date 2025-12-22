using System.Runtime.CompilerServices;

namespace WarpCode.Smidgen;

/// <summary>
/// Generates monotonically increasing identifiers based on configurable time and entropy settings.
/// </summary>
/// <remarks>
/// The generator ensures that generated identifiers always increase, even when called concurrently
/// from multiple threads or when the system clock moves backwards.
/// </remarks>
public sealed class IdGenerator
{
    private readonly Func<ulong> _getTimeElement;
    private readonly Func<ulong> _getEntropyElement;
    private readonly Func<ulong> _increment;
    private readonly EntropySize _entropySize;
    private readonly string _rawStringTemplate;

    // Split UInt128 into two ulong fields for lock-free atomic operations
    private ulong _lastIdLower;
    private ulong _lastIdUpper;
    internal DateTime Since { get; }

    internal TimeAccuracy TimeAccuracy { get; }

    /// <summary>
    /// Gets the total number of bits used in generated identifiers.
    /// </summary>
    public int TotalBits { get; }

    /// <summary>
    /// Gets the number of bits allocated to the time component of generated identifiers.
    /// </summary>
    public byte TimeBits { get; }

    /// <summary>
    /// Gets the number of bits allocated to the entropy component of generated identifiers.
    /// </summary>
    public byte EntropyBits { get; }

    /// <summary>
    /// Gets the size in characters of the Base32-encoded representation of generated identifiers.
    /// This value is useful for determining how many placeholders to use in format templates.
    /// </summary>
    public int Base32Size { get; }


    /// <summary>
    /// Initializes a new instance of the <see cref="IdGenerator"/> class with the specified configuration.
    /// </summary>
    /// <param name="configure">An optional action to configure the generator options. If null, default options (SmallId preset) are used.</param>
    public IdGenerator(Action<GeneratorOptions>? configure = null)
        : this(configure, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IdGenerator"/> class with custom time and entropy functions.
    /// This constructor is intended for testing purposes to control determinism.
    /// </summary>
    /// <param name="configure">An optional action to configure the generator options.</param>
    /// <param name="getTimeElement">Optional custom time element function. If null, uses default implementation.</param>
    /// <param name="getEntropyElement">Optional custom entropy element function. If null, uses default implementation.</param>
    internal IdGenerator(
        Action<GeneratorOptions>? configure,
        Func<ulong>? getTimeElement,
        Func<ulong>? getEntropyElement,
        Func<ulong>? increment)
    {
        var options = new GeneratorOptions();
        configure?.Invoke(options);

        // Store configuration
        Since = options.SinceEpoch;
        TimeAccuracy = options.TimeAccuracy;
        _entropySize = options.EntropySize;

        // Calculate time bits based on date range and accuracy
        TimeBits = CalculateTimeBits(options.SinceEpoch, options.UntilDate, options.TimeAccuracy);
        EntropyBits = (byte)options.EntropySize;

        TotalBits = TimeBits + EntropyBits;
        Base32Size = (TotalBits + 4) / 5;

        // Set delegates to custom or default implementations
        _getTimeElement = getTimeElement ?? GetDefaultTimeElement;
        _getEntropyElement = getEntropyElement ?? GetDefaultEntropyElement;
        _rawStringTemplate = new string('#', Base32Size);
        _increment = increment ?? EntropyElements.GetIncrementByte;
    }

    /// <summary>
    /// Calculates the number of bits required for the time component based on the date range and time accuracy.
    /// </summary>
    private static byte CalculateTimeBits(DateTime since, DateTime until, TimeAccuracy timeAccuracy)
    {
        TimeSpan range = until - since;
        var maxValue = timeAccuracy switch
        {
            TimeAccuracy.Seconds => (ulong)range.TotalSeconds,
            TimeAccuracy.Milliseconds => (ulong)range.TotalMilliseconds,
            TimeAccuracy.Microseconds => (ulong)(range.Ticks / 10), // 1 microsecond = 10 ticks
            TimeAccuracy.Ticks => (ulong)range.Ticks,
            _ => throw new InvalidOperationException($"Unsupported time accuracy: {timeAccuracy}")
        };

        // Calculate bits needed: 64 - leading zero count
        var bitsNeeded = 64 - (int)ulong.LeadingZeroCount(maxValue);

        // Ensure at least 32 bits for time component
        return (byte)Math.Max(32, bitsNeeded);
    }

    /// <summary>
    /// Default implementation: Gets the current time element value based on the configured time accuracy.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong GetDefaultTimeElement()
    {
        TimeSpan elapsed = DateTime.UtcNow - Since;

        return TimeAccuracy switch
        {
            TimeAccuracy.Seconds => (ulong)elapsed.TotalSeconds,
            TimeAccuracy.Milliseconds => (ulong)elapsed.TotalMilliseconds,
            TimeAccuracy.Microseconds => (ulong)(elapsed.Ticks / 10), // 1 microsecond = 10 ticks
            TimeAccuracy.Ticks => (ulong)elapsed.Ticks,
            _ => throw new InvalidOperationException($"Unsupported time accuracy: {TimeAccuracy}")
        };
    }

    /// <summary>
    /// Default implementation: Gets the current entropy element value based on the configured entropy size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong GetDefaultEntropyElement() => _entropySize switch
    {
        EntropySize.Bits16 => EntropyElements.Get16Bits(),
        EntropySize.Bits24 => EntropyElements.Get24Bits(),
        EntropySize.Bits32 => EntropyElements.Get32Bits(),
        EntropySize.Bits40 => EntropyElements.Get40Bits(),
        EntropySize.Bits48 => EntropyElements.Get48Bits(),
        EntropySize.Bits56 => EntropyElements.Get56Bits(),
        EntropySize.Bits64 => EntropyElements.Get64Bits(),
        _ => throw new InvalidOperationException($"Unsupported entropy size: {_entropySize}")
    };

    /// <summary>
    /// Gets the last generated ID as a UInt128 value.
    /// </summary>
    private UInt128 GetLastId()
    {
        var lower = Volatile.Read(ref _lastIdLower);
        var upper = Volatile.Read(ref _lastIdUpper);
        return new UInt128(upper, lower);
    }

    /// <summary>
    /// Attempts to set the last generated ID using atomic operations.
    /// </summary>
    /// <returns>True if the update succeeded, false if another thread modified the value.</returns>
    private bool TrySetLastId(UInt128 comparand, UInt128 newValue)
    {
        var comparandLower = (ulong)comparand;
        var comparandUpper = (ulong)(comparand >> 64);
        var newLower = (ulong)newValue;
        var newUpper = (ulong)(newValue >> 64);

        // First, try to update the lower 64 bits
        var originalLower = Interlocked.CompareExchange(ref _lastIdLower, newLower, comparandLower);
        if (originalLower != comparandLower)
            return false;

        // Then update the upper 64 bits
        var originalUpper = Interlocked.CompareExchange(ref _lastIdUpper, newUpper, comparandUpper);
        if (originalUpper != comparandUpper)
        {
            // Rollback the lower bits if upper bits update failed
            Interlocked.CompareExchange(ref _lastIdLower, comparandLower, newLower);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Generates the next monotonically increasing identifier as a 128-bit unsigned integer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method combines the current time and entropy values from the configured settings
    /// to create a unique identifier. The identifier is guaranteed to be greater than any
    /// previously generated identifier from this instance, even in concurrent scenarios.
    /// </para>
    /// <para>
    /// If the generated raw identifier is not greater than the last generated identifier
    /// (e.g., due to clock drift or high throughput), the method increments the last identifier
    /// by a random amount to maintain monotonicity.
    /// </para>
    /// <para>
    /// This method is thread-safe and uses lock-free atomic operations for optimal performance.
    /// </para>
    /// </remarks>
    /// <returns>A monotonically increasing 128-bit unsigned integer identifier.</returns>
    public UInt128 NextUInt128()
    {
        UInt128 rawId = GenerateId();
        while (true)
        {
            UInt128 lastId = GetLastId();

            UInt128 nextId = rawId > lastId
                ? rawId
                : lastId + _increment();

            if (TrySetLastId(lastId, nextId))
                return nextId;
        }
    }

    /// <summary>
    /// Generates the next monotonically increasing identifier as a raw Crockford Base32 string.
    /// </summary>
    /// <remarks>
    /// This method is useful for scenarios where a simple, compact string representation is needed
    /// without additional formatting. The returned string will have a fixed length based on the
    /// configured total bits.
    /// </remarks>
    /// <returns>A Crockford Base32-encoded string representation of the generated identifier.</returns>
    public string NextRawStringId() => NextFormattedId(_rawStringTemplate);

    /// <summary>
    /// Generates the next monotonically increasing identifier and formats it according to the provided template.
    /// </summary>
    /// <param name="formatTemplate">The template string containing placeholder characters that will be replaced with encoded identifier characters.</param>
    /// <param name="placeholder">The character used as a placeholder in the template (default is '#').</param>
    /// <returns>A formatted string representation of the generated identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatTemplate"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the template contains more than 26 placeholders.</exception>
    /// <exception cref="FormatException">Thrown when the template has insufficient placeholders for the generated ID.</exception>
    public string NextFormattedId(ReadOnlySpan<char> formatTemplate, char placeholder = IdFormatter.DefaultPlaceholder)
    {
        UInt128 id = NextUInt128();
        var formatter = new IdFormatter(Base32Size, formatTemplate, placeholder);
        return formatter.Format(id);
    }

    /// <summary>
    /// Generates a raw identifier by combining time and entropy elements.
    /// </summary>
    /// <remarks>
    /// The time element is shifted left by the number of entropy bits, then combined
    /// with the entropy element using bitwise OR. This creates an identifier where
    /// the most significant bits represent time and the least significant bits represent entropy.
    /// </remarks>
    private UInt128 GenerateId()
    {
        var timeElement = (UInt128)_getTimeElement();
        var entropyElement = (UInt128)_getEntropyElement();

        return (timeElement << EntropyBits) | entropyElement;
    }
}
