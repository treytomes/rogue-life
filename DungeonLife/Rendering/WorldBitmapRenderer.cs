using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DungeonLife.Rendering
{
    sealed class WorldBitmapRenderer : IDisposable
    {
        #region Constants

        private const int BytesPerPixel = 4;
        private const float BlinkSpeed = 0.01f;

        #endregion

        #region Fields

        private readonly GlyphAtlas _atlas;
        private WriteableBitmap _bitmap;
        private byte[] _pixels;
        private int _width;
        private int _height;

        #endregion

        #region Constructors

        public WorldBitmapRenderer(int cellWidth, int cellHeight)
        {
            _atlas = new GlyphAtlas(cellWidth, cellHeight);
        }

        #endregion

        #region Properties

        public int CellWidth => _atlas.CellWidth;
        public int CellHeight => _atlas.CellHeight;
        public TimeSpan LastRenderDuration { get; private set; }
        public int LastVisibleCellCount { get; private set; }

        #endregion

        #region Methods

        public void Render(DrawingContext context, WorldSimulation simulation, double offsetX, double offsetY, double viewportWidth, double viewportHeight)
        {
            var start = Stopwatch.GetTimestamp();
            var roundedOffsetX = (int)Math.Round(offsetX);
            var roundedOffsetY = (int)Math.Round(offsetY);

            var worldPixelWidth = LifeAppSettings.WorldWidth * CellWidth;
            var worldPixelHeight = LifeAppSettings.WorldHeight * CellHeight;
            var width = Math.Max(1, Math.Min((int)Math.Ceiling(viewportWidth), worldPixelWidth));
            var height = Math.Max(1, Math.Min((int)Math.Ceiling(viewportHeight), worldPixelHeight));

            EnsureBuffer(width, height);
            ClearBuffer();

            var startX = Math.Max(0, (int)Math.Floor((double)roundedOffsetX / CellWidth) - 1);
            var endX = Math.Min(LifeAppSettings.WorldWidth - 1, (int)Math.Ceiling((double)(roundedOffsetX + width) / CellWidth) + 1);
            var startY = Math.Max(0, (int)Math.Floor((double)roundedOffsetY / CellHeight) - 1);
            var endY = Math.Min(LifeAppSettings.WorldHeight - 1, (int)Math.Ceiling((double)(roundedOffsetY + height) / CellHeight) + 1);

            var visibleCellCount = 0;
            for (var y = startY; y <= endY; y++)
            {
                for (var x = startX; x <= endX; x++)
                {
                    var cell = simulation.Cells[x, y];
                    if (cell == null)
                    {
                        continue;
                    }

                    var glyph = GetGlyph(cell, simulation);
                    DrawCell(x, y, glyph, cell.ForegroundColor, cell.BackgroundColor, roundedOffsetX, roundedOffsetY);
                    visibleCellCount++;
                }
            }

            foreach (var entity in simulation.Entities)
            {
                var x = (int)entity.Position.X;
                var y = (int)entity.Position.Y;
                if ((x < startX) || (x > endX) || (y < startY) || (y > endY))
                {
                    continue;
                }

                var background = simulation.Cells[x, y]?.BackgroundColor ?? RgbColor.Black;
                if (entity.IsSelected)
                {
                    var amount = 0.5f * ((float)Math.Sin(BlinkSpeed * simulation.AnimationTime.TotalMilliseconds) + 1.0f);
                    background = RgbColor.Lerp(background, RgbColor.Red, amount);
                }

                DrawCell(x, y, entity.Glyph, entity.ForegroundColor, background, roundedOffsetX, roundedOffsetY);
            }

            UploadBuffer();
            context.DrawImage(_bitmap, new Rect(roundedOffsetX, roundedOffsetY, width, height));

            LastVisibleCellCount = visibleCellCount;
            LastRenderDuration = Stopwatch.GetElapsedTime(start);
        }

        public void Dispose()
        {
            _bitmap?.Dispose();
        }

        private void EnsureBuffer(int width, int height)
        {
            if ((_bitmap != null) && (_width == width) && (_height == height))
            {
                return;
            }

            _bitmap?.Dispose();
            _width = width;
            _height = height;
            _bitmap = new WriteableBitmap(
                new PixelSize(width, height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);
            _pixels = new byte[width * height * BytesPerPixel];
        }

        private void ClearBuffer()
        {
            for (var i = 0; i < _pixels.Length; i += BytesPerPixel)
            {
                _pixels[i] = 0;
                _pixels[i + 1] = 0;
                _pixels[i + 2] = 0;
                _pixels[i + 3] = 255;
            }
        }

        private void DrawCell(int cellX, int cellY, int glyph, RgbColor foreground, RgbColor background, int offsetX, int offsetY)
        {
            var left = (cellX * CellWidth) - offsetX;
            var top = (cellY * CellHeight) - offsetY;

            FillRectangleCell(left, top, background);
            BlendGlyphCell(left, top, glyph, foreground);
        }

        private void FillRectangleCell(int left, int top, RgbColor color)
        {
            var x0 = Math.Max(0, left);
            var x1 = Math.Min(_width, left + CellWidth);
            var y0 = Math.Max(0, top);
            var y1 = Math.Min(_height, top + CellHeight);
            if ((x0 >= x1) || (y0 >= y1))
            {
                return;
            }

            FillRectangle(x0, y0, x1, y1, color);
        }

        private void FillRectangle(int x0, int y0, int x1, int y1, RgbColor color)
        {
            for (var y = y0; y < y1; y++)
            {
                var index = ((y * _width) + x0) * BytesPerPixel;
                for (var x = x0; x < x1; x++)
                {
                    _pixels[index] = color.B;
                    _pixels[index + 1] = color.G;
                    _pixels[index + 2] = color.R;
                    _pixels[index + 3] = color.A;
                    index += BytesPerPixel;
                }
            }
        }

        private void BlendGlyphCell(int cellLeft, int cellTop, int glyph, RgbColor foreground)
        {
            var x0 = Math.Max(0, cellLeft);
            var x1 = Math.Min(_width, cellLeft + CellWidth);
            var y0 = Math.Max(0, cellTop);
            var y1 = Math.Min(_height, cellTop + CellHeight);
            if ((x0 >= x1) || (y0 >= y1))
            {
                return;
            }

            var alpha = _atlas.Alpha;
            var maskOffset = _atlas.GetOffset(glyph);
            var foregroundAlpha = foreground.A;

            for (var y = y0; y < y1; y++)
            {
                var maskRow = maskOffset + ((y - cellTop) * CellWidth) + (x0 - cellLeft);
                var index = ((y * _width) + x0) * BytesPerPixel;

                for (var x = x0; x < x1; x++)
                {
                    var maskAlpha = alpha[maskRow++];
                    if (maskAlpha == 0)
                    {
                        index += BytesPerPixel;
                        continue;
                    }

                    var sourceAlpha = (maskAlpha * foregroundAlpha + 127) / 255;
                    var inverseAlpha = 255 - sourceAlpha;

                    _pixels[index] = (byte)(((foreground.B * sourceAlpha) + (_pixels[index] * inverseAlpha) + 127) / 255);
                    _pixels[index + 1] = (byte)(((foreground.G * sourceAlpha) + (_pixels[index + 1] * inverseAlpha) + 127) / 255);
                    _pixels[index + 2] = (byte)(((foreground.R * sourceAlpha) + (_pixels[index + 2] * inverseAlpha) + 127) / 255);
                    _pixels[index + 3] = 255;

                    index += BytesPerPixel;
                }
            }
        }

        private void UploadBuffer()
        {
            using (var framebuffer = _bitmap.Lock())
            {
                if (framebuffer.RowBytes == (_width * BytesPerPixel))
                {
                    Marshal.Copy(_pixels, 0, framebuffer.Address, _pixels.Length);
                    return;
                }

                var rowLength = _width * BytesPerPixel;
                for (var y = 0; y < _height; y++)
                {
                    var source = y * rowLength;
                    var destination = IntPtr.Add(framebuffer.Address, y * framebuffer.RowBytes);
                    Marshal.Copy(_pixels, source, destination, rowLength);
                }
            }
        }

        private static int GetGlyph(WorldCell cell, WorldSimulation simulation)
        {
            if (cell is WaterSourceCell)
            {
                var phase = (int)(simulation.AnimationTime.TotalMilliseconds / WaterSourceCell.ANIMATE_MS);
                return (phase % 2 == 0) ? WaterSourceCell.GLYPH_WATER1 : WaterSourceCell.GLYPH_WATER2;
            }

            return cell.Glyph;
        }

        #endregion
    }
}
