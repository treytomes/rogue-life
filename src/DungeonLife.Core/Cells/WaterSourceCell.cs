using System;
namespace DungeonLife
{
    /// <summary>Water defies temperature changes, and will spread humidity as it warms up.</summary>
    public class WaterSourceCell : WorldCell
    {
        public const int GLYPH_WATER1 = '~';
        public const int GLYPH_WATER2 = 247;
        public const int ANIMATE_MS = 500;

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
            if (nextTemperature > WorldSettings.IdealTemperature)
            {
                nextTemperature -= 0.001f;
            }
            else if (nextTemperature < WorldSettings.IdealTemperature)
            {
                nextTemperature += 0.001f;
            }

            return new WorldCellNextState(nextTemperature, nextHumidity, 0);
        }
    }
}

