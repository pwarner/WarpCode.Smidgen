namespace WarpCode.Smidgen.Tests;

/// <summary>
/// Fake time provider for deterministic testing.
/// Allows control over time values in tests.
/// </summary>
internal class FakeTimeProvider : TimeProvider
{
    private DateTime _currentTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeTimeProvider"/> class with a fixed time.
    /// </summary>
    /// <param name="fixedTime">The fixed time to use.</param>
    public FakeTimeProvider(DateTime fixedTime)
    {
        _currentTime = fixedTime;
    }

    /// <summary>
    /// Sets the current time to a specific value.
    /// </summary>
    /// <param name="time">The time to set.</param>
    public void SetTime(DateTime time) => _currentTime = time;

    /// <summary>
    /// Increments the current time by the specified span.
    /// </summary>
    /// <param name="span">The time span to add.</param>
    public void Increment(TimeSpan span) => _currentTime += span;

    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    /// <returns>The fixed or modified time as a DateTimeOffset.</returns>
    public override DateTimeOffset GetUtcNow() => new(_currentTime);
}

/// <summary>
/// Fake entropy provider for deterministic testing.
/// Returns fixed entropy values for predictable test results.
/// </summary>
internal class FakeEntropyProvider : EntropyProvider
{
    private ulong _entropy;
    private readonly ulong _increment;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeEntropyProvider"/> class.
    /// </summary>
    /// <param name="fixedEntropy">The fixed entropy value to return.</param>
    /// <param name="fixedIncrement">The fixed increment value to return.</param>
    public FakeEntropyProvider(ulong fixedEntropy = 0, ulong fixedIncrement = 0) : base(0)
    {
        _entropy = fixedEntropy;
        _increment = fixedIncrement;
    }

    /// <summary>
    /// Sets the entropy value to be returned.
    /// </summary>
    /// <param name="value">The entropy value.</param>
    public void SetEntropy(ulong value) => _entropy = value;

    /// <summary>
    /// Returns the fixed increment value.
    /// </summary>
    /// <returns>The fixed increment value.</returns>
    public override ulong GetIncrementByte() => _increment;

    /// <summary>
    /// Returns the fixed entropy value as a 16-bit value.
    /// </summary>
    /// <returns>The fixed entropy value.</returns>
    public override ulong Get16Bits() => _entropy;

    /// <summary>
    /// Returns the fixed entropy value as a 24-bit value.
    /// </summary>
    /// <returns>The fixed entropy value.</returns>
    public override ulong Get24Bits() => _entropy;

    /// <summary>
    /// Returns the fixed entropy value as a 32-bit value.
    /// </summary>
    /// <returns>The fixed entropy value.</returns>
    public override ulong Get32Bits() => _entropy;

    /// <summary>
    /// Returns the fixed entropy value as a 40-bit value.
    /// </summary>
    /// <returns>The fixed entropy value.</returns>
    public override ulong Get40Bits() => _entropy;

    /// <summary>
    /// Returns the fixed entropy value as a 48-bit value.
    /// </summary>
    /// <returns>The fixed entropy value.</returns>
    public override ulong Get48Bits() => _entropy;

    /// <summary>
    /// Returns the fixed entropy value as a 56-bit value.
    /// </summary>
    /// <returns>The fixed entropy value.</returns>
    public override ulong Get56Bits() => _entropy;

    /// <summary>
    /// Returns the fixed entropy value as a 64-bit value.
    /// </summary>
    /// <returns>The fixed entropy value.</returns>
    public override ulong Get64Bits() => _entropy;
}
