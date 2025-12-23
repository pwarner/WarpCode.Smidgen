using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace WarpCode.Smidgen;

/// <summary>
/// Provides thread-safe helper functions for getting cryptographically secure random entropy elements of varying widths.
/// Uses a shared buffer that is refilled as needed to balance security and performance.
/// </summary>
internal static class EntropyElements
{
    private const int DefaultBufferSize = 4096;
    private static readonly byte[] s_buffer = new byte[DefaultBufferSize];
    #if NET10_0_OR_GREATER
    private static readonly Lock s_refillLock = new();
    #else
    private static readonly object s_refillLock = new();
    #endif
    private static int _position = 0;
    static EntropyElements() =>
        // Initialize buffer on first use
        RandomNumberGenerator.Fill(s_buffer);

    /// <summary>
    /// Gets a random byte value, ensuring the returned value is at least 37.
    /// Used for increment operations to avoid predictable patterns.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetIncrementByte()
    {
        var value = GetSlice(1)[0];
        return value < 37 ? 37UL : value;
    }

    /// <summary>
    /// Gets a 16-bit random value with the top bit cleared (15 bits effective).
    /// The top bit is reserved as a carry bit for monotonic ID generation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Get16Bits()
    {
        var value = BinaryPrimitives.ReadUInt16BigEndian(GetSlice(2));
        return (ulong)(value & 0x7FFF); // Clear top bit
    }

    /// <summary>
    /// Gets a 24-bit random value with the top bit cleared (23 bits effective).
    /// The top bit is reserved as a carry bit for monotonic ID generation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Get24Bits()
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
    public static ulong Get32Bits()
    {
        var value = BinaryPrimitives.ReadUInt32BigEndian(GetSlice(4));
        return value & 0x7FFFFFFF; // Clear top bit
    }

    /// <summary>
    /// Gets a 40-bit random value with the top bit cleared (39 bits effective).
    /// The top bit is reserved as a carry bit for monotonic ID generation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Get40Bits()
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
    public static ulong Get48Bits()
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
    public static ulong Get56Bits()
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
    public static ulong Get64Bits()
    {
        var value = BinaryPrimitives.ReadUInt64BigEndian(GetSlice(8));
        return value & 0x7FFFFFFFFFFFFFFF; // Clear top bit
    }

    /// <summary>
    /// Gets a slice of the random buffer with the specified number of bytes.
    /// Ensures the buffer has enough bytes available and refills if needed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<byte> GetSlice(int bytesNeeded)
    {
        while (true)
        {
            var currentPos = _position;

            // Check if we need to refill
            if (currentPos > DefaultBufferSize - bytesNeeded)
            {
                lock (s_refillLock)
                {
                    // Double-check after acquiring lock
                    if (_position > DefaultBufferSize - bytesNeeded)
                    {
                        RandomNumberGenerator.Fill(s_buffer);
                        _position = 0;
                    }
                }
                continue; // Retry after refill
            }

            // Try to claim the position atomically
            var newPos = currentPos + bytesNeeded;
            if (Interlocked.CompareExchange(ref _position, newPos, currentPos) == currentPos)
                return s_buffer.AsSpan(currentPos, bytesNeeded);

            // If CAS failed, retry
        }
    }
}
