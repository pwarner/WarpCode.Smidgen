
namespace WarpCode.Smidgen;

public class IdGenerator
{
    private ulong _lastGenerated;

    public virtual string GenerateString() => IdFormatter.Short.ToFormattedString(Generate());

    public ulong Generate()
    {
        ulong current, next = 0;
        do
        {
            current = _lastGenerated;
            if (next <= current)
                next = MoreThan(current);

        } while (current != Interlocked.CompareExchange(ref _lastGenerated, next, current));

        return next;
    }

    protected virtual ushort NextEntropy()
    {
        Span<byte> bytes = stackalloc byte[2];
        Random.Shared.NextBytes(bytes);
        return BitConverter.ToUInt16(bytes);
    }

    protected virtual DateTime Epoch() => DateTime.UnixEpoch;

    protected virtual DateTime NextTime() => DateTime.UtcNow;

    private ulong MoreThan(ulong current)
    {
        var next = Calculate();
        return next > current ? next : current + 13;
    }
    private ulong Calculate()
    {
        var time = (ulong)NextTime().Subtract(Epoch()).TotalMilliseconds;
        return time << 16 | NextEntropy();
    }
}
