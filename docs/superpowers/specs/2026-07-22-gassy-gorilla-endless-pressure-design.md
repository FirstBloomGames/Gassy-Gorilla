# Gassy Gorilla Endless Pressure Design

**Status:** Approved for implementation

**Date:** 2026-07-22

**Supersedes:** The post-550 m plateau in the 2026-07-16 difficulty design

## Purpose

Endless mode must keep asking more of a skilled player for as long as the run continues. The opening remains generous and every Expedition keeps its authored food, hazards, and teaching order. Only the procedural Endless route receives this continuous pressure system.

## Player Contract

1. The first 70 m remains a comfortable introduction.
2. From 90 m onward, optional food slots thin gradually instead of disappearing at a stage boundary.
3. From 400 m onward, obstacle and late-gauntlet weighting continues to rise on a smooth asymptotic curve. It never reaches a flat gameplay plateau.
4. Speed increases gently toward a hard readability cap; boost force, vine magnetism, input timing, and fuel cost do not become less comfortable.
5. Remaining food is valuable. Normal chunks may eventually contain no food, but low-fuel rescue and recovery chunks always retain at least one reachable pickup.
6. No hazard appears immediately after another hazard. Predator cooldown and forced recovery remain intact.
7. Runs stay deterministic for a given seed, including which optional pickup slots are retained.

## Tuning Targets

| Distance | Food-slot retention | Hazard pressure | Speed target |
| --- | ---: | --- | ---: |
| 0-90 m | 100% | Teaching layouts | 1.00-1.03x |
| 400 m | about 62% | Existing Legend mix | about 1.11x |
| 800 m | about 49% | Gauntlets become meaningful | about 1.15x |
| 1,600 m | about 39% | Strong hazard preference | about 1.18x |
| 3,000 m | approaches 34% floor | Maximum readable pressure | approaches 1.20x cap |

Fractional pickup retention uses seeded stochastic rounding per chunk so the long-run economy matches the curve without a visibly rigid pattern.

## Late-Run Content

Two stage-four-only gauntlets join the main pool:

- `DoubleThornGauntlet`: two clearly spaced stump jumps with one optional bean line.
- `GeyserThornGauntlet`: a mud geyser followed by a readable stump decision with one optional banana reward.

Both use a new `Gauntlet` tag. Their selection multiplier rises continuously after Legend while ordinary hazard weighting also rises more gently. They remain single authored chunks, so the existing no-consecutive-hazard rule still provides a recovery beat between demanding layouts.

## Fairness And Fuel

- Low fuel begins at 30% and ends above 45%, unchanged.
- A fuel/recovery opportunity is still forced within two generated chunks.
- During active low-fuel rescue, a selected Fuel or Recovery chunk keeps at least one pickup regardless of normal scarcity.
- Recovery chunks always keep at least one pickup.
- Pickup refill values remain unchanged; scarcity comes from fewer opportunities, keeping each collected food item satisfying.
- Expeditions and their finish-line objectives bypass pickup thinning entirely.

## Architecture

`RunDifficultyProfile` owns inspector-friendly continuous tuning values and pure evaluators for late pressure, pickup retention, continuous tag weight, and post-Legend speed. `RunChunkDirector` applies those values only to non-finite Endless generation, restores every pooled pickup before reuse, then deterministically disables optional food for the current chunk.

Validation simulates representative distances rather than only the five stage midpoints. It must prove that food availability decreases, hazard/gauntlet pressure increases, speed rises without exceeding 1.20x, low-fuel recovery remains available, and all existing transition rules remain valid.

## Acceptance Gates

1. Pickup retention is strictly lower at 800, 1,600, and 3,000 m than at each preceding checkpoint, and never below its configured floor.
2. Hazard candidate weight and gauntlet candidate weight strictly increase beyond 400 m.
3. Speed at 1,600 m is greater than at 800 m and never exceeds 1.20x.
4. Opening chunks and finite Expeditions activate every authored pickup.
5. A low-fuel Endless recovery chunk retains at least one pickup at every tested distance.
6. The same seed produces the same chunk and pickup-retention sequence.
7. Full scene validation, PlayMode verification, Release WebGL build, and browser playthrough pass without regressions.
