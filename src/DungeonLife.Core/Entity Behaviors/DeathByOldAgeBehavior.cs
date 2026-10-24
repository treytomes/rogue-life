using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonLife
{
    [DisplayName("Dead")]
    class DeathByOldAgeBehavior : EntityBehavior
    {
        /// <summary>
        /// Check once a day to see if the entity should die.
        /// </summary>
        private const int CHECK_INTERVAL = 1;

        private TimeSpan _lastDeathCheck = TimeSpan.Zero;

        public DeathByOldAgeBehavior(Entity entity, TimeSpan lifeSpan)
            : base(entity)
        {
            MaxLifeSpan = lifeSpan;
        }

        public TimeSpan MaxLifeSpan { get; }

        public override bool Update(IWorldState world)
        {
            if ((_entity.Age - _lastDeathCheck).TotalDays < CHECK_INTERVAL)
            {
                // Check about once a day to see if the entity is dead yet.
                return false;
            }
            else
            {
                // You're living on borrowed time.

                _lastDeathCheck = _entity.Age;

                // Delta is the max remaining lifespan.  This will reduce over time.
                var delta = MaxLifeSpan - _entity.Age;

                //// As the delta goes to 0, increase the chance of suddenly dying.
                //var deathPercent = (double)delta.Ticks / (double)MaxLifeSpan.Ticks;
                //if (deathPercent < 1)
                //{
                //    deathPercent = Math.Tan(deathPercent * deathPercent);
                //}

                if (delta.TotalDays < 0)
                {
                    var deathPercent = _entity.Age.TotalDays / MaxLifeSpan.TotalDays - 1;
                    if (_random.NextDouble() > (1 - deathPercent))  // Increased chance of random death the farther past it's lifespan the entity gets.
                    {
                        // You're dead!
                        _entity.IsDead = true;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
