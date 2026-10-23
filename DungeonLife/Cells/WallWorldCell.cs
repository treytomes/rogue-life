namespace DungeonLife
{

    /// <summary>
    /// A wall cannot be passed.
    /// </summary>
    class WallWorldCell : WorldCell
    {
        public WallWorldCell(int x, int y)
            : base(x, y)
        {
            ForegroundColor = RgbColor.AnsiWhite;
            BackgroundColor = RgbColor.AnsiBlackBright;
            Glyph = '#';
            MovementSpeedMultiplier = 0.0f;
        }

        public override void Update(IWorldState state)
        {
            base.Update(state);

            var newTemp = state.Cells.GetRegionTemperature(Position, TEMPERATURE_SEARCH_RADIUS);

            // Walls act as a temperature stabilizer; much more so than water.
            Temperature = (5 * Temperature + newTemp) / 6.0f;
            if (Temperature > LifeAppSettings.IdealTemperature)
            {
                Temperature -= 0.001f;
            }
            else if (Temperature < LifeAppSettings.IdealTemperature)
            {
                Temperature += 0.001f;
            }

            Humidity = 0;
        }
    }
}
