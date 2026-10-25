using System;

namespace DungeonLife;

/// <summary>
/// A seedable pseudo-random generator implementing the PCG32 algorithm
/// (permuted congruential generator, "XSH RR" 64/32 variant by Melissa O'Neill).
/// </summary>
/// <remarks>
/// <para>
/// PCG32 is small (two 64-bit words of state), fast, and has markedly better statistical quality
/// than a bare linear congruential generator while remaining trivial to reproduce across machines
/// and languages — exactly what a reproducible simulation needs. The same
/// (<c>seed</c>, <c>sequence</c>) pair always yields the same stream.
/// </para>
/// <para>
/// This type is <b>not</b> thread-safe: a single instance mutates its state on every draw. Give
/// each thread or independent stochastic component its own instance with its own seed (or distinct
/// <c>sequence</c>) rather than sharing one.
/// </para>
/// <para>Reference: https://www.pcg-random.org/ .</para>
/// </remarks>
public sealed class Pcg32Random : IRandom
{
    #region Constants

    /// <summary>The LCG multiplier specified by PCG32. Do not change; it defines the stream.</summary>
    private const ulong MULTIPLIER = 6364136223846793005UL;

    #endregion

    #region Fields

    private ulong _seed;

    private ulong _sequence;

    /// <summary>The internal 64-bit LCG state. Advances on every <see cref="NextUInt"/>.</summary>
    private ulong _state;

    /// <summary>
    /// The stream selector, forced odd. Two generators with the same seed but different
    /// <see cref="_increment"/> traverse distinct, non-overlapping sequences.
    /// </summary>
    private ulong _increment;

    /// <summary>Holds the second value produced by a Box–Muller pair until the next Gaussian draw.</summary>
    private double _spareGaussian;

    /// <summary>Whether <see cref="_spareGaussian"/> currently holds an unused sample.</summary>
    private bool _hasSpareGaussian;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a generator seeded deterministically from the given values.
    /// </summary>
    /// <param name="seed">
    /// The initial state seed. Any value is valid; a given seed fully determines the stream
    /// (together with <paramref name="sequence"/>).
    /// </param>
    /// <param name="sequence">
    /// Selects which of the 2^63 distinct streams to draw from. Two instances that share a
    /// <paramref name="seed"/> but differ here produce independent sequences, which is useful for
    /// giving separate simulation components their own uncorrelated randomness.
    /// </param>
    public Pcg32Random(ulong seed, ulong sequence = 0)
    {
        Reseed(seed, sequence);
    }

    /// <summary>Captures every mutable value needed to resume this exact random stream.</summary>
    public Pcg32RandomState CaptureState() => new(
        _seed, _sequence, _state, _increment, _spareGaussian, _hasSpareGaussian);

    /// <summary>Restores a previously captured stream state.</summary>
    public void RestoreState(Pcg32RandomState state)
    {
        if ((state.Increment & 1UL) == 0)
        {
            throw new ArgumentException("PCG increment must be odd.", nameof(state));
        }

        _seed = state.Seed;
        _sequence = state.Sequence;
        _state = state.State;
        _increment = state.Increment;
        _spareGaussian = state.SpareGaussian;
        _hasSpareGaussian = state.HasSpareGaussian;
    }

    #endregion

    #region Methods

    /// <inheritdoc />
    public void Reset()
    {
        Reseed(_seed, _sequence);
    }

    /// <inheritdoc />
    public void Reseed(ulong seed, ulong sequence = 0)
    {
        _seed = seed;
        _sequence = sequence;
        
        // Standard PCG seeding routine: set the (odd) increment, then fold the seed into the state
        // across two advances so that nearby seeds produce well-separated streams.
        _increment = (sequence << 1) | 1UL;
        _state = 0UL;
        StepState();
        _state += seed;
        StepState();

        // Discard any half-used Box-Muller pair so a (re)seed returns the generator to the exact
        // state of a freshly constructed one. Leaving a cached sample here would make the next
        // NextGaussian return pre-reset data, silently breaking determinism after a Reset.
        _hasSpareGaussian = false;
        _spareGaussian = 0.0;
    }

    /// <inheritdoc />
    public uint NextUInt()
    {
        ulong oldState = _state;
        StepState();

        // Output permutation (XSH RR): xorshift the high bits down, then rotate by the top 5 bits.
        uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
        int rot = (int)(oldState >> 59);
        return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
    }

    /// <inheritdoc />
    public double NextDouble()
    {
        // Divide by 2^32 to land in [0, 1): the largest possible numerator is 2^32 - 1.
        return NextUInt() * (1.0 / 4294967296.0);
    }

    /// <inheritdoc />
    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxExclusive), maxExclusive, "Upper bound must be greater than zero.");
        }

        // Unbiased bounded sampling by rejection: discard the low "remainder" band of the 32-bit
        // range so that every value in [0, bound) is equally likely (plain modulo would favour the
        // smaller residues).
        uint bound = (uint)maxExclusive;
        uint threshold = (uint)-(int)bound % bound; // == 2^32 mod bound
        while (true)
        {
            uint candidate = NextUInt();
            if (candidate >= threshold)
            {
                return (int)(candidate % bound);
            }
        }
    }

    /// <inheritdoc />
    public double NextGaussian()
    {
        // Box–Muller produces two independent standard-normal samples per pair of uniforms; cache
        // the second so every other call is almost free and no draws are wasted.
        if (_hasSpareGaussian)
        {
            _hasSpareGaussian = false;
            return _spareGaussian;
        }

        double u1;
        do
        {
            u1 = NextDouble();
        }
        while (u1 <= double.Epsilon); // guard against log(0)

        double u2 = NextDouble();
        double magnitude = Math.Sqrt(-2.0 * Math.Log(u1));
        double angle = 2.0 * Math.PI * u2;

        _spareGaussian = magnitude * Math.Sin(angle);
        _hasSpareGaussian = true;
        return magnitude * Math.Cos(angle);
    }

    /// <summary>Advances the internal LCG state by one step.</summary>
    private void StepState() => _state = (_state * MULTIPLIER) + _increment;

    #endregion
}

