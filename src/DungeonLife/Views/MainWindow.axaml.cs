using Avalonia.Controls;
using Avalonia.Input;
using DungeonLife.ViewModels;

namespace DungeonLife.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;
        private WorldView _worldView;

        public MainWindow()
        {
            InitializeComponent();

            Width = LifeAppSettings.InitialWindowWidth;
            Height = LifeAppSettings.InitialWindowHeight;

            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            _worldView = this.FindControl<WorldView>("WorldView");
            if (_worldView != null)
            {
                _worldView.Simulation = _viewModel.Simulation;
                _worldView.CellSelected += OnCellSelected;
            }

            _viewModel.WorldUpdated += OnWorldUpdated;

            KeyDown += OnKeyDown;
        }

        private void OnCellSelected(object sender, CellSelectedEventArgs e)
        {
            _viewModel.SelectCell(e.WorldX, e.WorldY);
        }

        private void OnWorldUpdated(object sender, System.EventArgs e)
        {
            _worldView?.InvalidateVisual();
            _viewModel.UpdatePerformance(
                _viewModel.Simulation.LastTickDuration,
                _worldView?.LastRenderDuration ?? System.TimeSpan.Zero,
                _worldView?.LastVisibleCellCount ?? 0);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
                e.Handled = true;
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            if (_worldView != null)
            {
                _worldView.CellSelected -= OnCellSelected;
            }
            _viewModel.WorldUpdated -= OnWorldUpdated;
            _viewModel.Dispose();
        }
    }
}
