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

        public float AlgaeLevel { get; set; }

        private float GetRegionAlgae(WorldCellCollection cells, Vector2 position, int radius)
        {
            return cells.SumOver(position, radius, c => (c as FloorWorldCell)?.AlgaeLevel ?? 0);
        }

        /// <returns>The amount of life to set to the cell's algae level.</returns>
        private float ConwayFrameRule(float cellValue, float regionValue)
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

            if (Humidity < 0.01f)
            {
                // TODO: Do dry, hot algae still need to burn here?
                cellDelta = 0.0f;
            }
            else if (Humidity < 0.5f)
            {
                // Dry weather slows growth.
                cellDelta *= (1 - Humidity);
            }
            else
            {
                // Wet weather encourages growth.
                cellDelta *= (1 + (Humidity - 0.5f));
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
                    newValue -= AGE_RATE * Humidity;
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

        public override void Update(IWorldState state)
        {
            base.Update(state);

            var regionValue = GetRegionAlgae(state.Cells, Position, ALGAE_SEARCH_RADIUS);

            AlgaeLevel = ConwayFrameRule(AlgaeLevel, regionValue);

            if (_random.NextDouble() > (1.0f - SPONTANEOUS_LIFE_CHANCE * Humidity)) // Greater chance of spawning new cells in humid weather.
            {
                AlgaeLevel = (float)(_random.NextDouble() * 0.5 + 0.5);
            }

            // Humidity should prevent temperature from spreading as quickly.
            var avgTemp = state.Cells.GetRegionTemperature(Position, TEMPERATURE_SEARCH_RADIUS);
            var deltaTemp = avgTemp - Temperature;
            Temperature += (1.0f - Humidity) * deltaTemp;

            // Floors act as a heat sink.
            if (Temperature < WorldSettings.IdealTemperature)
            {
                Temperature += HEAT_SINK_VALUE;
            }
            else if (Temperature > WorldSettings.IdealTemperature)
            {
                Temperature -= HEAT_SINK_VALUE;
            }

            /*
            // Floors act as a temperature stabilizer. 
            if (Temperature > IDEAL_TEMPERATURE)
            {
                Temperature -= HEAT_SINK_VALUE;
            }
            else if (Temperature < IDEAL_TEMPERATURE)
            {
                Temperature += HEAT_SINK_VALUE;
            }
            */
        }
    }
}
