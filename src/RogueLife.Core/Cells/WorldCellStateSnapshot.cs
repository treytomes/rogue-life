using System;
using System.Collections.Generic;
using System.Numerics;

namespace RogueLife;

internal readonly record struct WorldCellNextState(float Temperature, float Humidity, float AlgaeLevel);

internal readonly record struct WorldCellStateValue(
    bool Exists,
    Vector2 Position,
    Type CellType,
    float Temperature,
    float Humidity,
    float AlgaeLevel,
    float MovementSpeedMultiplier);

/// <summary>Immutable cell values captured at a tick boundary.</summary>
internal sealed class WorldCellStateSnapshot
{
    private readonly WorldCellStateValue[,] _values;

    public WorldCellStateSnapshot(WorldCellCollection cells)
    {
        Width = cells.Width;
        Height = cells.Height;
        _values = new WorldCellStateValue[Width, Height];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                WorldCell cell = cells[x, y];
                if (cell == null) continue;

                float algaeLevel = cell is FloorWorldCell floor ? floor.AlgaeLevel : 0;
                _values[x, y] = new WorldCellStateValue(
                    true,
                    new Vector2(x, y),
                    cell.GetType(),
                    cell.Temperature,
                    cell.Humidity,
                    algaeLevel,
                    cell.MovementSpeedMultiplier);
            }
        }
    }

    public int Width { get; }
    public int Height { get; }

    public float GetRegionTemperature(Vector2 position, int radius) =>
        Average(position, radius, value => value.Temperature);

    public float GetRegionHumidity(Vector2 position, int radius) =>
        Average(position, radius, value => value.Humidity);

    public float GetRegionAlgae(Vector2 position, int radius)
    {
        float total = 0;
        GetBounds(position, radius, out int xMin, out int xMax, out int yMin, out int yMax);
        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                WorldCellStateValue value = _values[x, y];
                if (value.Exists) total += value.AlgaeLevel;
            }
        }

        return total;
    }

    public WorldCellStateValue GetCell(Vector2 position) => GetCell((int)position.X, (int)position.Y);

    public WorldCellStateValue GetCell(int x, int y)
    {
        if ((x < 0) || (x >= Width) || (y < 0) || (y >= Height))
        {
            return default;
        }

        return _values[x, y];
    }

    public IEnumerable<WorldCellStateValue> IterateOver(Vector2 position, float radius)
    {
        int xMin = Math.Max(0, (int)(position.X - radius));
        int xMax = Math.Min(Width - 1, (int)(position.X + radius));
        int yMin = Math.Max(0, (int)(position.Y - radius));
        int yMax = Math.Min(Height - 1, (int)(position.Y + radius));

        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                WorldCellStateValue value = _values[x, y];
                if (value.Exists) yield return value;
            }
        }
    }

    private float Average(Vector2 position, int radius, Func<WorldCellStateValue, float> selector)
    {
        float total = 0;
        int count = 0;
        GetBounds(position, radius, out int xMin, out int xMax, out int yMin, out int yMax);
        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                WorldCellStateValue value = _values[x, y];
                if (!value.Exists) continue;
                total += selector(value);
                count++;
            }
        }

        return count == 0 ? 0 : total / count;
    }

    private void GetBounds(Vector2 position, int radius, out int xMin, out int xMax, out int yMin, out int yMax)
    {
        xMin = Math.Max(0, (int)position.X - radius);
        xMax = Math.Min(Width - 1, (int)position.X + radius);
        yMin = Math.Max(0, (int)position.Y - radius);
        yMax = Math.Min(Height - 1, (int)position.Y + radius);
    }
}
