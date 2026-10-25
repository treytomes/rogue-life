using Avalonia.Media;
using Avalonia.Threading;
using DungeonLife.Views;
using DungeonLife.Hosting;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;

namespace DungeonLife.ViewModels
{
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        #region Constants

        private static readonly IBrush MaleBrush = new SolidColorBrush(Color.FromRgb(0, 0, 255));
        private static readonly IBrush FemaleBrush = new SolidColorBrush(Color.FromRgb(255, 192, 203));

        #endregion

        #region Fields

        private readonly DispatcherTimer _timer;
        private WorldCell _selectedCell;
        private Entity _selectedEntity;
        private bool _isRunning = true;
        private bool _isStepping;
        private string _timeText = "Time: 0 days, 0 hours, 0 minutes";
        private string _performanceText = "Tick: 0.0 ms  Render: 0.0 ms  Cells: 0";
        private string _cellHeader = "Cell Details";
        private string _algaeText = "Algae: 0";
        private string _temperatureText = "Temperature: 0C";
        private string _humidityText = "Humidity: 0%";
        private string _entityHeader = "Entity Details";
        private string _entityName = "Name:";
        private IBrush _entityNameBrush = FemaleBrush;
        private string _entityAgeText = "Age:";
        private string _entityHungerText = "Hunger: 0%";
        private double _entityHunger;
        private string _entityThirstText = "Thirst: 0%";
        private double _entityThirst;
        private string _entityMetabolismText = "Metabolism:";
        private string _entityIsChildText = "Is child? no";
        private string _entityStateText = "State:";
        private bool _hasSelectedEntity;

        #endregion

        #region Constructors

        public MainWindowViewModel(WorldSimulation simulation, AppStartupSettings startupSettings)
        {
            Simulation = simulation;

            RunCommand = new DelegateCommand(Run, () => !IsRunning);
            PauseCommand = new DelegateCommand(Pause, () => IsRunning);
            StepCommand = new DelegateCommand(Step, () => !IsRunning);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(startupSettings.TickIntervalMilliseconds)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();

            RefreshDetails();
        }

        #endregion

        #region Events

        public event EventHandler WorldUpdated;

        #endregion

        #region Properties

        internal WorldSimulation Simulation { get; private set; }

        public event EventHandler SimulationReplaced;

        public DelegateCommand RunCommand { get; }
        public DelegateCommand PauseCommand { get; }
        public DelegateCommand StepCommand { get; }

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public string TimeText
        {
            get => _timeText;
            private set => SetProperty(ref _timeText, value);
        }

        public string PerformanceText
        {
            get => _performanceText;
            private set => SetProperty(ref _performanceText, value);
        }

        public string CellHeader
        {
            get => _cellHeader;
            private set => SetProperty(ref _cellHeader, value);
        }

        public string AlgaeText
        {
            get => _algaeText;
            private set => SetProperty(ref _algaeText, value);
        }

        public string TemperatureText
        {
            get => _temperatureText;
            private set => SetProperty(ref _temperatureText, value);
        }

        public string HumidityText
        {
            get => _humidityText;
            private set => SetProperty(ref _humidityText, value);
        }

        public bool HasSelectedEntity
        {
            get => _hasSelectedEntity;
            private set => SetProperty(ref _hasSelectedEntity, value);
        }

        public string EntityHeader
        {
            get => _entityHeader;
            private set => SetProperty(ref _entityHeader, value);
        }

        public string EntityName
        {
            get => _entityName;
            private set => SetProperty(ref _entityName, value);
        }

        public IBrush EntityNameBrush
        {
            get => _entityNameBrush;
            private set => SetProperty(ref _entityNameBrush, value);
        }

        public string EntityAgeText
        {
            get => _entityAgeText;
            private set => SetProperty(ref _entityAgeText, value);
        }

        public string EntityHungerText
        {
            get => _entityHungerText;
            private set => SetProperty(ref _entityHungerText, value);
        }

        public double EntityHunger
        {
            get => _entityHunger;
            private set => SetProperty(ref _entityHunger, value);
        }

        public string EntityThirstText
        {
            get => _entityThirstText;
            private set => SetProperty(ref _entityThirstText, value);
        }

        public double EntityThirst
        {
            get => _entityThirst;
            private set => SetProperty(ref _entityThirst, value);
        }

        public string EntityMetabolismText
        {
            get => _entityMetabolismText;
            private set => SetProperty(ref _entityMetabolismText, value);
        }

        public string EntityIsChildText
        {
            get => _entityIsChildText;
            private set => SetProperty(ref _entityIsChildText, value);
        }

        public string EntityStateText
        {
            get => _entityStateText;
            private set => SetProperty(ref _entityStateText, value);
        }

        #endregion

        #region Methods

