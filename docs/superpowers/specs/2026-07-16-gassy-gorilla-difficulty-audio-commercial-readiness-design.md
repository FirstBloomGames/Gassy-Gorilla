# Gassy Gorilla Difficulty, Audio, and Premium Readiness Design

**Status:** Approved design, awaiting written-spec review

**Date:** 2026-07-16

**Project:** Gassy Gorilla / First Bloom Arcade Framework
**Release target:** Premium small arcade game with WebGL/mobile quality suitable for an eventual $9.99 App Store release

## 1. Purpose

This pass turns the strong current movement and vine loop into a run that develops over time and sounds intentionally authored. It must preserve the comfortable controls, magnetic vine attachment, mobile performance, and compact WebGL payload established by the previous polish pass.

The pass also produces an evidence-based commercial-readiness report. It may declare the core experience release-ready, identify remaining launch blockers, or recommend a lower price/content expansion. It must not claim that market success is guaranteed.

## 2. Player Experience

The first minute teaches a dependable rhythm: boost, collect, grab, swing, release. The next few minutes ask for more deliberate releases and fuel decisions without changing the control rules. Later play combines familiar challenges more tightly, but every failure should remain readable and attributable to a player decision.

The audio should sound like a heroic jungle adventure that knows the hero is propelled by farts. The joke comes from contrast and timing rather than constant noise. Music supports flow, important actions have distinct sonic signatures, and the mix leaves room for the existing gorilla voice lines.

## 3. Design Principles

1. Difficulty increases through authored situations, not weakened controls.
2. Speed changes are gradual and capped; reaction windows remain readable.
3. Pressure alternates with relief. Random generation may vary a run but may not create unfair streaks.
4. Low fuel is strategically meaningful but never becomes an unavoidable death spiral.
5. Audio communicates state before it decorates it.
6. Runtime synthesis is only a fallback. Shippable audio is imported, deterministic, and validated.
7. New generic audio capabilities live in `ArcadeFramework`; gorilla-specific content and tuning stay in `Games/GassyGorilla`.
8. WebGL/mobile load time and frame pacing remain release gates.

## 4. Difficulty Model

### 4.1 Continuous intensity

Run distance produces a normalized intensity value from 0 to 1. Stage boundaries provide readable tuning anchors, while speed and music interpolate smoothly between them.

| Stage | Distance | Speed multiplier | Intended feeling |
| --- | ---: | ---: | --- |
| Welcome | 0-70 m | 1.00-1.02 | Learn the rhythm and trust the controls |
| Groove | 70-150 m | 1.02-1.05 | Make confident vine releases |
| Canopy | 150-260 m | 1.05-1.08 | Choose between fuel and distance |
| Wild | 260-400 m | 1.08-1.11 | Combine hazards with longer boost gaps |
| Legend | 400 m onward | 1.11-1.14 | Sustained mastery without chaos |

The current base forward speed remains `4.65`. The maximum target is approximately `5.30`, reached gradually near 550 m. Boost lift, cooldown, fuel drain, vine magnetism, input buffering, and grab radius do not become harsher with difficulty.

### 4.2 Dynamic chunk weighting

`RunChunkDirector` will retain deterministic weighted selection and transition compatibility, then apply stage-aware multipliers by `RunChunkTag`:

| Tag | Early behavior | Late behavior |
| --- | --- | --- |
| `Beginner` | Favored | Tapers but never disappears |
| `Recovery` | Common | Slightly less common, still guaranteed after pressure |
| `Fuel` | Normal | Responsive to the player's fuel state |
| `Vine` | Reliable | Remains available; harder release opportunities come from layouts |
| `Boost` / `NoVine` | Introduced gently | Becomes moderately more common |
| `Hazard` | Sparse and clearly introduced | More frequent, never stacked unfairly |
| `Predator` | Suppressed before 90 m | Rare but memorable, capped in every stage |

The exact multipliers live in an inspector-friendly `RunDifficultyProfile` ScriptableObject rather than being spread across gameplay code.

### 4.3 Fairness state

The director tracks a small pressure state in addition to recent chunk history:

- Recovery/Beginner beat: pressure decreases.
- Boost gap or hazard beat: pressure increases by 1.
- Predator beat: pressure increases by 2 and starts a predator cooldown.
- At pressure 3, the next compatible selection must be a recovery beat.
- A predator must be followed by recovery and cannot recur for at least four generated chunks.
- Hazard or predator chunks cannot appear back-to-back.
- If fuel falls below 30%, `Fuel` and `Recovery` weights increase and a recovery beat is guaranteed within two generated chunks.
- The invisible safeguard ends once fuel recovers above 45%, preventing oscillation.
- Existing route-envelope, transition, reaction-distance, and recent-history checks remain mandatory.

