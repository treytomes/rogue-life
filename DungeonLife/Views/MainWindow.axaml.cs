using Avalonia.Controls;
using Avalonia.Input;
using DungeonLife.ViewModels;

namespace DungeonLife.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            Width = LifeAppSettings.InitialWindowWidth;
            Height = LifeAppSettings.InitialWindowHeight;

            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            var worldView = this.FindControl<WorldView>("WorldView");
            if (worldView != null)
            {
                worldView.Simulation = _viewModel.Simulation;
                worldView.CellSelected += OnCellSelected;
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
            var worldView = this.FindControl<WorldView>("WorldView");
            worldView?.InvalidateVisual();
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
            var worldView = this.FindControl<WorldView>("WorldView");
            if (worldView != null)
            {
                worldView.CellSelected -= OnCellSelected;
            }
            _viewModel.WorldUpdated -= OnWorldUpdated;
            _viewModel.Dispose();
        }
    }
}
