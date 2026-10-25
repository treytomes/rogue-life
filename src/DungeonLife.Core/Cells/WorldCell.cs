using System;
using System.Numerics;

namespace DungeonLife
{
    /// <summary>Represents WorldCell in the simulation.</summary>
    public abstract class WorldCell
    {
        #region Constants

        /// <summary>Defines the TEMPERATURE_SEARCH_RADIUS value.</summary>
        protected const int TEMPERATURE_SEARCH_RADIUS = 1;
        /// <summary>Defines the HUMIDITY_SEARCH_RADIUS value.</summary>
        protected const int HUMIDITY_SEARCH_RADIUS = 2;
        /// <summary>Defines the SHOW_TEMPERATURE value.</summary>
        protected const bool SHOW_TEMPERATURE = true;
        /// <summary>Defines the SHOW_HUMIDITY value.</summary>
        protected const bool SHOW_HUMIDITY = true;
        /// <summary>Defines the HUMIDITY_DELTA value.</summary>
        protected const float HUMIDITY_DELTA = 0.1f;

        #endregion

        #region Fields

        private RgbColor _backgroundColor = RgbColor.Black;

        #endregion

        #region Constructors

        /// <summary>Creates an instance of WorldCell.</summary>
        public WorldCell(int x, int y)
        {
            Position = new Vector2(x, y);
        }

        #endregion

        #region Properties

        /// <summary>Gets the Position value.</summary>
        public Vector2 Position { get; }
        /// <summary>Gets or sets the Random value.</summary>
        public IRandom Random { get; internal set; } = null!;
        /// <summary>Gets or sets the MovementSpeedMultiplier value.</summary>
        public float MovementSpeedMultiplier { get; set; } = 1.0f;

        /// <summary>Documents this simulation API member.</summary>
        public bool BlocksMovement
        {
            get => MovementSpeedMultiplier == 0;
        }

        /// <summary>Gets or sets the Temperature value.</summary>
        public float Temperature { get; set; } = WorldSettings.IdealTemperature;
        /// <summary>Gets or sets the Humidity value.</summary>
        public float Humidity { get; set; }

        internal void RestoreSavedState(SavedCellState state)
        {
            Temperature = state.Temperature;
            Humidity = state.Humidity;
            MovementSpeedMultiplier = state.MovementSpeedMultiplier;
            if (this is FloorWorldCell floor) floor.AlgaeLevel = state.AlgaeLevel;
            if (Random is not Pcg32Random random)
                throw new InvalidOperationException("Saved state requires PCG32 cell streams.");
            random.RestoreState(state.RandomState);
        }

        internal SavedCellState CaptureSavedState()
        {
            if (Random is not Pcg32Random random)
                throw new InvalidOperationException("Saved state requires PCG32 cell streams.");
            return new SavedCellState(GetType().FullName ?? GetType().Name, Temperature, Humidity,
                MovementSpeedMultiplier, this is FloorWorldCell floor ? floor.AlgaeLevel : 0,
                random.CaptureState());
        }

        /// <summary>Documents this simulation API member.</summary>
        public virtual RgbColor BackgroundColor
        {
            get
            {
                var color = _backgroundColor;
                if (SHOW_TEMPERATURE)
                {
                    var tempColor = RgbColor.Lerp(RgbColor.Blue, RgbColor.Red, (Temperature - WorldSettings.MinimumTemperature) / WorldSettings.TemperatureRange);
                    color = RgbColor.Lerp(color, tempColor, 0.5f);
                }
                if (SHOW_HUMIDITY)
                {
                    color = RgbColor.Lerp(color, RgbColor.AnsiWhite, Humidity);
                }

                return color;
            }
            set => _backgroundColor = value;
        }

        /// <summary>Gets or sets the ForegroundColor value.</summary>
        public virtual RgbColor ForegroundColor { get; set; } = RgbColor.White;
        /// <summary>Gets or sets the Glyph value.</summary>
        public virtual int Glyph { get; set; } = '.';

        #endregion

        #region Methods

        internal virtual WorldCellNextState CalculateNextState(WorldCellStateSnapshot snapshot, TimeSpan worldTime)
        {
            return new WorldCellNextState(Temperature, CalculateNextHumidity(snapshot), 0);
        }

        private protected float CalculateNextHumidity(WorldCellStateSnapshot snapshot)
        {
            float humidity = snapshot.GetRegionHumidity(Position, HUMIDITY_SEARCH_RADIUS) * 0.999f;
            return Math.Clamp(humidity, 0.0f, 1.0f);
        }

        #endregion
    }
}


