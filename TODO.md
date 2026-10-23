# Dungeon Life: .NET 10 + Avalonia Migration Plan

## Goal

Migrate Dungeon Life from .NET 5.0 and SadConsole/MonoGame to .NET 10.0 and Avalonia 12.1.4 while preserving the existing ASCII/tile presentation and simulation behavior.

## Locked Decisions

- Go directly to .NET 10 + Avalonia; do not keep SadConsole or MonoGame.
- Preserve the current ASCII/tile look, colors, panning, and details panel.
- Keep a single project for now, with clear `Simulation`, `Rendering`, and `UI`/`Views` boundaries.
- Delete the unused `ComputeSharp-1.4.1/` source tree and `ComputeSharp-1.4.1.zip`.
- Do not add gameplay features or refactor unrelated simulation code during this migration.
- Do not enable nullable reference types as part of this migration.
- Do not split out a separate `DungeonLife.Core` project unless a later need appears.

## Current State

- Target framework: `net10.0`.
- UI stack: Avalonia 12.1.4.
- No SadConsole, SadRogue, MonoGame, or ComputeSharp references remain in the project.
- The unused `ComputeSharp-1.4.1/` source tree and `ComputeSharp-1.4.1.zip` have been deleted.
- Installed SDKs include `10.0.204`; plain `dotnet run --project DungeonLife` works without `DOTNET_ROLL_FORWARD`.
- The app is a GUI `WinExe`; there is no headless mode.
- No tests, lint config, or CI exist. `dotnet build DungeonLife.sln` is the only automated check.
- The world is 256x256 cells; the default viewport is 200x60 character cells at 8x16 px per cell.
- The simulation is driven by a 100 ms `DispatcherTimer` in `MainWindowViewModel`, advancing one world minute per tick.
- Cells and entities are updated with `Parallel.ForEach`; use `ThreadSafeRandom.Instance`, never `new Random()`.
- `CavernGenerator` is active; `SpaciousCavernGenerator` remains the alternate.
- `AGENTS.md` and `README.md` have been updated for .NET 10 + Avalonia.
- Phases 1-4 are implemented and the project builds. Manual UI verification is still pending.

## Target Architecture

```text
DungeonLife/
  Program.cs                    Avalonia entry point
  App.axaml / App.axaml.cs      Application setup and Fluent theme
  Views/
    MainWindow.axaml / .cs      toolbar + world viewport + details panel
    WorldView.cs                custom rendering and input control
    DetailsPanel.axaml / .cs    selected cell/entity details
  ViewModels/
    MainWindowViewModel.cs      simulation ownership, timer, button state
  Rendering/
    RgbColor.cs                 UI-free color type
    GlyphMap.cs                 CP437 code to Unicode mapping
    WorldRenderer.cs            rendering helpers/caches (if needed)
  Simulation/
    WorldSimulation.cs          replaces WorldConsole
    IWorldState.cs
    WorldCellCollection.cs
    EntityCollection.cs
    Entity.cs
    OinkEntity.cs
    Cells/
    Entity Behaviors/
    World Building/
```

Rules:

- No SadConsole, SadRogue, MonoGame, or Avalonia types in `Simulation/`.
- World positions remain `System.Numerics.Vector2`.
- UI/grid positions use integers or Avalonia `Point`; do not mix them.
- Keep `WorldCellCollection` returning `null` out of bounds.
- Keep entity positions floating point and entity lookup distance-based.
- Keep all world mutation on the UI thread initially.

## Phase 1: Project and Package Swap

> Phase 1 intentionally leaves the branch non-building. The SadConsole package is removed before the old SadConsole UI and `Color` usages are ported in Phases 2-3.

