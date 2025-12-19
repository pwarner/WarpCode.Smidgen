using System.Collections.Concurrent;

namespace WarpCode.Smidgen.Tests;

public class EntropyElementsTests
{
    [Fact]
    public void GetIncrementByte_ReturnsValueAtLeast37()
    {
        for (var i = 0; i < 1000; i++)
        {
            var value = EntropyElements.GetIncrementByte();
            Assert.True(value >= 37, $"Expected value >= 37, but got {value}");
        }
    }

    [Fact]
    public void GetIncrementByte_ReturnsVariedValues()
    {
        var values = new HashSet<ulong>();
        for (var i = 0; i < 1000; i++)
            values.Add(EntropyElements.GetIncrementByte());

        // Should have multiple unique values (not all the same)
        Assert.True(values.Count > 10, $"Expected varied values, got only {values.Count} unique values");
    }

    [Fact]
    public void Get16Bits_ReturnsValueWithTopBitCleared()
    {
        for (var i = 0; i < 1000; i++)
        {
            var value = EntropyElements.Get16Bits();
            // Top bit should be 0, meaning value should be <= 0x7FFF
            Assert.True(value <= 0x7FFF, $"Expected value <= 0x7FFF, but got 0x{value:X4}");
        }
    }

    [Fact]
    public void Get16Bits_ReturnsVariedValues()
    {
        var values = new HashSet<ulong>();
        for (var i = 0; i < 1000; i++)
            values.Add(EntropyElements.Get16Bits());

        Assert.True(values.Count > 100, $"Expected varied values, got only {values.Count} unique values");
    }

    [Fact]
    public void Get32Bits_ReturnsValueWithTopBitCleared()
    {
        for (var i = 0; i < 1000; i++)
        {
            var value = EntropyElements.Get32Bits();
            // Top bit should be 0, meaning value should be <= 0x7FFFFFFF
            Assert.True(value <= 0x7FFFFFFF, $"Expected value <= 0x7FFFFFFF, but got 0x{value:X8}");
        }
    }

    [Fact]
    public void Get32Bits_ReturnsVariedValues()
    {
        var values = new HashSet<ulong>();
        for (var i = 0; i < 1000; i++)
            values.Add(EntropyElements.Get32Bits());

        Assert.True(values.Count > 900, $"Expected varied values, got only {values.Count} unique values");
    }

    [Fact]
    public void Get40Bits_ReturnsValueWithTopBitCleared()
    {
        for (var i = 0; i < 1000; i++)
        {
            var value = EntropyElements.Get40Bits();
            // Top bit should be 0, meaning value should be <= 0x7FFFFFFFFF (40 bits, top bit clear)
            Assert.True(value <= 0x7FFFFFFFFF, $"Expected value <= 0x7FFFFFFFFF, but got 0x{value:X10}");
        }
    }

    [Fact]
    public void Get40Bits_ReturnsVariedValues()
    {
        var values = new HashSet<ulong>();
        for (var i = 0; i < 1000; i++)
            values.Add(EntropyElements.Get40Bits());

        Assert.True(values.Count > 900, $"Expected varied values, got only {values.Count} unique values");
    }

    [Fact]
    public void Get48Bits_ReturnsValueWithTopBitCleared()
    {
        for (var i = 0; i < 1000; i++)
        {
            var value = EntropyElements.Get48Bits();
            // Top bit should be 0, meaning value should be <= 0x7FFFFFFFFFFF (48 bits, top bit clear)
            Assert.True(value <= 0x7FFFFFFFFFFF, $"Expected value <= 0x7FFFFFFFFFFF, but got 0x{value:X12}");
        }
    }

    [Fact]
    public void Get48Bits_ReturnsVariedValues()
    {
        var values = new HashSet<ulong>();
        for (var i = 0; i < 1000; i++)
            values.Add(EntropyElements.Get48Bits());

        Assert.True(values.Count > 900, $"Expected varied values, got only {values.Count} unique values");
    }

    [Fact]
    public void Get64Bits_ReturnsValueWithTopBitCleared()
    {
        for (var i = 0; i < 1000; i++)
        {
            var value = EntropyElements.Get64Bits();
            // Top bit should be 0, meaning value should be <= 0x7FFFFFFFFFFFFFFF
            Assert.True(value <= 0x7FFFFFFFFFFFFFFF, $"Expected value <= 0x7FFFFFFFFFFFFFFF, but got 0x{value:X16}");
        }
    }

    [Fact]
    public void Get64Bits_ReturnsVariedValues()
    {
        var values = new HashSet<ulong>();
        for (var i = 0; i < 1000; i++)
            values.Add(EntropyElements.Get64Bits());

        Assert.True(values.Count > 900, $"Expected varied values, got only {values.Count} unique values");
    }

