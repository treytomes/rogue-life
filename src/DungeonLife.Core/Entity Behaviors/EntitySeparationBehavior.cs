using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace DungeonLife
{
    [DisplayName("Separating")]
    class EntitySeparationBehavior : EntityBehavior
    {
        private const int MAINTENANCE_BIAS = 1;
        private readonly float _separationAmountSquared;

        public EntitySeparationBehavior(Entity entity, float separationAmount = 2) : base(entity)
        {
            _separationAmountSquared = separationAmount * separationAmount;
        }

        internal override bool TryDecide(EntityDecisionContext context, out EntityIntent intent)
        {
            var entities = context.World.InArea(context.Self.Position, context.Self.RangeOfSight, context.Self.EntityType).ToArray();
            if (entities.Length <= 1)
            {
                intent = EntityIntent.None(_entity);
                return false;
            }

            foreach (EntityStateSnapshot entity in entities)
            {
                if (entity.Id == context.Self.Id) continue;

                Vector2 delta = context.Self.Position - entity.Position;
                float distanceSquared = delta.LengthSquared();
                if (distanceSquared <= _separationAmountSquared)
                {
                    if (distanceSquared == 0)
                    {
                        intent = EntityIntent.None(_entity);
                        return false;
                    }

                    delta = Vector2.Normalize(delta);
                    Vector2 direction = Vector2.Normalize((context.Self.MovingDirection * MAINTENANCE_BIAS + delta) / (MAINTENANCE_BIAS + 1));
                    intent = new EntityIntent(_entity, MovingDirection: direction);
                    return true;
                }
            }

            intent = EntityIntent.None(_entity);
            return false;
        }
    }
}