- [x] Create a migration branch, for example `migrate/net10-avalonia`.
- [x] Confirm the current build and record existing warnings (6 warnings: 2x `NETSDK1138`, 4x `MSB3245`).
- [x] Update `DungeonLife/DungeonLife.csproj`:
  - [x] Change `TargetFramework` from `net5.0` to `net10.0`.
  - [x] Keep `<OutputType>WinExe</OutputType>`.
  - [x] Keep `<RootNamespace>DungeonLife</RootNamespace>`.
  - [x] Remove `PackageReference Include="SadConsole.Host.MonoGame" Version="9.1.1"`.
  - [x] Remove all four `ComputeSharp` `<Reference>` entries.
  - [x] Add `Avalonia` `12.1.4`.
  - [x] Add `Avalonia.Desktop` `12.1.4`.
  - [x] Add `Avalonia.Themes.Fluent` `12.1.4`.
  - [x] Add `AvaloniaUI.DiagnosticsSupport` `2.2.3` for Debug builds. Avalonia 12 removed the old `Avalonia.Diagnostics` package.
  - [x] Add `Prism.Core` when the view model needs MVVM infrastructure (commands, property-change notifications); do not hand-roll `RelayCommand`/`INotifyPropertyChanged` helpers.
- [x] Use `Prism.Core` for MVVM needs if required, keeping the app consistent with the other Avalonia projects.
- [x] Remove the need for `DOTNET_ROLL_FORWARD`; plain `dotnet run --project DungeonLife` must work after migration.

## Phase 2: Decouple the Simulation from SadConsole

### Colors

- [x] Add `Rendering/RgbColor.cs` with:
  - [x] Byte fields `R`, `G`, `B`, and optional `A`.
  - [x] A float-based factory that clamps values to 0..1.
  - [x] `Lerp` with the same behavior as `SadRogue.Primitives.Color.Lerp`.
  - [x] The named colors used by the app.
- [x] Copy the exact ARGB values for these colors from `TheSadRogue.Primitives 1.0.0` before removing the package:
  - [x] `Color.Black`
  - [x] `Color.White`
  - [x] `Color.Blue`
  - [x] `Color.Red`
  - [x] `Color.Pink`
  - [x] `Color.AnsiWhite`
  - [x] `Color.AnsiWhiteBright`
  - [x] `Color.AnsiBlackBright`
  - [x] `Color.AnsiBlue`
- [x] Replace `SadRogue.Primitives.Color` in:
  - [x] `Cells/WorldCell.cs`
  - [x] `Cells/FloorWorldCell.cs`
  - [x] `Cells/WallWorldCell.cs`
  - [x] `Cells/WaterSourceCell.cs`
  - [x] `Entity.cs`
  - [x] `OinkEntity.cs`
  - [x] The Avalonia details panel replacement
- [x] Preserve the `FloorWorldCell` algae color: `RgbColor.FromFloat(0, AlgaeLevel, 0)`.
- [x] Preserve temperature/humidity background blending in `WorldCell.BackgroundColor`.
- [x] Convert `RgbColor` to `Avalonia.Media.Color` only inside the renderer.

### Remove draw/render methods from the simulation

- [x] Delete `WorldCell.Draw`.
- [x] Delete `WaterSourceCell.Draw`.
- [x] Delete `Entity.Render`.
- [x] Remove all `using SadConsole;` and `using SadRogue.Primitives;` statements from simulation files.
- [x] Keep visual state on the model:
  - [x] `WorldCell.Glyph`
  - [x] `WorldCell.ForegroundColor`
  - [x] `WorldCell.BackgroundColor`
  - [x] `Entity.Glyph`
  - [x] `Entity.ForegroundColor`
  - [x] `Entity.IsSelected`
- [x] Move water glyph animation into the Avalonia renderer:
  - [x] Keep the base glyph `~` in `WaterSourceCell`.
  - [x] Toggle between `~` and CP437 `247` (`≈`) every 500 ms.
  - [x] Freeze the animation when the simulation is paused.
- [x] Move selected-entity blinking into the Avalonia renderer:
  - [x] Keep `Entity.IsSelected`.
  - [x] Use the same red lerp/overlay concept as the old `Entity.Render`.
  - [x] Freeze the animation when paused.

### Add `WorldSimulation`

- [x] Replace `WorldConsole.cs` with `Simulation/WorldSimulation.cs`.
- [x] Make `WorldSimulation` implement `IWorldState`.
- [x] Own:
  - [x] `WorldCellCollection Cells`
  - [x] `EntityCollection Entities`
  - [x] `TimeSpan WorldTime`
  - [x] `bool IsRunning` (initially `true`)
  - [x] `bool IsStepping` (initially `false`)
  - [x] A paused-aware animation/tick counter (`AnimationTime` and `TickCount`)
