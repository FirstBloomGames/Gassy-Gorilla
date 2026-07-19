# Gassy Gorilla Dessert Rescue Expedition Expansion

**Status:** Approved for implementation under the standing production approval

**Date:** 2026-07-18

**Project:** Gassy Gorilla / First Bloom Arcade Framework

## 1. Product Goal

Expand Expeditions from a five-level introduction into a ten-level, two-chapter campaign that teaches new obstacles and interactions before combining them in a finale. The expansion must make the game easier to learn, richer to master, and easier to market without changing the one-touch control contract.

Chapter 1 remains intact as **Home for Dinner**. Chapter 2 is **Dessert Rescue**, a five-Expedition story in which the family dessert rolls back into the jungle immediately after Gassy Gorilla arrives home.

## 2. Why This Structure

The existing five Expeditions already teach the original game:

1. Basic boost movement and reaching a finish.
2. Food collection and fuel routing.
3. Magnetic vine catches and player-controlled release.
4. Crocodile warning recognition and boost-to-dodge timing.
5. Combined route reading and fuel conservation.

Adding unfamiliar threats directly to Endless would increase surprise deaths and weaken the game's fairness promise. A second chapter allows each new system to follow a teach, practice, and mastery sequence while reusing the same authored chunk format in both modes.

## 3. Chapter 2 Story And Level Order

### 6. Stump Jump

- **Story:** Tracks from the runaway dessert cart cross a patch of thorny stumps.
- **Objective:** Clear three thorn stumps, then reach the finish.
- **Lesson:** Read the low thorn silhouette and tap shortly before contact to boost over it.
- **Purpose:** Formally teaches a standard obstacle that already exists in the game.

### 7. Geyser Gulch

- **Story:** The cart rolls into a bubbling mud field.
- **Objective:** Dodge three mud geyser eruptions, then reach the finish.
- **Lesson:** Yellow bubbles and a rising warning pulse appear before eruption. Boost before the warning ends.
- **Purpose:** Introduces a timed hazard with a clear visual and audio telegraph.

### 8. Sap Happens

- **Story:** The dessert trail passes through sticky jungle sap.
- **Objective:** Pop free from two sap traps, then reach the finish.
- **Lesson:** Sap slows forward movement but does not kill. The next tap creates a free breakout boost and spends no fart fuel.
- **Purpose:** Teaches a recoverable mistake and gives one-touch input a new contextual response.

### 9. Ride The Breeze

- **Story:** Spiraling canopy currents reveal a fuel-saving shortcut.
- **Objective:** Ride three updrafts, then reach the finish.
- **Lesson:** Drift into the green leaf spiral to receive free lift and conserve fuel.
- **Purpose:** Adds a beneficial environmental interaction and route-planning option.

### 10. Dessert Rescue

- **Story:** The runaway dessert is visible beyond a compact jungle examination.
- **Objective:** Complete one thorn dodge, one geyser dodge, one sap escape, and one updraft ride before reaching the finish.
- **Lesson:** Apply all four Chapter 2 responses in a readable sequence with recovery space between them.
- **Purpose:** Provides a marketable chapter finale and confirms mastery without an arbitrary difficulty spike.

## 4. One-Touch Interaction Contract

The new content cannot add directional controls or extra action buttons.

- **Thorn stump:** Tap to boost over the low obstacle.
- **Mud geyser:** Read the warning and tap early enough to leave the eruption lane.
- **Sticky sap:** Contact slows the gorilla. The next tap pops him free with a fuel-free boost.
- **Updraft:** No tap is required. Entering the current provides lift, allowing the player to conserve fuel.

If the gorilla is attached to a vine, vine release retains priority. Pause input remains separate from gameplay input.

## 5. Encounter Fairness

### Thorn Stump

- Uses the supplied textured 3D stump model.
- Keeps the existing low-lane silhouette and fatal collision behavior.
- Provides at least 4.5 metres of approach space.
- Reports a successful dodge only after the gorilla passes the obstacle alive.

### Mud Geyser

- Uses the supplied textured 3D mud geyser model.
- Activates once per spawned encounter rather than looping unpredictably.
- Begins warning when the gorilla enters a measured approach range.
- Uses at least 0.8 seconds of yellow bubbles, pulsing light, and a warning sound.
- Enables its hazard column only during the eruption window.
- Leaves enough vertical clearance for one normal boost from a reasonable entry height.
- Reports a dodge after the eruption has resolved or the gorilla has safely passed.

### Sticky Sap

- Uses the supplied textured 3D sticky sap model.
- Uses a trigger, not a fatal `ArcadeHazard`.
- Applies a clear temporary speed reduction and visible squash.
- Changes the next normal gameplay tap into a fuel-free breakout boost.
- Cannot consume fuel, chain repeatedly from the same blob, or leave the player permanently stuck.
- Reports completion when the breakout succeeds.

### Canopy Updraft

- Uses supplied textured 3D leaf and fern assets arranged in a lightweight spiral.
- Uses a trigger column with visible upward flow.
- Applies bounded upward velocity once per encounter.
- Does not refill fuel directly; its reward is fuel-free lift.
- Reports completion on first entry.

## 6. Endless Run Integration

The Chapter 2 encounters also enrich Endless Run through authored chunks:

