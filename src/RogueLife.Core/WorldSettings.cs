using System;

namespace RogueLife
{
    /// <summary>
    /// Constants that define the world model independently of its presentation.
    /// </summary>
    public static class WorldSettings
    {
        /// <summary>Defines the RulesVersion value.</summary>
        public const int RulesVersion = 2;

        /// <summary>Gets the MinimumTemperature value.</summary>
        public static float MinimumTemperature { get; } = 0.0f;
        /// <summary>Gets the MaximumTemperature value.</summary>
        public static float MaximumTemperature { get; } = 50.0f;
        /// <summary>Gets the TemperatureRange value.</summary>
        public static float TemperatureRange { get; } = MaximumTemperature - MinimumTemperature;
        internal const float TemperatureIdealStep = 0.001f;
        /// <summary>Gets the WorldWidth value.</summary>
        public static int WorldWidth { get; } = 256;
        /// <summary>Gets the WorldHeight value.</summary>
        public static int WorldHeight { get; } = 256;

        /// <summary>
        /// The ideal temperature for water; also encourages algae growth.
        /// </summary>
        public static float IdealTemperature { get; } = 21;

        internal static float MoveTowardsIdealTemperature(float temperature)
        {
            if (temperature > IdealTemperature)
            {
                return Math.Max(IdealTemperature, temperature - TemperatureIdealStep);
            }

            if (temperature < IdealTemperature)
            {
                return Math.Min(IdealTemperature, temperature + TemperatureIdealStep);
            }

            return temperature;
        }
    }
}