- [x] Constructor:
  - [x] Create the 256x256 cell collection.
  - [x] Create the entity collection.
  - [x] Call `generator.Generate(this)`.
- [x] Add `Tick()`:
  - [x] Advance `WorldTime` by one minute.
  - [x] Increment the tick/animation counter.
  - [x] Run `Parallel.ForEach(Cells, cell => cell.Update(this))`.
  - [x] Run `Parallel.ForEach(Entities, entity => entity.Update(this, TimeSpan.FromMinutes(1)))`.
  - [x] Do no drawing.
- [x] Preserve `IsMovementBlocked(float, float)` and `IsMovementBlocked(Vector2)`.
- [x] Remove `MS_PER_FRAME`, `_totalElapsedTime`, `_lastUpdateTime`, `_sharedSurface`, and viewer/input fields.

### Delete unused SadConsole files

- [x] Delete `DemoProgram.cs`.
- [x] Delete `ScrollableConsoleDemo.cs`.
- [x] Delete `ScrollableViewDemo.cs`.
- [x] Delete `CellInfoWindow.cs`.
- [x] Replace or delete `CellSelectedEventArgs.cs`.

## Phase 3: Avalonia Application Shell

### Entry point and application

- [x] Replace `Program.cs` with an Avalonia entry point:

```csharp
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace();

public static void Main(string[] args)
    => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
```

- [x] Add `App.axaml` with the Fluent theme.
- [x] Add `App.axaml.cs` that creates `MainWindow` on desktop startup.
- [x] Set the window title from `LifeAppSettings.WindowTitle`.

### Main window layout

- [x] Add `Views/MainWindow.axaml` / `.cs`.
- [x] Use a top toolbar (`DockPanel.Dock="Top"`) with:
  - [x] Run button.
  - [x] Pause button.
  - [x] Step button.
  - [x] Time label.
- [x] Use a two-column body:
  - [x] Left: `ScrollViewer` containing `WorldView`.
  - [x] Right: fixed-width details panel, initially about 280 px.
- [x] Initialize the window size from the viewport + details panel + toolbar.
- [x] Keep the details panel visible at all times.

### View model and timer

- [x] Add `MainWindowViewModel`.
- [x] If MVVM helpers are needed, use `Prism.Core` (`BindableBase`, `DelegateCommand`, etc.) instead of custom helpers.
- [x] Construct `WorldSimulation` with `CavernGenerator`; keep the `SpaciousCavernGenerator` line commented as today.
- [x] Own a `DispatcherTimer` with a `100ms` interval.
- [x] On each timer tick:
  - [x] If `IsRunning` or `IsStepping`, call `WorldSimulation.Tick()`.
  - [x] Reset `IsStepping` after one tick.
  - [x] Refresh time text and details.
  - [x] Invalidate/refresh the world view.
- [x] Button state:
  - [x] Initial: running, Run disabled, Pause enabled, Step disabled.
  - [x] Run: set running, disable Run, enable Pause, disable Step.
  - [x] Pause: stop running, enable Run, disable Pause, enable Step.
  - [x] Step: only while paused; advance exactly one tick.
- [x] Time format must match the old label: `Time: {days} days, {hours} hours, {minutes} minutes`.

### Details panel

- [x] Add `Views/DetailsPanel.axaml` / `.cs`.
- [x] Preserve the current cell fields:
  - [x] Cell header with `(x, y)`.
  - [x] Algae.
  - [x] Temperature.
  - [x] Humidity.
- [x] Preserve the current entity fields:
  - [x] Entity header with `(x, y)`.
  - [x] Name and gender icon.
  - [x] Age.
  - [x] Hunger and progress bar.
  - [x] Thirst and progress bar.
  - [x] Metabolism.
  - [x] Is child.
  - [x] Active state/behavior.
- [x] Use `ProgressBar.Maximum = 1` and bind values in the 0..1 range.
- [x] Fix the old bug at `DetailsPanel.cs:142`: hunger must update the hunger progress bar, not the thirst bar.
- [x] Keep the gender color behavior: blue for male, pink for female.
- [x] Handle a missing `DisplayNameAttribute` without throwing.

