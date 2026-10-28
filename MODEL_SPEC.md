# RogueLife model specification

**Status:** working draft. This documents the current implementation and records
the model decisions agreed so far. Observed implementation details are not
automatically intended rules. Open implementation details and later-version
rules remain marked inline and in `TODO.md`.

## Experiment contract

- **Purpose:** use RogueLife to run interpretable artificial-life experiments.
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
- **Border temperature:** **use a precise 24-hour sinusoid (agreed),** from the
  configured minimum at midnight to the maximum at midday and back to the
  minimum at the next midnight. With current limits, these are 0°C and 50°C.
  The phase is based on time of day, not an approximate elapsed-hour period.
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
   humidity source/sink and wall and border humidity resets as part of this
   derivation; both wall types commit humidity zero.
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
**Averages divide by the number of in-bounds cells sampled.** `AverageOver`
counts the cells yielded by the clipped square, so edge neighborhoods are
normalized by their actual sample count. Cell-target searches first scan a
bounding square, then discard cells outside the requested Euclidean radius.
Radius 1 is used for temperature and algae sums; radius 2 is used for humidity.

### Shared humidity update

Before a normal cell's specialized update, humidity becomes 0.999 times the
average humidity in its radius-2 square. At map edges, the average uses only
the in-bounds cells in the clipped square. Specialized rules may then change
humidity.

### Coupled heat and humidity audit

The model reads all source values from one immutable pre-cell snapshot. For an
ordinary floor, let `Havg` be the radius-2 humidity average and `Tavg` the
radius-1 temperature average. The calculated next humidity is
`Hnext = clamp(0.999 * Havg, 0, 1)`. This is a smoothing-to-neighborhood rule,
not a conventional relaxation of the form `H + rate * (Havg - H)`: the 0.999
factor is a per-tick 0.1% sink. A spatially uniform source-free humidity field
therefore decays to `0.999 * H` each tick.

Water first calculates that same `Hnext`, then applies its temperature-driven
term using the water cell's old temperature: add `0.1 * (T - idealTemperature)`
when `T` is above ideal, subtract 0.1 when below ideal, and add nothing at
exactly ideal. The result is clamped to [0, 1]. This is the only explicit
humidity source; the cold-water term, the 0.999 factor, clamps, and wall/border
resets are sinks. There is no humidity transport from out-of-bounds locations.

Temperature uses the radius-1 average from the old snapshot. Before the small
ideal-temperature adjustment, the diffusion equations are:

| Cell type | Temperature after diffusion | Effective neighbor rate |
| --- | --- | --- |
| Floor | `T + (1 - Hnext) * (Tavg - T)` | `1 - Hnext`, from 0 to 1; humidity slows diffusion |
| Water | `(2 * T + Tavg) / 3` | 1/3 |
| Wall | `(5 * T + Tavg) / 6` | 1/6 |
| Border | Replaced by the 24-hour sinusoidal forcing | External time-dependent source |

For floors, water, and walls, the resulting temperature then moves by at most
0.001 degrees toward the 21°C ideal. If the remaining difference is at most
0.001 degrees, temperature is set exactly to ideal rather than crossing it.
This shared rule gives exact convergence without a small alternating offset.
The border forcing is
`minimumTemperature + temperatureRange / 2 * (1 - cos(phase))`, an external
heat source bounded by the configured minimum and maximum temperatures.

Under the v1 initial bounds `temperature ∈ [0, 50]` and `humidity ∈ [0, 1]`,
humidity remains in range because each normal/water calculation clamps and
walls/borders reset to zero. Temperature diffusion is a convex blend of the
cell and an in-bounds average, followed by an inward ideal-temperature step;
the border forcing is also in [0, 50]. These rules therefore preserve those
ranges without an additional temperature clamp, assuming valid initial or
loaded state. A uniform temperature field has no diffusion flux; only the
ideal-temperature adjustment or border forcing changes it.

The implementation avoids cell-order bias: `WorldSimulation.Tick` advances
the clock, captures one cell snapshot, calculates all `WorldCellNextState`
values from it in parallel, and commits only after every calculation finishes.
Humidity-sensitive floor algae and floor heat use that cell's calculated
`Hnext`; all neighbor averages still read old snapshot values. Water's humidity
source likewise uses its old temperature. Thus no cell sees a neighbor's
partially updated heat or humidity during the same tick.

### Floor and algae