        public void UpdatePerformance(TimeSpan tickDuration, TimeSpan renderDuration, int visibleCellCount)
        {
            PerformanceText = $"Tick: {tickDuration.TotalMilliseconds:0.0} ms  Render: {renderDuration.TotalMilliseconds:0.0} ms  Cells: {visibleCellCount}";
        }

        public void SelectCell(int x, int y)
        {
            _selectedCell = Simulation.Cells[x, y];

            if (_selectedEntity != null)
            {
                _selectedEntity.IsSelected = false;
            }

            _selectedEntity = Simulation.Entities.At(x, y);
            if (_selectedEntity != null)
            {
                _selectedEntity.IsSelected = true;
            }

            RefreshDetails();
            WorldUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
        }

        public async Task SaveSimulationAsync(string path)
        {
            Pause();
            await SimulationStateFile.SaveAsync(path, Simulation.CaptureState("cavern"));
        }

        public async Task LoadSimulationAsync(string path)
        {
            Pause();
            SimulationSaveState state = await SimulationStateFile.LoadAsync(path);
            Simulation = WorldSimulation.FromSavedState(state);
            Simulation.IsRunning = false;
            _selectedCell = null;
            _selectedEntity = null;
            TimeText = FormatTime(Simulation.WorldTime);
            RefreshDetails();
            SimulationReplaced?.Invoke(this, EventArgs.Empty);
            WorldUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void Run()
        {
            if (IsRunning)
            {
                return;
            }

            Simulation.IsRunning = true;
            IsRunning = true;
        }

        private void Pause()
        {
            Simulation.IsRunning = false;
            Simulation.IsStepping = false;
            _isStepping = false;
            IsRunning = false;
        }

        private void Step()
        {
            if (IsRunning)
            {
                return;
            }

            _isStepping = true;
            Simulation.IsStepping = true;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (!IsRunning && !_isStepping)
            {
                return;
            }

            Simulation.Tick();
            _isStepping = false;
            Simulation.IsStepping = false;

            TimeText = FormatTime(Simulation.WorldTime);
            RefreshDetails();
            WorldUpdated?.Invoke(this, EventArgs.Empty);
        }

        private static string FormatTime(TimeSpan time)
        {
            var days = (int)time.TotalDays;
            return $"Time: {days} days, {time.Hours} hours, {time.Minutes} minutes";
        }

        private void RefreshDetails()
        {
            var cell = _selectedCell;
            if (cell == null)
            {
                CellHeader = "Cell Details";
                AlgaeText = "Algae: 0";
                TemperatureText = "Temperature: 0C";
                HumidityText = "Humidity: 0%";
            }
            else
            {
                var algae = (cell as FloorWorldCell)?.AlgaeLevel ?? 0;
                CellHeader = $"Cell Details ({(int)cell.Position.X}, {(int)cell.Position.Y})";
                AlgaeText = $"Algae: {algae}";
                TemperatureText = $"Temperature: {(int)cell.Temperature}C";
                HumidityText = $"Humidity: {(int)(cell.Humidity * 100)}%";
            }

            var entity = _selectedEntity;
            HasSelectedEntity = entity != null;
            if (entity == null)
            {
                EntityHeader = "Entity Details";
                EntityName = "Name:";
                EntityNameBrush = FemaleBrush;
                EntityAgeText = "Age:";
                EntityHungerText = "Hunger: 0%";
                EntityHunger = 0;
                EntityThirstText = "Thirst: 0%";
                EntityThirst = 0;
                EntityMetabolismText = "Metabolism:";
                EntityIsChildText = "Is child? no";
                EntityStateText = "State:";
            }
            else
            {
                var displayName = entity.GetType().GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                    ?? entity.GetType().Name;
                var genderIcon = (entity.Gender == Gender.Male) ? "\u2642" : "\u2640";

                EntityHeader = $"Entity Details ({(int)entity.Position.X}, {(int)entity.Position.Y})";
                EntityName = $"Name: {displayName} ({genderIcon})";
                EntityNameBrush = (entity.Gender == Gender.Male) ? MaleBrush : FemaleBrush;
                EntityAgeText = $"Age: {(int)entity.Age.TotalDays}";
                EntityHungerText = $"Hunger: {(int)(entity.Hunger * 100)}%";
                EntityHunger = entity.Hunger;
                EntityThirstText = $"Thirst: {(int)(entity.Thirst * 100)}%";
                EntityThirst = entity.Thirst;
                EntityMetabolismText = $"Metabolism: {entity.Metabolism}";
                EntityIsChildText = $"Is child? {(entity.IsChild ? "yes" : "no")}";
                EntityStateText = $"State: {entity.ActiveBehavior?.ToString() ?? "None"}";
            }
        }

        private void RaiseCommandStates()
        {
            RunCommand.RaiseCanExecuteChanged();
            PauseCommand.RaiseCanExecuteChanged();
            StepCommand.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
