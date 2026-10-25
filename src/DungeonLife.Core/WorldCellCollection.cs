using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DungeonLife
{
    /// <summary>Represents WorldCellCollection in the simulation.</summary>
    public class WorldCellCollection : IEnumerable<WorldCell>
    {
        private readonly WorldCell[,] _cells;

        /// <summary>Creates an instance of WorldCellCollection.</summary>
        public WorldCellCollection(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new WorldCell[width, height];
        }

        /// <summary>Gets the Width value.</summary>
        public int Width { get; }
        /// <summary>Gets the Height value.</summary>
        public int Height { get; }

        /// <summary>Gets the cell or entity at the specified coordinates.</summary>
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

        /// <summary>Gets the cell or entity at the specified coordinates.</summary>
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

        /// <summary>Executes the IterateOver behavior.</summary>
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

        /// <summary>Executes the SumOver behavior.</summary>
        public float SumOver(Vector2 position, float radius, Func<WorldCell, float> property)
        {
            return IterateOver(position, radius).Sum(property);
        }

        /// <summary>Returns the average of a property over sampled in-bounds cells.</summary>
        public float AverageOver(Vector2 position, float radius, Func<WorldCell, float> property)
        {
            ArgumentNullException.ThrowIfNull(property);

            float sum = 0;
            int count = 0;
            foreach (WorldCell cell in IterateOver(position, radius))
            {
                sum += property(cell);
                count++;
            }

            return count == 0 ? 0 : sum / count;
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

        /// <summary>Executes the IsMovementBlocked behavior.</summary>
        public bool IsMovementBlocked(Vector2 position)
        {
            return (this[position]?.BlocksMovement).GetValueOrDefault(true);
        }

        internal WorldCellStateSnapshot CaptureSnapshot() => new(this);

        internal void Commit(WorldCellNextState[,] nextStates)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    WorldCell cell = _cells[x, y];
                    if (cell == null) continue;

                    WorldCellNextState nextState = nextStates[x, y];
                    cell.Temperature = nextState.Temperature;
                    cell.Humidity = nextState.Humidity;
                    if (cell is FloorWorldCell floor)
                    {
                        floor.AlgaeLevel = nextState.AlgaeLevel;
                    }
                }
            }
        }
        /// <summary>Executes the GetEnumerator behavior.</summary>
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