- A floor's radius-1 algae value is the sum of `AlgaeLevel` over floor cells in
  the 3x3 square, including itself. Wall and water cells contribute zero.
- For an occupied floor (`AlgaeLevel > 0`), the initial delta is +0.1 when the
  neighborhood sum is in the inclusive interval [2.5, 4.5], otherwise -0.1.
  For an empty floor, it is +1.0 when the sum is in [3.5, 4.5], otherwise
  zero.
- The delta is multiplied by a smooth bounded temperature response. Define
  `d = clamp((idealTemperature - T) / (idealTemperature - minimumTemperature), 0, 1)`
  below ideal, and
  `d = clamp((T - idealTemperature) / (maximumTemperature - idealTemperature), 0, 1)`
  at or above ideal. Then `F(T) = 1 - 0.8 * d^2`. At ideal, `F = 1`; at either
  configured temperature bound, `F = 0.2`. The curve is continuous and has
  zero slope at ideal. If a configured span is zero, its normalized distance
  uses zero, avoiding division by zero.
- Humidity response is piecewise: below 0.01 the delta becomes zero; from 0.01
  (inclusive) to below 0.5 it is multiplied by `1 - humidity`; from 0.5
  (inclusive) through 1 it is multiplied by `1 + humidity - 0.5`.
- After applying the resulting delta, if algae is positive it loses 0.01 per
  tick when temperature is greater than ideal by more than 0.001 degrees;
  otherwise it loses `0.01 * humidity`. The result is clamped by mapping values
  below 0.1 to zero and values above 1 to 1. Exact values 0.1 and 1 are retained.
- Finally, a floor independently gains algae with probability `0.01 * humidity`,
  at a random level in [0.5, 1). This spontaneous-generation event replaces
  the computed value, including any result of the clamp.
- Boundary cases for the current equation:

  | Input | Boundary behavior |
  | --- | --- |
  | Neighborhood sum 2.5 or 4.5, occupied | Uses the +0.1 branch; immediately outside uses -0.1. |
  | Neighborhood sum 3.5 or 4.5, empty | Uses the +1.0 branch; immediately outside uses 0. |
  | Humidity 0 | Delta and spontaneous generation are zero; at or below ideal + 0.001, algae aging is also zero. |
  | Humidity just below 0.01 / exactly 0.01 | Delta is suppressed below the threshold and scaled by 0.99 at the threshold. |
  | Humidity 0.5 / 1 | Wet scaling is 1 / 1.5; spontaneous-generation chance is 0.5% / 1%. |
  | Algae result 0.099 / 0.1 / 1 / 1.001 | Clamps to 0 / remains 0.1 / remains 1 / clamps to 1. |
  | Temperature 0 / exactly ideal / 50 | Response is 0.2 / 1 / 0.2 for the current [0, 50] bounds and 21°C ideal. |
- Deterministic local equilibrium occurs when the scaled delta equals the aging
  loss and the resulting algae value remains in [0.1, 1], or when upper/lower
  clamping holds the value fixed. In particular, at humidity 0 and temperature
  at or below ideal + 0.001, valid algae values in [0.1, 1] remain unchanged;
  values below 0.1 are still mapped to zero by the lower threshold. Spontaneous
  generation can move a local state away from that deterministic equilibrium
  whenever humidity is positive.
- Floors move at multiplier 1 and move their temperature toward the radius-1
  average by `(1 - nextHumidity)`, then toward ideal by at most 0.001 degrees,
  stopping exactly at ideal when the remaining difference is smaller.

### Water

- Water has movement multiplier 0.25. After shared humidity diffusion, if its
  temperature is above ideal it adds `0.1 * (temperature - ideal)` humidity;
  if below ideal it removes 0.1. Humidity is clamped to [0, 1].
- Temperature becomes two-thirds its previous value plus one-third of the
  radius-1 average, then moves at most 0.001 degrees toward ideal, stopping
  exactly at ideal.

### Walls and border

- Walls block movement. After shared humidity diffusion, wall temperature
  becomes five-sixths its previous value plus one-sixth of the radius-1 average,
  then moves at most 0.001 degrees toward ideal, stopping exactly at ideal.
  Wall humidity is set to zero.
- Border cells override the wall temperature update and set it to
  `minimumTemperature + temperatureRange / 2 * (1 - cos(2 * pi * minuteOfDay / 1440))`,
  giving the configured minimum at midnight, maximum at midday, and minimum at
  the next midnight. The initial border temperature is set to the minimum so
  the pre-first-tick state matches world time zero. The cycle is smooth at both
  extrema; the advanced world timestamp is used each tick. For v1, border
  humidity resets to zero each tick, matching ordinary walls.

