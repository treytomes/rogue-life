#nullable enable
using System;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace DungeonLife
{
    /// <summary>Represents EntityBehavior in the simulation.</summary>
    public abstract class EntityBehavior
    {
        /// <summary>Defines the _entity value.</summary>
        protected readonly Entity _entity;
        /// <summary>Defines the Random value.</summary>
        protected IRandom Random => _entity.Random;

        /// <summary>Executes the EntityBehavior behavior.</summary>
        protected EntityBehavior(Entity entity)
        {
            _entity = entity;
        }

        /// <summary>Executes the TryDecide behavior.</summary>
        internal abstract bool TryDecide(EntityDecisionContext context, out EntityIntent intent);

        /// <summary>Executes the ToString behavior.</summary>
        public override string ToString()
        {
            return GetType().GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? GetType().Name;
        }

        private protected WorldCellStateValue? FindClosest<TCell>(
            EntityDecisionContext context,
            Vector2 position,
            float radius,
            Func<WorldCellStateValue, float>? propertyToMaximize = null)
            where TCell : WorldCell
        {
            WorldCellStateValue[] cells = context.World.Cells
                .IterateOver(position, radius)
                .Where(cell => typeof(TCell).IsAssignableFrom(cell.CellType))
                .ToArray();
            if (cells.Length == 0) return null;

            propertyToMaximize ??= _ => 1.0f;
            int firstIndex = Array.FindIndex(cells, cell => propertyToMaximize(cell) != 0);
            if (firstIndex < 0) return null;

            WorldCellStateValue result = cells[firstIndex];
            float resultDistance = Vector2.DistanceSquared(context.Self.Position, result.Position);
            float resultValue = propertyToMaximize(result);

            for (int index = firstIndex + 1; index < cells.Length; index++)
            {
                WorldCellStateValue cell = cells[index];
                float cellValue = propertyToMaximize(cell);
                if (cellValue > resultValue)
                {
                    float distance = Vector2.DistanceSquared(context.Self.Position, cell.Position);
                    if ((distance < resultDistance) || ((distance == resultDistance) && (cellValue > resultValue)))
                    {
                        result = cell;
                        resultDistance = distance;
                        resultValue = cellValue;
                    }
                }
            }

            return result;
        }
    }
}



