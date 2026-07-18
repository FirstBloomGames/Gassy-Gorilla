# Gassy Gorilla

Gassy Gorilla is a one-touch comedy arcade runner built in Unity. The player uses fart-powered boosts to stay airborne, follows food routes to refill fuel, catches glowing jungle vines, and chases a new best distance through a textured 3D world.

This repository is the production Unity project and the first implementation of the First Bloom Arcade Framework.

The current release candidate includes:

- Endless Run plus five finite story Expeditions.
- Magnetic vine catches, player-held swinging, release momentum, crocodile ambushes, and progressive difficulty.
- Persistent best distance, Expedition stars, and eight Jungle Badges.
- Music, voice, calibrated comedic SFX, pause mixing, and volume controls.
- Pause/resume, controller-equivalent input, Reduced Motion, and optional iOS haptics.
- Optimized desktop and phone WebGL presentation with a portrait rotation gate.

## Play

[Play the current Gassy Gorilla WebGL build](https://firstbloomgames.github.io/Gassy-Gorilla/)

The optimized web release is published from the `gh-pages` branch. Production Unity source remains on `main`.

The verified WebGL package is 8.6 MB compressed and 14.6 MB uncompressed.

## Unity Version

- Unity `6000.5.2f1`
- Landscape 2.5D presentation
- WebGL phone preview and desktop editor workflows

## Project Structure

- `Assets/_FirstBloom/ArcadeFramework` contains reusable arcade systems.
- `Assets/_FirstBloom/Games/GassyGorilla` contains game-specific code, scenes, prefabs, art, audio, and tuning.
- `Packages` and `ProjectSettings` pin the reproducible Unity configuration.

## Getting Started

1. Install Git LFS.
2. Clone the repository and run `git lfs pull`.
3. Open the repository folder through Unity Hub with Unity `6000.5.2f1`.
4. Open `Assets/_FirstBloom/Games/GassyGorilla/Scenes/MainMenu.unity`.

Generated Unity folders, local builds, and original Meshy ZIP deliveries are intentionally excluded. Extracted production FBX files, textures, and audio are tracked through Git LFS.

No open-source license has been selected for this project.