Fallback selection must relax cosmetic history before it relaxes fairness. If no fair selection exists, the director chooses a compatible recovery chunk and logs a development warning; it must not silently choose an unsafe predator or hazard transition.

### 4.4 Content variety

The six current main-pool chunks are insufficient for a premium difficulty ramp on their own. This pass expands the library to at least twelve reusable authored layouts using the existing 3D assets and pooled prefabs. Candidate beats are:

- High Vine Arc
- Low Vine Rescue
- Vine-to-Boost Relay
- Fuel Choice Fork
- Long Boost Gap
- Thorn Timing Lane
- Crocodile Bait-and-Lift
- Post-Predator Recovery Feast

Every new layout must pass route-envelope and reaction-distance validation. No new decorative shrub density is added.

## 5. Runtime Architecture

### 5.1 `RunDifficultyProfile`

A Gassy Gorilla ScriptableObject owns:

- distance/intensity curve;
- speed curve;
- per-stage tag multipliers;
- predator unlock distance and cooldown;
- pressure and fuel-rescue thresholds;
- optional stage names for QA and telemetry.

It exposes pure evaluation methods so the same profile can be simulation-tested without running a scene.

### 5.2 `RunChunkDirector`

The existing director gains:

- a `GorillaController` fuel source and `RunDifficultyProfile` reference;
- continuous distance/intensity calculation;
- dynamic candidate weight evaluation;
- pressure, predator-cooldown, and fuel-rescue state;
- `DifficultyChanged(float intensity, int stage)` event;
- deterministic simulation metrics by stage;
- explicit fairness validation failures.

Opening-sequence behavior, pooling, seed support, cleanup, and transition compatibility remain intact.

### 5.3 `GorillaController`

The controller gains a single public difficulty-speed input. Internally it computes an effective cruise speed and eases toward it so stage boundaries cannot create a velocity jump. All code that currently assumes the serialized `forwardSpeed` uses the effective cruise speed where appropriate, including boost minimums and vine-release minimums.

No difficulty logic enters the input, boost, swing, or animation state machines.

## 6. Audio Design

### 6.1 Original audio inventory

Because the production music and SFX folders are currently empty, this pass creates and imports an original lightweight audio set. Source masters are generated offline in the Unity Editor and committed as standard WAV assets; runtime audio is not synthesized.

Music is a seamless 8-bar loop near 88 BPM, approximately 22 seconds:

- `GG_Music_JungleStride_Base`: marimba/bamboo plucks, warm bass, restrained hand percussion.
- `GG_Music_JungleStride_Intensity`: synchronized shakers, tom accents, and mock-heroic brass color.
- `GG_Ambience_JungleWater`: subtle water and canopy texture, deliberately quiet.

Required SFX families:

| Family | Variants | Purpose |
| --- | ---: | --- |
| Fart boost | 4 | Different body/resonance and tail lengths |
| Empty-fuel sputter | 2 | Clear failed-input feedback without harshness |
| Food pickup | 3 | Bright, short, pitch-safe reward |
| Vine grab | 2 | Magnetic catch impact plus leaf snap |
| Vine swing/creak | 2 loops | Low-level motion texture while attached |
| Vine release | 3 | Rope release plus forward whoosh |
| Crocodile warning | 2 | Readable threat cue before emergence |
| Splash | 2 | Small emergence and large player impact |
| Chomp | 2 | Comedic wooden impact/jaw closure |
| Crash/fall | 2 | Failure punctuation without excessive bass |
| Milestone sting | 2 | Brief mock-heroic acknowledgement |
| UI | 3 | Confirm, back, and disabled/error |
| Game over | 1 | Short cadence that leaves room for results UI |

The existing three milestone voice WAVs remain in use. New voice lines are not required for this pass.

### 6.2 `ArcadeAudioLibrary`

A reusable framework ScriptableObject maps `ArcadeSfxType` values to clip arrays plus volume and pitch ranges. Random selection avoids immediately repeating the same variant. Missing entries fall back to the existing generated clip in development builds and produce a validator warning; production validation requires every used type to be assigned.

`ArcadeSfxType` expands to include:

- `BoostFailed`
- `VineSwing`
- `CrocodileWarning`
- `Milestone`
- `GameOver`
- `UiBack`
- `UiError`

### 6.3 `ArcadeAudioManager`

The framework manager gains:

- an eight-source SFX voice pool for clean overlap and independent pitch;
- synchronized base, intensity, and ambience music sources;
- smoothed `SetMusicIntensity(float)` crossfading;
- explicit start/stop support for looping movement sounds;
- music ducking during voice playback;
- first-user-gesture audio activation for mobile WebGL;
- correct persistence of master, music, SFX, and voice settings.

The manager remains compatible with existing `PlaySfx(ArcadeSfxType)` call sites.

### 6.4 `GassyGorillaAudioDirector`

A game-specific scene component translates game events into audio behavior:

