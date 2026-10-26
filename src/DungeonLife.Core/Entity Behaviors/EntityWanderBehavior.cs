using System.ComponentModel;
using System.Numerics;

namespace DungeonLife
{
    [DisplayName("Wandering")]
    class EntityWanderBehavior : EntityBehavior
    {
        private const int MAINTENANCE_BIAS = 1;
        private float _changeDirectionChance;

        internal float ChangeDirectionChance => _changeDirectionChance;
        internal void RestoreChangeDirectionChance(float value) => _changeDirectionChance = value;

        public EntityWanderBehavior(Entity entity) : base(entity)
        {
            _changeDirectionChance = (float)Random.NextDouble();
            _entity.MovingDirection = Random.NextDirection();
        }

        internal override bool TryDecide(EntityDecisionContext context, out EntityIntent intent)
        {
            if (Random.NextDouble() > _changeDirectionChance)
            {
                Vector2 newDirection = Random.NextDirection();
                Vector2 blendedDirection = (context.Self.MovingDirection * MAINTENANCE_BIAS + newDirection) / (MAINTENANCE_BIAS + 1);
                if (!TryNormalize(blendedDirection, out Vector2 direction))
                {
                    intent = EntityIntent.None(_entity);
                    return false;
                }

                intent = new EntityIntent(_entity, MovingDirection: direction);
                return true;
            }

            intent = EntityIntent.None(_entity);
            return false;
        }
    }
}

