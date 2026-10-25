using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace DungeonLife
{
    [DisplayName("Cohesion")]
    class EntityCohesionBehavior : EntityBehavior
    {
        private const int MAINTENANCE_BIAS = 1;

        public EntityCohesionBehavior(Entity entity) : base(entity) { }

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

            Vector2 center = Vector2.Zero;
            foreach (EntityStateSnapshot entity in entities)
            {
                center += entity.Position;
            }
            center /= entities.Length;
            if (center == context.Self.Position)
            {
                intent = EntityIntent.None(_entity);
                return false;
            }

            Vector2 delta = center - context.Self.Position;
            if (!TryNormalize(delta, out delta))
            {
                intent = EntityIntent.None(_entity);
                return false;
            }

            Vector2 blendedDirection = (context.Self.MovingDirection * MAINTENANCE_BIAS + delta) / (MAINTENANCE_BIAS + 1);
            if (!TryNormalize(blendedDirection, out Vector2 direction))
            {
                intent = EntityIntent.None(_entity);
                return false;
            }

            intent = new EntityIntent(_entity, MovingDirection: direction);
            return true;
        }
    }
}