### Selection

- [x] `WorldView` raises a cell-selected event with world coordinates.
- [x] On selection:
  - [x] Store the selected cell.
  - [x] Look up `world.Entities.At(worldX, worldY)`.
  - [x] Clear `IsSelected` on the previously selected entity.
  - [x] Set `IsSelected` on the new entity.
  - [x] Refresh the details panel.
  - [x] Invalidate the world view.

## Phase 4: World Rendering and Input Parity

### `WorldView`

- [x] Add `Views/WorldView.cs` deriving from Avalonia `Control`.
- [x] Expose:
  - [x] `WorldSimulation Simulation`.
  - [x] `double CellWidth` (initially 8).
  - [x] `double CellHeight` (initially 16).
  - [x] Visible world range or viewport offset.
  - [x] Cell-selected event.
- [x] Set the control size to `WorldWidth * CellWidth` by `WorldHeight * CellHeight`.
- [x] Place the control inside a `ScrollViewer` with scrollbars enabled.
- [x] On scroll, update the visible range from `ScrollViewer.Offset` and invalidate.
- [x] In `Render`:
  - [x] Clear/draw the background.
  - [x] Compute the visible cell range from the scroll offset and viewport size.
  - [x] Draw only visible cells, with a one-cell margin.
  - [x] Skip `null` cells defensively.
  - [x] Draw cell background first.
  - [x] Draw cell glyph and foreground.
  - [x] Draw entities after cells, in collection order.
  - [x] Draw the selected entity with a red background lerp driven by the animation clock.
- [x] Use a monospace font such as Cascadia Mono or Consolas.
- [x] Use cached `FormattedText`/brush objects keyed by glyph and quantized color.
- [x] If profiling shows excessive cost, replace the internals with a glyph atlas + `WriteableBitmap`, keeping the `WorldView` public API unchanged.

### Glyph mapping

- [x] Add `Rendering/GlyphMap.cs`.
- [x] Map CP437 codes to Unicode strings.
- [x] Important mappings:
  - [x] `176` → `░`
  - [x] `247` → `≈`
  - [x] `148` → `ö`
  - [x] `153` → `Ö`
  - [x] `11` → `♂`
  - [x] `12` → `♀`
- [x] Keep ASCII glyphs unchanged.
- [ ] Verify all glyphs render with the chosen font; use fallback or custom drawing if necessary.

### Rendering details

- [x] Background color comes from `WorldCell.BackgroundColor` with the existing temperature/humidity blending.
- [x] Foreground color comes from `WorldCell.ForegroundColor` / `Entity.ForegroundColor`.
- [x] Algae floor glyph/color behavior must match the old app.
- [x] Water alternates `~` and `≈` every 500 ms.
- [x] Selected entity background blinks red using a paused-aware animation clock.
- [x] When paused, water and blink animations freeze.
- [x] Clear selection when the user clicks outside the world? Preserve the old behavior: no explicit clear unless implemented intentionally.

### Input

- [x] Left click selects the cell under the pointer.
- [x] Left drag continues selecting cells as the pointer moves.
- [x] Right click toggles algae on a `FloorWorldCell` once per press.
- [x] Middle-button drag pans by adjusting `ScrollViewer.Offset`.
- [x] Capture the pointer during drags so movement outside the control still works.
- [x] F11 toggles `WindowState.FullScreen` / `WindowState.Normal`.
- [x] Preserve scrollbar-based panning.

## Phase 5: Cleanup and Documentation

- [x] Delete `ComputeSharp-1.4.1/`.
- [x] Delete `ComputeSharp-1.4.1.zip`.
- [x] Delete the old `WorldConsole.cs`, `DetailsPanel.cs`, and SadConsole-specific UI files.
- [x] Confirm no source file references:
  - [x] `SadConsole`
  - [x] `SadRogue`
  - [x] `MonoGame`
  - [x] `ComputeSharp`
