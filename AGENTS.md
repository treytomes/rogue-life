# AGENTS.md

Dungeon Life is a single-project .NET 10 desktop simulation/game built on Avalonia 12.1.4. There are no tests, lint/format config, or CI; `dotnet build` is the only automated check.

## Build and run

- Build: `dotnet build DungeonLife.sln`
- Run: `dotnet run --project DungeonLife`
- It is a GUI app (`WinExe`) with no headless mode. Verify changes with a build and a short manual run.
- Do not add `DOTNET_ROLL_FORWARD`; the project targets the installed `net10.0` runtime.

## Simulation architecture

- `DungeonLife/Program.cs` builds the Avalonia app; `DungeonLife/App.axaml.cs` creates `Views/MainWindow`.
- `ViewModels/MainWindowViewModel.cs` owns the `WorldSimulation` and a 100 ms `DispatcherTimer`:
  - On each timer tick, call `WorldSimulation.Tick()` when running or stepping.
  - `Tick()` advances `WorldTime` by one minute, advances `AnimationTime`/`TickCount`, then calls `Update` on every cell and entity via `Parallel.ForEach` (65,536 cells).
  - Put world logic in `WorldCell.Update`, `Entity.Update`, or behaviors. Avoid unsynchronized shared mutable state; use `ThreadSafeRandom.Instance`, never `new Random()`.
- `Simulation/WorldSimulation.cs` implements `IWorldState`, the seam shared by cells, entities, behaviors, and generators. World data lives in `WorldCellCollection` and `EntityCollection`.
- `WorldGenerator` exposes `BuildWorld` + `PlaceEntities`; `CavernGenerator` is active (chosen in `MainWindowViewModel`), `SpaciousCavernGenerator` is the alternate.
- Cells (`DungeonLife/Cells/`) model terrain/environment: `FloorWorldCell` (algae/temperature/humidity), `WaterSourceCell`, `WallWorldCell`, and `BorderWorldCell` (day/night heat source).
- Entities run an ordered, first-wins behavior list built in `OinkEntity` through `BehaviorFactory`; behaviors live in `Entity Behaviors/`.
- World/screen/temperature constants are in `LifeAppSettings` (256x256 world). Some generator code still hardcodes 256 (e.g. `CavernGenerator.PlaceEntities`); use `Cells.Width`/`Cells.Height` in new code.

## UI and rendering

- `Views/WorldView.cs` is a custom Avalonia `Control` that reads model state and renders visible cells/entities. It owns input, water animation, and selected-entity blinking; simulation classes must not draw.
- `Views/MainWindow.axaml` contains the toolbar, `ScrollViewer` + `WorldView`, and fixed-width `DetailsPanel`. XAML files use `.axaml` and `x:DataType` for compiled bindings.
- Use `Prism.Core` (`BindableBase`, `DelegateCommand`) for MVVM; do not hand-roll property-change or command helpers.
- `Rendering/RgbColor.cs` is the UI-free color type; convert to `Avalonia.Media.Color` only inside the renderer.

## Conventions and gotchas

- No SadConsole, SadRogue, MonoGame, Avalonia, or Prism types in `Simulation/`.
- World positions use `System.Numerics.Vector2`; UI/grid coordinates use integers or Avalonia `Point`. Don't mix them.
- `WorldCellCollection`'s indexer returns `null` for out-of-bounds coordinates instead of throwing; missing cells are treated as movement-blocking by `IsMovementBlocked`.
- `EntityCollection` is a linear list and entity positions are floats; lookups are distance-based, not grid-indexed.
- Match each file's existing spacing: most use spaces and `#region` blocks, while `CavernGenerator.cs` uses tabs.
