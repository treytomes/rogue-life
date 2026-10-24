using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace DungeonLife
{
    [DisplayName("Separating")]
    class EntitySeparationBehavior : EntityBehavior
    {
        private const int MAINTENANCE_BIAS = 1;

        private float _separationAmount;
        private float _separationAmountSquared;

        public EntitySeparationBehavior(Entity entity, float separationAmount = 2)
            : base(entity)
        {
            _separationAmount = separationAmount;
            _separationAmountSquared = _separationAmount * _separationAmount;
        }

        public override bool Update(IWorldState world)
        {
            var entities = world.Entities.InArea(_entity.Position, _entity.RangeOfSight, _entity.GetType());
            var count = entities.Count();
            if (count <= 1)
            {
                return false;
            }

            foreach (var entity in entities)
            {
                if (entity == _entity)
                {
                    continue;
                }

                var delta = _entity.Position - entity.Position;
                var distance = delta.LengthSquared();
                if (distance <= _separationAmountSquared)
                {
                    // It's too close!

                    // The normalized distance is the new movement direction.
                    distance = delta.Length();
                    delta /= distance;

                    if (delta == Vector2.Zero)
                    {
                        return false;
                    }

                    delta = Vector2.Normalize(delta);
                    _entity.MovingDirection = Vector2.Normalize((_entity.MovingDirection * MAINTENANCE_BIAS + delta) / (MAINTENANCE_BIAS + 1));
                    return true;
                }
            }
            return false;
        }
    }
}
