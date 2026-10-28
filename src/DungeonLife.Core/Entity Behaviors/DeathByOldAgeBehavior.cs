using System;
using System.ComponentModel;

namespace DungeonLife
{
    [DisplayName("Dead")]
    class DeathByOldAgeBehavior : EntityBehavior
    {
        private const int CHECK_INTERVAL = 1;
        private TimeSpan _lastDeathCheck = TimeSpan.Zero;

        public DeathByOldAgeBehavior(Entity entity, TimeSpan lifeSpan) : base(entity)
        {
            MaxLifeSpan = lifeSpan;
        }

        public TimeSpan MaxLifeSpan { get; }

        internal TimeSpan LastDeathCheck
        {
            get => _lastDeathCheck;
            set => _lastDeathCheck = value;
        }

        internal override bool TryDecide(EntityDecisionContext context, out EntityIntent intent)
        {
            EntityStateSnapshot self = context.Self;
            if ((self.Age - _lastDeathCheck).TotalDays < CHECK_INTERVAL)
            {
                intent = EntityIntent.None(_entity);
                return false;
            }

            _lastDeathCheck = self.Age;
            TimeSpan remainingLife = MaxLifeSpan - self.Age;
            if (remainingLife.TotalDays >= 0)
            {
                intent = EntityIntent.None(_entity);
                return false;
            }

            double deathProbability = Math.Clamp(self.Age.TotalDays / MaxLifeSpan.TotalDays - 1.0, 0.0, 1.0);
            if (Random.NextDouble() < deathProbability)
            {
                intent = new EntityIntent(_entity, Dies: true);
                return true;
            }

            intent = EntityIntent.None(_entity);
            return false;
        }
    }
}

