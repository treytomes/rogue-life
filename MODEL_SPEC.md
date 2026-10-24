# DungeonLife model specification

**Status:** working draft. This documents the current implementation and records
the model decisions agreed so far. Observed implementation details are not
automatically intended rules. Open implementation details and later-version
rules remain marked inline and in `TODO.md`.

## Experiment contract

- **Purpose:** use DungeonLife to run interpretable artificial-life experiments.
- **Rules version:** immutable integer. Version 1 will be assigned only after
  the model is specified and known defects are corrected. Increment it whenever
  a state-transition rule, update order, boundary rule, or random-choice
  semantics changes. The current behavior is documented as a legacy observation,
  not the version 1 reference model.
- **Random seed:** not currently accepted or recorded by a run.
- **Determinism:** required for a fixed rules version, seed, parameters, and
  tick count.
- **State invariants:** **approved.** Algae, humidity, hunger, and thirst are
  finite and bounded by [0, 1]; temperature is finite and bounded by [0, 50]
  degrees; organism position is finite, in-world, and traversable after movement
  resolution; direction is finite, with zero allowed for stationary organisms.
- **Cell update semantics:** **synchronous (agreed).** Every cell reads the same
  pre-tick state, computes its next state, and commits only after all cell
  calculations finish. Neighbor reads must not observe partial updates from the
  current tick.
- **Within-cell humidity dependency:** **use calculated next humidity for other
  same-tick floor rules (agreed).** First derive all cells' humidity outputs
  from the shared pre-cell snapshot, including terrain-specific humidity rules.
  Then calculate algae and temperature using the old snapshot plus each cell's
  derived humidity. Commit all fields together.
- **Model time step:** **one simulated minute per tick (agreed).** The UI controls
  only wall-clock pacing. Pausing or rendering slowly must not change the amount
  of simulated time represented by one tick.
- **Cell forcing timestamp:** **use the advanced tick timestamp (agreed).**
  Advance world time by one minute before the cell phase; time-dependent cell
  rules evaluate at that advanced timestamp.
- **Organism update semantics:** **snapshot, then resolve intents
  deterministically (agreed).** Organisms decide from a shared snapshot rather
  than observing other organisms' in-progress actions. Resource conflicts use
  rotating priority; organisms may overlap.
- **Entity sensing geometry:** current social sensing uses circular Euclidean
  distance, while cell-target searches use square scans. **For v1, both Oink
  social sensing and cell-target sensing use circular Euclidean range.** Cell
  automaton neighborhoods remain square. **Target choice combines resource
  amount and distance (agreed):** score is `amount / (1 + EuclideanDistance²)`.
  Algae amount is `AlgaeLevel`, water amount is 1, and the thirsty fallback uses
  cell humidity. Candidates must have a positive amount and be within smell
  range. Equal scores are broken by ascending cell X, then Y.
- **Cell-to-organism order:** **organisms see the committed cell state for the
  current tick (agreed).** Cells compute from their pre-cell snapshot and commit
  together; then organisms take a shared snapshot and decide their intents from
  that committed cell state. No organism decision sees another organism's
  in-progress intent.
- **Need and age timing:** **decide and resolve actions first, then accrue age
  and metabolic needs (agreed).** Decisions use the start-of-organism-phase
  values; action effects such as eating or drinking apply before this tick's
  metabolism and age increment.
- **Intent priority:** **rotate by tick over stable organism IDs (agreed).**
  Each tick begins with organisms sorted by stable ID, then rotates the starting
  priority by the tick index. This avoids permanently favoring the same ID while
  preserving deterministic resolution.
- **Organism overlap:** **allowed (agreed).** Organisms do not block movement
  into positions occupied by other organisms; same-destination intents may
  succeed together. Terrain still blocks movement.
- **Blocked movement:** **reverse direction (agreed).** If the resolved
  destination is outside the world or on movement-blocking terrain, the organism
  remains at its prior position and reverses direction. Other organisms do not
  block movement.
- **Movement distance:** **keep terrain multipliers (agreed).** Use a normalized
  direction each tick: floor moves one cell per tick and water moves one-quarter
  cell per tick. Movement is applied using the multiplier of the cell occupied
  at the start of the movement step.
- **Border humidity:** **reset to zero each tick (agreed),** matching ordinary
  walls. Its generated initial humidity does not persist into simulation ticks.
- **Border temperature:** **use a precise 24-hour sinusoid (agreed),** from 0°C
  at midnight to 50°C at midday and back to 0°C at the next midnight. The phase
  is based on time of day, not elapsed-hour arithmetic with an approximate period.
