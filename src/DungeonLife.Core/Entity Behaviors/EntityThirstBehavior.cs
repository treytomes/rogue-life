using System.ComponentModel;
using System.Numerics;

namespace DungeonLife
{
    /// <summary>Seek out water to satisfy thirst.</summary>
    [DisplayName("Thirsty")]
    class EntityThirstBehavior : EntityBehavior
    {
        private const float WATER_QUENCH_FACTOR = 1.0f;
        private readonly float _drinkSpeed;

        public EntityThirstBehavior(Entity entity, float drinkSpeed = 0.1f) : base(entity)
        {
            _drinkSpeed = drinkSpeed;
        }

        internal override bool TryDecide(EntityDecisionContext context, out EntityIntent intent)
        {
            EntityStateSnapshot self = context.Self;
            if ((self.Thirst == 0) || (Random.NextDouble() >= self.Thirst))
            {
                intent = EntityIntent.None(_entity);
                return false;
            }

            WorldCellStateValue currentCell = context.World.Cells.GetCell(self.Position);
            if (currentCell.Exists && typeof(WaterSourceCell).IsAssignableFrom(currentCell.CellType))
            {
                intent = new EntityIntent(_entity, MovingDirection: Vector2.Zero, Action: EntityAction.Drink, ActionTarget: currentCell.Position, ActionAmount: _drinkSpeed * WATER_QUENCH_FACTOR);
                return true;
            }

            WorldCellStateValue? target = FindClosest<WaterSourceCell>(context, self.Position, self.RangeOfSmell);
            target ??= FindClosest<WorldCell>(context, self.Position, self.RangeOfSmell, cell => cell.Humidity);
            if (target is WorldCellStateValue cellTarget)
            {
                float distanceSquared = Vector2.DistanceSquared(self.Position, cellTarget.Position);
                if ((distanceSquared < 2) && typeof(WaterSourceCell).IsAssignableFrom(cellTarget.CellType))
                {
                    intent = new EntityIntent(_entity, MovingDirection: Vector2.Zero, Action: EntityAction.Drink, ActionTarget: cellTarget.Position, ActionAmount: _drinkSpeed * WATER_QUENCH_FACTOR);
                    return true;
                }

                Vector2 delta = cellTarget.Position - self.Position;
                if (TryNormalize(delta, out Vector2 direction))
                {
                    intent = new EntityIntent(_entity, MovingDirection: direction);
                    return true;
                }
            }

            intent = EntityIntent.None(_entity);
            return false;
        }
    }
}


