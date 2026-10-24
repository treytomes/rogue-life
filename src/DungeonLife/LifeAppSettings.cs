namespace DungeonLife
{
    public static class LifeAppSettings
    {
        public static string WindowTitle { get; } = "Dungeon Life";
        public static int GlyphWidth { get; } = 8;
        public static int GlyphHeight { get; } = 8;
        public static int DefaultViewportWidthInCells { get; } = 200;
        public static int DefaultViewportHeightInCells { get; } = 60;
        public static int DetailsPanelWidth { get; } = 280;
        public static int ToolbarHeight { get; } = 40;
        public static int InitialWindowWidth { get; } = DefaultViewportWidthInCells * GlyphWidth + DetailsPanelWidth;
        public static int InitialWindowHeight { get; } = DefaultViewportHeightInCells * GlyphHeight + ToolbarHeight;
    }
}
