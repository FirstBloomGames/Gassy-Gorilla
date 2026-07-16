# Gassy Gorilla Vine Confidence and Fuel HUD Release Design

**Date:** July 16, 2026
**Status:** Implemented and production-validated
**Release target:** Polished desktop/mobile WebGL build and production-ready Unity source

## Problem

The recorded playtest shows that successful use of a low vine can place the gorilla at the lagoon edge, force multiple emergency boosts, or hand the player directly to a crocodile. The authored `LowVineRescue` grab point reaches roughly `0.78` world Y while the controller can release with only `2.4` vertical velocity under approximately `1.5g`. The mechanic therefore breaks its promise: grabbing the fun traversal tool can be less safe than ignoring it.

The fuel HUD also reads as prototype UI. It is a flat rectangular fill with centered title text and a competing `current / maximum` number inside the same visual lane.

## Vine Confidence Contract

1. A standard forward-half release must remain above the lagoon death line for at least one second without a boost.
2. A late or weak release must remain recoverable with one boost, while still travelling less far than a strong release.
3. Release power continues to scale forward reach and lift; the safety floor must not erase skilled timing.
4. No authored standard vine may put its lowest grab pose below the minimum safe grab height.
5. A vine chunk may not feed directly into a predator or hard-hazard chunk.
6. Post-vine pickups should trace a readable recovery or reward route instead of pulling the player down toward the water.
7. Safety comes from placement and trajectory, not invulnerability, hidden death immunity, or automatic boosting.

## Runtime Design

At release, calculate the minimum vertical velocity needed to keep the player above the configured danger line after the configured survival time:

`requiredLift = (dangerY + clearance - releaseY + 0.5 * gravity * time^2) / time`

The final release lift is the greater of the authored swing result, the baseline minimum lift, and the calculated survival lift, then clamped by the controller's existing maximum vertical speed. Forward speed and release-power bonuses remain unchanged so timing still controls distance.

The release safety values remain serialized and inspector-friendly. The project builder writes the production defaults into the generated gorilla prefab.

## Authored Route Design

- Raise `LowVineRescue` while keeping it visibly lower than the safe and high-vine variants.
- Raise and normalize all standard vine placements to the shared minimum-grab-height contract.
- Block `Hazard` and `Predator` tags immediately after vine chunks.
- Lift the low-vine reward pickup into the natural release corridor.
- Validate every main-pool vine spawn against the vine prefab's real grab-point offset.

## Fuel HUD Design

Replace the single flat bar with a compact jungle canister gauge:

- charcoal instrument body with restrained brass edge and top rail;
- dedicated gas-cloud icon well;
- separate label, number, and meter lanes;
- ten stable luminous segments over a dark glass track;
- green normal state, cool full-state shimmer, and amber-red low-fuel pulse;
- shorter `LOW FUEL` warning copy that cannot collide with the number;
- smooth value animation and existing refill/failure punch feedback;
- fixed responsive dimensions that remain legible at desktop and landscape-phone sizes.

Segmented meter support belongs in the reusable Arcade Framework `MeterFillUI`; Gassy Gorilla's icon, copy, colors, and layout remain game-specific.

## Release Validation

1. Automated scene validation rejects unsafe vine grab heights, missing post-vine hazard blocks, or release settings that fail the one-second ballistic contract.
2. The fuel HUD must contain ten wired segments, a dedicated icon, separate label/value lanes, and no text overflow.
3. Unity compiles without errors and all existing scene, asset, geometry, audio, and route validators pass.
4. Desktop and landscape-mobile WebGL playtests repeatedly grab, hold, and release low vines without an unavoidable water loss.
5. A strong forward release travels visibly farther than a weak release.
6. A weak release remains recoverable with one boost.
7. The published build retains orientation gating, acceptable frame pacing, working audio, and clean browser console output.
