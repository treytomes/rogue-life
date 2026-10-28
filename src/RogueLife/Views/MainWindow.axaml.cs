using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using System;
using System.Threading.Tasks;
using RogueLife.ViewModels;

namespace RogueLife.Views
{
    /// <summary>Represents MainWindow in the simulation.</summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private WorldView _worldView;

        /// <summary>Creates an instance of MainWindow.</summary>
        public MainWindow()
        {
            InitializeComponent();

            Width = LifeAppSettings.InitialWindowWidth;
            Height = LifeAppSettings.InitialWindowHeight;
        }

        /// <summary>Creates an instance of MainWindow.</summary>
        public MainWindow(MainWindowViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = _viewModel;

            _worldView = this.FindControl<WorldView>("WorldView");
            if (_worldView != null)
            {
                _worldView.Simulation = _viewModel.Simulation;
                _worldView.CellSelected += OnCellSelected;
            }

            _viewModel.WorldUpdated += OnWorldUpdated;
            _viewModel.SimulationReplaced += OnSimulationReplaced;

            KeyDown += OnKeyDown;
        }

        private async void OnSaveClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                IStorageFile file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save Rogue Life",
                    SuggestedFileName = $"dungeon-life-{_viewModel.Simulation.TickCount}.dlsave.json",
                    DefaultExtension = "dlsave.json",
                    FileTypeChoices = new[] { new FilePickerFileType("Rogue Life save") { Patterns = new[] { "*.dlsave.json" } } },
                });
                if (file?.TryGetLocalPath() is string path)
                    await _viewModel.SaveSimulationAsync(path);
            }
            catch (Exception exception)
            {
                await ShowErrorAsync("Could not save simulation", exception.Message);
            }
        }

        private async void OnLoadClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Load Rogue Life",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { new FilePickerFileType("Rogue Life save") { Patterns = new[] { "*.dlsave.json" } } },
                });
                if (files.Count > 0 && files[0].TryGetLocalPath() is string path)
                    await _viewModel.LoadSimulationAsync(path);
            }
            catch (Exception exception)
            {
                await ShowErrorAsync("Could not load simulation", exception.Message);
            }
        }

        private void OnSimulationReplaced(object sender, System.EventArgs e)
        {
            if (_worldView != null) _worldView.Simulation = _viewModel.Simulation;
        }

        private async Task ShowErrorAsync(string title, string message)
        {
            var okButton = new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
            var panel = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    okButton,
                },
            };
            var dialog = new Window
            {
                Title = title,
                Width = 480,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = panel,
            };
            okButton.Click += (_, _) => dialog.Close();
            await dialog.ShowDialog(this);
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

        /// <summary>Executes the OnClosed behavior.</summary>
        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            if (_worldView != null)
            {
                _worldView.CellSelected -= OnCellSelected;
            }
            if (_viewModel != null)
            {
                _viewModel.WorldUpdated -= OnWorldUpdated;
                _viewModel.SimulationReplaced -= OnSimulationReplaced;
            }
        }
    }
}
