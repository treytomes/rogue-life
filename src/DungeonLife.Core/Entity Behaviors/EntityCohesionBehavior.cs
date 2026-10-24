using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace DungeonLife
{
    [DisplayName("Cohesing")]
    class EntityCohesionBehavior : EntityBehavior
    {
        private const int MAINTENANCE_BIAS = 1;

        public EntityCohesionBehavior(Entity entity)
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

            var center = Vector2.Zero;
            foreach (var entity in entities)
            {
                center += entity.Position;
                count++;
            }
            center /= count;

            if (center == _entity.Position)
            {
                return false;
            }

            var delta = _entity.Position - center;

            if (delta == Vector2.Zero)
            {
                return false;
            }
            delta = Vector2.Normalize(delta);

            _entity.MovingDirection = (_entity.MovingDirection * MAINTENANCE_BIAS + delta) / (MAINTENANCE_BIAS + 1);
            return true;
        }
    }
}
