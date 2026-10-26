using System;
using System.ComponentModel;

namespace DungeonLife
{
    /// <summary>Represents OinkEntity in the simulation.</summary>
    [DisplayName("Oink")]
    public class OinkEntity : Entity
    {
        private const int GLYPH_BABY = 148;
        private const int GLYPH_ADULT = 153;

        /// <summary>Creates an instance of OinkEntity.</summary>
        public OinkEntity(int x, int y, IRandom random, Gender? gender = null)
            : base(x, y, random, gender)
        {
            MaturityAge = TimeSpan.FromDays(3);
            BabyGlyph = GLYPH_BABY;
            AdultGlyph = GLYPH_ADULT;
            ForegroundColor = RgbColor.Pink;
            Metabolism = 0.0001f;
            BasePerception = 18;

            // Oinks are hungry creatures.
            HungerMultiplier = 5.0f;
            ThirstMultiplier = 1.0f;

            // Oinks can smell better than they can see.
            RangeOfSmellMultiplier = 0.5f;
            RangeOfSightMultiplier = 0.3f;

            Behaviors.Add(BehaviorFactory.Death(this, MaturityAge * 4));
            // TODO: Oxygen
            Behaviors.Add(BehaviorFactory.Thirst(this));
            Behaviors.Add(BehaviorFactory.Hunger(this));
            // TODO: Temperature; metabolism and speed increase with heat and decrease with cold.  Too much heat or cold affects health.
            // TODO: Wetness; much much do the oinks like to be wet?  Wetness should also affect how much heat transfer occurs.
            Behaviors.Add(BehaviorFactory.Separation(this));
            Behaviors.Add(BehaviorFactory.Alignment(this));
            Behaviors.Add(BehaviorFactory.Wander(this));  // TODO: This would be the place to insert some type of neural network.
            Behaviors.Add(BehaviorFactory.Cohesion(this));
        }
    }
}

