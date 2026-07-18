# Gassy Gorilla Commercial Foundation Implementation Plan

**Status:** Implemented and verified

**Date:** 2026-07-18

**Design authority:** `docs/superpowers/specs/2026-07-18-gassy-gorilla-commercial-foundation-design.md`

## 1. Framework Settings And Feedback

- Add persistent reduced-motion and haptics settings.
- Add a platform-safe haptics facade and iOS native bridge.
- Extend the settings panel with inspector-friendly toggles.
- Route camera shake, slow motion, and panel transitions through reduced-motion state.
- Add audio pause-mix support without changing authored user volumes.

## 2. Input And Pause

- Extend one-touch input with controller face-button support.
- Add a reusable pause-panel presenter.
- Add Gassy Gorilla pause ownership to the game manager.
- Support on-screen pause, Escape, P, controller Start, and focus-loss pause.
- Keep Settings nested inside the paused state and restore the correct panel.
- Add Resume, Restart, Settings, and Main Menu actions.

## 3. Jungle Badges

- Add reusable monotonic achievement persistence.
- Add eight stable Gassy Gorilla badge definitions and counters.
- Track boosts, vine releases, food, crocodile dodges, Endless distance, Expedition stars, and chapter completion.
- Reconcile qualifying existing progress on scene startup.
- Add a restrained unlock toast.
- Add a compact main-menu badge panel and completion count.

## 4. Generated Content And Validation

- Extend the project builder for pause, settings toggles, badge UI, and iOS plugin import-safe content.
- Extend scene validation for every new component, threshold, reference, and UI contract.
- Regenerate both scenes and generated assets.
- Run zero-error Unity compilation and full scene/content validation.

## 5. WebGL And Runtime QA

- Build the optimized phone WebGL target.
- Verify complete served size remains below 16 MB.
- Exercise pause/resume, paused Settings, restart/menu, focus loss, settings persistence, badge unlock/persistence, Endless, and Expeditions.
- Check 1280 x 720 desktop, 844 x 390 phone landscape, and 390 x 844 portrait.
- Require zero warning or error logs.

## 6. Release

- Update the design, plan, and Game Bible with verified evidence.
- Fast-forward production `main`.
- Publish an immutable WebGL payload to `gh-pages`.
- Activate and verify the live public URL.

## 7. Verification Evidence

Completed on 2026-07-18 with Unity `6000.5.2f1`:

- Full project builder completed with exit code `0`.
- Scene validator passed for Endless Run, five Expeditions, pause, accessibility, Jungle Badges, textured 3D world art, audio, camera, and gameplay wiring.
- Automated Play Mode verification passed for the badge panel, settings persistence, pause, paused Settings return, audio pause mix, resume, and monotonic achievement persistence.
- Optimized WebGL build completed with exit code `0`.
- Served WebGL payload measured `8.6 MB` compressed and `14.6 MB` uncompressed.
- Browser walkthrough verified main menu, badges, settings, one-touch play, vine hold, pause, paused Settings, resume, and game-over presentation.
- Responsive QA passed at `1440 x 900`, `844 x 390`, and `390 x 844`.
- Portrait mobile correctly presents the landscape rotation gate.
- Browser console contained zero warnings and zero errors after desktop and mobile walkthroughs.
