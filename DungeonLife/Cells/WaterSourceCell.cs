namespace DungeonLife
{
    /// <summary>
    /// Water defies temperature changes, and will spread humidity as it warms up.
    /// </summary>
    class WaterSourceCell : WorldCell
    {
        public const int GLYPH_WATER1 = '~';
        public const int GLYPH_WATER2 = 247;
        public const int ANIMATE_MS = 500;

        public WaterSourceCell(int x, int y)
            : base(x, y)
        {
            ForegroundColor = RgbColor.AnsiWhite;
            BackgroundColor = RgbColor.AnsiBlue;
            Glyph = GLYPH_WATER1;
            MovementSpeedMultiplier = 0.25f;
        }

        public override void Update(IWorldState state)
        {
            base.Update(state);

            var tempDistance = Temperature - LifeAppSettings.IdealTemperature;
            // Water will evaporate or condense depending on how hot or cold it is.
            if ((tempDistance > 0) && (Humidity < 1))
            {
                Humidity += HUMIDITY_DELTA * tempDistance;
            }
            else if ((tempDistance < 0) && (Humidity > 0))
            {
                Humidity -= HUMIDITY_DELTA;
            }
            if (Humidity > 1)
            {
                Humidity = 1;
            }
            else if (Humidity < 0)
            {
                Humidity = 0;
            }

            var newTemp = state.Cells.GetRegionTemperature(Position, TEMPERATURE_SEARCH_RADIUS);

            // Water act as a temperature stabilizer; much more so than floors.
            Temperature = (2 * Temperature + newTemp) / 3.0f;
            if (Temperature > LifeAppSettings.IdealTemperature)
            {
                Temperature -= 0.001f;
            }
            else if (Temperature < LifeAppSettings.IdealTemperature)
            {
                Temperature += 0.001f;
            }
        }
    }
}
