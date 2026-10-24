using System;

namespace DungeonLife
{
    /// <summary>
    /// A wall that emanates heat throughout the day.
    /// </summary>
    public class BorderWorldCell : WallWorldCell
    {
        public BorderWorldCell(int x, int y)
            : base(x, y)
        {
        }

        public override void Update(IWorldState state)
        {
            //// Temperature will range from 10C to 35C over the course of the day.
            //Temperature = -12.5f * (float)Math.Cos(state.WorldTime.Hours / 3.8) + 12.5f + 10;

            // Temperature will range from 0C to 50C over the course of the day.
            Temperature = WorldSettings.TemperatureRange / 2.0f * (-1 * (float)Math.Cos(state.WorldTime.Hours / 3.8) + 1);
        }
    }
}
