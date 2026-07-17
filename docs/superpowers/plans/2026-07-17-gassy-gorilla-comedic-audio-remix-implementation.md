# Gassy Gorilla Comedic Audio Remix Implementation Plan

**Design:** `docs/superpowers/specs/2026-07-17-gassy-gorilla-comedic-audio-remix-design.md`

**Status:** Implemented, verified, and published

## Goal

Ship a quieter, non-stacking pickup family and a six-variant comedic fart family while preserving WebGL audio unlock, settings persistence, frame pacing, and download budgets.

## 1. Framework Playback Policies

Modify:

- `Assets/_FirstBloom/ArcadeFramework/Scripts/Audio/ArcadeAudioLibrary.cs`
- `Assets/_FirstBloom/ArcadeFramework/Scripts/Audio/ArcadeAudioManager.cs`

Add inspector-friendly one-shot policy to `ArcadeSfxEntry`:

- maximum simultaneous voices (`0` means use the global pool);
- minimum retrigger interval;
- voice-limit behavior (`ReplaceOldest` or `RejectNewest`);
- optional rare clip index and play-count cooldown.

Track one-shot type, start time, and clip index in each pooled voice. Enforce policies without scene searches, LINQ, or per-play allocations. Preserve all existing public playback APIs and add a pitch-scale overload for game-specific pickup cadence.

## 2. Gassy Gorilla Pickup Cadence

Modify:

- `Assets/_FirstBloom/Games/GassyGorilla/Scripts/FartFuelPickup.cs`

Track a short shared pickup streak:

- reset after 0.45 seconds without a pickup;
- rise through restrained semitone steps;
- cap the cadence before it becomes shrill;
- send the pitch scale through the framework playback overload.

Visual collection, fuel refill, and pooling remain unchanged.

## 3. Production Audio Families

Modify:

- `Assets/_FirstBloom/Games/GassyGorilla/Editor/GassyGorillaAudioAssetGenerator.cs`
- `Assets/_FirstBloom/Games/GassyGorilla/ScriptableObjects/GG_AudioLibrary.asset`

Add:

- six mono successful-boost WAV masters;
- three mono failed-boost WAV masters;
- four mono pickup WAV masters;
- Unity metadata for each imported clip.

Generate the boost and sputter source performances with literal, family-specific prompts. Trim, resample, fade, and peak-normalize them locally. Generate deterministic short pickup plinks in the existing editor audio pipeline.

The generator must preserve approved boost masters on rebuild, regenerate deterministic pickup masters, assign the new family counts, and configure:

- Boost: six clips, gain `0.62`, subtle pitch range, heroic clip at index 5, eight-play rare cooldown.
- BoostFailed: three clips, gain `0.42`.
- Pickup: four clips, gain `0.20`, one-voice cap, `0.06`-second retrigger interval, replace-oldest behavior.
- Framework SFX bus: fixed `0.72` mix headroom after the persistent user slider.
- Fresh-install SFX default: `0.70`.

Rebalance other library gains only where needed to retain the approved mix hierarchy.

## 4. Validation

Modify:

- `Assets/_FirstBloom/ArcadeFramework/Scripts/Audio/ArcadeAudioLibrary.cs`
- `Assets/_FirstBloom/Games/GassyGorilla/Editor/GassyGorillaSceneValidator.cs`

Validate:

- policy ranges and rare clip index;
- exactly six Boost, three BoostFailed, and four Pickup clips;
- Pickup gain and two-voice limit;
- Boost rare cooldown;
- source peak/headroom limits;
- preload, mono import, and production library assignment.

Add query-only WebGL QA logging for selected variant, active family voice count, cadence pitch, and rare cooldown state. Normal players incur no visible UI and no continuous logging.

## 5. Build and Playtest

Run:

1. Unity import and compilation.
2. Production audio generation/configuration.
3. Scene regeneration or targeted audio-library repair.
4. Full scene/audio validation.
5. WebGL build.
6. Local browser QA with `?qa-audio`.

Golden path:

> Start from the menu, begin a run, collect a dense pickup chain, perform at least 30 successful boosts, exhaust fuel for failed sputters, grab/release a vine, encounter a crocodile, and reach game over while all audio channels remain active.

Edge probes:

- pickup burst greater than two simultaneous collection events;
- repeated boost input and rare-variant cooldown;
- muted/unmuted settings and reload persistence;
- restart and return-to-menu audio cleanup.

## 6. Release

1. Measure source and compressed WebGL payload deltas.
2. Update the Notion Game Bible with final assets, policies, gains, and QA evidence.
3. Commit and push source changes.
4. Fast-forward the production checkout.
5. Publish immutable WebGL payload and activate GitHub Pages.
6. Verify the public URL on desktop and mobile-sized viewports.

Published release:

- implementation source: `4bae33e9f7bd88bf9bf40a6f28fe9c37766eb82c`;
- immutable WebGL payload: `910730aa1ceb096a371ee8e5c53b1b761a3fc045`;
- Pages activation: `937589748655fd8df1ae2bb97aef9062b6717c1f`;
- playable URL: `https://firstbloomgames.github.io/Gassy-Gorilla/`.

The focused pickup correction passed public browser stress tests at the `0.70` default and with the SFX slider at its maximum setting. Eight rapid pickup attempts consolidated to one active voice at no more than `0.14` effective gain, approximately 9 dB below the previous worst-case overlapping peak. A real collected food pickup used the same `0.14` path. Boost and failed-boost balance remained unchanged, and warning/error-level browser logs remained at zero.
