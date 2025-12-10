namespace WarpCode.Smidgen;

public sealed class IdGenerator
{
    public IdGenerator(Func<long>? timeProvider = null, Func<int>? entropyProvider = null)
    {
        _timeProvider = timeProvider ?? DefaultTimeProvider;
        _entropyProvider = entropyProvider ?? DefaultEntropyProvider;
    }
    public IdGenerator()
    {
        _timeProvider = DefaultTimeProvider;
        _entropyProvider = DefaultEntropyProvider;
    }

    public const byte TimeWidth = 48;
    public const byte RandomWidth = 64 - TimeWidth;
    public const byte Interval = 39;
    public const ushort IdsPerMs = 200;
    public const int MaxRandom = (1 << RandomWidth) - Interval * IdsPerMs;
    public const long MaxTime = (1L << TimeWidth) - 1;
    private readonly Func<long> _timeProvider;
    private readonly Func<int> _entropyProvider;
    private ulong _latest;

    public ulong Generate()
    {
        var time = _timeProvider();
        if (time > MaxTime)
            throw new ArgumentOutOfRangeException(nameof(time),
                $"Time has exceeded max value of {MaxTime}");

        var random = _entropyProvider();
        if (random > MaxRandom)
            throw new ArgumentOutOfRangeException(nameof(random),
                $"Random has exceeded max value of {MaxRandom}");

        ulong id = (ulong)time << RandomWidth | (uint)random;
        ulong initialValue;
        do
        {
            initialValue = _latest;
            id = Math.Max(id, initialValue + Interval);
        } while (initialValue != Interlocked.CompareExchange(ref _latest, id, initialValue));

        return _latest;
    }

    private static long DefaultTimeProvider() => DateTime.UtcNow.Subtract(DateTime.UnixEpoch).Ticks / 10000;

    private static int DefaultEntropyProvider() => Random.Shared.Next(MaxRandom);
}
