namespace WarpCode.Smidgen.Tests;

/// <summary>
/// Fake time provider for deterministic testing.
/// Allows control over time values in tests.
/// </summary>
internal sealed class FakeTimeProvider : TimeProvider
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
/// Uses an increment of 1 for monotonic increments unless <see cref="Increments"/> says otherwise.
/// </summary>
internal sealed class FakeEntropyProvider : EntropyProvider
{
    private byte[] _fixedBytes;
    private int _incrementIndex = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeEntropyProvider"/> class.
    /// </summary>
    /// <param name="fixedEntropy">The fixed 20-bit entropy value to return (only lower 20 bits used).</param>
    public FakeEntropyProvider(int fixedEntropy = 0) : base(0) => _fixedBytes = ToBytes(fixedEntropy);

    /// <summary>
    /// Initializes a new instance with explicit byte array.
    /// </summary>
    /// <param name="fixedBytes">The fixed bytes to return. Will be copied to prevent external modification.</param>
    public FakeEntropyProvider(byte[] fixedBytes) : base(0) => _fixedBytes = [.. fixedBytes];

    /// <summary>
    /// The increments <see cref="GetByte"/> returns, in order, repeating the last one. Defaults to always 1.
    /// </summary>
    public byte[] Increments { get; init; } = [1];

    /// <summary>
    /// The number of times <see cref="GetBytes"/> has been called, i.e. how many IDs started from fresh entropy.
    /// </summary>
    public int GetBytesCalls { get; private set; }

    /// <summary>
    /// Changes the fixed 20-bit entropy value returned from now on.
    /// </summary>
    /// <param name="fixedEntropy">The new entropy value (only lower 20 bits used).</param>
    public void SetEntropy(int fixedEntropy) => _fixedBytes = ToBytes(fixedEntropy);

    /// <summary>
    /// Fills the buffer with the fixed entropy bytes.
    /// </summary>
    /// <param name="buffer">The buffer to fill.</param>
    public override void GetBytes(Span<byte> buffer)
    {
        GetBytesCalls++;
        if (buffer.IsEmpty)
            return;

        // Fill buffer with fixed bytes, repeating if necessary
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = _fixedBytes[i % _fixedBytes.Length];
        }
    }

    /// <summary>
    /// Returns the next value from <see cref="Increments"/>, repeating the last one once they run out.
    /// </summary>
    /// <returns>The next increment.</returns>
    public override byte GetByte() => Increments[Math.Min(Interlocked.Increment(ref _incrementIndex), Increments.Length - 1)];

    // Convert 20-bit entropy to 3 bytes for GetBytes calls
    private static byte[] ToBytes(int entropy) =>
        [(byte)((entropy >> 16) & 0xFF), (byte)((entropy >> 8) & 0xFF), (byte)(entropy & 0xFF)];
}
