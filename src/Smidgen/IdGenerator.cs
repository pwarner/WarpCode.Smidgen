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
    private readonly GeneratorSettings _settings;

    // Split UInt128 into two ulong fields for lock-free atomic operations
    private ulong _lastIdLower;
    private ulong _lastIdUpper;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdGenerator"/> class with the specified settings.
    /// </summary>
    /// <param name="settings">The configuration settings that define how identifiers are generated.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
    public IdGenerator(GeneratorSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }

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
    /// Generates the next monotonically increasing identifier.
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
    public UInt128 Next()
    {
        UInt128 rawId = GenerateId();
        while (true)
        {
            UInt128 lastId = GetLastId();

            UInt128 nextId = rawId > lastId
                ? rawId
                : lastId + _settings.IncrementFunction();

            if (TrySetLastId(lastId, nextId))
                return nextId;
        }
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
        var timeElement = (UInt128)_settings.GetTimeElement();
        var entropyElement = (UInt128)_settings.GetEntropyElement();

        return (timeElement << _settings.EntropyBits) | entropyElement;
    }

    /// <summary>
    /// Extracts the time component from a generated identifier and converts it to a DateTime.
    /// </summary>
    /// <param name="id">The identifier to extract the time from.</param>
    /// <returns>The DateTime value encoded in the identifier's time component.</returns>
    public DateTime GetDateTime(UInt128 id)
    {
        var timeValue = (ulong)(id >> _settings.EntropyBits);
        return _settings.GetDateTimeFromId(timeValue);
    }
}