- Thorn teaching chunks may appear from the Groove difficulty band.
- Mud geysers and sticky sap may appear from the Canopy difficulty band.
- Updraft recovery chunks may appear from Groove onward.
- Dynamic threats cannot follow another hazard or predator chunk.
- Sap and geyser chunks must be followed by compatible recovery space.
- New chunks use the existing deterministic seed, pressure, fuel, height-envelope, and adjacency validation.
- The controlled opening remains unchanged.

This introduces the new systems gradually even when a player chooses Endless first, while Expeditions remain the clearest and safest way to learn them.

## 7. Expedition Data Model

`GassyExpeditionDefinition` gains:

- Chapter index and chapter title.
- A concise player-facing lesson line.
- A generic interaction objective type.
- A required interaction value for repeated lessons.
- A required interaction bitmask for the finale.

`GassyInteractionType` is a flagged enum containing:

- `ThornDodge`
- `GeyserDodge`
- `SapEscape`
- `UpdraftRide`

Spawned encounter prefabs carry a marker identifying which interaction they can complete. Catalog validation counts matching opportunities and rejects missing finale skills.

## 8. Runtime Architecture

### Reusable Event Layer

`GassyRunEvents` publishes interaction completion events. Expedition objectives, feedback, QA, and future badges can subscribe without coupling encounter controllers to menu or progression code.

### Encounter Components

- `GassyHazardPassReporter` reports nonfatal passage of a static lesson hazard.
- `GassyMudGeyserController` owns approach detection, warning, eruption, collision, and dodge completion.
- `GassyStickySapTrap` owns one-time contact and presentation while `GorillaController` owns the contextual breakout input.
- `GassyCanopyUpdraft` owns one-time trigger lift and visual motion.
- `GassyInteractionMarker` exposes encounter type for validation.

Each component has inspector-visible timing and force values and resets cleanly when a run scene reloads.

### Player Extension

`GorillaController` gains bounded methods for:

- Entering sticky sap.
- Performing a fuel-free sap escape.
- Receiving one-time updraft lift.

Normal boosts, vines, crocodile capture, pause, failure, and intro state remain authoritative and clear any transient interaction state when necessary.

## 9. Teaching Presentation

- The Expedition select detail includes a short **LESSON** line.
- The pre-run story card includes the objective and response in plain language.
- A compact coach banner repeats the lesson briefly when the run begins.
- Sticky sap displays an urgent `STUCK - TAP TO POP FREE` prompt until the player responds.
- Objective progress updates immediately after each successful lesson action.
- Interaction success uses a restrained green-gold pulse, short sound, and optional haptic.
- Active gameplay never pauses for tutorial text.

## 10. Chapter Navigation

The Expedition selector continues to show five level buttons at phone-readable size. Chapter arrows page between:

- **Chapter 1: Home for Dinner**
- **Chapter 2: Dessert Rescue**

Existing save data remains valid:

- Previously unlocked and completed Chapter 1 levels retain their stars.
- Chapter 2 begins locked until Expedition 5 is complete.
- Completing Expedition 5 unlocks Expedition 6.
- The selector may preview a locked chapter, but locked levels cannot start.
- Completing Expedition 10 ends the current story and returns to Expedition select.

The chapter pager is designed to support future five-level chapters without increasing visible menu density.

## 11. Audio And Feedback

Add restrained production SFX families at the end of the existing enum to preserve serialized values:

- Geyser warning
- Geyser burst
- Sap catch
- Sap pop
- Updraft

Routine interaction sounds remain under movement-critical and crocodile cues. New clips are mono, preloaded, gain-limited, and generated through the existing production audio pipeline.

Reduced Motion suppresses decorative pulse scale and camera shake but preserves telegraphs, collider timing, and readable color changes. Haptics remain optional and use the reusable First Bloom facade.

## 12. Performance Budget

- No additional cameras.
- No unbounded particles or runtime material creation.
- One active controller per spawned encounter.
- Geyser particles remain finite and below 32 live particles.
- Updraft uses no more than eight low-cost leaf visuals.
- No new skinned meshes.
- The complete served WebGL build remains below 16 MB unless a measured quality tradeoff is approved.
- Existing authored chunk prewarming and cleanup own encounter lifetime.

## 13. Validation Gates

The expansion is complete only when:

- The catalog contains exactly ten unique, ordered Expeditions in two five-level chapters.
- Existing Chapter 1 stars and unlock data remain compatible.
- Every Chapter 2 objective has enough matching authored opportunities.
- The finale contains all four required interaction types.
- Geyser warning, eruption, collider timing, and safe reaction window pass validation.
- Sap is nonfatal, fuel-free to escape, one-shot per blob, and cannot persist through failure or restart.
- Updraft lift is bounded and one-shot.
- New chunks respect minimum difficulty, pressure, adjacency, height, fuel, and recovery rules.
- Menu chapter paging, locked preview, selection, next-level flow, and final chapter completion work on landscape phones.
- Dedicated runtime verification completes all ten Expeditions in order and confirms persistence after relaunch.
- Endless Run still passes movement, vine, crocodile, audio, pause, accessibility, badge, and deterministic difficulty checks.
- Desktop, phone landscape, and portrait rotation protection pass in the final WebGL build.
- The public build loads the exact immutable payload with zero warning- or error-level browser logs.

## 14. Commercial Boundary

This expansion materially improves onboarding, content breadth, trailer readability, and repeatable progression. It does not by itself complete native App Store readiness. Signed iOS builds, real-device performance and thermal testing, store media and metadata, privacy and accessibility declarations, and external retention evidence remain separate commercial gates.
