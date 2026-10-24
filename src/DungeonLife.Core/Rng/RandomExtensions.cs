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