- **Reproduction:** **no births in version 1 (agreed).** Reproduction is deferred
  to a later rules version; the active population is not replenished.
- **Old-age mortality:** **keep the probabilistic current rule (agreed).** No
  old-age death occurs through age 12 days. Thereafter, make one daily check
  with death probability `min(1, ageInDays / 12 - 1)`.
- **Need-related mortality:** **none in version 1 (agreed).** Hunger and thirst
  remain bounded needs that drive behavior; neither directly kills an Oink.
- **Shared algae:** **consume in rotating priority until depleted (agreed).**
  Eating intents are applied in priority order and cannot consume more algae
  than remains. A final partial request receives only the remaining amount;
  later requests see the updated remainder.
- **Neighborhood map edges:** **sample in-bounds cells only (agreed).** A
  neighborhood average divides by the number of cells actually sampled; cells
  beyond the finite world are excluded rather than treated as zero.
- **Cell neighborhood geometry:** **axis-aligned square, center included
  (agreed).** The square is clipped to the finite world. Current cell rules use
  radius 1 for algae sums and temperature averages, and radius 2 for humidity
  averages. Generator smoothing is separate and counts the eight adjacent cells
  while excluding the center.
- **First ruleset:** **specify and correct before version 1 (agreed).** The
  current implementation is a legacy observation, not version 1.
- **Rendering:** observational only; it must not affect simulation state or
  random-number consumption.

### Proposed run manifest

Record these values with every experiment result:

- immutable rules version and exact parameter values;
- source revision, world-generator identity/version, world dimensions, and
  model time step;
- random seed and PRNG algorithm/version;
- requested and completed tick counts, plus initial and final state hashes.

This is the minimum proposed manifest. The headless-runner phase will define its
machine-readable format and ensure every field can be reproduced or validated.

## World and clocks as currently implemented

- The active world is 256 by 256 cells. `CavernGenerator` is active; a spacious
  generator exists as an alternate.
- `WorldSimulation.Tick()` advances `WorldTime` by one minute and
  `AnimationTime` by 100 milliseconds. The Avalonia timer currently requests a
  tick every 100 milliseconds while running. At that nominal pace the world
  advances ten simulated minutes per wall-clock second; this is pacing, not the
  model's time step.
- A tick currently updates every cell, then every entity. Both passes use
  `Parallel.ForEach`; this is not yet deterministic or synchronous in practice.
- `TickCount` starts at zero and increments once per call. There is no persisted
  state or rules version.

## Agreed tick schedule

1. Advance `TickCount` and `WorldTime` by one tick. Time-dependent cell forcing
   uses this advanced timestamp.
2. Capture the cell state at the start of the cell phase.
3. Derive each cell's next humidity from that same snapshot. Apply the water
   humidity source/sink and ordinary wall humidity reset as part of this
   derivation. Border humidity behavior remains open for v1 review.
4. Compute next algae and temperature from the old snapshot, using the cell's
   derived humidity for humidity-sensitive rules. Temperature averages read old
   neighbor temperatures. Commit all cell fields together.
5. Capture organism state and let every organism decide from that same organism
   snapshot and the newly committed cell state.
6. Resolve organism intents deterministically in rotating stable-ID priority.
   Organisms may overlap; terrain blocks movement. Shared algae requests consume
   only what remains, including a partial final request.
7. Apply one tick of age and metabolic need to each living organism after action
   effects.

Organism decisions use the start-of-phase state. After action resolution, living
organisms accrue one tick of age and metabolic hunger/thirst. Action effects
such as eating or drinking are applied before that increment. This preserves the
current behavior-before-cost order while making it explicit for v1.

## World construction as currently implemented

- The cavern starts with independent random cell types: 45% wall, 35% water,
  and 20% floor. Four smoothing passes follow.
- Smoothing scans cells in a fixed x/y order and edits the live map during the
  scan. It counts the eight surrounding cells, excluding the center. More than
  four walls produces a wall; otherwise, when there are fewer than four walls,
  more than four water neighbors produces water and fewer than four produces
  floor. Exact ties preserve the current type.
- The map border is then replaced with heat-emitting border walls. Small wall
  and floor regions are culled, and floor regions are connected.
- Each floor is initialized with random algae in [0, 1) and humidity in [0,
  0.02). Water and walls start with no algae; water starts with humidity in
  [0, 0.02). Initial floor/water/wall temperatures are set to the ideal
  temperature, 21 degrees. Border temperature is subsequently determined by
  its heat cycle.
