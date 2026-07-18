# Gassy Gorilla Two-Mode Story Contract

**Status:** Complete - released and publicly verified July 18, 2026

**Date:** 2026-07-17

**Project:** Gassy Gorilla / First Bloom Arcade Framework

## 1. Release Requirement

Gassy Gorilla Version 1.0 contains two clearly separated game modes:

1. **Endless Run** is the existing procedural score-chasing game.
2. **Expeditions** are finite authored levels with objectives, finish lines, story progression, and persistent completion results.

The current polished Endless Run is not the complete Version 1.0 product by itself. Expeditions and their first story chapter remain a release requirement.

## 2. Endless Run

Endless Run preserves the game already built:

- Procedural authored-chunk flow with no finish line.
- Distance and best distance as the primary score.
- Gradually increasing difficulty.
- Unlimited play until failure.
- Crocodile ambushes, vines, pickups, milestones, and recovery rules.
- Immediate retry and a direct return to the main menu.

Expedition objectives, story gates, and level completion must not alter Endless Run spawning, scoring, records, or difficulty balance.

## 3. Expeditions

Each Expedition is a finite authored route with:

- A title and short pre-run story setup.
- One primary objective stated before play.
- A visible remaining-distance or objective-progress display.
- A physical, readable finish line in the 3D world.
- A clear success sequence distinct from game over.
- A short post-level story beat.
- Persistent completion, best result, and earned medal or star.
- Retry, next level, and level-select actions.

Failure restarts the current Expedition. Success unlocks the next eligible level. The player may return to Endless Run at any time.

## 4. First Story Chapter

The first chapter follows Gassy Gorilla's sincere attempt to get home for dinner through an increasingly unruly jungle.

1. **The Dinner Bell**
   Reach the old banyan gate. This is a comfortable movement introduction with a safe first vine.
2. **The Bean Trail**
   Reach the finish while collecting the required beans. Teaches route reading and fuel planning.
3. **Canopy Shortcut**
   Complete a required number of vine releases before the finish. Teaches held swings and release timing.
4. **Crocodile Crossing**
   Cross the lagoon and survive authored crocodile warnings. Teaches the boost-to-dodge response.
5. **Home Before Dessert**
   Reach the home clearing with a minimum amount of fuel. Combines boosts, pickups, vines, and predators in a short finale.

Story presentation remains brief and playable. Use menu staging, short text, environmental landmarks, existing voice support, and in-engine camera moments. Do not stop active gameplay for exposition.

## 5. Mode Flow

The main menu presents two equally legible choices:

- **Endless Run**
- **Expeditions**

Expeditions open a level-select screen showing unlock state, objective, completion result, and best medal. Starting either mode creates an explicit run configuration so game rules never depend on scene-name guesses or leftover state from a previous run.

Reusable mode selection, finite-run completion, objective tracking, level unlocks, and result persistence belong in `Assets/_FirstBloom/ArcadeFramework` when game-agnostic. Gassy Gorilla owns level content, story copy, objective tuning, landmarks, jokes, and rewards.

## 6. Acceptance Gates

The two-mode release is complete only when:

- Endless Run still passes its current movement, vine, crocodile, difficulty, audio, and performance tests.
- All five Expeditions can be selected, completed, failed, retried, and resumed after relaunch.
- Every Expedition has a finish line and exactly one clearly communicated primary objective.
- Success cannot be confused with failure.
- Level unlocks and medals persist independently from best Endless distance.
- Story panels are readable on landscape phones and skippable without breaking progression.
- Completing one mode never leaks score, objective, spawn, or result state into the other.
- The public WebGL build passes the full menu-to-mode-to-result flow with zero warning or error logs.

## 7. Acceptance Result

All acceptance gates passed. Endless Run remains intact, all five story Expeditions support their complete select-to-result flows, progression persists independently, and the final desktop and mobile WebGL checks reported no warnings or errors.

Detailed implementation and release evidence is recorded in `docs/superpowers/plans/2026-07-17-gassy-gorilla-two-mode-story-implementation.md`.