- difficulty intensity drives the music stem;
- vine grab starts a quiet swing loop and release stops it;
- crocodile telegraph, leap, splash, chomp, and dodge use distinct cues;
- voice playback ducks music;
- returning to menu or game over resets intensity gracefully.

Direct duplicate calls are removed so each action produces one intentional cue.

### 6.5 Mix and payload budgets

- Music peaks below -6 dBFS; SFX peaks below -3 dBFS; generated masters must contain no clipped samples.
- Voice remains intelligible at default settings.
- Ambience is felt more than heard and follows the music setting.
- Short SFX import as mono compressed/decompressed clips based on length; music imports as compressed stereo.
- Added compressed WebGL payload target: no more than 1.5 MB.
- Final initial WebGL payload target: no more than 16 MB unless a measured quality tradeoff is approved.

## 7. Feedback and UI

Difficulty stage transitions should primarily be felt through pacing and music. Existing distance milestone presentation remains the main visual celebration. At most two later-stage transitions may trigger the brief milestone sting; no persistent difficulty meter or tutorial text is added.

Audio settings remain available from the current settings menu. Critical hazards continue to have visual telegraphs, so muted play remains fair.

## 8. Tooling and Validation

### 8.1 Editor generation

An idempotent Gassy Gorilla audio generator creates the original WAV masters and configures import settings. Re-running it produces byte-stable output and does not duplicate assets. Scene/content setup assigns the difficulty profile, audio library, and new chunks.

### 8.2 Automated validation

The build preflight expands to check:

- all required audio families are assigned;
- synchronized stems have matching sample rate, length, and channel expectations;
- no source master contains clipped samples or insufficient headroom;
- difficulty profile curves are ordered and bounded;
- predator unlock/cooldown and fuel thresholds are valid;
- at least twelve main-pool chunks exist;
- 5,000 seeded selections per representative stage never exceed pressure limits;
- no predator appears before unlock or inside cooldown;
- low-fuel simulation reaches recovery within two chunks;
- stage hazard/predator rates rise within configured bands;
- runtime geometry and WebGL dependency budgets still pass.

### 8.3 Manual playtest matrix

1. Main menu audio begins after the first valid browser gesture and settings persist after reload.
2. Welcome stage is calm and teaches boost/vine/release without surprise predators.
3. A 400+ m run feels faster and more demanding while controls remain equally responsive.
4. Deliberately low fuel produces a fair recovery opportunity without visibly repeating one chunk.
5. Vine grab, held swing, and release have continuous, non-overlapping sound behavior.
6. Crocodile warning is audible before the threat but the encounter remains playable while muted.
7. Voice lines duck music and do not cut off important SFX.
8. Game over stops motion loops, plays one result cue, and restart resets intensity/state.
9. Landscape mobile WebGL maintains stable frame pacing; portrait guidance still works.
10. Browser console and Unity logs contain no errors or missing-clip warnings.

## 9. Release and Documentation

After implementation:

1. Run Unity compilation, scene validation, difficulty simulation, audio validation, and geometry budgets.
2. Build the optimized WebGL phone target.
3. Measure compressed payload and browser startup/frame behavior.
4. Playtest the full menu-to-run-to-game-over loop locally and on the public build.
5. Update the Notion Game Bible with the final implemented values and design rationale.
6. Commit source changes, publish the immutable CDN asset commit, then update GitHub Pages.
7. Verify the public link on desktop and mobile-sized viewports.

## 10. Premium Commercial-Readiness Gate

The final report scores these areas as pass, conditional, or blocker:

- core control feel and fairness;
- visual consistency and animation readability;
- music, SFX, voice, and mix quality;
- content variety over a ten-minute run;
- performance, loading, memory, and device compatibility;
- accessibility and settings;
- onboarding, restart flow, and retention hooks;
- native iOS build readiness, signing, privacy disclosures, store metadata, screenshots, and legal/licensing records;
- crash-free QA evidence and unresolved defects.

This implementation can complete the game's premium core loop and WebGL showcase. A $9.99 App Store launch cannot honestly be called ready until native iOS testing, signing, store compliance, and sufficient content-depth evidence also pass. Any blockers found in the audit will be reported plainly and converted into the next ordered implementation list.

## 11. Acceptance Criteria

The pass is complete when:

- difficulty rises perceptibly but smoothly across a long run;
- no tested seed violates the fairness invariants;
- the chunk pool contains at least twelve validated layouts;
- all required production audio is imported and used;
- placeholder music is disabled in the shipping scene;
- audio settings, voice ducking, mobile unlock, and overlapping SFX work;
- the optimized WebGL build remains at or below the agreed payload and performance budgets;
- Unity and browser validation complete without errors;
- the Game Bible matches the implementation;
- the public playable link is updated and verified;
- the commercial-readiness report states what is and is not ready for a $9.99 launch.
