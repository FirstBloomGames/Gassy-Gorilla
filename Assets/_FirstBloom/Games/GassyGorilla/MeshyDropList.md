# Gassy Gorilla Meshy Drop List

Use this as the first 3D art pass for a premium 2.5D arcade look. Keep the models stylized, chunky, readable from a side-view camera, and brighter than realistic jungle art. Export one clean FBX or GLB per model with named materials, no baked floor, origin at bottom center, and the model facing right.

## Global Style Prompt

Stylized 3D arcade game asset, hand-painted tropical jungle look, chunky readable silhouette, soft rounded forms, playful comedy tone, saturated greens and warm browns, clean topology, low-to-mid poly, mobile friendly, no photorealism, no horror, no gritty texture, no dark realism, no background, no base pedestal.

## Priority 1 - Hero Gorilla Pose Pack

Make these first. They will change the game feel the most.

- `GG_HeroGorilla_Idle`
- `GG_HeroGorilla_Boost`
- `GG_HeroGorilla_Swing`

Prompt:
Stylized heroic cartoon gorilla for a premium 2.5D arcade runner, funny expressive face, strong rounded body, warm brown fur, tan belly and muzzle, slightly goofy but brave, readable side-view silhouette, facing right, hand-painted texture, Apple Arcade quality, clean mobile game asset, no background.

Notes:
- If Meshy can rig it cleanly, make one rigged gorilla.
- If not, make three separate pose models matching idle, upward boost, and vine swing.
- Keep hands and face oversized for readability.

## Priority 2 - Glowing Swing Vine Pack

These should replace the flat vine and become a signature mechanic.

- `GG_Vine_Short_Glow`
- `GG_Vine_Medium_Glow`
- `GG_Vine_Long_Glow`
- `GG_Vine_GrabCluster_Glow`

Prompt:
Stylized twisted jungle vine for a 2.5D arcade runner, thick braided vine, bright glowing green leaves around the grab point, friendly magical bioluminescent cue, readable side-view silhouette, hand-painted texture, premium mobile game asset, no background.

Notes:
- Make the grab cluster very obvious from a distance.
- The vine should look safe and fun, not threatening.

## Priority 3 - Food Pickup Pack

These are constant reward objects, so they should be juicy and toy-like.

- `GG_Pickup_Bean`
- `GG_Pickup_Burrito`
- `GG_Pickup_BananaBunch`
- `GG_Pickup_SodaCan`

Prompt:
Stylized collectible food pickup for a funny jungle arcade runner, chunky toy-like shape, hand-painted texture, bright rim highlight, readable from far away, slightly exaggerated proportions, premium mobile game asset, no background.

Notes:
- Use separate files for each pickup.
- Add a small glow ring or gem-like highlight if Meshy handles it well.

## Priority 4 - Foreground Jungle Kit

This will give the ground and lower screen depth without blocking gameplay.

- `GG_Foreground_Fern_A`
- `GG_Foreground_Fern_B`
- `GG_Foreground_BroadLeaf_A`
- `GG_Foreground_RootCluster_A`
- `GG_GroundEdge_GrassChunk_A`
- `GG_GroundEdge_GrassChunk_B`

Prompt:
Stylized modular tropical jungle foreground prop for a 2.5D side-scrolling arcade game, big readable leaves, rounded hand-painted shapes, bright but not noisy, mobile friendly, clean silhouette, no background.

Notes:
- These should be modular pieces we can repeat.
- Keep them low enough that they do not hide the gorilla, food, or vines.

## Priority 5 - Obstacle Hazard Pack

The game needs hazards that read instantly without feeling mean.

- `GG_Hazard_ThornLog`
- `GG_Hazard_SpikyStump`
- `GG_Hazard_MudGeyser`
- `GG_Hazard_StickySapBlob`

Prompt:
Stylized jungle obstacle for a comedy arcade runner, readable hazard shape, rounded toy-like forms, clear danger cues, warm hand-painted texture, playful not scary, side-view game asset, no background.

Notes:
- Hazards should be funny and readable, not realistic.
- Add bright tips, shine, or motion-friendly shapes so players understand them quickly.

## Priority 6 - Background Canopy Parallax Kit

This upgrades the world from flat backdrop to layered jungle.

- `GG_BG_CanopyCluster_A`
- `GG_BG_CanopyCluster_B`
- `GG_BG_DistantTreeTrunk_A`
- `GG_BG_HangingLeaves_A`

Prompt:
Stylized tropical jungle background prop for a 2.5D arcade runner, broad simple forms, soft hand-painted texture, layered parallax friendly, low detail, readable silhouette, premium mobile game look, no background.

Notes:
- Background pieces should be calmer and less saturated than pickups/vines.
- Make large shapes, not tiny detail.

## Bonus - Main Menu Diorama

Make this after the gameplay objects.

- `GG_Menu_JunglePlatform`
- `GG_Menu_FoodPile`
- `GG_Menu_ComedyFartCloud`

Prompt:
Stylized 3D main menu diorama prop for a funny jungle arcade game, playful premium mobile look, rounded hand-painted shapes, bright and polished, no background.

## Import Targets

Drop finished models into:

`Assets/_FirstBloom/Games/GassyGorilla/Models/Meshy`

Use extracted Meshy folders, not just the zip file. The folder name or model file name should contain one of the exact target names below. The project builder will auto-detect supported names and wire the model into the generated prefabs/scenes while preserving the gameplay colliders.

Current art rule: gameplay/world visuals are 3D-only. Missing art gets a simple 3D placeholder mesh, not a 2D sprite fallback.

Currently auto-wired:

- Hero: `GG_HeroGorilla`
- Pickups: `GG_Pickup_Bean`, `GG_Pickup_Burrito`, `GG_Pickup_SodaCan`, `GG_Pickup_Soda`, `GG_Pickup_BananaBunch`
- Vines: `GG_Vine_Medium_Glow`, `GG_Vine_Short_Glow`, `GG_Vine_Long_Glow`
- Hazards: `GG_Hazard_ThornLog`, `GG_Hazard_SpikyStump`, `GG_Hazard_MudGeyser`, `GG_Hazard_StickySapBlob`
- Foreground: `GG_Foreground_Fern_A`, `GG_Foreground_Fern_B`, `GG_Foreground_BroadLeaf_A`, `GG_Foreground_RootCluster_A`
- Ground trim: `GG_GroundEdge_GrassChunk_A`, `GG_GroundEdge_GrassChunk_B`
- Background: `GG_BG_CanopyCluster_A`, `GG_BG_CanopyCluster_B`, `GG_BG_DistantTreeTrunk_A`, `GG_BG_HangingLeaves_A`
- Menu: `GG_Menu_JunglePlatform`, `GG_Menu_FoodPile`, `GG_Menu_ComedyFartCloud`

Generated prefabs live under:

`Assets/_FirstBloom/Games/GassyGorilla/Prefabs`

## First Batch To Generate

1. `GG_HeroGorilla_Idle`
2. `GG_HeroGorilla_Boost`
3. `GG_HeroGorilla_Swing`
4. `GG_Vine_Medium_Glow`
5. `GG_Pickup_Bean`
6. `GG_Pickup_Burrito`
7. `GG_Foreground_Fern_A`
8. `GG_Hazard_ThornLog`
