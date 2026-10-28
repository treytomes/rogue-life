using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using DungeonLife.Rendering;
using System;

namespace DungeonLife.Views
{
    /// <summary>Represents WorldView in the simulation.</summary>
    public class WorldView : Control
    {
        #region Constants

        #endregion

        #region Fields

        private readonly WorldBitmapRenderer _renderer;
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

        /// <summary>Creates an instance of WorldView.</summary>
        public WorldView()
        {
            Width = WorldSettings.WorldWidth * CellWidth;
            Height = WorldSettings.WorldHeight * CellHeight;
            Focusable = true;
            ClipToBounds = true;
            RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
            _renderer = new WorldBitmapRenderer((int)CellWidth, (int)CellHeight);
        }

        #endregion

        #region Events

        /// <summary>Defines the CellSelected value.</summary>
        public event EventHandler<CellSelectedEventArgs> CellSelected;

        #endregion

        #region Properties

        /// <summary>Gets the CellWidth value.</summary>
        public double CellWidth { get; } = LifeAppSettings.GlyphWidth;
        /// <summary>Gets the CellHeight value.</summary>
        public double CellHeight { get; } = LifeAppSettings.GlyphHeight;
        /// <summary>Defines the LastRenderDuration value.</summary>
        public TimeSpan LastRenderDuration => _renderer.LastRenderDuration;
        /// <summary>Defines the LastVisibleCellCount value.</summary>
        public int LastVisibleCellCount => _renderer.LastVisibleCellCount;

        internal WorldSimulation Simulation { get; set; }

        #endregion

        #region Rendering

        /// <summary>Executes the Render behavior.</summary>
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var simulation = Simulation;
            if (simulation == null)
            {
                return;
            }

            var scrollViewer = GetScrollViewer();
            var offset = scrollViewer?.Offset ?? default;
            var viewportWidth = scrollViewer?.Viewport.Width ?? Bounds.Width;
            var viewportHeight = scrollViewer?.Viewport.Height ?? Bounds.Height;

            using (context.PushClip(new Rect(offset.X, offset.Y, viewportWidth, viewportHeight)))
            {
                context.FillRectangle(Brushes.Black, new Rect(offset.X, offset.Y, viewportWidth, viewportHeight));
                _renderer.Render(context, simulation, offset.X, offset.Y, viewportWidth, viewportHeight);
            }
        }

        #endregion

        #region Input

        /// <summary>Executes the OnAttachedToVisualTree behavior.</summary>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var scrollViewer = GetScrollViewer();
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += OnScrollChanged;
            }
        }

        /// <summary>Executes the OnDetachedFromVisualTree behavior.</summary>
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

        /// <summary>Executes the OnPointerPressed behavior.</summary>
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

        /// <summary>Executes the OnPointerMoved behavior.</summary>
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

        /// <summary>Executes the OnPointerReleased behavior.</summary>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            _isSelecting = false;
            _isPanning = false;
            _rightToggled = false;
            e.Pointer.Capture(null);
        }

        /// <summary>OnPointerCaptureLost simulation state.</summary>
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

            if ((x < 0) || (x >= WorldSettings.WorldWidth) || (y < 0) || (y >= WorldSettings.WorldHeight))
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
