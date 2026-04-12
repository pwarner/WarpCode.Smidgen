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
    public FakeTimeProvider(DateTime fixedTime) => _currentTime = fixedTime;

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
/// Always uses increment of 1 for monotonic increments.
/// </summary>
internal class FakeEntropyProvider : EntropyProvider
{
    private readonly byte[] _fixedBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeEntropyProvider"/> class.
    /// </summary>
    /// <param name="fixedEntropy">The fixed 20-bit entropy value to return (only lower 20 bits used).</param>
    public FakeEntropyProvider(int fixedEntropy = 0) : base(0)
    {
        // Convert 20-bit entropy to 3 bytes for GetBytes calls
        _fixedBytes = new byte[3];
        _fixedBytes[0] = (byte)((fixedEntropy >> 16) & 0xFF);
        _fixedBytes[1] = (byte)((fixedEntropy >> 8) & 0xFF);
        _fixedBytes[2] = (byte)(fixedEntropy & 0xFF);
    }

    /// <summary>
    /// Initializes a new instance with explicit byte array.
    /// </summary>
    /// <param name="fixedBytes">The fixed bytes to return. Will be copied to prevent external modification.</param>
    public FakeEntropyProvider(byte[] fixedBytes) : base(0)
    {
        _fixedBytes = new byte[fixedBytes.Length];
        fixedBytes.CopyTo(_fixedBytes, 0);
    }

    /// <summary>
    /// Fills the buffer with the fixed entropy bytes.
    /// </summary>
    /// <param name="buffer">The buffer to fill.</param>
    public override void GetBytes(Span<byte> buffer)
    {
        if (buffer.IsEmpty)
            return;

        // Fill buffer with fixed bytes, repeating if necessary
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = _fixedBytes[i % _fixedBytes.Length];
        }
    }

    /// <summary>
    /// Returns 1 for deterministic monotonic increments.
    /// </summary>
    /// <returns>Always returns 1.</returns>
    public override byte GetByte() => 1;
}
