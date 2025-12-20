namespace WarpCode.Smidgen;

/// <summary>
/// Defines preset configurations for ID generation with different size and precision characteristics.
/// </summary>
public enum GeneratorPreset
{
    /// <summary>
    /// Small 64-bit identifiers using 48 bits for millisecond-precision time and 16 bits for entropy.
    /// </summary>
    SmallId,

    /// <summary>
    /// 80-bit identifiers using 48 bits for millisecond-precision time and 32 bits for entropy.
    /// </summary>
    Id80,

    /// <summary>
    /// 96-bit identifiers using 56 bits for microsecond-precision time and 40 bits for entropy.
    /// </summary>
    Id96,

    /// <summary>
    /// Large 128-bit identifiers using 64 bits for tick-precision time and 64 bits for entropy.
    /// </summary>
    BigId
}
