# Gassy Gorilla Crocodile

Stylized Blender-authored lagoon crocodile for Gassy Gorilla.

- Forward axis: local +X
- Up axis: local +Z in Blender, converted to Unity Y-up by FBX export
- Approximate triangles: 6632
- Texture: one 1024x1024 embedded color atlas
- Root motion: armature object remains stationary
- Materials: 1 atlas-backed slots
- Runtime FBX: `GG_Crocodile_Rigged.fbx` contains every clip below

## Clips

- `Idle_Submerged`: frames 1-61 at 30 FPS
- `Lunge_Snap`: frames 1-24 at 30 FPS
- `Settle_Submerge`: frames 1-36 at 30 FPS

`Idle_Submerged` should loop. `Lunge_Snap` and `Settle_Submerge` should not loop.
