# Meshy Imports

Drop extracted Meshy FBX or GLB folders here first.

Unity does not use the `.zip` directly. If Meshy gives you a zip, extract it into this folder so the folder or model file name still contains the target name from `MeshyDropList.md`, such as `GG_Pickup_Bean` or `GG_Vine_Medium_Glow`.

Keep one model per folder, keep the main color texture beside the model, and avoid baked floors or large background chunks.

The project builder now auto-detects supported Meshy names and wires them into the generated prefabs and scenes. Gassy Gorilla is now 3D-only for gameplay/world visuals: if a supported 3D model is missing, the builder uses a simple 3D placeholder mesh instead of a sprite.
