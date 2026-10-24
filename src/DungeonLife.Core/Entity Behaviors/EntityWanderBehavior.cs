using System.ComponentModel;
using System.Numerics;

namespace DungeonLife
{
    [DisplayName("Wandering")]
    class EntityWanderBehavior : EntityBehavior
    {
        private const int MAINTENANCE_BIAS = 1;
        private readonly float _changeDirectionChance;

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
                Vector2 direction = Vector2.Normalize((context.Self.MovingDirection * MAINTENANCE_BIAS + newDirection) / (MAINTENANCE_BIAS + 1));
                intent = new EntityIntent(_entity, MovingDirection: direction);
                return true;
            }

            intent = EntityIntent.None(_entity);
            return false;
        }
    }
}

