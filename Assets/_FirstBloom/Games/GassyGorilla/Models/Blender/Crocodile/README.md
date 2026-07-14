# Gassy Gorilla Crocodile

Stylized Blender-authored lagoon crocodile for Gassy Gorilla.

- Forward axis: local +X
- Up axis: local +Z in Blender, converted to Unity Y-up by FBX export
- Approximate triangles: 6632
- Texture: one 1024x1024 embedded color atlas
- Root motion: armature object remains stationary
- Materials: 1 atlas-backed slot
- Runtime FBX: `GG_Crocodile_Rigged.fbx` contains every clip below

## Clips

- `Idle_Submerged`: frames 1-61 at 30 FPS
- `Lunge_Snap`: frames 1-24 at 30 FPS
- `Settle_Submerge`: frames 1-36 at 30 FPS

`Idle_Submerged` should loop. `Lunge_Snap` and `Settle_Submerge` should not loop.

## Unity Finish Contract

- Keep the crocodile inactive during normal play.
- Activate it only for a lagoon fall and animate with unscaled time.
- Play `Lunge_Snap` immediately after the water impact.
- At 0.46 seconds, hide the gorilla renderers and play the chomp sound.
- At 0.72 seconds, play `Settle_Submerge`.
- Reveal the result panel no earlier than 1.02 seconds.
- Retry must restore the gorilla, hide the crocodile, and reset both animations.

The runtime instance must keep one skinned renderer, one material, no root
motion, and no more than 7,000 triangles. The lagoon reflection remains a
lightweight proxy; do not add a reflection camera or duplicate animated hero.