- [x] Update `README.md`:
  - [x] Toolchain: .NET 10 + Avalonia.
  - [x] Controls: left click/drag select, right click algae, middle drag pan, F11 fullscreen.
  - [x] Remove the ComputeSharp and NAudio claims.
  - [x] Keep the simulation/environment documentation.
- [x] Update `AGENTS.md`:
  - [x] Build command remains `dotnet build DungeonLife.sln`.
  - [x] Run command becomes `dotnet run --project DungeonLife`; remove the roll-forward requirement.
  - [x] Document the Avalonia UI and `WorldSimulation` boundary.
  - [x] Document that rendering reads model state and does not live in simulation classes.
  - [x] Remove references to deleted demo files and expected ComputeSharp warnings.
- [ ] Optionally add `global.json` pinned to SDK `10.0.204` with `rollForward: latestFeature`.
- [x] Review `git status` and ensure only intended files changed/deleted.

## LifeAppSettings Changes

- [x] Keep `WorldWidth = 256` and `WorldHeight = 256`.
- [x] Keep the temperature constants and `IdealTemperature`.
- [x] Add rendering/viewport settings or replace the old screen constants:
  - [x] `GlyphWidth = 8`
  - [x] `GlyphHeight = 16`
  - [x] `DefaultViewportWidthInCells = 200`
  - [x] `DefaultViewportHeightInCells = 60`
  - [x] `DetailsPanelWidth = 280`
- [x] Compute the initial window size from the viewport and details panel.
- [x] Keep `WindowTitle = "Dungeon Life"`.

## Verification

### Build

```powershell
dotnet build DungeonLife.sln
```

Expected:

- No `MSB3245` ComputeSharp warnings.
- No .NET 5 end-of-support warning.
- No new compiler errors.

### Run

```powershell
dotnet run --project DungeonLife
```

Manual checklist:

- [x] Window opens and the cavern renders.
- [x] Run starts the simulation.
- [x] Pause stops the simulation.
- [x] Step advances exactly one tick while paused.
- [x] Time advances one minute per tick and displays the old format.
- [x] Scrollbars work.
- [x] Middle-drag panning works.
- [x] Left click selects a cell and updates details.
- [x] Left drag selects continuously.
- [x] Right click toggles algae on floor cells.
- [x] Selected entity details update correctly.
- [x] Hunger and thirst progress bars show the correct values.
- [x] Water animation works and freezes while paused.
- [x] Selected entity blink works and freezes while paused.
- [x] F11 toggles fullscreen.
- [x] No exceptions after several minutes of simulation.
- [ ] Tick/render performance is acceptable at 10 Hz.
- [ ] Window resizing and scrolling do not produce visual corruption.

### Optional performance check

- [ ] Measure `WorldSimulation.Tick()` duration.
- [ ] Measure `WorldView.OnRender` duration and visible cell count.
- [ ] If rendering is too slow, implement the glyph-atlas/`WriteableBitmap` renderer before merging.

## Risks and Mitigations

- **Rendering cost:** Thousands of glyph draws at 10 Hz may be slow. Mitigate with cached text/brushes, visible-range-only rendering, and a glyph atlas if needed.
- **Font coverage:** `░`, `≈`, `♂`, and `♀` may not exist in every font. Verify and use fallback/custom symbols.
- **Behavior drift:** Mouse selection, panning, algae toggling, and animation timing can regress. Use the manual checklist above.
- **Temporary non-building branch:** The direct rewrite removes the old UI before the new UI is complete. Keep `main` green and merge only after verification.
- **Animation smoothness:** Blinking at 10 Hz may look less smooth than the old render loop. Add a lightweight invalidation timer only if needed after functional parity.
- **Threading:** Keep simulation on the UI thread at first. If ticks cause jank, move `Tick()` to a background task with an overlap guard and marshal UI updates back to the dispatcher.

## Out of Scope

- New gameplay or artificial-life behavior.
- Z-levels, entity sizes, reproduction, or audio.
- Re-enabling ComputeSharp or adding GPU compute.
- Nullable-reference-type cleanup.
- Splitting into `DungeonLife.Core` and a separate app project.
- CI, test framework, or lint/format configuration.
- Redesigning the UI away from the ASCII/tile presentation.
