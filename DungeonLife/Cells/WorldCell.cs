using System.Numerics;

namespace DungeonLife
{
    abstract class WorldCell
    {
        #region Constants

        protected const int TEMPERATURE_SEARCH_RADIUS = 1;
        protected const int HUMIDITY_SEARCH_RADIUS = 2;
        protected const bool SHOW_TEMPERATURE = true;
        protected const bool SHOW_HUMIDITY = true;
        protected const float HUMIDITY_DELTA = 0.1f;

        #endregion

        #region Fields

        protected static IRandom _random = ThreadSafeRandom.Instance;
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
        public float MovementSpeedMultiplier { get; set; } = 1.0f;

        public bool BlocksMovement
        {
            get
            {
                return MovementSpeedMultiplier == 0;
            }
        }

        public float Temperature { get; set; } = LifeAppSettings.IdealTemperature;
        public float Humidity { get; set; } = 0;

        public virtual RgbColor BackgroundColor
        {
            get
            {
                var color = _backgroundColor;
                if (SHOW_TEMPERATURE)
                {
                    var tempColor = RgbColor.Lerp(RgbColor.Blue, RgbColor.Red, (Temperature - LifeAppSettings.MinimumTemperature) / LifeAppSettings.TemperatureRange);
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

        public virtual void Update(IWorldState state)
        {
            Humidity = state.Cells.GetRegionHumidity(Position, HUMIDITY_SEARCH_RADIUS) * 0.999f;
            if (Humidity > 1)
            {
                Humidity = 1;
            }
            else if (Humidity < 0)
            {
                Humidity = 0;
            }
        }

        #endregion
    }
}
