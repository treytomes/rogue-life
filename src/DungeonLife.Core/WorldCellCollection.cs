using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DungeonLife
{
    public class WorldCellCollection : IEnumerable<WorldCell>
    {
        private readonly WorldCell[,] _cells;

        public WorldCellCollection(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new WorldCell[width, height];
        }

        public int Width { get; }
        public int Height { get; }

        public WorldCell this[int x, int y]
        {
            get
            {
                if ((x < 0) || (x >= Width) || (y < 0) || (y >= Height))
                {
                    return null;
                }
                return _cells[x, y];
            }
            set
            {
                if ((x < 0) || (x >= Width) || (y < 0) || (y >= Height))
                {
                    throw new ArgumentOutOfRangeException();
                }
                _cells[x, y] = value;
            }
        }

        public WorldCell this[Vector2 pnt]
        {
            get
            {
                return this[(int)pnt.X, (int)pnt.Y];
            }
            set
            {
                this[(int)pnt.X, (int)pnt.Y] = value;
            }
        }

        public IEnumerable<WorldCell> IterateOver(Vector2 position, float radius)
        {
            var xMin = (int)Math.Max(0, position.X - radius);
            var xMax = (int)Math.Min(Width - 1, position.X + radius);
            var yMin = (int)Math.Max(0, position.Y - radius);
            var yMax = (int)Math.Min(Height - 1, position.Y + radius);

            // I could search an actual circle area, but this bit is calculated tons of times and I don't want the extra processing.
            for (var xx = xMin; xx <= xMax; xx++)
            {
                for (var yy = yMin; yy <= yMax; yy++)
                {
                    yield return _cells[xx, yy];
                }
            }
        }

        public float SumOver(Vector2 position, float radius, Func<WorldCell, float> property)
        {
            return IterateOver(position, radius).Sum(property);
        }

        public float AverageOver(Vector2 position, float radius, Func<WorldCell, float> property)
        {
            var numCells = 4 * radius * radius + 2 * (radius * 2) + 1; // (radius * 2 + 1) * (radius * 2 + 1);
            return SumOver(position, radius, property) / numCells;
        }

        /// <summary>
        /// Get the average temperature for the region.
        /// </summary>
        public float GetRegionTemperature(Vector2 position, float radius)
        {
            return AverageOver(position, radius, c => c.Temperature);
        }

        /// <summary>
        /// Get the average humidity for the region.
        /// </summary>
        public float GetRegionHumidity(Vector2 position, float radius)
        {
            return AverageOver(position, radius, c => c.Humidity);
        }

        public bool IsMovementBlocked(Vector2 position)
        {
            return (this[position]?.BlocksMovement).GetValueOrDefault(true);
        }

        public IEnumerator<WorldCell> GetEnumerator()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    yield return _cells[x, y];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
