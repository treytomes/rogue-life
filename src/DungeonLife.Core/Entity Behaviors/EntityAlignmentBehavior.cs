using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace DungeonLife
{
    [DisplayName("Aligning")]
    class EntityAlignmentBehavior : EntityBehavior
    {
        private const int MAINTENANCE_BIAS = 1;

        public EntityAlignmentBehavior(Entity entity)
            : base(entity)
        {
        }

        public override bool Update(IWorldState world)
        {
            var entities = world.Entities.InArea(_entity.Position, _entity.RangeOfSight, _entity.GetType());
            var count = entities.Count();
            if (count <= 1)
            {
                // Can't flock with yourself.
                return false;
            }

            var d = Vector2.Zero;
            foreach (var entity in entities)
            {
                d += entity.MovingDirection;
            }
            d /= count;

            if (d == Vector2.Zero)
            {
                return false;
            }

            d = Vector2.Normalize(d);
            _entity.MovingDirection = Vector2.Normalize((_entity.MovingDirection * MAINTENANCE_BIAS + d) / (MAINTENANCE_BIAS + 1));
            return true;
        }
    }
}
