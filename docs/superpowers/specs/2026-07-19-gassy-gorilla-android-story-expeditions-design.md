# Gassy Gorilla Android And Story Expeditions Design

**Status:** Approved for implementation

**Date:** 2026-07-19

**Project:** Gassy Gorilla / First Bloom Arcade Framework

## 1. Product Goal

Prepare Gassy Gorilla for serious Android device testing while expanding Expeditions into a concise, marketable Story Mode. The pass must improve native platform behavior, character presentation, accessibility, and campaign breadth without destabilizing the movement and vine mechanics that already test well on mobile.

The launch content target becomes:

- **Endless Run:** the existing score-driven arcade mode.
- **Story Expeditions:** fifteen finite missions in three five-mission chapters.
- **Chapter 1, Home for Dinner:** movement, food, vines, crocodiles, and fuel mastery.
- **Chapter 2, Dessert Rescue:** thorn, geyser, sap, and updraft lessons.
- **Chapter 3, Moonlit Ruins:** spring-leaf bounces and advanced combined mastery.

## 2. Commercial Direction

Story Mode should feel like a playable comedy adventure, not an audiobook. Voice is used for signature character barks and adaptive help, while concise text, environmental staging, objectives, and finish moments carry most of the story.

This is more marketable than a long exposition mode because:

- Players reach control quickly.
- Every story beat teaches or rewards play.
- The same authored systems enrich Endless Run.
- Download size and repeated-play fatigue stay controlled.
- Additional recorded lines can be added without changing runtime architecture.

The pass creates Android release tooling and a QA APK. A Play Store production release still requires the owner's private signing key, Play Console account, listing assets, declarations, tester track, and real-device evidence.

## 3. Android Platform Contract

### Player Settings

- Application identifier: `com.firstbloomgames.gassygorilla`
- Product: `Gassy Gorilla`
- Company: `First Bloom Games`
- Minimum Android API: 26
- Target Android API: 36
- Scripting backend: IL2CPP
- Architectures: ARM64, with ARMv7 permitted only for QA compatibility
- Orientation: landscape left and landscape right only
- Fullscreen: enabled
- Target frame rate: 60
- Managed stripping: high
- IL2CPP code generation: optimize for size
- Build App Bundle for Play; APK for local QA

### Signing

No keystore, password, alias, or signing secret may be committed.

The Play build command reads:

- `GG_ANDROID_KEYSTORE_PATH`
- `GG_ANDROID_KEYSTORE_PASS`
- `GG_ANDROID_KEY_ALIAS`
- `GG_ANDROID_KEY_ALIAS_PASS`

It fails with a clear error if any value is missing. A separate QA command produces a development APK with Unity's debug signing.

### Native Behavior

- Android haptics map the existing First Bloom light, medium, heavy, success, and failure types to native view feedback.
- Unsupported devices safely no-op.
- Haptics obey the persistent player setting.
- The Android back button pauses an active run and backs out of secondary menu panels.
- The game never launches into portrait gameplay.
- Audio focus loss pauses active play and requires deliberate resume.

## 4. Story Voice And Subtitle Contract

`GassyExpeditionDefinition` supports optional voice moments:

- Opening bark
- Lesson bark
- Success bark
- Adaptive failure hint

Every voice moment has authored subtitle text. Missing clips are valid and fall back to text without blocking a mission.

Runtime rules:

- Opening story voice may play when the story card appears after audio is unlocked.
- Starting a mission may play one brief lesson bark.
- Success voice plays after the finish reveal.
- A failure hint appears after two unsuccessful attempts and may repeat every second failure.
- Normal failure attempts do not repeat full exposition.
- A replay-voice command is available on the story card.
- Voice playback uses the existing Voice channel and ducks music.
- Subtitles default on and can be disabled independently of voice volume.
- Gameplay instructions remain visible even when subtitles are disabled.
- Story voice never pauses active play.

The three supplied heroic voice clips remain signature barks in contexts that match their recorded wording. New recording assets remain optional data additions rather than code dependencies.

## 5. Chapter 3: Moonlit Ruins

After dessert is rescued, a moonbeam reveals a shiny jungle gem inside old ruins. Gassy Gorilla follows the glow before the crocodiles can add it to their collection.

### 11. Bounce By Moonlight

- **Objective:** Trigger three spring-leaf bounces and reach the finish.
- **Lesson:** Land on the broad glowing leaves for a free upward launch.
- **Purpose:** Introduce a readable, beneficial automatic interaction.

### 12. Moonbeam Buffet

- **Objective:** Collect twelve foods and reach the finish.
- **Lesson:** Use bounce pads and updrafts to follow elevated food arcs without wasting fuel.
- **Purpose:** Teach route planning around free lift.

### 13. Vine Cathedral

- **Objective:** Release from four vines and reach the finish.
- **Lesson:** Stay attached, read the full arc, and release on the forward rise.
- **Purpose:** Turn the signature vine mechanic into an advanced mastery sequence.

### 14. Crocodile Moon