## Oink organism as currently implemented

- Oinks begin at integer cell coordinates with a random gender and direction.
  Maturity age is three simulated days; maximum lifespan is twelve simulated
  days. Metabolism is 0.0001 per tick, hunger multiplier 5, and thirst
  multiplier 1. Their perception radius is 18, with sight factor 0.3 and smell
  factor 0.5.
- Behavior order is old-age death, thirst, hunger, separation, alignment,
  wandering, cohesion. The first behavior returning true stops evaluation.
- Behavior priority is first-wins in that listed order; lower-priority rules
  are not evaluated after one acts. If every behavior declines, the organism is
  idle for decision display: `ActiveBehavior` is cleared, but its direction is
  retained and it continues normal age, need, and movement updates. Idle does
  not mean stationary unless the retained direction is zero.
- A dead organism is excluded from living behavior evaluation. Its death
  behavior may be displayed on the tick it acts; subsequent dead resolutions
  clear `ActiveBehavior` and do not advance age, needs, or movement.
- Thirst and hunger each activate probabilistically, with probability equal to
  the current need. Thirst seeks nearby water, or the most humid cell if no
  water is found, and reduces thirst by 0.1 when drinking.
- Hunger seeks algae. An eating intent requests `eatSpeed / ALGAE_FOOD_FACTOR`
  algae, which is 0.5 with the v1 values `eatSpeed = 0.1` and
  `ALGAE_FOOD_FACTOR = 0.2`. Resolution consumes the lesser of that request and
  the floor's available algae, then reduces hunger by `consumedAlgae * factor`.
  A partial patch therefore provides proportionally partial food, and algae
  never becomes negative. Competing eating intents resolve in rotating stable
  ID priority, each observing the remaining algae after earlier intents.
- Need rises by metabolism times its multiplier, bounded to [0, 1]. Age and
  needs advance by one tick's values. The organism then moves by
  `normalizedDirection * currentCellSpeed`, where speed comes from the cell it
  occupies at the start of movement: 1 cell per tick on floor, one-quarter cell
  per tick in water, and zero on walls. Thus an organism entering water from
  floor uses the floor distance for that tick and the water distance beginning
  next tick. A zero direction leaves it in place. If the destination is blocked
  by terrain or the world boundary, position is unchanged and direction is
  reversed; entity occupancy does not block movement. Nonzero direction
  assignments are normalized, and non-finite direction components are rejected.
- Old-age mortality checks use the organism age at the start of the decision
  phase and occur no more than once per simulated day. The age-12-day check has
  probability zero; the first possible death is at the age-13-day check, with
  probability 1/12. Probability then rises by 1/12 per daily check and is
  clamped to 1 at age 24 days. The check uses `random < probability`, so zero
  probability never kills and certainty always kills. Since age increments
  after decisions, reaching an age boundary during a tick is observed by the
  mortality behavior on the following tick.
- Hunger and thirst do not directly cause death in v1; old age is the only
  mortality rule.
- Reproduction code exists but is not attached to Oinks and currently returns
  before its gestation logic. **Version 1 has no births;** reproduction is
  deferred to a later rules version.
- Cohesion uses the circular sight-range snapshot, including the organism
  itself, to calculate the arithmetic mean position. If the center differs
  from the organism's position, it blends its current direction equally with
  the normalized direction toward that center, then normalizes the result. If
  the center is coincident or the blend cancels to zero, it does not act.
  Because cohesion follows wandering in the first-wins behavior list, it acts
  only when all earlier behaviors, including wandering, decline.
- Organisms may overlap. A coincident organism has no defined separation
  direction, so separation skips that neighbor and checks the remaining
  neighbors. If all neighbors coincide, separation declines. Flocking,
  wandering, and target-seeking decline when their direction vector cannot be
  normalized; they do not write NaN directions. A zero direction is allowed
  and leaves the organism stationary. Non-finite directions are rejected;
  nonzero directions assigned to an organism are normalized. If the random
  direction sampler returns its exact zero vector, it uses the fixed +X
  direction as a finite fallback.

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

## Organism behavior engine decision

The current behavior system is an ordered, first-wins decision pipeline: each
organism evaluates its behaviors against the same immutable decision snapshot,
returns at most one intent, and the simulation resolves all intents in rotating
stable-ID priority. Behaviors are ordinary Core classes with direct access to
simulation-specific context, including the run-owned random source.

