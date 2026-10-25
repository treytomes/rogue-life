# TODO

## Goal

Make DungeonLife a platform for experiments in artificial life. A run should have
specified rules, be reproducible from its inputs, and expose enough state to
explain why it changed.

## Phase 0: Specify the experiment

- [x] Write a short model specification for the current world: cell types and
  state, neighborhood definitions, update equations, organism needs and actions,
  movement, births, deaths, and boundary conditions.
- [x] Define one simulated minute per model tick; UI cadence controls only
  wall-clock pacing. Record animation-time behavior in `MODEL_SPEC.md`.
- [x] Advance world time before the cell phase; use the advanced timestamp for
  time-dependent cell rules.
- [x] Decide cell updates are synchronous: all cells read the prior state and
  commit together. Record this in `MODEL_SPEC.md`.
- [x] Calculate next humidity from the cell snapshot, then use that value for
  same-tick humidity-sensitive rules before committing the full cell state.
- [x] Organisms decide from a shared snapshot of organism state and the cell
  state after the synchronous cell phase commits.
- [x] Organism actions resolve before the tick's age and metabolic need
  increments.
- [x] Define organism updates as shared-snapshot decisions followed by
  deterministic intent resolution; record the rule in `MODEL_SPEC.md`.
- [x] Organisms read the committed cell state after the synchronous cell phase.
- [x] Resolve actions before age and metabolic needs advance.
- [x] Rotate intent-resolution priority by tick over stable organism IDs.
- [x] Define shared algae consumption: rotating priority, bounded by remaining
  algae, with a partial final request.
- [x] Allow organism overlap; multiple organisms may choose the same
  destination. Terrain remains movement-blocking.
- [x] When terrain or the world boundary blocks movement, keep the organism in
  place and reverse its direction.
- [x] Reset border-cell humidity to zero each tick, matching ordinary walls.
- [x] Set border temperature by a precise 24-hour sinusoid: 0°C at midnight,
  50°C at midday, and 0°C at the next midnight.
- [x] Specify no births in version 1; defer reproduction to a later ruleset.
- [x] Keep probabilistic old-age mortality: after age 12 days, check once daily
  with probability `min(1, ageInDays / 12 - 1)`.
- [x] Specify old age as the only mortality rule in version 1; hunger and thirst
  do not directly kill organisms.
- [x] Use in-bounds square cell neighborhoods including the center; keep
  generator smoothing as a separate eight-neighbor rule.
- [x] Use circular Euclidean range for organism social sensing and cell-target
  sensing.
- [x] Select targets using both distance and resource amount.
- [x] Score targets as `amount / (1 + EuclideanDistance²)` with ascending X/Y
  coordinates as deterministic tie-break.
- [x] Define finite-state invariants for algae, humidity, hunger, thirst,
  temperature, and organism positions/directions. The model allows total
  resources to be created and dissipated through explicit sources and sinks.
- [x] Choose immutable integer rule versions, with parameters recorded per run;
  define the run-manifest fields in `MODEL_SPEC.md`.

## Phase 1: Make runs reproducible

- [x] Extract simulation rules, world state, entities, cells, behaviors, and
  generators into a platform-neutral `DungeonLife.Core` project. Keep Avalonia,
  Prism, rendering, and desktop concerns out of Core; have the UI reference Core.
- [x] Add Generic Host startup to the Avalonia app and use Microsoft DI for
  composition. Keep host setup in the executable shell rather than Core.
- [x] Add YAML `appsettings.yaml` configuration for simulation defaults and
  runtime settings, with command-line overrides for run-specific values such as
  seed, configuration path, and log level.  The `appsettings.yaml` file should be stored and referenced from the repository root.
- [x] Add Microsoft.Extensions.Logging backed by Serilog, including console and
  rolling file output, and log the resolved run configuration and seed.
- [x] Add a console harness that references `DungeonLife.Core`, builds its own
  Generic Host, and can run without Avalonia or a desktop session. Share
  configuration and logging conventions with the UI while keeping host builders
  in their respective executable projects.
- [x] Replace the process-wide `ThreadSafeRandom` usage with an injected,
  seedable random source owned by a simulation run. Record the seed with results.  Review and copy the `~\projects\learning\src\WormForaging.Core\Rng\Pcg32Random.cs` class along with it's interface.
- [x] Make random streams independent of thread scheduling. Prefer stable streams
  keyed by run seed plus cell/entity identity and tick, or use a deterministic
  single-threaded update until an equivalent parallel scheme is established.
- [x] Give entities stable IDs and define stable iteration and conflict-resolution
  order for movement, resource use, births, and deaths.
- [x] Make each cell update read a consistent prior-state snapshot and write to a
  next-state buffer. Commit the buffer at a defined tick boundary.
- [x] Make entity decisions from a defined snapshot, then resolve their intents
  (movement, eating, drinking, reproduction) deterministically. Avoid concurrent
  mutation of neighboring cells and entities.
- [x] Add a replay check: the same model version, seed, parameters, and tick count
  must produce the same state hash, including when rendering is disabled.
- [x] Add a saved-state format with model version, seed/RNG state, tick number,
  world clock, cell state, and entity state; verify save/load continuation against
  an uninterrupted run.

## Phase 2: Validate existing rules

- [x] Configure every `.csproj` to require documentation, and to treat warnings as errors, then build and fix the errors you find.
- [x] Fix neighborhood averages at world edges. `AverageOver` now divides by
  the number of cells yielded by the in-bounds clipped neighborhood.
