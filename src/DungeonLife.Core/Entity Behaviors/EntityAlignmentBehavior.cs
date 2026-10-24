using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace DungeonLife
{
    [DisplayName("Aligning")]
    class EntityAlignmentBehavior : EntityBehavior
    {
        private const int MAINTENANCE_BIAS = 1;

        public EntityAlignmentBehavior(Entity entity) : base(entity) { }

        internal override bool TryDecide(EntityDecisionContext context, out EntityIntent intent)
        {
            EntityStateSnapshot[] entities = context.World
                .InArea(context.Self.Position, context.Self.RangeOfSight, context.Self.EntityType)
                .ToArray();
            if (entities.Length <= 1)
            {
                intent = EntityIntent.None(_entity);
                return false;
            }

            Vector2 averageDirection = Vector2.Zero;
            foreach (EntityStateSnapshot entity in entities) averageDirection += entity.MovingDirection;
            averageDirection /= entities.Length;
            if (averageDirection == Vector2.Zero)
            {
                intent = EntityIntent.None(_entity);
                return false;
            }

            averageDirection = Vector2.Normalize(averageDirection);
            Vector2 direction = Vector2.Normalize((context.Self.MovingDirection * MAINTENANCE_BIAS + averageDirection) / (MAINTENANCE_BIAS + 1));
            intent = new EntityIntent(_entity, MovingDirection: direction);
            return true;
        }
    }
}

