using System;

namespace DungeonLife
{
    /// <summary>
    /// A UI-agnostic 32-bit color. Values match the palette used by the old
    /// SadRogue.Primitives color type.
    /// </summary>
    public readonly struct RgbColor
    {
        public RgbColor(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;

        public static RgbColor FromFloat(float r, float g, float b, float a = 1.0f)
        {
            return new RgbColor(ToByte(r), ToByte(g), ToByte(b), ToByte(a));
        }

        public static RgbColor Lerp(RgbColor from, RgbColor to, float amount)
        {
            amount = Math.Clamp(amount, 0.0f, 1.0f);

            return new RgbColor(
                Blend(from.R, to.R, amount),
                Blend(from.G, to.G, amount),
                Blend(from.B, to.B, amount),
                Blend(from.A, to.A, amount));
        }

        private static byte ToByte(float value)
        {
            return (byte)(Math.Clamp(value, 0.0f, 1.0f) * 255.0f);
        }

        private static byte Blend(byte from, byte to, float amount)
        {
            return (byte)(from + (to - from) * amount);
        }

        public static readonly RgbColor Black = new RgbColor(0, 0, 0);
        public static readonly RgbColor White = new RgbColor(255, 255, 255);
        public static readonly RgbColor Blue = new RgbColor(0, 0, 255);
        public static readonly RgbColor Red = new RgbColor(255, 0, 0);
        public static readonly RgbColor Pink = new RgbColor(255, 192, 203);
        public static readonly RgbColor AnsiWhite = new RgbColor(170, 170, 170);
        public static readonly RgbColor AnsiWhiteBright = new RgbColor(255, 255, 255);
        public static readonly RgbColor AnsiBlackBright = new RgbColor(85, 85, 85);
        public static readonly RgbColor AnsiBlue = new RgbColor(0, 0, 170);
    }
}
