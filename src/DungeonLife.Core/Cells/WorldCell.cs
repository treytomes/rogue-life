using System;
using System.Numerics;

namespace DungeonLife
{
    public abstract class WorldCell
    {
        #region Constants

        protected const int TEMPERATURE_SEARCH_RADIUS = 1;
        protected const int HUMIDITY_SEARCH_RADIUS = 2;
        protected const bool SHOW_TEMPERATURE = true;
        protected const bool SHOW_HUMIDITY = true;
        protected const float HUMIDITY_DELTA = 0.1f;

        #endregion

        #region Fields

        private RgbColor _backgroundColor = RgbColor.Black;

        #endregion

        #region Constructors

        public WorldCell(int x, int y)
        {
            Position = new Vector2(x, y);
        }

        #endregion

        #region Properties

        public Vector2 Position { get; }
        public IRandom Random { get; internal set; } = null!;
        public float MovementSpeedMultiplier { get; set; } = 1.0f;

        public bool BlocksMovement
        {
            get => MovementSpeedMultiplier == 0;
        }

        public float Temperature { get; set; } = WorldSettings.IdealTemperature;
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

        public virtual RgbColor ForegroundColor { get; set; } = RgbColor.White;
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


