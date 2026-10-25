using System;
namespace DungeonLife
{
    /// <summary>Represents WaterSourceCell in the simulation.</summary>
    public class WaterSourceCell : WorldCell
    {
        /// <summary>Defines the GLYPH_WATER1 value.</summary>
        public const int GLYPH_WATER1 = '~';
        /// <summary>Defines the GLYPH_WATER2 value.</summary>
        public const int GLYPH_WATER2 = 247;
        /// <summary>Defines the ANIMATE_MS value.</summary>
        public const int ANIMATE_MS = 500;

        /// <summary>Creates an instance of WaterSourceCell.</summary>
        public WaterSourceCell(int x, int y) : base(x, y)
        {
            ForegroundColor = RgbColor.AnsiWhite;
            BackgroundColor = RgbColor.AnsiBlue;
            Glyph = GLYPH_WATER1;
            MovementSpeedMultiplier = 0.25f;
        }

        internal override WorldCellNextState CalculateNextState(WorldCellStateSnapshot snapshot, TimeSpan worldTime)
        {
            float nextHumidity = CalculateNextHumidity(snapshot);
            float tempDistance = Temperature - WorldSettings.IdealTemperature;
            if ((tempDistance > 0) && (nextHumidity < 1))
            {
                nextHumidity += HUMIDITY_DELTA * tempDistance;
            }
            else if ((tempDistance < 0) && (nextHumidity > 0))
            {
                nextHumidity -= HUMIDITY_DELTA;
            }
            nextHumidity = Math.Clamp(nextHumidity, 0.0f, 1.0f);

            float averageTemperature = snapshot.GetRegionTemperature(Position, TEMPERATURE_SEARCH_RADIUS);
            float nextTemperature = (2 * Temperature + averageTemperature) / 3.0f;
            nextTemperature = WorldSettings.MoveTowardsIdealTemperature(nextTemperature);

            return new WorldCellNextState(nextTemperature, nextHumidity, 0);
        }
    }
}

