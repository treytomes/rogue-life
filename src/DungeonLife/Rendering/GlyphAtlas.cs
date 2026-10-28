using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Runtime.InteropServices;

namespace DungeonLife.Rendering
{
    sealed class GlyphAtlas
    {
        #region Constants

        private const int GlyphCount = 256;
        private const int SourceCellWidth = 8;
        private const int SourceCellHeight = 8;

        #endregion

        #region Fields

        private readonly int _cellWidth;
        private readonly int _cellHeight;
        private readonly byte[] _alpha;

        #endregion

        #region Constructors

        public GlyphAtlas(int cellWidth, int cellHeight)
        {
            _cellWidth = cellWidth;
            _cellHeight = cellHeight;
            _alpha = new byte[GlyphCount * cellWidth * cellHeight];

            using (var stream = AssetLoader.Open(new Uri("avares://DungeonLife/Assets/oem437_8.png")))
            using (var bitmap = new Bitmap(stream))
            using (var source = new WriteableBitmap(bitmap.PixelSize, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul))
            using (var framebuffer = source.Lock())
            {
                bitmap.CopyPixels(framebuffer);
                var pixels = new byte[framebuffer.RowBytes * bitmap.PixelSize.Height];
                Marshal.Copy(framebuffer.Address, pixels, 0, pixels.Length);

                for (var glyph = 0; glyph < GlyphCount; glyph++)
                {
                    var sourceX = (glyph % 16) * SourceCellWidth;
                    var sourceY = (glyph / 16) * SourceCellHeight;
                    var targetOffset = glyph * cellWidth * cellHeight;

                    for (var y = 0; y < cellHeight; y++)
                    {
                        // Scale from the 8x8 sprite cell to the configured grid cell.
                        var sy = sourceY + (y * SourceCellHeight / cellHeight);
                        var sourceRow = sy * framebuffer.RowBytes;
                        var targetRow = targetOffset + y * cellWidth;

                        for (var x = 0; x < cellWidth; x++)
                        {
                            var sx = sourceX + (x * SourceCellWidth / cellWidth);
                            var pixelOffset = sourceRow + sx * 4;
                            var red = pixels[pixelOffset + 2];
                            var green = pixels[pixelOffset + 1];
                            var blue = pixels[pixelOffset];
                            _alpha[targetRow + x] = Math.Max(pixels[pixelOffset + 3], Math.Max(red, Math.Max(green, blue)));
                        }
                    }
                }
            }
        }

        #endregion

        #region Properties

        public int CellWidth => _cellWidth;
        public int CellHeight => _cellHeight;
        public byte[] Alpha => _alpha;

        #endregion

        #region Methods

        public int GetOffset(int glyph)
        {
            return glyph * _cellWidth * _cellHeight;
        }

        #endregion
    }
}
