using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace WarpCode.Smidgen;

/// <summary>
/// Provides thread-safe cryptographically secure random entropy.
/// Uses a shared buffer that is refilled as needed to balance security and performance.
/// </summary>
/// <remarks>
/// The <see cref="EntropyProvider"/> class is designed for high-performance scenarios where 
/// generating fresh entropy for each operation would be too expensive. It maintains an internal
/// buffer of random bytes that is filled using <see cref="RandomNumberGenerator"/> and refilled
/// when exhausted.
/// <para>
/// Thread Safety: All methods are thread-safe and can be called from multiple threads concurrently.
/// The implementation uses lock-free reads with locked refills to minimize contention.
/// </para>
/// </remarks>
public class EntropyProvider
{
    /// <summary>
    /// The default buffer size used for entropy generation (2048 bytes).
    /// </summary>
    public const int DefaultBufferSize = 2048;

    private readonly byte[] _buffer;
    private readonly Lock _refillLock = new();
    private int _position;

    /// <summary>
    /// Gets the shared instance of the entropy provider with default buffer size.
    /// This instance can be safely shared across multiple <see cref="IdGenerator"/> instances.
    /// </summary>
    public static EntropyProvider Shared { get; } = new EntropyProvider();

    /// <summary>
    /// Initializes a new instance of the <see cref="EntropyProvider"/> class with the specified buffer size.
    /// </summary>
    /// <param name="bufferSize">The size of the internal buffer used for entropy generation. Defaults to <see cref="DefaultBufferSize"/>.</param>
    public EntropyProvider(int bufferSize = DefaultBufferSize)
    {
        if (bufferSize <= 0)
        {
            _buffer = [];
            return;
        }
        _buffer = new byte[bufferSize];
        RandomNumberGenerator.Fill(_buffer);
    }

    /// <summary>
    /// Fills the specified buffer with cryptographically secure random bytes.
    /// </summary>
    /// <param name="buffer">The buffer to fill with random bytes.</param>
    /// <remarks>
    /// This method is thread-safe and can be called concurrently from multiple threads.
    /// The implementation uses lock-free reads with locked refills for optimal performance.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void GetBytes(Span<byte> buffer)
    {
        if (buffer.IsEmpty)
            return;

        var slice = GetSlice(buffer.Length);
        slice.CopyTo(buffer);
    }

    /// <summary>
    /// Gets a single random byte value.
    /// </summary>
    /// <returns>A cryptographically secure random byte.</returns>
    /// <remarks>
    /// This method is thread-safe and optimized for scenarios where only a single byte is needed,
    /// such as incrementing IDs to maintain monotonicity.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual byte GetByte()
    {
        return GetSlice(1)[0];
    }

    /// <summary>
    /// Gets a slice of the random buffer with the specified number of bytes.
    /// Ensures the buffer has enough bytes available and refills if needed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> GetSlice(int bytesNeeded)
    {
        while (true)
        {
            var currentPos = _position;

            // Check if we need to refill
            if (currentPos > _buffer.Length - bytesNeeded)
            {
                lock (_refillLock)
                {
                    // Double-check after acquiring lock
                    if (_position > _buffer.Length - bytesNeeded)
                    {
                        RandomNumberGenerator.Fill(_buffer);
                        _position = 0;
                    }
                }
                continue; // Retry after refill
            }

            // Try to claim the position atomically
            var newPos = currentPos + bytesNeeded;
            if (Interlocked.CompareExchange(ref _position, newPos, currentPos) == currentPos)
                return _buffer.AsSpan(currentPos, bytesNeeded);

            // If CAS failed, retry
        }
    }
}
