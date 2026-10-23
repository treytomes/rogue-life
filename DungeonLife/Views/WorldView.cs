using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using DungeonLife.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DungeonLife.Views
{
    public class WorldView : Control
    {
        #region Constants

        private const double FontSize = 14.0;
        private const float BlinkSpeed = 0.01f;

        #endregion

        #region Fields

        private readonly Dictionary<long, FormattedText> _textCache = new Dictionary<long, FormattedText>();
        private readonly Dictionary<uint, IBrush> _brushCache = new Dictionary<uint, IBrush>();
        private readonly Typeface _typeface = new Typeface(new FontFamily("Consolas"));
        private bool _isSelecting;
        private bool _isPanning;
        private bool _rightToggled;
        private Point _panStartPoint;
        private Vector _panStartOffset;
        private int _lastSelectedX = int.MinValue;
        private int _lastSelectedY = int.MinValue;
        private ScrollViewer _scrollViewer;

        #endregion

        #region Constructors

        public WorldView()
        {
            Width = LifeAppSettings.WorldWidth * CellWidth;
            Height = LifeAppSettings.WorldHeight * CellHeight;
            Focusable = true;
            ClipToBounds = true;
        }

        #endregion

        #region Events

        public event EventHandler<CellSelectedEventArgs> CellSelected;

        #endregion

        #region Properties

        public double CellWidth { get; } = LifeAppSettings.GlyphWidth;
        public double CellHeight { get; } = LifeAppSettings.GlyphHeight;

        internal WorldSimulation Simulation { get; set; }

        #endregion

        #region Rendering

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var simulation = Simulation;
            if (simulation == null)
            {
                return;
            }

            context.FillRectangle(Brushes.Black, new Rect(0, 0, Bounds.Width, Bounds.Height));

            var scrollViewer = GetScrollViewer();
            var offset = scrollViewer?.Offset ?? default;
            var viewportWidth = scrollViewer?.Viewport.Width ?? Bounds.Width;
            var viewportHeight = scrollViewer?.Viewport.Height ?? Bounds.Height;

            var startX = Math.Max(0, (int)Math.Floor(offset.X / CellWidth) - 1);
            var endX = Math.Min(LifeAppSettings.WorldWidth - 1, (int)Math.Ceiling((offset.X + viewportWidth) / CellWidth) + 1);
            var startY = Math.Max(0, (int)Math.Floor(offset.Y / CellHeight) - 1);
            var endY = Math.Min(LifeAppSettings.WorldHeight - 1, (int)Math.Ceiling((offset.Y + viewportHeight) / CellHeight) + 1);

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
                    DrawCell(context, x, y, glyph, cell.ForegroundColor, cell.BackgroundColor);
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

                DrawCell(context, x, y, entity.Glyph, entity.ForegroundColor, background);
            }
        }

        private void DrawCell(DrawingContext context, int x, int y, int glyph, RgbColor foreground, RgbColor background)
        {
            var bounds = new Rect(x * CellWidth, y * CellHeight, CellWidth, CellHeight);
            context.FillRectangle(GetBrush(background), bounds);

            var text = GetText(glyph, foreground);
            var textY = y * CellHeight + (CellHeight - text.Height) / 2.0;
            context.DrawText(text, new Point(x * CellWidth, textY));
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

        private FormattedText GetText(int glyph, RgbColor color)
        {
            var quantized = Quantize(color);
            var key = ((long)(uint)glyph << 32) | Pack(quantized);

            if (!_textCache.TryGetValue(key, out var text))
            {
                text = new FormattedText(
                    GlyphMap.ToUnicode(glyph),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    _typeface,
                    FontSize,
                    GetBrush(quantized));
                _textCache.Add(key, text);
            }

            return text;
        }

        private IBrush GetBrush(RgbColor color)
        {
            var quantized = Quantize(color);
            var key = Pack(quantized);

            if (!_brushCache.TryGetValue(key, out var brush))
            {
                brush = new SolidColorBrush(Color.FromArgb(quantized.A, quantized.R, quantized.G, quantized.B));
                _brushCache.Add(key, brush);
            }

            return brush;
        }

        private static RgbColor Quantize(RgbColor color)
        {
            return new RgbColor(
                (byte)(color.R & 0xF8),
                (byte)(color.G & 0xF8),
                (byte)(color.B & 0xF8),
                color.A);
        }

        private static uint Pack(RgbColor color)
        {
            return ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
        }

        #endregion

        #region Input

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var scrollViewer = GetScrollViewer();
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += OnScrollChanged;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            var scrollViewer = GetScrollViewer();
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged -= OnScrollChanged;
            }

            base.OnDetachedFromVisualTree(e);
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            InvalidateVisual();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            var point = e.GetCurrentPoint(this);
            var properties = point.Properties;

            if (properties.IsMiddleButtonPressed)
            {
                var scrollViewer = GetScrollViewer();
                if (scrollViewer != null)
                {
                    _isPanning = true;
                    _panStartPoint = e.GetPosition(scrollViewer);
                    _panStartOffset = scrollViewer.Offset;
                    e.Pointer.Capture(this);
                    e.Handled = true;
                }
            }
            else if (properties.IsLeftButtonPressed)
            {
                _isSelecting = true;
                e.Pointer.Capture(this);
                SelectCell(point.Position);
                e.Handled = true;
            }
            else if (properties.IsRightButtonPressed)
            {
                if (!_rightToggled)
                {
                    ToggleAlgae(point.Position);
                    _rightToggled = true;
                }
                e.Handled = true;
            }

            Focus();
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            if (_isPanning)
            {
                var scrollViewer = GetScrollViewer();
                if (scrollViewer != null)
                {
                    var current = e.GetPosition(scrollViewer);
                    var delta = current - _panStartPoint;
                    scrollViewer.Offset = new Vector(
                        _panStartOffset.X - delta.X,
                        _panStartOffset.Y - delta.Y);
                }
            }
            else if (_isSelecting)
            {
                SelectCell(e.GetPosition(this));
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            _isSelecting = false;
            _isPanning = false;
            _rightToggled = false;
            e.Pointer.Capture(null);
        }

        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            base.OnPointerCaptureLost(e);

            _isSelecting = false;
            _isPanning = false;
            _rightToggled = false;
        }

        private void SelectCell(Point point)
        {
            var x = (int)(point.X / CellWidth);
            var y = (int)(point.Y / CellHeight);

            if ((x < 0) || (x >= LifeAppSettings.WorldWidth) || (y < 0) || (y >= LifeAppSettings.WorldHeight))
            {
                return;
            }

            if ((x == _lastSelectedX) && (y == _lastSelectedY))
            {
                return;
            }

            _lastSelectedX = x;
            _lastSelectedY = y;
            CellSelected?.Invoke(this, new CellSelectedEventArgs(x, y));
        }

        private void ToggleAlgae(Point point)
        {
            var simulation = Simulation;
            if (simulation == null)
            {
                return;
            }

            var x = (int)(point.X / CellWidth);
            var y = (int)(point.Y / CellHeight);
            var cell = simulation.Cells[x, y] as FloorWorldCell;
            if (cell != null)
            {
                cell.AlgaeLevel = (cell.AlgaeLevel > 0) ? 0.0f : 1.0f;
                InvalidateVisual();
            }
        }

        private ScrollViewer GetScrollViewer()
        {
            return _scrollViewer ?? (_scrollViewer = this.FindAncestorOfType<ScrollViewer>());
        }

        #endregion
    }
}
