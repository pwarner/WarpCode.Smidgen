namespace WarpCode.Smidgen;

/// <summary>
/// Configures the behavior of an <see cref="IdGenerator"/>, including time and entropy bit allocations,
/// and the functions used to generate time elements, entropy elements, and increment values.
/// </summary>
public sealed class GeneratorSettings
{
    /// <summary>
    /// Gets the number of bits allocated to the time component of generated identifiers.
    /// </summary>
    public byte TimeBits { get; }

    /// <summary>
    /// Gets the number of bits allocated to the entropy component of generated identifiers.
    /// </summary>
    public byte EntropyBits { get; }

    /// <summary>
    /// Gets the function that provides the current time element value.
    /// </summary>
    public Func<ulong> GetTimeElement { get; }

    /// <summary>
    /// Gets the function that provides random entropy element values.
    /// </summary>
    public Func<ulong> GetEntropyElement { get; }

    /// <summary>
    /// Gets the function that provides increment values when monotonic adjustment is needed.
    /// </summary>
    public Func<ulong> IncrementFunction { get; }

    /// <summary>
    /// Gets the function that converts a time element value back to a <see cref="DateTime"/>.
    /// </summary>
    public Func<ulong, DateTime> GetDateTimeFromId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratorSettings"/> class with the specified configuration.
    /// </summary>
    /// <param name="timeBits">The number of bits to allocate for the time component (32-64).</param>
    /// <param name="entropyBits">The number of bits to allocate for the entropy component (16-64, must be multiple of 8).</param>
    /// <param name="getTimeElement">Function to get the current time element.</param>
    /// <param name="getEntropyElement">Function to get random entropy elements.</param>
    /// <param name="incrementFunction">Function to get increment values for monotonic adjustment.</param>
    /// <param name="getDateTimeFromId">Function to convert time element values back to DateTime.</param>
    /// <exception cref="ArgumentNullException">Thrown when any function parameter is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when bit allocations are invalid.</exception>
    public GeneratorSettings(
        byte timeBits,
        byte entropyBits,
        Func<ulong> getTimeElement,
        Func<ulong> getEntropyElement,
        Func<ulong> incrementFunction,
        Func<ulong, DateTime> getDateTimeFromId)
    {
        ArgumentNullException.ThrowIfNull(getTimeElement);
        ArgumentNullException.ThrowIfNull(getEntropyElement);
        ArgumentNullException.ThrowIfNull(incrementFunction);
        ArgumentNullException.ThrowIfNull(getDateTimeFromId);

        if (timeBits is < 32 or > 64)
            ThrowInvalidTimeBits(timeBits);

        if (entropyBits is < 16 or > 64)
            ThrowInvalidEntropyBits(entropyBits);

        if (entropyBits % 8 != 0)
            ThrowEntropyBitsNotMultipleOf8(entropyBits);

        if (timeBits + entropyBits > 128)
            ThrowTotalBitsExceedsLimit(timeBits, entropyBits);

        TimeBits = timeBits;
        EntropyBits = entropyBits;
        GetTimeElement = getTimeElement;
        GetEntropyElement = getEntropyElement;
        IncrementFunction = incrementFunction;
        GetDateTimeFromId = getDateTimeFromId;
    }

    /// <summary>
    /// Gets the default settings for small identifiers (64 bits total).
    /// Uses 48 bits for millisecond-precision time and 16 bits for entropy.
    /// </summary>
    public static readonly GeneratorSettings SmallId = new(
        timeBits: 48,
        entropyBits: 16,
        getTimeElement: TimeElements.MillisecondsSinceUnixEpoch,
        getEntropyElement: EntropyElements.Get16Bits,
        incrementFunction: EntropyElements.GetIncrementByte,
        getDateTimeFromId: TimeElements.DateTimeFromMillisecondsSinceUnixEpoch
    );

    /// <summary>
    /// Gets settings for 80-bit identifiers with increased entropy.
    /// Uses 48 bits for millisecond-precision time and 32 bits for entropy.
    /// </summary>
    public static readonly GeneratorSettings Id80 = new(
        timeBits: 48,
        entropyBits: 32,
        getTimeElement: TimeElements.MillisecondsSinceUnixEpoch,
        getEntropyElement: EntropyElements.Get32Bits,
        incrementFunction: EntropyElements.GetIncrementByte,
        getDateTimeFromId: TimeElements.DateTimeFromMillisecondsSinceUnixEpoch
    );

    /// <summary>
    /// Gets settings for 96-bit identifiers with high-precision time.
    /// Uses 56 bits for microsecond-precision time and 40 bits for entropy.
    /// </summary>
    public static readonly GeneratorSettings Id96 = new(
        timeBits: 56,
        entropyBits: 40,
        getTimeElement: TimeElements.MicrosecondsSinceUnixEpoch,
        getEntropyElement: EntropyElements.Get40Bits,
        incrementFunction: EntropyElements.GetIncrementByte,
        getDateTimeFromId: TimeElements.DateTimeFromMicrosecondsSinceUnixEpoch
    );

    /// <summary>
    /// Gets settings for large 128-bit identifiers with maximum precision.
    /// Uses 64 bits for tick-precision time and 64 bits for entropy.
    /// </summary>
    public static readonly GeneratorSettings BigId = new(
        timeBits: 64,
        entropyBits: 64,
        getTimeElement: TimeElements.TicksSinceUnixEpoch,
        getEntropyElement: EntropyElements.Get64Bits,
        incrementFunction: EntropyElements.GetIncrementByte,
        getDateTimeFromId: TimeElements.DateTimeFromTicksSinceUnixEpoch
    );

    private static void ThrowInvalidTimeBits(byte timeBits) =>
        throw new ArgumentOutOfRangeException(
            nameof(timeBits),
            timeBits,
            $"TimeBits must be between 32 and 64, but was {timeBits}.");

    private static void ThrowInvalidEntropyBits(byte entropyBits) =>
        throw new ArgumentOutOfRangeException(
            nameof(entropyBits),
            entropyBits,
            $"EntropyBits must be between 16 and 64, but was {entropyBits}.");

    private static void ThrowEntropyBitsNotMultipleOf8(byte entropyBits) =>
        throw new ArgumentOutOfRangeException(
            nameof(entropyBits),
            entropyBits,
            $"EntropyBits must be a multiple of 8, but was {entropyBits}.");

    private static void ThrowTotalBitsExceedsLimit(byte timeBits, byte entropyBits) =>
        throw new ArgumentOutOfRangeException(
            nameof(timeBits),
            $"The sum of TimeBits ({timeBits}) and EntropyBits ({entropyBits}) must not exceed 128, but was {timeBits + entropyBits}.");
}
