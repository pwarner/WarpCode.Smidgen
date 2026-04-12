using System.Collections.Concurrent;

namespace WarpCode.Smidgen.Tests;

public class EntropyProviderTests
{
    private const int MinimumUniqueByteValues = 200;
    [Fact]
    public void Shared_ReturnsSameInstance()
    {
        EntropyProvider instance1 = EntropyProvider.Shared;
        EntropyProvider instance2 = EntropyProvider.Shared;

        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void GetByte_ReturnsVariedValues()
    {
        EntropyProvider provider = EntropyProvider.Shared;
        var values = new HashSet<byte>();

        for (var i = 0; i < 1000; i++)
            values.Add(provider.GetByte());

        // Should have multiple unique values (at least 200 out of 256 possible)
        Assert.True(values.Count > MinimumUniqueByteValues, $"Expected varied values, got only {values.Count} unique values");
    }

    [Fact]
    public void GetByte_ThreadSafe_ProducesVariedValues()
    {
        EntropyProvider provider = EntropyProvider.Shared;
        const int threadCount = 10;
        const int iterationsPerThread = 100;
        var values = new ConcurrentBag<byte>();

        Parallel.For(0, threadCount, _ =>
        {
            for (var i = 0; i < iterationsPerThread; i++)
                values.Add(provider.GetByte());
        });

        Assert.Equal(threadCount * iterationsPerThread, values.Count);

        // Should have reasonable uniqueness
        var uniqueCount = values.Distinct().Count();
        Assert.True(uniqueCount > MinimumUniqueByteValues, $"Expected >{MinimumUniqueByteValues} unique byte values, got {uniqueCount}");
    }

    [Fact]
    public void GetBytes_FillsBuffer()
    {
        EntropyProvider provider = EntropyProvider.Shared;
        Span<byte> buffer = stackalloc byte[10];

        provider.GetBytes(buffer);

        // All bytes should have been filled (non-zero at least once in multiple tries)
        var hasNonZero = false;
        for (var i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != 0)
            {
                hasNonZero = true;
                break;
            }
        }

        Assert.True(hasNonZero, "Expected at least one non-zero byte");
    }

    [Fact]
    public void GetBytes_EmptyBuffer_DoesNotThrow()
    {
        EntropyProvider provider = EntropyProvider.Shared;
        byte[] buffer = [];

        // Should not throw when called directly
        provider.GetBytes(buffer);

        // Test passed if we got here
        Assert.True(true);
    }

    [Fact]
    public void GetBytes_LargeBuffer_WorksCorrectly()
    {
        EntropyProvider provider = EntropyProvider.Shared;
        var buffer = new byte[1000];

        provider.GetBytes(buffer);

        // Check that we got varied data
        var distinctBytes = buffer.Distinct().Count();
        Assert.True(distinctBytes > MinimumUniqueByteValues, $"Expected varied data, got only {distinctBytes} distinct byte values");
    }

    [Fact]
    public void GetBytes_MultipleCallsProduceDifferentResults()
    {
        EntropyProvider provider = EntropyProvider.Shared;
        var buffer1 = new byte[20];
        var buffer2 = new byte[20];

        provider.GetBytes(buffer1);
        provider.GetBytes(buffer2);

        // Buffers should be different (extremely unlikely to be identical with 20 random bytes)
        Assert.False(buffer1.SequenceEqual(buffer2), "Expected different random data in consecutive calls");
    }

    [Fact]
    public void GetBytes_ThreadSafe_ProducesUniqueData()
    {
        EntropyProvider provider = EntropyProvider.Shared;
        const int threadCount = 10;
        const int bufferSize = 16;
        var results = new ConcurrentBag<byte[]>();

        Parallel.For(0, threadCount, _ =>
        {
            var buffer = new byte[bufferSize];
            provider.GetBytes(buffer);
            results.Add(buffer);
        });

        Assert.Equal(threadCount, results.Count);

        // Convert to set of strings for easy comparison
        var uniqueBuffers = results.Select(Convert.ToBase64String).Distinct().Count();

        // All should be unique
        Assert.Equal(threadCount, uniqueBuffers);
    }

    [Fact]
    public void CustomBufferSize_CreatesNewInstance()
    {
        var provider1 = new EntropyProvider(1024);
        var provider2 = new EntropyProvider(2048);

        Assert.NotSame(provider1, provider2);
    }

    [Fact]
    public void CustomBufferSize_WorksCorrectly()
    {
        var provider = new EntropyProvider(512);
        var buffer = new byte[100];

        Exception? exception = Record.Exception(() => provider.GetBytes(buffer));

        Assert.Null(exception);
    }

    [Fact]
    public void BufferRefill_LargeNumberOfCalls_DoesNotThrow()
    {
        EntropyProvider provider = EntropyProvider.Shared;

        // This test ensures the buffer refill mechanism works correctly
        // by requesting more entropy than the default buffer size (2048 bytes)
        Exception? exception = Record.Exception(() =>
        {
            for (var i = 0; i < 1000; i++)
            {
                var buffer = new byte[8];
                provider.GetBytes(buffer);
            }
        });

        Assert.Null(exception);
    }

    [Fact]
    public void ThreadSafety_MixedGetByteAndGetBytes_ProducesVariedValues()
    {
        EntropyProvider provider = EntropyProvider.Shared;
        const int iterations = 500;
        var bytes = new ConcurrentBag<byte>();
        var buffers = new ConcurrentBag<byte[]>();

        Parallel.For(0, iterations, _ =>
        {
            bytes.Add(provider.GetByte());

            var buffer = new byte[4];
            provider.GetBytes(buffer);
            buffers.Add(buffer);
        });

        Assert.Equal(iterations, bytes.Count);
        Assert.Equal(iterations, buffers.Count);

        // Check for reasonable uniqueness
        Assert.True(bytes.Distinct().Count() > MinimumUniqueByteValues);
        Assert.True(buffers.Select(Convert.ToBase64String).Distinct().Count() > 400);
    }
}
