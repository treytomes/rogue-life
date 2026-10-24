using System;

namespace DungeonLife
{
    /// <summary>A wall that emanates heat throughout the day.</summary>
    public class BorderWorldCell : WallWorldCell
    {
        private const double MINUTES_PER_DAY = 24 * 60;

        public BorderWorldCell(int x, int y) : base(x, y)
        {
        }

        internal override WorldCellNextState CalculateNextState(WorldCellStateSnapshot snapshot, TimeSpan worldTime)
        {
            double minuteOfDay = worldTime.TotalMinutes % MINUTES_PER_DAY;
            double phase = 2.0 * Math.PI * minuteOfDay / MINUTES_PER_DAY;
            float temperature = WorldSettings.TemperatureRange / 2.0f * (1.0f - (float)Math.Cos(phase));
            return new WorldCellNextState(temperature, 0, 0);
        }
    }
}
