namespace RogueLife;

/// <summary>The complete resumable state of a PCG32 stream.</summary>
public readonly record struct Pcg32RandomState(
    ulong Seed, ulong Sequence, ulong State, ulong Increment,
    double SpareGaussian, bool HasSpareGaussian);
