# Gassy Gorilla Comedic Audio Remix Design

**Status:** Implemented and browser-verified

**Date:** 2026-07-17

**Project:** Gassy Gorilla / First Bloom Arcade Framework

## 1. Purpose

Replace the synthetic-feeling fart boost family, stop pickup chains from becoming harsh or disproportionately loud, and rebalance the shipping mix around a clear comedic identity. The pass must preserve the working WebGL audio output repair, settings persistence, mobile performance, and compact download size.

Expeditions and unlockable progression remain the next separate feature. They do not block this focused audio-quality release.

## 2. Current Problems

- The three pickup clips are 0.36-second, three-note sounds normalized near full scale. Rapid collections overlap through the eight-voice SFX pool and produce an overly loud chord.
- Pickup library gain is `0.82`, too close to primary action and hazard sounds for such a frequent event.
- The four boost clips are simple procedural combinations of pitch sweep, noise, drum, and bubble layers. They vary in length but do not sound like distinct comic performances.
- Existing SFX are individually normalized to similar peaks, so perceived hierarchy depends too heavily on entry gain and event density.

## 3. Sound Identity

Fart boosts are family-friendly, physical, and proudly comedic. They emphasize popping, squeaking, air, and rubbery character without wet or gross detail.

The production family contains six genuinely different performances:

1. Punchy cork pop
2. Rubbery squeak-pop
3. Quick double-pop
4. Fluttering air burst
5. Tiny mock-trumpet toot
6. Rare heroic launch blast

The rare heroic variant remains short enough to preserve control feedback and may not play more than once within eight successful boosts. Normal variants do not repeat immediately. Runtime pitch variation stays subtle so the clips retain their authored character.

Empty fuel uses three separate dry sputter performances. It must read as a failed boost without resembling a successful launch.

## 4. Pickup Redesign

The pickup family contains four short, soft reward sounds between approximately 0.10 and 0.18 seconds. Each uses one clear attack and no more than two musical notes.

Pickup playback follows these rules:

- Shipping library gain is `0.20`.
- Only one pickup voice may play at a time; a new valid pickup replaces the previous pickup voice.
- Dense chains consolidate through a `0.06`-second retrigger guard.
- Rapid pickups form a restrained rising cadence through small pitch steps.
- A new pickup lowers or replaces the oldest pickup voice instead of stacking another full-volume chime.
- Pitch and cadence reset after a short break in collections.
- Every collected item still provides immediate visual and fuel feedback even when an audio retrigger is intentionally consolidated.

The generic concurrency and retrigger controls belong in `ArcadeFramework`. Pickup cadence behavior remains Gassy Gorilla-specific unless a later First Bloom game needs the same mechanic.

## 5. Mix Hierarchy

At default settings, the intended order of attention is:

1. Crocodile warnings, failure impacts, and important voice
2. Vine grab/release and successful fart boost
3. Food pickups and milestone accents
4. Swing loops, ambience, and decorative UI

Targets:

- Final one-shot masters peak at or below `-6 dBFS`.
- Successful boost playback is strong but leaves headroom for warnings and voice.
- Pickup playback is approximately 8 dB quieter than the current effective level.
- The SFX bus applies a fixed `0.72` mix ceiling after the user slider, so a saved `100%` setting remains comfortable.
- The fresh-install SFX slider default is `0.70`.
- No normal two-sound gameplay combination clips the output path.
- Voice remains intelligible without requiring the player to reduce SFX manually.
- The heroic boost variant is exciting through shape and timing, not excess loudness.

## 6. Runtime Design

`ArcadeSfxEntry` gains optional inspector-friendly playback policy:

- maximum simultaneous voices;
- minimum retrigger interval;
- same-family replacement or rejection behavior;
- optional rare-variant cooldown.

Defaults preserve existing behavior for other First Bloom games. `ArcadeAudioManager` tracks active one-shot metadata so policies can be applied without scene searches or per-frame allocations.

Gassy Gorilla configures:

- six successful boost clips with anti-repeat and heroic cooldown;
- three failed-boost clips;
- four pickup clips with a one-voice cap and cadence-friendly retrigger timing;
- revised entry gains and restrained pitch ranges;
- calibrated family gains of `0.62` boost, `0.42` failed boost, `0.20` pickup, `0.32` vine swing, `0.34` UI confirm, and no routine SFX family above `0.72`.

No runtime audio synthesis is used by the shipping library.

The framework SFX ceiling applies to library one-shots, direct clips, and loops. It is not exposed as a player setting: the player slider controls preference inside the authored mix, while the ceiling defines what `100%` means.

## 7. Asset and Payload Rules

- SFX remain mono.
- Short clips use mobile/WebGL-appropriate compressed import settings and preload.
- Added compressed WebGL payload should remain below 400 KB.
- Source clips and their Unity metadata are committed and deterministic.
- Re-running the project/audio builder must not overwrite approved production clips with older procedural versions.

## 8. Pre-Publish Consistency Pass

Before publishing, compare every major family against the new boost reference:

- pickup;
- vine grab, swing, and release;
- crocodile warning, splash, and chomp;
- crash and game over;
- UI confirmation/back/error;
- milestones and voice.

Only clear balance or masking problems are adjusted. Music composition, gameplay physics, visuals, and Expedition systems are outside this release.

## 9. Verification

1. Confirm no immediate boost repeat across at least 30 successful boosts.
2. Confirm the heroic boost respects its eight-boost cooldown.
3. Collect dense pickup chains and verify no more than two voices overlap.
4. Compare pickup, boost, vine, warning, failure, and voice levels at default settings.
5. Test master, music, SFX, and voice sliders plus mute/reload persistence.
6. Test desktop browser, landscape mobile viewport, and a real phone speaker when available.
7. Confirm the WebGL audio context unlocks after a valid user gesture.
8. Confirm music, ambience, SFX, and all milestone voices load and play without console errors.
9. Run Unity compilation, scene validation, audio validation, and WebGL build.
10. Measure final compressed payload and compare it with the current release.

Browser verification must include a previously persisted `100%` SFX slider. Under that condition, rapid pickups may not exceed one active family voice or `0.15` effective source gain, successful boosts may not exceed `0.45`, and failed boosts may not exceed `0.30` with the current production family gains.

## 10. Acceptance Criteria

- Fart boosts sound comedic, pop-forward, squeaky, varied, and family-friendly.
- Six successful variants and three failed variants are present and used.
- Pickup chains are clearly quieter and never become a harsh stacked chord.
- Critical warnings and voice remain above routine rewards in the mix.
- Settings and first-gesture audio behavior remain reliable.
- Browser and Unity validation complete without audio errors or missing clips.
- Performance and download size remain within existing release budgets.
- The Game Bible documents the final sound identity, playback rules, and mix hierarchy.
- The public playable build is updated and verified after all gates pass.
