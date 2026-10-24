using System;
using System.Numerics;

namespace DungeonLife;

public static class RandomExtensions
{
    public static int Next(this IRandom random) => random.NextInt(int.MaxValue);

    public static int Next(this IRandom random, int maxValue) => maxValue == 0 ? 0 : random.NextInt(maxValue);

    public static int Next(this IRandom random, int minValue, int maxValue)
    {
        if (maxValue < minValue) throw new ArgumentOutOfRangeException(nameof(maxValue));
        if (maxValue == minValue) return minValue;
        return minValue + random.NextInt(checked(maxValue - minValue));
    }

    public static Vector2 NextDirection(this IRandom random) =>
        Vector2.Normalize(new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 2.0f - Vector2.One);

    public static TEnum NextEnum<TEnum>(this IRandom random) where TEnum : struct, Enum
    {
        TEnum[] values = Enum.GetValues<TEnum>();
        return values[random.NextInt(values.Length)];
    }
}

/// <summary>Serializes access to a run-owned generator while simulation updates execute in parallel.</summary>
public sealed class SynchronizedRandom : IRandom
{
    private readonly object _gate = new();
    private readonly IRandom _inner;

    public SynchronizedRandom(IRandom inner) => _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    public void Reset() { lock (_gate) _inner.Reset(); }
    public void Reseed(ulong seed, ulong sequence = 0) { lock (_gate) _inner.Reseed(seed, sequence); }
    public uint NextUInt() { lock (_gate) return _inner.NextUInt(); }
    public double NextDouble() { lock (_gate) return _inner.NextDouble(); }
    public int NextInt(int maxExclusive) { lock (_gate) return _inner.NextInt(maxExclusive); }
    public double NextGaussian() { lock (_gate) return _inner.NextGaussian(); }
}
