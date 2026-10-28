using System.ComponentModel;
using System.Numerics;

namespace RogueLife
{
    /// <summary>Seek out algae to satisfy hunger.</summary>
    [DisplayName("Hungry")]
    class EntityHungerBehavior : EntityBehavior
    {
        private const float ALGAE_FOOD_FACTOR = 0.2f;
        private readonly float _eatSpeed;

        public EntityHungerBehavior(Entity entity, float eatSpeed = 0.1f) : base(entity)
        {
            _eatSpeed = eatSpeed;
        }

        internal override bool TryDecide(EntityDecisionContext context, out EntityIntent intent)
        {
            EntityStateSnapshot self = context.Self;
            if ((self.Hunger == 0) || (Random.NextDouble() >= self.Hunger))
            {
                intent = EntityIntent.None(_entity);
                return false;
            }

            WorldCellStateValue currentCell = context.World.Cells.GetCell(self.Position);
            if (currentCell.Exists && typeof(FloorWorldCell).IsAssignableFrom(currentCell.CellType) && currentCell.AlgaeLevel > 0)
            {
                intent = new EntityIntent(_entity, MovingDirection: Vector2.Zero, Action: EntityAction.Eat, ActionTarget: currentCell.Position, ActionAmount: _eatSpeed, ActionFactor: ALGAE_FOOD_FACTOR);
                return true;
            }

            WorldCellStateValue? target = FindClosest<FloorWorldCell>(context, self.Position, self.RangeOfSmell, cell => cell.AlgaeLevel);
            if (target is WorldCellStateValue food)
            {
                float distanceSquared = Vector2.DistanceSquared(self.Position, food.Position);
                if ((distanceSquared < 2) && (food.AlgaeLevel > 0))
                {
                    intent = new EntityIntent(_entity, MovingDirection: Vector2.Zero, Action: EntityAction.Eat, ActionTarget: food.Position, ActionAmount: _eatSpeed, ActionFactor: ALGAE_FOOD_FACTOR);
                    return true;
                }

                Vector2 delta = food.Position - self.Position;
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