    [Fact]
    public void ThreadSafety_ConcurrentAccess_ProducesUniqueValues()
    {
        const int threadCount = 10;
        const int iterationsPerThread = 1000;
        var values = new ConcurrentBag<ulong>();

        Parallel.For(0, threadCount, _ =>
        {
            for (var i = 0; i < iterationsPerThread; i++)
                values.Add(EntropyElements.Get64Bits());
        });

        var uniqueCount = values.Distinct().Count();
        var totalCount = threadCount * iterationsPerThread;

        // Should have high uniqueness (>95%)
        Assert.True(uniqueCount > totalCount * 0.95,
            $"Expected >95% unique values, got {uniqueCount}/{totalCount} ({(double)uniqueCount / totalCount:P})");
    }

    [Fact]
    public void ThreadSafety_Mixed_ProducesVariedValues()
    {
        const int iterations = 1000;
        var bytes = new ConcurrentBag<ulong>();
        var shorts = new ConcurrentBag<ulong>();
        var ints = new ConcurrentBag<ulong>();
        var longs = new ConcurrentBag<ulong>();

        Parallel.For(0, iterations, i =>
        {
            bytes.Add(EntropyElements.GetIncrementByte());
            shorts.Add(EntropyElements.Get16Bits());
            ints.Add(EntropyElements.Get32Bits());
            longs.Add(EntropyElements.Get64Bits());
        });

        Assert.Equal(iterations, bytes.Count);
        Assert.Equal(iterations, shorts.Count);
        Assert.Equal(iterations, ints.Count);
        Assert.Equal(iterations, longs.Count);

        // Check for reasonable uniqueness
        Assert.True(bytes.Distinct().Count() > 50);
        Assert.True(shorts.Distinct().Count() > 500);
        Assert.True(ints.Distinct().Count() > 900);
        Assert.True(longs.Distinct().Count() > 900);
    }

    [Fact]
    public void BufferRefill_LargeNumberOfCalls_DoesNotThrow()
    {
        // This test ensures the buffer refill mechanism works correctly
        // by requesting more entropy than the buffer size (4096 bytes)
        Exception? exception = Record.Exception(() =>
        {
            for (var i = 0; i < 10000; i++)
                EntropyElements.Get64Bits();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Get40Bits_ValueFitsIn40Bits()
    {
        for (var i = 0; i < 100; i++)
        {
            var value = EntropyElements.Get40Bits();
            // 40 bits max value is 0xFFFFFFFFFF, with top bit clear it's 0x7FFFFFFFFF
            Assert.True(value < 1UL << 40, $"Value {value:X} exceeds 40 bits");
        }
    }

    [Fact]
    public void Get48Bits_ValueFitsIn48Bits()
    {
        for (var i = 0; i < 100; i++)
        {
            var value = EntropyElements.Get48Bits();
            // 48 bits max value is 0xFFFFFFFFFFFF, with top bit clear it's 0x7FFFFFFFFFFF
            Assert.True(value < 1UL << 48, $"Value {value:X} exceeds 48 bits");
        }
    }

    [Fact]
    public void AllMethods_ReturnNonZeroValuesEventually()
    {
        // Ensure methods don't consistently return 0 (which would indicate a buffer issue)
        var hasNonZeroByte = false;
        var hasNonZero16 = false;
        var hasNonZero32 = false;
        var hasNonZero40 = false;
        var hasNonZero48 = false;
        var hasNonZero64 = false;

        for (var i = 0; i < 100; i++)
        {
            if (EntropyElements.GetIncrementByte() != 0) hasNonZeroByte = true;
            if (EntropyElements.Get16Bits() != 0) hasNonZero16 = true;
            if (EntropyElements.Get32Bits() != 0) hasNonZero32 = true;
            if (EntropyElements.Get40Bits() != 0) hasNonZero40 = true;
            if (EntropyElements.Get48Bits() != 0) hasNonZero48 = true;
            if (EntropyElements.Get64Bits() != 0) hasNonZero64 = true;
        }

        Assert.True(hasNonZeroByte, "GetIncrementByte returned only zeros");
        Assert.True(hasNonZero16, "Get16Bits returned only zeros");
        Assert.True(hasNonZero32, "Get32Bits returned only zeros");
        Assert.True(hasNonZero40, "Get40Bits returned only zeros");
        Assert.True(hasNonZero48, "Get48Bits returned only zeros");
        Assert.True(hasNonZero64, "Get64Bits returned only zeros");
    }
}
