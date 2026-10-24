using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace DungeonLife
{
    [DisplayName("Cohesing")]
    class EntityCohesionBehavior : EntityBehavior
    {
        private const int MAINTENANCE_BIAS = 1;

        public EntityCohesionBehavior(Entity entity) : base(entity) { }

        internal override bool TryDecide(EntityDecisionContext context, out EntityIntent intent)
        {
            EntityStateSnapshot[] entities = context.World
                .InArea(context.Self.Position, context.Self.RangeOfSight, context.Self.EntityType)
                .ToArray();
            int count = entities.Length;
            if (count <= 1)
            {
                intent = EntityIntent.None(_entity);
                return false;
            }

            Vector2 center = Vector2.Zero;
            foreach (EntityStateSnapshot entity in entities)
            {
                center += entity.Position;
                count++;
            }
            center /= count;
            if (center == context.Self.Position)
            {
                intent = EntityIntent.None(_entity);
                return false;
            }

            Vector2 delta = context.Self.Position - center;
            if (delta == Vector2.Zero)
            {
                intent = EntityIntent.None(_entity);
                return false;
            }

            delta = Vector2.Normalize(delta);
            Vector2 direction = (context.Self.MovingDirection * MAINTENANCE_BIAS + delta) / (MAINTENANCE_BIAS + 1);
            intent = new EntityIntent(_entity, MovingDirection: direction);
            return true;
        }
    }
}

