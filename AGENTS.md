# AGENTS.md

Dungeon Life is a .NET 10 solution with an Avalonia 12.1.4 desktop app and a platform-neutral simulation Core project. There are no tests, lint/format config, or CI; `dotnet build` is the only automated check.

## Build and run

- Build: `dotnet build DungeonLife.sln`
- Run: `dotnet run --project src/DungeonLife`
- It is a GUI app (`WinExe`) with no headless mode. Verify changes with a build and a short manual run.
- Do not add `DOTNET_ROLL_FORWARD`; the project targets the installed `net10.0` runtime.

## Simulation architecture

- `src/DungeonLife/Program.cs` builds the Avalonia app; `src/DungeonLife/App.axaml.cs` creates `Views/MainWindow`.
- `src/DungeonLife.Core/` contains the platform-neutral simulation model, cells, entities, behaviors, and world generators. Keep Avalonia and Prism types out of Core.
- `src/DungeonLife/Hosting/AppHostBuilder.cs` is the UI composition root. The Generic Host owns DI registrations; keep hosting and container dependencies out of Core.
- The repository-root `appsettings.yaml` is found by walking up from the executable directory; `--config`, `--seed`, and `--log-level` are parsed at startup. The UI tick interval is runtime-configurable, while each model tick remains one simulated minute.
- Microsoft.Extensions.Logging is routed through Serilog. Keep the console sink and daily rolling file sink configured by `Serilog` in the root YAML; relative `LogDirectory` paths resolve beside the active configuration file.
- `src/DungeonLife/ViewModels/MainWindowViewModel.cs` owns the `WorldSimulation` and a 100 ms `DispatcherTimer`:
  - On each timer tick, call `WorldSimulation.Tick()` when running or stepping.
  - `Tick()` advances `WorldTime` by one minute, advances `AnimationTime`/`TickCount`, then calls `Update` on every cell and entity via `Parallel.ForEach` (65,536 cells).
  - Put world logic in `WorldCell.Update`, `Entity.Update`, or behaviors. Avoid unsynchronized shared mutable state; use `ThreadSafeRandom.Instance`, never `new Random()`.
- `src/DungeonLife.Core/Simulation/WorldSimulation.cs` implements `IWorldState`, the seam shared by cells, entities, behaviors, and generators. World data lives in `WorldCellCollection` and `EntityCollection`.
- `WorldGenerator` exposes `BuildWorld` + `PlaceEntities`; `CavernGenerator` is active (chosen in `MainWindowViewModel`), `SpaciousCavernGenerator` is the alternate. Both live in `src/DungeonLife.Core/World Building/`.
- Cells (`src/DungeonLife.Core/Cells/`) model terrain/environment: `FloorWorldCell` (algae/temperature/humidity), `WaterSourceCell`, `WallWorldCell`, and `BorderWorldCell` (day/night heat source).
- Entities run an ordered, first-wins behavior list built in `OinkEntity` through `BehaviorFactory`; behaviors live in `src/DungeonLife.Core/Entity Behaviors/`.
- World dimensions and temperature constants are in Core's `WorldSettings`; screen dimensions are in the UI's `LifeAppSettings`. Some generator code still hardcodes 256 (e.g. `CavernGenerator.PlaceEntities`); use `Cells.Width`/`Cells.Height` in new code.

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
