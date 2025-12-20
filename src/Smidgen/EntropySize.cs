namespace WarpCode.Smidgen;

/// <summary>
/// Defines the number of bits allocated for the entropy component of generated identifiers.
/// </summary>
public enum EntropySize
{
    /// <summary>
    /// 16 bits of entropy (15 bits effective with top bit reserved).
    /// </summary>
    Bits16 = 16,

    /// <summary>
    /// 24 bits of entropy (23 bits effective with top bit reserved).
    /// </summary>
    Bits24 = 24,

    /// <summary>
    /// 32 bits of entropy (31 bits effective with top bit reserved).
    /// </summary>
    Bits32 = 32,

    /// <summary>
    /// 40 bits of entropy (39 bits effective with top bit reserved).
    /// </summary>
    Bits40 = 40,

    /// <summary>
    /// 48 bits of entropy (47 bits effective with top bit reserved).
    /// </summary>
    Bits48 = 48,

    /// <summary>
    /// 56 bits of entropy (55 bits effective with top bit reserved).
    /// </summary>
    Bits56 = 56,

    /// <summary>
    /// 64 bits of entropy (63 bits effective with top bit reserved).
    /// </summary>
    Bits64 = 64
}
