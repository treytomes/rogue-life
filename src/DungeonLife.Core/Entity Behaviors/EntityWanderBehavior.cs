using System.ComponentModel;
using System.Numerics;

namespace DungeonLife
{
    /// <summary>
    /// The entity will wander aimlessly.
    /// </summary>
    [DisplayName("Wandering")]
    class EntityWanderBehavior : EntityBehavior
    {
        private const int MAINTENANCE_BIAS = 1;

        private float _changeDirectionChance = (float)_random.NextDouble();

        public EntityWanderBehavior(Entity entity)
            : base(entity)
        {
            _entity.MovingDirection = _random.NextDirection();
        }

        public override bool Update(IWorldState world)
        {
            if (_random.NextDouble() > _changeDirectionChance)
            {
                var newDirection = _random.NextDirection();
                _entity.MovingDirection = Vector2.Normalize((_entity.MovingDirection * MAINTENANCE_BIAS + new Vector2(newDirection.X, newDirection.Y)) / (MAINTENANCE_BIAS + 1));
                return true;
            }
            return false;
        }
    }
}
