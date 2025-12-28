using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace WarpCode.Smidgen;

/// <summary>
/// Provides thread-safe cryptographically secure random entropy elements of varying widths.
/// Uses a shared buffer that is refilled as needed to balance security and performance.
/// </summary>
internal class EntropyProvider
{
    private const int DefaultBufferSize = 4096;
    private readonly byte[] _buffer;
#if NET10_0_OR_GREATER
    private readonly Lock _refillLock = new();
#else
    private readonly object _refillLock = new();
#endif
    private int _position = 0;

    /// <summary>
    /// Gets the default shared instance of the entropy provider.
    /// </summary>
    public static EntropyProvider Default { get; } = new EntropyProvider(DefaultBufferSize);

    /// <summary>
    /// Initializes a new instance of the <see cref="EntropyProvider"/> class with a custom buffer size.
    /// </summary>
    /// <param name="bufferSize">The size of the internal buffer used for entropy generation.</param>
    protected EntropyProvider(int bufferSize)
    {
        if(bufferSize <= 0)
        {
            _buffer = [];
            return;
        }
        _buffer = new byte[bufferSize];
        RandomNumberGenerator.Fill(_buffer);
    }

    /// <summary>
    /// Gets a random byte value, ensuring the returned value is at least 37.
    /// Used for increment operations to avoid predictable patterns.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual ulong GetIncrementByte()
    {
        var value = GetSlice(1)[0];
        return value < 37 ? 37UL : value;
    }

    /// <summary>
    /// Gets a 16-bit random value with the top bit cleared (15 bits effective).
    /// The top bit is reserved as a carry bit for monotonic ID generation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual ulong Get16Bits()
    {
        var value = BinaryPrimitives.ReadUInt16BigEndian(GetSlice(2));
        return (ulong)(value & 0x7FFF); // Clear top bit
    }

    /// <summary>
    /// Gets a 24-bit random value with the top bit cleared (23 bits effective).
    /// The top bit is reserved as a carry bit for monotonic ID generation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual ulong Get24Bits()
    {
        ReadOnlySpan<byte> slice = GetSlice(3);
        var value = ((ulong)slice[0] << 16)
            | ((ulong)slice[1] << 8)
            | slice[2];
        return value & 0x7FFFFF; // Clear top bit (24 bits -> 23 effective)
    }

    /// <summary>
    /// Gets a 32-bit random value with the top bit cleared (31 bits effective).
    /// The top bit is reserved as a carry bit for monotonic ID generation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual ulong Get32Bits()
    {
        var value = BinaryPrimitives.ReadUInt32BigEndian(GetSlice(4));
        return value & 0x7FFFFFFF; // Clear top bit
    }

    /// <summary>
    /// Gets a 40-bit random value with the top bit cleared (39 bits effective).
    /// The top bit is reserved as a carry bit for monotonic ID generation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual ulong Get40Bits()
    {
        ReadOnlySpan<byte> slice = GetSlice(5);
        var value = ((ulong)slice[0] << 32)
            | BinaryPrimitives.ReadUInt32BigEndian(slice[1..]);
        return value & 0x7FFFFFFFFF; // Clear top bit (40 bits -> 39 effective)
    }

    /// <summary>
    /// Gets a 48-bit random value with the top bit cleared (47 bits effective).
    /// The top bit is reserved as a carry bit for monotonic ID generation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual ulong Get48Bits()
    {
        ReadOnlySpan<byte> slice = GetSlice(6);
        var value = ((ulong)BinaryPrimitives.ReadUInt16BigEndian(slice[..2]) << 32)
            | BinaryPrimitives.ReadUInt32BigEndian(slice[2..]);
        return value & 0x7FFFFFFFFFFF; // Clear top bit (48 bits -> 47 effective)
    }

    /// <summary>
    /// Gets a 56-bit random value with the top bit cleared (55 bits effective).
    /// The top bit is reserved as a carry bit for monotonic ID generation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual ulong Get56Bits()
    {
        ReadOnlySpan<byte> slice = GetSlice(7);
        var value = ((ulong)BinaryPrimitives.ReadUInt16BigEndian(slice[..2]) << 40)
            | ((ulong)slice[2] << 32)
            | BinaryPrimitives.ReadUInt32BigEndian(slice[3..]);
        return value & 0x7FFFFFFFFFFFFF; // Clear top bit (56 bits -> 55 effective)
    }

    /// <summary>
    /// Gets a 64-bit random value with the top bit cleared (63 bits effective).
    /// The top bit is reserved as a carry bit for monotonic ID generation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual ulong Get64Bits()
    {
        var value = BinaryPrimitives.ReadUInt64BigEndian(GetSlice(8));
        return value & 0x7FFFFFFFFFFFFFFF; // Clear top bit
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
