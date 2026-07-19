# Gassy Gorilla Android And Story Expeditions Implementation Plan

**Status:** In progress

**Design:** `docs/superpowers/specs/2026-07-19-gassy-gorilla-android-story-expeditions-design.md`

## Phase 1: Android Foundation

1. Add deterministic Android player-setting configuration and validation.
2. Add QA APK and signed Play AAB menu and batch commands.
3. Read release signing only from environment variables and restore editor secrets after a build.
4. Add Android haptics and landscape-native lifecycle defaults.
5. Add focused Android configuration verification.

## Phase 2: Accessibility And Story Delivery

1. Add persistent subtitles to the First Bloom accessibility service and settings UI.
2. Add a reusable subtitle presenter.
3. Extend Expedition data with optional opening, lesson, success, and adaptive hint voice moments.
4. Add expedition-attempt, heard-state, and hint cadence persistence.
5. Add a narration director with replay, music ducking, subtitle timing, and text-only fallback.
6. Wire opening, lesson, success, and failure moments into the existing non-pausing story flow.

## Phase 3: Moonlit Ruins Content

1. Add the `BounceBloom` interaction type and bounded player bounce API.
2. Build a textured 3D spring-leaf bounce prefab from supplied assets.
3. Add a validated Endless bounce chunk with recovery protection.
4. Author Expeditions 11-15 and expand catalog validation to three chapters.
5. Preserve all existing progression and unlock Chapter 3 from Expedition 10 completion.

## Phase 4: Presentation Polish

1. Add a cached moonlit theme director for background, camera, ambient light, and gameplay readability.
2. Apply the theme only to Moonlit Ruins missions.
3. Replace the indefinitely frozen swing frame with a slow living loop while retaining hand grip alignment.
4. Tighten release and boost visual holds without changing authoritative movement.
5. Add restrained bounce sound, haptics, leaf motion, and coach feedback.

## Phase 5: Validation

1. Expand scene and catalog validation for fifteen missions, bounce content, subtitles, narration, and theme wiring.
2. Add focused Play Mode checks for Android-independent runtime behavior, all three chapters, old-save migration, bounce physics, narration fallback, adaptive hints, and theme restoration.
3. Run the authoritative builder and scene validator.
4. Run existing commercial, Dessert Rescue, vine, audio, and performance regressions.
5. Build and inspect the Android QA APK.
6. Build optimized WebGL and test desktop, phone landscape, and portrait protection.

## Phase 6: Release Integration

1. Update the Notion Game Bible with the implemented contract and verification evidence.
2. Commit the feature branch with generated assets only after deterministic rebuilds pass.
3. Fast-forward production `main`.
4. Publish the exact WebGL payload to `gh-pages`.
5. Verify immutable public files and zero warning/error browser logs.

