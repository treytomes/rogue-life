namespace DungeonLife;

/// <summary>
/// A deterministic, seedable source of pseudo-random numbers.
/// </summary>
/// <remarks>
/// <para>
/// Determinism is a hard requirement of this project: a run must be reproducible from its seed
/// so that experiments on the learning rule can be compared across conditions rather than across
/// lucky dice. Every stochastic path in the simulation — network initialization, action
/// sampling, food and heat-lamp placement, exploration noise — draws from an <see cref="IRandom"/>
/// rather than from an ambient <see cref="Random"/>. Nothing in <c>WormForaging.Core</c>
/// is allowed to reach for the system clock or a global RNG.
/// </para>
/// <para>
/// Routing all randomness through this interface also makes the simulation testable: a test can
/// inject a fixed-seed generator for repeatable assertions, or a scripted fake that returns a
/// chosen sequence to drive the agent down a specific path.
/// </para>
/// </remarks>
public interface IRandom
{
    /// <summary>
    /// Reset the generator to its initial state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Reset the generator to a new seed and optional sequence.
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
    void Reseed(ulong seed, ulong sequence = 0);

    /// <summary>
    /// Returns the next uniformly distributed 32-bit unsigned value, covering the full range
    /// <c>[0, 2^32)</c>. This is the generator's primitive output; the other members are derived
    /// from it.
    /// </summary>
    /// <returns>A pseudo-random <see cref="uint"/>.</returns>
    uint NextUInt();

    /// <summary>
    /// Returns a uniformly distributed double in the half-open interval <c>[0, 1)</c>.
    /// </summary>
    /// <returns>A pseudo-random <see cref="double"/> that is at least 0 and strictly less than 1.</returns>
    double NextDouble();

    /// <summary>
    /// Returns a uniformly distributed integer in the half-open interval
    /// <c>[0, <paramref name="maxExclusive"/>)</c>, without modulo bias.
    /// </summary>
    /// <param name="maxExclusive">The exclusive upper bound. Must be greater than zero.</param>
    /// <returns>A pseudo-random <see cref="int"/> in <c>[0, <paramref name="maxExclusive"/>)</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxExclusive"/> is less than or equal to zero.
    /// </exception>
    int NextInt(int maxExclusive);

    /// <summary>
    /// Returns a sample from the standard normal distribution (mean 0, standard deviation 1).
    /// </summary>
    /// <remarks>
    /// Mirrors the <c>randn()</c> helper from the original browser prototype, which the weight and
    /// wiring initializers scale to the variances they need.
    /// </remarks>
    /// <returns>A pseudo-random, normally distributed <see cref="double"/>.</returns>
    double NextGaussian();
}


