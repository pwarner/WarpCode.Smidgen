
namespace WarpCode.Smidgen;

public class IdGenerator
{
    public const byte TimeWidth = 48;
    public const byte RandomWidth = 64 - TimeWidth;
    public const byte Interval = 39;
    public const ushort IdsPerMs = 500;
    public const ushort Space = Interval * IdsPerMs;
    public const int MaxRandom = (1 << RandomWidth) - Space;
    public const long MaxTime = (1L << TimeWidth) - 1;
    private ulong _latest;

    protected virtual int NextRandom() => Random.Shared.Next(MaxRandom);

    protected virtual DateTime Epoch => DateTime.UnixEpoch;

    protected virtual DateTime NextTime => DateTime.UtcNow;

    public ulong Generate()
    {
        ulong initialValue;
        ulong id = CalculateSmallId();
        do
        {
            initialValue = _latest;
            id = Math.Max(id, initialValue + Interval);
        } while (initialValue != Interlocked.CompareExchange(ref _latest, id, initialValue));

        return _latest;
    }

    private ulong CalculateSmallId()
    {
        var time = NextTime.Subtract(Epoch).Ticks / 10000;
        if (time > MaxTime)
            throw new ArgumentOutOfRangeException(nameof(time),
                $"Time has exceeded max value of {Epoch + TimeSpan.FromMilliseconds(MaxTime)}");

        var random = NextRandom();
        if (random > MaxRandom)
            throw new ArgumentOutOfRangeException(nameof(random),
                $"Random has exceeded max value of {MaxRandom}");

        return (ulong)time << RandomWidth | (uint)random;
    }
}