- **Objective:** Dodge three crocodile ambushes and reach the finish.
- **Lesson:** Read warning ripples and use free-lift recovery beats to keep one boost available.
- **Purpose:** Escalate predator timing fairly.

### 15. The Shiny Gem

- **Objective:** Complete one thorn dodge, geyser dodge, sap escape, updraft ride, and spring-leaf bounce before reaching the gem gate.
- **Lesson:** Apply every environmental response in a readable sequence.
- **Purpose:** Deliver a launch-ready story finale with a clear collectible payoff.

## 6. Spring-Leaf Bounce Interaction

The bounce pad uses supplied textured 3D root, broad-leaf, and fern assets. A soft emissive pulse is VFX, not replacement art.

Behavior:

- A trigger activates once per spawned pad.
- Contact applies bounded forward and upward velocity without spending fuel.
- The pad compresses, rebounds, emits a short leaf burst, plays a soft comedic boing, and produces optional light haptics.
- The player retains normal one-touch control immediately after launch.
- It cannot activate while swinging, stuck in sap, captured, paused, or outside an active run.
- It reports `BounceBloom` completion through `GassyRunEvents`.
- Reduced Motion preserves the timing and color cue while reducing scale and sway.

The authored chunk guarantees recovery space and cannot place a predator immediately after the launch.

## 7. Moonlit Presentation

Chapter 3 applies a reversible scene theme through a reusable director:

- Cooler moon-blue camera and ambient colors
- Soft violet-blue fill light and warm gold interactable accents
- Darker painted jungle tint without hiding silhouettes
- Slightly brighter vine, food, finish, and hazard telegraphs
- No new camera, post-process stack, real-time shadow, or runtime material allocation

The theme uses `MaterialPropertyBlock` and serialized lights/renderers. Endless and Chapters 1-2 retain the established daytime jungle.

## 8. Gorilla Animation Polish

The supplied 3D rig remains the only hero visual.

- Swing no longer freezes the full Animator indefinitely.
- The swing clip loops slowly while hand alignment preserves a firm grip.
- Visual bank follows swing direction with a bounded lean.
- Release transitions start in the strongest airborne portion of `VineRelease`, then blend back to cruise.
- Boost and release holds are short enough to avoid skating.
- Grip release preserves world pose and blends the model back to its body anchor.
- Root motion remains disabled and gameplay physics remain authoritative.

No change may weaken the magnetic catch, player-controlled hold, release power curve, or comfortable non-swing movement.

## 9. Persistence And Compatibility

- Existing stars and unlocks for Expeditions 1-10 remain unchanged.
- Completing Expedition 10 unlocks Expedition 11.
- The highest unlocked index is reconciled from completed stars.
- Voice-heard and failure-attempt records use expedition IDs, not array indices.
- Chapter paging remains five buttons per page and expands automatically to three chapters.
- Completing Expedition 15 labels the result `STORY COMPLETE`.

## 10. Performance Budget

- No additional cameras or skinned meshes.
- One lightweight controller per bounce pad.
- Moonlit theme changes use cached references and property blocks.
- Story narration loads only referenced clips.
- Voice WAV import remains mono and compressed for platform builds.
- Android QA must run at a stable 60 fps on a representative mid-range device before store submission.
- WebGL served payload should remain near the current 16 MB budget; any increase must be measured and explained.

## 11. Validation Gates

Implementation is complete when:

- Android player settings validate to the platform contract.
- A QA APK builds without production signing secrets.
- The Play AAB command validates signing inputs and never writes secrets into source.
- Android haptics compile and unsupported platforms remain safe.
- Settings expose Reduced Motion, Haptics, and Subtitles.
- The catalog contains exactly fifteen ordered Expeditions in three chapters.
- Existing ten-level saves reconcile and unlock Chapter 3 correctly.
- Every Chapter 3 objective has enough authored opportunities.
- The finale contains all five required environmental interactions.
- Spring-leaf bounce is one-shot, fuel-free, bounded, and event-reporting.
- Moonlit theming activates only for Chapter 3 and restores cleanly.
- Swing animation remains alive while both hands stay aligned to the vine.
- Optional story voice, subtitles, replay, and adaptive failure logic pass with and without clips.
- Focused Play Mode verification completes all fifteen Expeditions in order.
- Existing Endless, vine, crocodile, pause, audio, accessibility, badge, and save checks still pass.
- WebGL desktop, phone landscape, and portrait-rotation protection remain regression-free.

## 12. Store Boundary

Passing local Unity, APK, and WebGL verification does not by itself authorize a `9.99` store claim. Before paid launch, the owner still needs:

- Private release keystore backup and Play App Signing enrollment
- Play Console account verification and required closed testing
- Real-device frame-time, thermal, memory, audio, haptics, suspend, and resume testing
- Privacy, data-safety, content-rating, accessibility, and target-audience declarations
- Store icon, feature graphic, screenshots, trailer, short description, and support/privacy URLs
- External playtest evidence for onboarding, difficulty, retention, and perceived value

