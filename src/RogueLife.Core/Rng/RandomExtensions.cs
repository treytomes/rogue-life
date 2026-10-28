using System;
using System.Numerics;

namespace RogueLife;

/// <summary>Represents RandomExtensions in the simulation.</summary>
public static class RandomExtensions
{
    /// <summary>Executes the Next behavior.</summary>
    public static int Next(this IRandom random) => random.NextInt(int.MaxValue);

    /// <summary>Executes the Next behavior.</summary>
    public static int Next(this IRandom random, int maxValue) => maxValue == 0 ? 0 : random.NextInt(maxValue);

    /// <summary>Executes the Next behavior.</summary>
    public static int Next(this IRandom random, int minValue, int maxValue)
    {
        if (maxValue < minValue) throw new ArgumentOutOfRangeException(nameof(maxValue));
        if (maxValue == minValue) return minValue;
        return minValue + random.NextInt(checked(maxValue - minValue));
    }

    /// <summary>Executes the NextDirection behavior.</summary>
    public static Vector2 NextDirection(this IRandom random)
    {
        Vector2 direction = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 2.0f - Vector2.One;
        if (direction == Vector2.Zero) return Vector2.UnitX;

        return Vector2.Normalize(direction);
    }

    /// <summary>Defines the struct value.</summary>
    public static TEnum NextEnum<TEnum>(this IRandom random) where TEnum : struct, Enum
    {
        TEnum[] values = Enum.GetValues<TEnum>();
        return values[random.NextInt(values.Length)];
    }
}

