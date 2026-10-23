namespace DungeonLife
{
    public static class LifeAppSettings
    {
        public static string WindowTitle { get; } = "Dungeon Life";
        public static int GlyphWidth { get; } = 8;
        public static int GlyphHeight { get; } = 16;
        public static int DefaultViewportWidthInCells { get; } = 200;
        public static int DefaultViewportHeightInCells { get; } = 60;
        public static int DetailsPanelWidth { get; } = 280;
        public static int ToolbarHeight { get; } = 40;
        public static int InitialWindowWidth { get; } = DefaultViewportWidthInCells * GlyphWidth + DetailsPanelWidth;
        public static int InitialWindowHeight { get; } = DefaultViewportHeightInCells * GlyphHeight + ToolbarHeight;
        public static float MinimumTemperature { get; } = 0.0f;
        public static float MaximumTemperature { get; } = 50.0f;
        public static float TemperatureRange { get; } = MaximumTemperature - MinimumTemperature;
        public static int WorldWidth { get; } = 256;
        public static int WorldHeight { get; } = 256;

        /// <summary>
        /// The ideal temperature for water; also encourages algae growth.
        /// </summary>
        public static float IdealTemperature = 21;
    }
}
