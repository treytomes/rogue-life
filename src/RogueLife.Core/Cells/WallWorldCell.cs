using System;
namespace RogueLife
{
    /// <summary>Represents WallWorldCell in the simulation.</summary>
    public class WallWorldCell : WorldCell
    {
        /// <summary>Creates an instance of WallWorldCell.</summary>
        public WallWorldCell(int x, int y) : base(x, y)
        {
            ForegroundColor = RgbColor.AnsiWhite;
            BackgroundColor = RgbColor.AnsiBlackBright;
            Glyph = '#';
            MovementSpeedMultiplier = 0.0f;
        }

        internal override WorldCellNextState CalculateNextState(WorldCellStateSnapshot snapshot, TimeSpan worldTime)
        {
            float averageTemperature = snapshot.GetRegionTemperature(Position, TEMPERATURE_SEARCH_RADIUS);
            float nextTemperature = (5 * Temperature + averageTemperature) / 6.0f;
            nextTemperature = WorldSettings.MoveTowardsIdealTemperature(nextTemperature);

            return new WorldCellNextState(nextTemperature, 0, 0);
        }
    }
}