- The generator attempts to place 50–99 Oinks, with at most ten random
  placements per requested animal. Failed placements reduce the actual initial
  population. Placement currently uses hard-coded coordinates in [0, 255].

## Cell rules as currently implemented

All cells have temperature, humidity, a movement multiplier, and a terrain
type. A missing or out-of-bounds cell blocks movement. Neighborhood sampling
uses an axis-aligned square that includes the center and is clipped to the world.
**Averages divide by the number of in-bounds cells sampled.** Current
`AverageOver` instead divides by the unclipped square area, so it does not yet
implement this rule and edge averages are biased low. Radius 1 is used for
temperature and algae sums; radius 2 is used for humidity.

### Shared humidity update

Before a normal cell's specialized update, humidity becomes 0.999 times the
average humidity in its radius-2 square. The average currently uses the fixed
25-cell denominator even at map edges. Specialized rules may then change
humidity.

### Floor and algae

- A floor's radius-1 algae value is the sum of `AlgaeLevel` over floor cells in
  the 3x3 square, including itself. Wall and water cells contribute zero.
- For an occupied floor (`AlgaeLevel > 0`), the proposed delta is +0.1 when the
  neighborhood sum is in [2.5, 4.5], otherwise -0.1. For an empty floor, it is
  +1.0 when the sum is in [3.5, 4.5], otherwise zero.
- The delta is multiplied by `1 - 0.8 * abs(sin(tempFactor))`, where
  `tempFactor = 2 - 1 / (2 * (temperature - idealTemperature))`; when the
  denominator is zero, the reciprocal term is set to 1 instead.
- If humidity is below 0.01, the delta is zeroed. Below 0.5, it is multiplied
  by `1 - humidity`; at or above 0.5, by `1 + humidity - 0.5`.
- After applying the delta, positive algae loses 0.01 per tick when temperature
  is above ideal by more than 0.001 degrees; otherwise it loses `0.01 *
  humidity`. Values below 0.1 become zero; values above 1 become 1.
- Finally, a floor independently gains algae with probability `0.01 * humidity`,
  at a random level in [0.5, 1). This can replace the computed value.
- Floors move at multiplier 1 and move their temperature toward the radius-1
  average by `(1 - humidity)`, then toward ideal temperature by 0.001 degrees.

### Water

- Water has movement multiplier 0.25. After shared humidity diffusion, if its
  temperature is above ideal it adds `0.1 * (temperature - ideal)` humidity;
  if below ideal it removes 0.1. Humidity is clamped to [0, 1].
- Temperature becomes two-thirds its previous value plus one-third of the
  radius-1 average, then moves 0.001 degrees toward ideal temperature.

### Walls and border

- Walls block movement. After shared humidity diffusion, wall temperature
  becomes five-sixths its previous value plus one-sixth of the radius-1 average,
  then moves 0.001 degrees toward ideal. Wall humidity is set to zero.
- Border cells override the wall temperature update and set it to
  `temperatureRange / 2 * (1 - cos(2 * pi * hourOfDay / 24))`, giving 0°C at
  midnight and 50°C at midday when the configured range is 0–50°C. For v1,
  border humidity resets to zero each tick, matching ordinary walls.

## Oink organism as currently implemented

- Oinks begin at integer cell coordinates with a random gender and direction.
  Maturity age is three simulated days; maximum lifespan is twelve simulated
  days. Metabolism is 0.0001 per tick, hunger multiplier 5, and thirst
  multiplier 1. Their perception radius is 18, with sight factor 0.3 and smell
  factor 0.5.
- Behavior order is old-age death, thirst, hunger, separation, alignment,
  wandering, cohesion. The first behavior returning true stops evaluation.
- Thirst and hunger each activate probabilistically, with probability equal to
  the current need. Thirst seeks nearby water, or the most humid cell if no
  water is found, and reduces thirst by 0.1 when drinking.
- Hunger seeks algae. Current code uses `eatSpeed = 0.1` and
  `ALGAE_FOOD_FACTOR = 0.2`: when algae exceeds `eatSpeed`, it subtracts
  `eatSpeed / ALGAE_FOOD_FACTOR` from algae and subtracts
  `eatSpeed * ALGAE_FOOD_FACTOR` from hunger. Otherwise it subtracts the
  remaining algae amount and reduces hunger by that amount times the factor.
  The first branch can drive algae below zero when the patch is between 0.1 and
  0.5; this is a known defect to correct before v1.
