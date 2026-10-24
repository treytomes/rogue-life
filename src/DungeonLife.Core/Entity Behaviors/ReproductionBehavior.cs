using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonLife
{
    class ReproductionBehavior : EntityBehavior
    {
        private const int COOLDOWN_DAYS = 3;

        private float _desirability;
        private float _needToReproduce;
        private bool _canBecomePregnant;
        private bool _isPregnant;
        private float _gestationAmount;
        private TimeSpan _gestationCooldown;

        public ReproductionBehavior(Entity entity)
            : base(entity)
        {
            _desirability = (float)_random.NextDouble() * 0.5f + 0.5f;
            _needToReproduce = 0.0f;
            _canBecomePregnant = _entity.Gender == Gender.Female; // and is an adult, and age < 75% ???
            _isPregnant = false;
            _gestationAmount = 0.0f;
            _gestationCooldown = TimeSpan.Zero;
        }

        public override bool Update(IWorldState world)
        {
            if (_entity.Gender == Gender.Male)
            {
                return UpdateMale(world);
            }
            else
            {
                return UpdateFemale(world);
            }
        }

        private bool UpdateMale(IWorldState world)
        {
            return false;
        }

        private bool UpdateFemale(IWorldState world)
        {
            return false;
            if (_isPregnant)
            {
                _gestationAmount += 0.01f; // TODO: Need to toggle this.  Maybe relate it to metabolism.
                if (_gestationAmount >= 1.0f)
                {
                    _needToReproduce = 0.0f;
                    _isPregnant = false;
                    _gestationAmount = 0.0f;
                    _gestationCooldown = TimeSpan.FromDays(COOLDOWN_DAYS);
                    // TODO: Plant a baby in the world.
                }
            }
            else
            {
                if (_gestationCooldown > TimeSpan.Zero)
                {
                    // TODO: Need the delta.
                    //_gestationCooldown -= delta;
                }
                _needToReproduce = 0.01f; // TODO: Need to toggle this.
                if (_needToReproduce > 1.0f)
                {
                    _needToReproduce = 1.0f;
                }
            }
        }
    }
}
