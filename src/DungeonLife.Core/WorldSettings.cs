namespace DungeonLife
{
    /// <summary>
    /// Constants that define the world model independently of its presentation.
    /// </summary>
    public static class WorldSettings
    {
        /// <summary>Increment when model rules or state semantics change.</summary>
        public const int RulesVersion = 1;

        public static float MinimumTemperature { get; } = 0.0f;
        public static float MaximumTemperature { get; } = 50.0f;
        public static float TemperatureRange { get; } = MaximumTemperature - MinimumTemperature;
        public static int WorldWidth { get; } = 256;
        public static int WorldHeight { get; } = 256;

        /// <summary>
        /// The ideal temperature for water; also encourages algae growth.
        /// </summary>
        public static float IdealTemperature { get; } = 21;
    }
}