- [x] Implement and validate the specified cell-neighborhood geometry and edge
  normalization: square in-bounds cell neighborhoods including the center,
  circular Euclidean entity sensing and cell-target sensing, and actual
  in-bounds sample counts.
- [x] Review and document the algae growth/death equation, including its
  temperature response, humidity scaling, spontaneous generation probability,
  and aging. Added boundary cases for minimum/maximum inputs and equilibrium
  behavior to `MODEL_SPEC.md`, including the smooth bounded temperature response.
- [x] Review the heat and humidity update equations as a coupled system. Define
  expected sources, sinks, diffusion rates, and stable ranges; verify that update
  order does not bias the result. See the coupled-system audit in `MODEL_SPEC.md`.
- [x] Correct and validate flocking rules. Cohesion now averages its snapshot
  positions correctly and steers toward the center with a normalized direction;
  the rule and its behavior priority are documented in `MODEL_SPEC.md`.
- [x] Handle coincident entities and zero movement vectors without producing
  invalid floating-point positions or directions. Zero/cancelling vectors now
  decline safely, overlapping neighbors are skipped for separation, and
  direction assignment rejects non-finite components.
- [x] Validate implementation of movement using normalized direction and the
  agreed terrain multipliers: 1 cell per tick on floor and 0.25 in water.
  The current-cell timing, stationary zero direction, and blocked-destination
  reversal are recorded in `MODEL_SPEC.md`.
- [x] Validate target selection for food and water against the agreed scoring
  formula, sensing geometry, and tie-break in `MODEL_SPEC.md`. Candidates now
  require positive resource, use `amount / (1 + distanceSquared)` within the
  circular range, and break equal scores by ascending X/Y.
- [x] Define behavior priority and idle behavior. The displayed active state
  clears when no behavior acts or when a dead entity is resolved; dead entities
  do not run living behaviors. Priority and momentum-preserving idle behavior
  are documented in `MODEL_SPEC.md`.
- [x] Review time-dependent rules for boundary/discontinuity errors, including
  the border heat cycle and lifespan checks. Initial border heat now matches
  world time zero, and mortality probabilities 0 and 1 are exact; boundary
  timing is documented in `MODEL_SPEC.md`.
- [x] Review the identified v1 model findings and record the agreed corrections
  in `MODEL_SPEC.md`: bounded partial algae consumption, a smooth bounded algae
  temperature response, and exact convergence to ideal temperature.

## Phase 3: Complete organism and population dynamics

- [ ] Review feasibility of using NRules for managing organism behavior.
- [ ] Implement reproduction in a later rules version. Specify mating,
  eligibility, gestation, cooldown, offspring placement, and population limits
  before enabling the existing unfinished `ReproductionBehavior`.
- [ ] Define hunger and thirst as resource flows: intake, metabolism, starvation
  thresholds, and the effect of consuming algae or water on the environment.
- [ ] Decide which organism traits are fixed and which vary or inherit, and define
  how traits affect survival and reproduction before adding learning or neural
  behavior.
- [ ] Add explicit handling for extinction, overcrowding, and failed offspring
  placement so population outcomes are measurable rather than accidental.

## Phase 4: Build an experiment harness

- [ ] Add automated simulation-level tests, independent of Avalonia, for rule
  invariants, edge neighborhoods, movement, lifecycle transitions, and seeded
  replay. Keep GUI rendering out of these checks.
- [ ] Create small deterministic scenarios (for example, isolated algae patch,
  water beside dry floor, organism beside food, and blocked corridor) with
  expected state transitions.
- [ ] Add configurable experiment parameters and world dimensions; remove
  hard-coded 256-wide placement ranges from generators.
- [ ] Extend the basic headless runner to accept scenarios, model version,
  parameters, and tick count, then emit a machine-readable result.
- [ ] Record time-series metrics: algae biomass and occupied cells, temperature
  and humidity distributions, organism counts by age/state, births, deaths,
  resource intake, and extinction time.
- [ ] Add optional periodic snapshots and a compact run manifest containing code
  revision, model version, seed, parameters, tick count, and output hashes.
- [ ] Compare repeated runs and parameter sweeps with summary statistics; clearly
  distinguish deterministic replay from intentionally stochastic ensembles.

## Phase 5: Keep the UI an observer

- [ ] Keep rendering, selection, animation, and display formatting outside the
  simulation rules; the same experiment should run without creating a window.
- [ ] Display or export the run seed, model version, tick number, and experiment
  parameters alongside performance information.
- [ ] Provide a way to pause, single-step, and inspect before/after state without
  changing simulation outcomes.
- [ ] Separate simulation speed from world-time progression so rendering load or
  timer jitter cannot silently change an experiment.

## Current review findings to resolve

- [ ] Future runtime-created entities need component RNG streams keyed from their
  stable IDs, as their IDs are assigned when added to the collection.
- [x] Bound eating by available algae while preserving partial consumption;
  hunger reduction is proportional to the amount consumed.
- [x] Replace the unbounded algae temperature response with a smooth bounded
  quadratic response, with value 1 at ideal and 0.2 at either temperature bound.
- [x] Clamp the environmental temperature adjustment to ideal when within
  0.001 degrees, preventing overshoot and oscillation.
- [ ] Entity reproduction remains unfinished and disabled in v1; population
  replacement, eligibility, and mating rules belong to a later version.
- [ ] UI cadence currently advances world time by one minute per timer tick;
  experiments need an explicit, independently controlled tick duration.








