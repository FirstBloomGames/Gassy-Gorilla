# Gassy Gorilla Two-Mode Story Implementation Plan

**Status:** In progress

**Date:** 2026-07-17

**Design authority:** `docs/superpowers/specs/2026-07-17-gassy-gorilla-two-mode-story-contract.md`

## 1. Framework Foundation

- Add an explicit reusable run mode and session selection API.
- Add reusable key-based integer progression persistence.
- Extend the shared game-state enum with a completed state.
- Keep all selected-run state independent from score and scene names.

## 2. Expedition Content Model

- Add a Gassy Gorilla Expedition ScriptableObject with identity, story, objective, fixed chunk route, finish copy, and star thresholds.
- Add a catalog asset containing all five Expeditions in unlock order.
- Add a game-specific progress wrapper for unlock and best-star persistence.
- Generate and validate all assets through the existing project builder.

## 3. Runtime Objective Flow

- Configure the existing chunk director for either procedural Endless generation or a finite authored sequence.
- Track food collection, vine releases, crocodile dodges, finish reach, and final fuel without changing one-touch movement.
- Add a physical textured 3D finish gate and trigger.
- Add distinct Expedition success, objective-missed, and ordinary failure results.
- Save completion, best stars, and the next unlocked Expedition.

## 4. Menu and In-Run UX

- Replace the single Play action with Endless Run and Expeditions.
- Add a responsive five-level Expedition select panel with lock state, objective, story, and best result.
- Add a skippable pre-run story card, compact objective HUD, remaining-distance readout, and post-level story result.
- Preserve Settings access, immediate retry, main-menu return, and existing portrait rotation protection.

## 5. Authored Chapter

1. The Dinner Bell: reach the banyan finish.
2. The Bean Trail: collect the required food before finishing.
3. Canopy Shortcut: complete the required vine releases.
4. Crocodile Crossing: survive two authored ambushes.
5. Home Before Dessert: finish with the required fuel reserve.

Each route reuses validated optimized chunks and intentionally alternates pressure with recovery.

## 6. Quality Gates

- Builder regeneration and zero-error Unity compilation.
- Scene validator checks catalog order, five unique levels, objective feasibility, finish gate wiring, menu flow, HUD, success UI, and progress isolation.
- Endless regression simulation remains unchanged.
- Automated Expedition content simulation confirms objective opportunities and route length.
- Local WebGL checks: Endless failure/retry/menu plus all five Expedition select/start/success/failure/next-level paths.
- Desktop, phone landscape, and portrait rotation checks.
- Public immutable-payload deployment and warning/error-free browser verification.
- Game Bible and release evidence updated only after the public build passes.

