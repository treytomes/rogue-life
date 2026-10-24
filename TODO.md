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

- [ ] Replace the process-wide `ThreadSafeRandom` usage with an injected,
  seedable random source owned by a simulation run. Record the seed with results.
- [ ] Make random streams independent of thread scheduling. Prefer stable streams
  keyed by run seed plus cell/entity identity and tick, or use a deterministic
  single-threaded update until an equivalent parallel scheme is established.
- [ ] Give entities stable IDs and define stable iteration and conflict-resolution
  order for movement, resource use, births, and deaths.
- [ ] Make each cell update read a consistent prior-state snapshot and write to a
  next-state buffer. Commit the buffer at a defined tick boundary.
- [ ] Make entity decisions from a defined snapshot, then resolve their intents
  (movement, eating, drinking, reproduction) deterministically. Avoid concurrent
  mutation of neighboring cells and entities.
- [ ] Add a replay check: the same model version, seed, parameters, and tick count
  must produce the same state hash, including when rendering is disabled.
- [ ] Add a saved-state format with model version, seed/RNG state, tick number,
  world clock, cell state, and entity state; verify save/load continuation against
  an uninterrupted run.

## Phase 2: Validate existing rules

- [ ] Fix neighborhood averages at world edges. `AverageOver` currently divides
  by the full square area even when `IterateOver` clips the sample to the map.
- [ ] Implement and validate the specified cell-neighborhood geometry and edge
  normalization: square in-bounds cell neighborhoods including the center,
  circular Euclidean entity sensing, and actual in-bounds sample counts.
- [ ] Review and document the algae growth/death equation, including its
  temperature response, humidity scaling, spontaneous generation probability,
  and aging. Add boundary cases for minimum/maximum inputs and equilibrium
  behavior.
- [ ] Review the heat and humidity update equations as a coupled system. Define
  expected sources, sinks, diffusion rates, and stable ranges; verify that update
  order does not bias the result.
- [ ] Correct and validate flocking rules. `EntityCohesionBehavior` increments
  its count while summing positions and steers away from its computed center.
- [ ] Handle coincident entities and zero movement vectors without producing
  invalid floating-point positions or directions.
- [ ] Validate implementation of movement using normalized direction and the
  agreed terrain multipliers: 1 cell per tick on floor and 0.25 in water.
- [ ] Validate target selection for food and water against the agreed scoring
  formula, sensing geometry, and tie-break in `MODEL_SPEC.md`.
- [ ] Define behavior priority and idle behavior. Ensure the displayed active
  state is cleared when no behavior acts and dead entities stop running living
  behaviors.
- [ ] Review time-dependent rules for boundary/discontinuity errors, including
  the border heat cycle and lifespan checks.

## Phase 3: Complete organism and population dynamics

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
- [ ] Add a headless runner that accepts a scenario, model version, seed,
  parameters, and tick count, then emits a machine-readable result.
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

- [ ] Cell and entity updates currently run in parallel while reading and
  mutating shared state, so outcomes can depend on scheduling.
- [ ] `ThreadSafeRandom` is safe for concurrent access but is not seeded or
  replayable, and its thread-local streams associate draws with scheduler threads.
- [ ] Entity reproduction is unfinished and not enabled; current populations
  cannot replace deaths.
- [ ] The cohesion rule's implementation contradicts its intended name.
- [ ] Edge-region averages use a denominator larger than the number of sampled
  cells.
- [ ] UI cadence currently advances world time by one minute per timer tick;
  experiments need an explicit, independently controlled tick duration.