NRules is a .NET inference engine based on Rete: it matches facts against rule
conditions, places activations on an agenda, and fires rule actions. That model
can suit large, compositional rule sets where matching facts against many
conditions is the primary problem. It is not a drop-in fit for this simulation's
ordered first-wins semantics. Introducing an agenda would require an explicit
adapter to preserve behavior priority, one-intent-per-organism decisions,
snapshot-only reads, deterministic random use, and the separate rotating
conflict-resolution phase. The current behavior count and per-organism workload
do not establish a need for Rete matching, so the added engine/session lifecycle
and determinism surface are not justified yet.

Decision: do not add NRules to Core at this stage. Keep behaviors as explicit
ordered strategies. Revisit if experiments require user-authored/composable
rules or profiling shows condition matching is a bottleneck; any evaluation
must demonstrate replay equivalence and preserve the decision/intent-resolution
boundary before adoption.

## Version 2 reproduction rules

Version 1 remains birth-free. The rules below define active version 2 behavior.

- **Eligibility:** both parents must be living, mature Oinks, with hunger and
  thirst each at or below 0.5. Females that are pregnant or in post-birth
  cooldown are ineligible. Males have no cooldown, but may pair at most once per
  world day. Each female's attempt schedule remains age-relative.
- **Attempt schedule:** each female's first attempt is at maturity; later
  attempts occur every 24 simulated hours relative to that maturity time.
- **Pairing:** each eligible female chooses the nearest eligible, not-yet-paired
  male within a circular radius of 2 cells, inclusive. Equal distances break by
  ascending male ID. Females are considered in rotating stable-ID priority; if
  no eligible male remains, that female skips the attempt. A failed conception
  still counts as a pairing for that world day.
- **Conception probability:** use the female's run-owned random stream. The
  baseline probability is 10% at maturity, rises smoothly to 90% at 60% of the
  24-day lifespan (age 14.4 days), then smoothly falls to zero at age 24 days.
  Use smoothstep `S(t) = 3t^2 - 2t^3` on each segment: maturity to peak uses
  `0.1 + 0.8*S(t)`; peak to endpoint uses `0.9*(1 - S(t))`. Apply the crowding
  multiplier `1 / (1 + (nearbyOinks / 8)^2)` to this baseline.
- **Density:** count living Oinks, including both parents, in an inclusive
  circular radius of 8 cells centered on the female at the conception attempt.
  Density reduces probability but never imposes a hard population cutoff.
- **Gestation and recovery:** gestation lasts 3 simulated days. A 3-day female
  cooldown begins at birth.
- **Litter and placement:** one offspring per pregnancy. At birth, choose the
  nearest floor cell within 8 cells of the female; break equal-distance ties by
  ascending X then Y. Oinks may overlap. Newborns start at age zero with default
  Oink traits and no parental trait inheritance. Gender and initial direction
  are generated from a deterministic RNG stream derived from the run seed and
  the newborn's stable ID.
- **Failed placement:** if no floor cell is available, defer birth and retry
  once per simulated day. If the mother dies before placement succeeds, the
  pregnancy ends without a birth and is recorded as a loss. Count each deferred
  placement attempt and each pregnancy loss as separate run events.
- **Tick order:** after all entities present at tick start resolve their intents
  and complete age/need/movement updates, process maternal deaths, due births,
  then eligible conception attempts. Eligibility, partner selection, density,
  and probability use post-movement positions and post-action/post-metabolism
  needs. Birth placement uses the mother's post-movement position. Newborns do
  not decide, age, or move until the next tick. Density includes all living
  Oinks, including newborns placed earlier in that reproduction phase.
- **Saved state:** persist each entity's pregnancy due time, next birth/retry
  time, cooldown end, last mating world-day, and wandering behavior's random
  threshold. Persist run-level conception, birth, deferred placement, and
  pregnancy-loss counters, as well as daily female readiness and mate-
  availability diagnostics. Newborn IDs are the next stable collection IDs and
  their random streams are derived from run seed plus ID. Reconstruct saved
  entities in ID order so births survive load.
- **Version boundary:** this implementation uses rules version 2. A v2-only
  build rejects v1 saves; it never continues them silently under v2 behavior.

The probability curve, density rule, eligibility, timing, pairing, placement,
event counters, and save/load fields above are part of the immutable version 2
contract.