- Need rises by metabolism times its multiplier, bounded to [0, 1]. Age and
  needs advance by one tick's values. The organism then moves one normalized
  direction vector scaled by the current cell's movement multiplier: one cell
  per tick on floor and one-quarter cell per tick in water. A blocked destination
  leaves it in place and reverses direction; entity occupancy does not block
  movement.
- Old-age death is checked once per simulated day after age 12 days. At each
  check, death probability is `min(1, ageInDays / 12 - 1)`: it rises linearly
  from zero just beyond the lifespan to certainty at 24 days.
- Hunger and thirst do not directly cause death in v1; old age is the only
  mortality rule.
- Reproduction code exists but is not attached to Oinks and currently returns
  before its gestation logic. **Version 1 has no births;** reproduction is
  deferred to a later rules version.
- Cohesion currently divides the summed positions by an incorrectly incremented
  count and steers away from the resulting center. This is a code defect, not an
  agreed flocking rule.

## Invariants for the experiment contract

These constraints are agreed; the implementation still needs to enforce and
validate them:

- Algae, humidity, hunger, and thirst remain finite and within [0, 1].
- Temperature remains finite and within the configured [0, 50] degree range.
- Entity positions remain finite, in bounds, and on traversable cells after
  movement resolution. Movement directions are finite; zero is allowed for an
  organism that is stationary.
- Total resources are not conserved: algae has an explicit spontaneous source,
  temperature has a border source and environmental sinks, and water and
  humidity have source/sink behavior. Individual state variables remain bounded.
- Given identical rules version, seed, parameters, and tick count, the complete
  state is identical, independent of rendering and worker scheduling.
- Every state transition has a defined tick boundary and order.

## Decisions recorded for phase 0

- [x] Cell updates use synchronous snapshot-and-commit semantics.
- [x] Calculate next humidity from the shared cell snapshot, then use that
  humidity for same-tick floor rules before committing all cell fields.
- [x] Organisms decide from a shared snapshot; resolve their intents
  deterministically after all decisions are made.
- [x] Organisms decide from the committed cell state after the synchronous cell
  phase.
- [x] Resolve organism actions before adding one tick of age and metabolic
  hunger/thirst.
- [x] Rotate deterministic intent priority by tick over stable organism IDs.
- [x] Allow organisms to overlap; only terrain blocks movement. Multiple
  organisms may choose the same destination.
- [x] On a blocked move, keep the organism in place and reverse its direction;
  other organisms do not block movement.
- [x] Use normalized direction and keep terrain movement multipliers: 1 cell per
  tick on floor and 0.25 cell per tick in water.
- [x] Reset border-cell humidity to zero each tick, matching ordinary walls.
- [x] Use the precise 24-hour border temperature cycle: 0°C at midnight, 50°C
  at midday, and 0°C at the next midnight.
- [x] Specify no births in version 1; defer reproduction to a later ruleset.
- [x] Keep probabilistic old-age mortality: after age 12 days, check once daily
  with probability `min(1, ageInDays / 12 - 1)`.
- [x] Specify that hunger and thirst do not directly kill organisms in v1;
  old age is the only mortality rule.
- [x] Shared algae is consumed in rotating priority order, limited by the
  remaining amount and allowing a partial final request.
- [x] Keep one simulated minute per model tick; UI pacing controls only how
  quickly ticks are requested in wall-clock time.
- [x] Advance world time before the cell phase; time-dependent rules use the
  advanced tick timestamp.
- [x] Neighborhood averages use only in-bounds cells and divide by the actual
  sample count.
- [x] Cell neighborhoods use in-bounds squares including the center; generator
  smoothing is a separate eight-neighbor rule.
- [x] Use circular Euclidean range for both organism social sensing and
  cell-target sensing; cell neighborhoods remain square.
- [x] Choose targets using both resource amount and distance.
- [x] Score targets as `amount / (1 + EuclideanDistance²)` and break equal
  scores by ascending cell X, then Y.
- [x] Specify the model and correct known defects before assigning version 1;
  retain the current-behavior description as legacy documentation.
- [x] Use a 24-hour sinusoidal border temperature cycle: 0°C at midnight,
  50°C at midday, 0°C at the next midnight.
- [x] Set finite-state bounds for algae, humidity, hunger, thirst, temperature,
  and organism position/direction. Total resources are intentionally
  nonconserving through explicit sources and sinks.
- [x] Choose immutable integer rule versions; parameter sets are recorded per
  run. Define a manifest with source revision, generator, dimensions, time step,
  PRNG/seed, tick counts, and initial/final state hashes.
