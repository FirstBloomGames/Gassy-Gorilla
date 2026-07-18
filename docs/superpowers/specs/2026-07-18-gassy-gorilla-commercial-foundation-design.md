# Gassy Gorilla Commercial Foundation Design

**Status:** Implemented and verified

**Date:** 2026-07-18

**Scope:** Pause, accessibility, platform feedback, controller support, and lightweight achievement-ready retention

## 1. Product Goal

Close the highest-value product-quality gaps that can be completed and verified on Windows without changing Gassy Gorilla's movement, difficulty, level routes, audio mix, visual direction, or two-mode progression.

This pass should make the current game safer to take into native iOS testing and give players a small amount of durable mastery progress beyond distance and Expedition stars.

## 2. Chosen Direction

Use a commercial-foundation-first approach:

- Add a complete pause and resume flow.
- Add persistent reduced-motion and haptics settings.
- Add controller-equivalent input for the one-touch action and pause.
- Add a compact Jungle Badges system with stable identifiers suitable for later Game Center mapping.
- Keep all systems lightweight enough to preserve the current WebGL load and runtime budgets.

Additional biomes, cosmetics, store assets, signed iOS builds, Game Center authentication, analytics SDKs, and monetization remain separate follow-up work.

## 3. Framework Architecture

### Arcade Accessibility Settings

Create a reusable static settings service in `Assets/_FirstBloom/ArcadeFramework`:

- Persist `ReducedMotion` and `HapticsEnabled` with namespaced PlayerPrefs keys.
- Expose current values and a change event.
- Save immediately when a player changes a setting.
- Default reduced motion to off and haptics to on.

The existing settings panel gains inspector-friendly toggles and remains responsible only for binding UI to services.

### Motion Consumers

Framework motion systems consult the shared setting:

- Camera shake is suppressed while reduced motion is enabled.
- Dynamic camera zoom returns smoothly to its base framing.
- Decorative slow motion is skipped.
- Panel scale/fade animation becomes immediate.

Gassy Gorilla camera intros and outros use a stable framed presentation when reduced motion is enabled while preserving required gameplay and result timing.

### Haptics

Create a reusable `ArcadeHaptics` facade and a small iOS native bridge:

- WebGL and unsupported platforms safely no-op.
- iOS supports light, medium, heavy, success, and failure feedback.
- Haptics never affect game state.
- Calls obey the persistent player toggle.
- Repeated routine events use light feedback; major success or failure uses one stronger event.

### Input

Extend the existing one-touch input without changing its public behavior:

- Keyboard remains supported.
- Controller face button maps to boost or vine release.
- Escape, P, controller Start, or the on-screen pause control toggles pause.
- UI presses never leak into gameplay input.

## 4. Pause Experience

The top-right gameplay control becomes a familiar pause icon instead of a text Settings button.

Pause is allowed only during an active run:

- Physics, spawning, score progression, and active gameplay stop.
- The current run remains in memory.
- Music and ambience continue at a quieter paused level rather than cutting abruptly.
- The overlay offers Resume, Settings, Restart, and Main Menu.
- Opening Settings from pause keeps the game paused.
- Closing Settings returns to the pause overlay.
- Losing application focus automatically pauses an active run.
- Returning from focus loss never resumes without player intent.

Story setup, success, and game-over panels retain their current behavior and cannot be covered by an accidental pause state.

## 5. Jungle Badges

Add eight durable, game-specific badges:

1. **First Blast:** perform the first successful boost.
2. **Vine Time:** release from 10 vines across all runs.
3. **Bean Counter:** collect 50 food pickups across all runs.
4. **Swamp Smarts:** dodge 5 crocodile ambushes across all runs.
5. **Hundred Meter Hero:** reach 100 m in Endless Run.
6. **Jungle Legend:** reach 500 m in Endless Run.
7. **Star Collector:** earn 10 Expedition stars in total.
8. **Home for Dinner:** complete all five Expeditions.

Badge rules:

- Stable lowercase IDs are permanent.
- Progress and unlock state persist independently from Endless best distance and Expedition progression.
- Unlocks are monotonic and cannot be lost through ordinary play.
- A restrained in-run toast announces a new badge without pausing or obscuring hazards.
- A compact main-menu panel shows all badges, current progress, and total completion.
- Existing progress is reconciled at startup, so qualifying distance and Expedition completion unlock their badges retroactively.
- The data shape must be compatible with a future Game Center adapter, but no Apple authentication is added in this pass.

## 6. UX And Visual Rules

- Pause and badge surfaces follow the existing charcoal, leaf-green, lagoon-blue, and brass UI language.
- No new full-screen marketing page is introduced.
- The main menu keeps Endless Run and Expeditions as the primary actions.
- Settings and Badges remain secondary compact actions.
- Touch targets remain comfortable at 844 x 390.
- Panels must fit the 960 x 600 reference canvas without text overlap.
- UI remains screen-space 2D; all gameplay-world art remains textured 3D.

## 7. Audio And Performance Rules

- Pausing applies a reusable audio pause mix rather than muting user settings.
- Resume restores the exact authored mix.
- Badge feedback reuses the calibrated SFX path and cannot stack rapidly.
- No new texture atlas, mesh, skinned renderer, runtime reflection, or continuous particle emitter is added.
- Added scripts and UI should keep the complete served WebGL build below 16 MB.

## 8. Validation

Unity validation must reject:

- A game scene without a pause controller, pause overlay, or pause button.
- A settings panel missing either accessibility toggle.
- Multiple or missing pause-time owners.
- Missing badge definitions, duplicate IDs, incorrect thresholds, or missing badge UI.
- Reduced-motion consumers that are not wired.

Browser QA must cover:

- Pause and resume during Endless and an Expedition.
- Settings opened and closed while paused.
- Restart and Main Menu from pause.
- Automatic focus-loss pause.
- Reduced-motion persistence after reload.
- Badge unlock toast and persisted badge panel state.
- Controller-equivalent keyboard paths.
- Existing Endless and Expedition completion/failure regressions.
- Desktop, phone landscape, and portrait rotation behavior.
- Zero warning or error logs.

## 9. Native Commercial Boundary

This pass prepares the project for native testing but does not claim App Store readiness. A signed iOS build, Xcode 26 toolchain, real-device performance and thermal testing, native audio/haptics verification, Game Center configuration, privacy and accessibility metadata, store assets, and external retention evidence remain required before a paid App Store launch.
