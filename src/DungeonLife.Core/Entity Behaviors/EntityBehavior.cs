#nullable enable
using System;
using System.ComponentModel;
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

        /// <summary>Normalizes a finite nonzero vector, returning false for zero or invalid input.</summary>
        /// <param name="value">The vector to normalize.</param>
        /// <param name="normalized">The unit vector, or zero when normalization fails.</param>
        /// <returns>True when a finite unit vector was produced.</returns>
        protected static bool TryNormalize(Vector2 value, out Vector2 normalized)
        {
            double length = Math.Sqrt((double)value.X * value.X + (double)value.Y * value.Y);
            if (!double.IsFinite(length) || length == 0)
            {
                normalized = Vector2.Zero;
                return false;
            }

            normalized = new Vector2((float)(value.X / length), (float)(value.Y / length));
            return float.IsFinite(normalized.X) && float.IsFinite(normalized.Y);
        }

        private protected WorldCellStateValue? FindClosest<TCell>(
            EntityDecisionContext context,
            Vector2 position,
            float radius,
            Func<WorldCellStateValue, float>? propertyToMaximize = null)
            where TCell : WorldCell
        {
            propertyToMaximize ??= _ => 1.0f;
            double radiusSquared = (double)radius * radius;
            double bestScore = double.NegativeInfinity;
            WorldCellStateValue? result = null;
            foreach (WorldCellStateValue cell in context.World.Cells.IterateOver(position, radius))
            {
                if (!typeof(TCell).IsAssignableFrom(cell.CellType)) continue;

                double dx = (double)position.X - cell.Position.X;
                double dy = (double)position.Y - cell.Position.Y;
                double distanceSquared = dx * dx + dy * dy;
                if (distanceSquared > radiusSquared) continue;

                float amount = propertyToMaximize(cell);
                if (!float.IsFinite(amount) || amount <= 0) continue;

                double score = amount / (1.0 + distanceSquared);
                if (result is not WorldCellStateValue current
                    || score > bestScore
                    || (score == bestScore && ComesBefore(cell.Position, current.Position)))
                {
                    result = cell;
                    bestScore = score;
                }
            }

            return result;
        }

        private static bool ComesBefore(Vector2 candidate, Vector2 current) =>
            candidate.X < current.X || (candidate.X == current.X && candidate.Y < current.Y);
    }
}



