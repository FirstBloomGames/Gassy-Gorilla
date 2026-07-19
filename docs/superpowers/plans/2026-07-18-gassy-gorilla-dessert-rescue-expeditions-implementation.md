# Gassy Gorilla Dessert Rescue Implementation Plan

**Status:** Complete

**Design:** `docs/superpowers/specs/2026-07-18-gassy-gorilla-dessert-rescue-expeditions-design.md`

## Phase 1: Interaction Contract

1. Add `GassyInteractionType`, `GassyInteractionMarker`, and a typed completion event in `GassyRunEvents`.
2. Extend `GassyExpeditionDefinition` with chapter metadata, lesson copy, repeated interaction objectives, and interaction-set objectives.
3. Extend catalog validation from five levels to two ordered chapters of five.
4. Extend `GassyExpeditionRunController` to track counts, finale bitmasks, lesson prompts, and sticky-sap coaching.

## Phase 2: Reusable Encounters

1. Add `GassyHazardPassReporter` for successful static hazard passage.
2. Add `GassyMudGeyserController` with approach warning, finite eruption, active hitbox, and dodge reporting.
3. Add `GassyStickySapTrap` and bounded sap state to `GorillaController`.
4. Add `GassyCanopyUpdraft` and bounded one-shot lift to `GorillaController`.
5. Clear transient interaction state through intro, start, vine, crocodile, game-over, and restart transitions.
6. Route interaction success through audio, camera, haptics, and `GassyRunEvents`.

## Phase 3: Production Content

1. Build dedicated prefabs for mud geyser, sticky sap, and canopy updraft.
2. Preserve supplied textured Meshy hazard and foliage visuals.
3. Add authored Endless chunks with difficulty, pressure, fuel, and adjacency envelopes.
4. Build Expeditions 6-10 and update the catalog to ten entries.
5. Ensure the mastery finale contains all four interaction markers.

## Phase 4: Marketable Campaign UI

1. Add chapter metadata and a two-page five-slot Expedition selector.
2. Add previous/next chapter controls, locked chapter preview, global level numbering, and completion counts.
3. Add lesson copy to Expedition select and pre-run story panels.
4. Add a compact in-run coach banner that never pauses play.
5. Label the transition after Expedition 5 as `NEXT CHAPTER` and the end of Expedition 10 as `STORY COMPLETE`.

## Phase 5: Audio

1. Append geyser warning, geyser burst, sap catch, sap pop, and updraft types to `ArcadeSfxType`.
2. Generate mono production clips in `GassyGorillaAudioAssetGenerator`.
3. Add gain-limited entries to the production audio library.
4. Extend audio validation to require the new families and headroom.

## Phase 6: Validation

1. Expand `GassyGorillaSceneValidator` for ten levels, two chapters, new prefabs, new controllers, encounter timing, and objective opportunities.
2. Add a focused Play Mode verifier for:
   - Chapter paging and lock preview.
   - Existing progress compatibility.
   - Thorn passage reporting.
   - Geyser warning and dodge.
   - Sap catch and fuel-free breakout.
   - Updraft one-shot lift.
   - All Chapter 2 objective types.
   - Ordered unlocks through Expedition 10.
3. Run the full project builder and scene validator.
4. Run the existing commercial foundation verifier.
5. Run the new Dessert Rescue verifier.
6. Build optimized WebGL and confirm the served build remains below 16 MB.

## Phase 7: Release

1. Playtest desktop menu, both chapters, one Chapter 2 teaching level, finale, pause, settings, and Endless regression.
2. Test 844x390 phone landscape and 390x844 portrait rotation protection.
3. Confirm warning/error-level browser log count is zero.
4. Update the Game Bible with the approved chapter contract and verified release evidence.
5. Commit source, fast-forward production `main`, publish an immutable `gh-pages` payload, activate its exact hash, and verify the public URL.

## Verification Record

- The authoritative Unity builder and ten-level scene validator passed.
- Runtime geometry validation passed 18 gameplay prefabs and 19 authored run chunks.
- The focused Play Mode campaign verifier completed all ten Expeditions, all four new interaction events, ordered unlocks, stars, and existing-save restoration.
- The commercial foundation regression verifier passed pause, settings, accessibility, audio pause mix, badges, and persistence.
- The optimized WebGL build completed at 15.47 MiB for the served `Build` payload.
- Browser QA passed desktop, 844x390 phone landscape, and the 390x844 portrait rotation gate with zero warning- or error-level logs.
