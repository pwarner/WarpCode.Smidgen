namespace WarpCode.Smidgen;

/// <summary>
/// Provides configuration options for ID generation with a fluent API.
/// </summary>
public sealed class GeneratorOptions
{
    private readonly DateTime _now;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratorOptions"/> class.
    /// </summary>
    internal GeneratorOptions() => _now = DateTime.UtcNow;

    /// <summary>
    /// Gets the time accuracy configured for ID generation.
    /// </summary>
    internal TimeAccuracy TimeAccuracy { get; private set; } = TimeAccuracy.Milliseconds;

    /// <summary>
    /// Gets the entropy size configured for ID generation.
    /// </summary>
    internal EntropySize EntropySize { get; private set; } = EntropySize.Bits16;

    /// <summary>
    /// Gets the custom epoch (start date) for time component calculation.
    /// </summary>
    internal DateTime SinceEpoch { get; private set; } = DateTime.UnixEpoch;

    /// <summary>
    /// Gets the end date for time component calculation.
    /// </summary>
    internal DateTime UntilDate { get; private set; } = DateTime.MaxValue;

    /// <summary>
    /// Configures the generator to use a preset configuration.
    /// This will override any previous time accuracy and entropy size settings, but will not affect Since/Until.
    /// </summary>
    /// <param name="preset">The preset configuration to use.</param>
    /// <returns>The current <see cref="GeneratorOptions"/> instance for method chaining.</returns>
    public GeneratorOptions UsePreset(GeneratorPreset preset)
    {
        switch (preset)
        {
            case GeneratorPreset.SmallId:
                TimeAccuracy = TimeAccuracy.Milliseconds;
                EntropySize = EntropySize.Bits16;
                break;

            case GeneratorPreset.Id80:
                TimeAccuracy = TimeAccuracy.Milliseconds;
                EntropySize = EntropySize.Bits32;
                break;

            case GeneratorPreset.Id96:
                TimeAccuracy = TimeAccuracy.Microseconds;
                EntropySize = EntropySize.Bits40;
                break;

            case GeneratorPreset.BigId:
                TimeAccuracy = TimeAccuracy.Ticks;
                EntropySize = EntropySize.Bits64;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(preset), preset, "Invalid preset value.");
        }

        return this;
    }

    /// <summary>
    /// Configures the time accuracy for the time component of generated IDs.
    /// </summary>
    /// <param name="timeAccuracy">The time accuracy to use.</param>
    /// <returns>The current <see cref="GeneratorOptions"/> instance for method chaining.</returns>
    public GeneratorOptions WithTimeAccuracy(TimeAccuracy timeAccuracy)
    {
        TimeAccuracy = timeAccuracy;
        return this;
    }

    /// <summary>
    /// Configures the entropy size for the entropy component of generated IDs.
    /// </summary>
    /// <param name="entropySize">The entropy size to use.</param>
    /// <returns>The current <see cref="GeneratorOptions"/> instance for method chaining.</returns>
    public GeneratorOptions WithEntropySize(EntropySize entropySize)
    {
        EntropySize = entropySize;
        return this;
    }

    /// <summary>
    /// Configures a custom epoch (start date) for time component calculation.
    /// </summary>
    /// <param name="since">The custom epoch date. Must be less than the current time.</param>
    /// <returns>The current <see cref="GeneratorOptions"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="since"/> is greater than or equal to the current time.</exception>
    public GeneratorOptions Since(DateTime since)
    {
        if (since >= _now)
            throw new ArgumentOutOfRangeException(nameof(since), since, $"Since date must be less than the current time ({_now:O}).");

        SinceEpoch = since;
        return this;
    }

    /// <summary>
    /// Configures a custom end date for time component calculation.
    /// </summary>
    /// <param name="until">The end date. Must be greater than the current time.</param>
    /// <returns>The current <see cref="GeneratorOptions"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="until"/> is less than or equal to the current time.</exception>
    public GeneratorOptions Until(DateTime until)
    {
        if (until <= _now)
            throw new ArgumentOutOfRangeException(nameof(until), until, $"Until date must be greater than the current time ({_now:O}).");

        UntilDate = until;
        return this;
    }
}
