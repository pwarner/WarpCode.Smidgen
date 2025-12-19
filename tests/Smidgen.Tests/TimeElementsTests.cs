namespace WarpCode.Smidgen.Tests;

public class TimeElementsTests
{
    [Fact]
    public void MillisecondsSinceUnixEpoch_ReturnsPositiveValue()
    {
        var result = TimeElements.MillisecondsSinceUnixEpoch();

        Assert.True(result > 0);
    }

    [Fact]
    public void MillisecondsSinceUnixEpoch_IsMonotonicallyIncreasing()
    {
        var first = TimeElements.MillisecondsSinceUnixEpoch();
        Thread.Sleep(10);
        var second = TimeElements.MillisecondsSinceUnixEpoch();

        Assert.True(second >= first);
    }

    [Fact]
    public void MicrosecondsSinceUnixEpoch_ReturnsPositiveValue()
    {
        var result = TimeElements.MicrosecondsSinceUnixEpoch();

        Assert.True(result > 0);
    }

    [Fact]
    public void MicrosecondsSinceUnixEpoch_IsMonotonicallyIncreasing()
    {
        var first = TimeElements.MicrosecondsSinceUnixEpoch();
        Thread.Sleep(1);
        var second = TimeElements.MicrosecondsSinceUnixEpoch();

        Assert.True(second > first);
    }

    [Fact]
    public void TicksSinceUnixEpoch_ReturnsPositiveValue()
    {
        var result = TimeElements.TicksSinceUnixEpoch();

        Assert.True(result > 0);
    }

    [Fact]
    public void TicksSinceUnixEpoch_IsMonotonicallyIncreasing()
    {
        var first = TimeElements.TicksSinceUnixEpoch();
        var second = TimeElements.TicksSinceUnixEpoch();

        Assert.True(second >= first);
    }

    [Fact]
    public void DateTimeFromMillisecondsSinceUnixEpoch_RoundTrip_IsAccurate()
    {
        var originalMs = TimeElements.MillisecondsSinceUnixEpoch();
        DateTime dateTime = TimeElements.DateTimeFromMillisecondsSinceUnixEpoch(originalMs);
        TimeSpan elapsed = dateTime - DateTime.UnixEpoch;
        var roundTripMs = (ulong)elapsed.TotalMilliseconds;

        Assert.Equal(originalMs, roundTripMs);
    }

    [Fact]
    public void DateTimeFromMicrosecondsSinceUnixEpoch_RoundTrip_IsAccurate()
    {
        var originalUs = TimeElements.MicrosecondsSinceUnixEpoch();
        DateTime dateTime = TimeElements.DateTimeFromMicrosecondsSinceUnixEpoch(originalUs);
        TimeSpan elapsed = dateTime - DateTime.UnixEpoch;
        var roundTripUs = (ulong)(elapsed.Ticks / 10);

        Assert.Equal(originalUs, roundTripUs);
    }

    [Fact]
    public void DateTimeFromTicksSinceUnixEpoch_RoundTrip_IsAccurate()
    {
        var originalTicks = TimeElements.TicksSinceUnixEpoch();
        DateTime dateTime = TimeElements.DateTimeFromTicksSinceUnixEpoch(originalTicks);
        TimeSpan elapsed = dateTime - DateTime.UnixEpoch;
        var roundTripTicks = (ulong)elapsed.Ticks;

        Assert.Equal(originalTicks, roundTripTicks);
    }

    [Fact]
    public void DateTimeFromMillisecondsSinceUnixEpoch_Zero_ReturnsUnixEpoch()
    {
        DateTime result = TimeElements.DateTimeFromMillisecondsSinceUnixEpoch(0);

        Assert.Equal(DateTime.UnixEpoch, result);
    }

    [Fact]
    public void DateTimeFromMicrosecondsSinceUnixEpoch_Zero_ReturnsUnixEpoch()
    {
        DateTime result = TimeElements.DateTimeFromMicrosecondsSinceUnixEpoch(0);

        Assert.Equal(DateTime.UnixEpoch, result);
    }

    [Fact]
    public void DateTimeFromTicksSinceUnixEpoch_Zero_ReturnsUnixEpoch()
    {
        DateTime result = TimeElements.DateTimeFromTicksSinceUnixEpoch(0);

        Assert.Equal(DateTime.UnixEpoch, result);
    }

    [Theory]
    [InlineData(1000UL)]
    [InlineData(86400000UL)] // 1 day in milliseconds
    [InlineData(31536000000UL)] // ~1 year in milliseconds
    public void DateTimeFromMillisecondsSinceUnixEpoch_KnownValues_ReturnsExpectedDateTime(ulong milliseconds)
    {
        DateTime result = TimeElements.DateTimeFromMillisecondsSinceUnixEpoch(milliseconds);
        DateTime expected = DateTime.UnixEpoch.AddMilliseconds(milliseconds);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1000000UL)] // 1 second in microseconds
    [InlineData(86400000000UL)] // 1 day in microseconds
    public void DateTimeFromMicrosecondsSinceUnixEpoch_KnownValues_ReturnsExpectedDateTime(ulong microseconds)
    {
        DateTime result = TimeElements.DateTimeFromMicrosecondsSinceUnixEpoch(microseconds);
        DateTime expected = DateTime.UnixEpoch.AddTicks((long)microseconds * 10);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void MicrosecondsSinceUnixEpoch_IsGreaterThanMilliseconds()
    {
        var milliseconds = TimeElements.MillisecondsSinceUnixEpoch();
        var microseconds = TimeElements.MicrosecondsSinceUnixEpoch();

        // Microseconds should be roughly 1000x larger than milliseconds
        Assert.True(microseconds > milliseconds * 100);
    }

    [Fact]
    public void TicksSinceUnixEpoch_IsGreaterThanMicroseconds()
    {
        var microseconds = TimeElements.MicrosecondsSinceUnixEpoch();
        var ticks = TimeElements.TicksSinceUnixEpoch();

        // Ticks should be roughly 10x larger than microseconds
        Assert.True(ticks > microseconds);
    }
}
