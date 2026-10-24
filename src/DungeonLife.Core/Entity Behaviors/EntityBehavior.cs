using System;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace DungeonLife
{
    public class SearchResult
    {
        public Vector2 Position;
        public float Distance;
        public float Value;
    }

    public class SearchResult<TCell> : SearchResult
        where TCell : WorldCell
    {
        public TCell Cell;
    }

    public abstract class EntityBehavior
    {
        protected static IRandom _random = ThreadSafeRandom.Instance;
        protected readonly Entity _entity;

        public EntityBehavior(Entity entity)
        {
            _entity = entity;
        }

        /// <returns>Did the behavior do anything?</returns>
        public abstract bool Update(IWorldState world);

        public override string ToString()
        {
            return GetType().GetCustomAttribute<DisplayNameAttribute>().DisplayName;
        }

        protected SearchResult<TCell> FindClosest<TCell>(IWorldState world, Vector2 position, float radius, Func<TCell, float> propertyToMaximize = null)
            where TCell : WorldCell
        {
            var cells = world.Cells.IterateOver(position, radius).OfType<TCell>();
            if (!cells.Any())
            {
                return null;
            }

            if (propertyToMaximize == null)
            {
                propertyToMaximize = cell => 1.0f;
            }

            var first = cells.FirstOrDefault(x => propertyToMaximize(x) != 0);
            if (first == null)
            {
                return null;
            }

            var result = new SearchResult<TCell>()
            {
                Cell = first,
                Position = first.Position,
                Distance = (_entity.Position - first.Position).LengthSquared(),
                Value = propertyToMaximize(first)
            };

            foreach (var cell in cells.Skip(1))
            {
                var cellValue = propertyToMaximize(cell);

                if (cellValue > result.Value)
                {
                    // Grab either the closest floor cell or the one with the highest food level.
                    var dist = (_entity.Position - cell.Position).LengthSquared();

                    // TODO: Total value is (radius - dist) * cellValue).  Order by this.

                    if ((dist < result.Distance) || ((dist == result.Distance) && (cellValue > result.Value)))
                    {
                        result = new SearchResult<TCell>()
                        {
                            Cell = cell,
                            Position = cell.Position,
                            Distance = dist,
                            Value = cellValue
                        };
                    }
                }
            }

            return result;
        }
    }
}
