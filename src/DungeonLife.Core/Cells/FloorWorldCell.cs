using System;
using System.Numerics;

namespace DungeonLife
{
    /// <summary>
    /// Algae spreads over floors.  Floors also act as minor heat sinks.
    /// </summary>
    public class FloorWorldCell : WorldCell
    {
        private const int ALGAE_SEARCH_RADIUS = 1;
        private const float SPONTANEOUS_LIFE_CHANCE = 0.01f;
        private const float HEAT_SINK_VALUE = 0.001f;

        /// <summary>
        /// If a cell is alive, it will slowly age.
        /// </summary>
        const float AGE_RATE = 0.01f; // 0.001f might work better.

        /// <summary>Creates an instance of FloorWorldCell.</summary>
        public FloorWorldCell(int x, int y)
            : base(x, y)
        {
            Temperature = 0.0f;
            Humidity = 0.0f;
            AlgaeLevel = 0.0f;

            Glyph = '.';
            BackgroundColor = RgbColor.Black;
            ForegroundColor = RgbColor.AnsiBlackBright;
        }

        /// <summary>Documents this simulation API member.</summary>
        public override int Glyph
        {
            get
            {
                if (AlgaeLevel > 0)
                {
                    return 176;
                }
                else
                {
                    return base.Glyph;
                }
            }
            set => base.Glyph = value;
        }

        /// <summary>Documents this simulation API member.</summary>
        public override RgbColor ForegroundColor
        {
            get
            {
                if (AlgaeLevel > 0)
                {
                    return RgbColor.FromFloat(0.0f, AlgaeLevel, 0.0f);
                }
                else
                {
                    return base.ForegroundColor;
                }
            }
            set => base.ForegroundColor = value;
        }

        /// <summary>Gets or sets the AlgaeLevel value.</summary>
        public float AlgaeLevel { get; set; }

        private float GetRegionAlgae(WorldCellStateSnapshot snapshot, Vector2 position, int radius)
        {
            return snapshot.GetRegionAlgae(position, radius);
        }

        /// <returns>The amount of life to set to the cell's algae level.</returns>
        private float ConwayFrameRule(float cellValue, float regionValue, float humidity)
        {
            // This gets small as the distance from ideal increases.
            var tempDistance = 2 * (Temperature - WorldSettings.IdealTemperature);
            if (tempDistance != 0)
            {
                tempDistance = 1.0f / tempDistance;
            }
            else
            {
                tempDistance = 1.0f;
            }

            // Now it gets larger as distance incrases.
            tempDistance = 2.0f - tempDistance;

            var cellDelta = 0.0f;
            var isAlive = cellValue > 0.0f;
            var isDead = !isAlive;

            if (isAlive)
            {
                if ((regionValue >= 2.5f) && (regionValue <= 4.5f))
                {
                    // Cozy surroundings leads to more life.
                    cellDelta = 0.1f;
                }
                else
                {
                    // Over- or under-population leads to death.
                    cellDelta = -0.1f; // = 1.0f - cellValue;
                }
            }

            if (isDead)
            {
                //if ((regionValue >= 2.9f) && (regionValue <= 3.1f))
                if ((regionValue >= 3.5f) && (regionValue <= 4.5f))
                {
                    // Cozy surroundings leads to reproduction.
                    cellDelta = 1.0f;
                }
                else
                {
                    // The cell is dead, and the area is either too empty or too full to do anything with.
                }
            }

            var newValue = cellValue;
            // Let's try only giving the algae a growth bonus if the area is humid.
            // Cell deltas are reduced as temperature moves away from ideal.
            // As the temperature difference approaches 1, the sine appoaches 0.  The perfectly ideal temperature will thereby freeze the state of the cell.
            // The 0.9f factor is there to try and reduce the amount of affect temperature has on cell activity.
            cellDelta *= (float)(1.0f - 0.8f * Math.Abs(Math.Sin(tempDistance)));

            if (humidity < 0.01f)
            {
                // TODO: Do dry, hot algae still need to burn here?
                cellDelta = 0.0f;
            }
            else if (humidity < 0.5f)
            {
                // Dry weather slows growth.
                cellDelta *= (1 - humidity);
            }
            else
            {
                // Wet weather encourages growth.
                cellDelta *= (1 + (humidity - 0.5f));
            }

            newValue += cellDelta;

            if (newValue > 0)
            {
                // A cell will slowly age and die.
                // The cell should age slower in dry climates.
                if ((Temperature - WorldSettings.IdealTemperature) > 0.001f)
                {
                    // Go ahead and age it if it gets too hot.  Frozen algae lasts forever.
                    newValue -= AGE_RATE;
                }
                else
                {
                    newValue -= AGE_RATE * humidity;
                }
            }

            if (newValue < 0.1f)
            {
                newValue = 0.0f;
            }

            if (newValue > 1.0f)
            {
                newValue = 1.0f;
            }

            return newValue;
        }

        internal override WorldCellNextState CalculateNextState(WorldCellStateSnapshot snapshot, TimeSpan worldTime)
        {
            float nextHumidity = CalculateNextHumidity(snapshot);
            float regionAlgae = GetRegionAlgae(snapshot, Position, ALGAE_SEARCH_RADIUS);
            float nextAlgae = ConwayFrameRule(AlgaeLevel, regionAlgae, nextHumidity);

            if (Random.NextDouble() > (1.0f - SPONTANEOUS_LIFE_CHANCE * nextHumidity))
            {
                nextAlgae = (float)(Random.NextDouble() * 0.5 + 0.5);
            }

            float averageTemperature = snapshot.GetRegionTemperature(Position, TEMPERATURE_SEARCH_RADIUS);
            float nextTemperature = Temperature + (1.0f - nextHumidity) * (averageTemperature - Temperature);
            if (nextTemperature < WorldSettings.IdealTemperature)
            {
                nextTemperature += HEAT_SINK_VALUE;
            }
            else if (nextTemperature > WorldSettings.IdealTemperature)
            {
                nextTemperature -= HEAT_SINK_VALUE;
            }

            return new WorldCellNextState(nextTemperature, nextHumidity, nextAlgae);
        }
    }
}


