namespace RogueLife
{
    /// <summary>Represents LifeAppSettings in the simulation.</summary>
    public static class LifeAppSettings
    {
        /// <summary>Gets the WindowTitle value.</summary>
        public static string WindowTitle { get; } = "Dungeon Life";
        /// <summary>Gets the GlyphWidth value.</summary>
        public static int GlyphWidth { get; } = 8;
        /// <summary>Gets the GlyphHeight value.</summary>
        public static int GlyphHeight { get; } = 8;
        /// <summary>Gets the DefaultViewportWidthInCells value.</summary>
        public static int DefaultViewportWidthInCells { get; } = 200;
        /// <summary>Gets the DefaultViewportHeightInCells value.</summary>
        public static int DefaultViewportHeightInCells { get; } = 60;
        /// <summary>Gets the DetailsPanelWidth value.</summary>
        public static int DetailsPanelWidth { get; } = 280;
        /// <summary>Gets the ToolbarHeight value.</summary>
        public static int ToolbarHeight { get; } = 40;
        /// <summary>Gets the InitialWindowWidth value.</summary>
        public static int InitialWindowWidth { get; } = DefaultViewportWidthInCells * GlyphWidth + DetailsPanelWidth;
        /// <summary>Gets the InitialWindowHeight value.</summary>
        public static int InitialWindowHeight { get; } = DefaultViewportHeightInCells * GlyphHeight + ToolbarHeight;
    }
}
