using System;
using System.Collections.Generic;
using FirstBloom.ArcadeFramework.Audio;
using FirstBloom.ArcadeFramework.Camera;
using FirstBloom.ArcadeFramework.Scoring;
using FirstBloom.ArcadeFramework.Spawning;
using FirstBloom.ArcadeFramework.UI;
using FirstBloom.ArcadeFramework.VFX;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FirstBloom.Games.GassyGorilla.EditorTools
{
    public static class GassyGorillaSceneValidator
    {
        private const string GameRoot = "Assets/_FirstBloom/Games/GassyGorilla";
        private const string MainMenuScenePath = GameRoot + "/Scenes/MainMenu.unity";
        private const string GameScenePath = GameRoot + "/Scenes/Game.unity";
        private const string MeshyGorillaModelPath = GameRoot + "/Models/Meshy/Meshy_AI_GG_HeroGorilla_Rigged_biped/Meshy_AI_GG_HeroGorilla_Rigged_biped_Character_output.fbx";
        private const string MeshyGorillaAnimatorPath = GameRoot + "/Animations/GG_HeroGorilla.controller";
        private const string PaintedJungleTexturePath = GameRoot + "/Textures/Generated3D/GG_JungleBackdrop_Painted3D_v1.png";

        [MenuItem("First Bloom/Gassy Gorilla/Validate Built Scenes")]
        public static void ValidateBuiltScenes()
        {
            List<string> errors = new List<string>();

            ValidateRequiredAssets(errors);
            ValidateMainMenu(errors);
            ValidateGameScene(errors);
            ValidateGeneratedPrefabs(errors);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException("Gassy Gorilla scene validation failed:\n - " + string.Join("\n - ", errors));
            }

            Debug.Log("Gassy Gorilla scene validation passed. Menu, authored run chunks, textured 3D world art, HUD, camera, tutorial, and game loop are wired.");
        }

        private static void ValidateRequiredAssets(List<string> errors)
        {
            RequireAsset(MainMenuScenePath, errors);
            RequireAsset(GameScenePath, errors);
            RequireAsset(PaintedJungleTexturePath, errors);

            if (HasMeshyGorilla())
            {
                RequireAsset(MeshyGorillaAnimatorPath, errors);
                ValidateVineReleaseAnimation(errors);
            }
        }

        private static void ValidateVineReleaseAnimation(List<string> errors)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MeshyGorillaAnimatorPath);
            if (controller == null || controller.layers.Length == 0)
            {
                return;
            }

            ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state == null || state.name != "VineRelease")
                {
                    continue;
                }

                if (state.motion == null)
                {
                    errors.Add("VineRelease animator state has no animation clip.");
                    return;
                }

                string motionName = state.motion.name.ToLowerInvariant();
                string motionPath = AssetDatabase.GetAssetPath(state.motion).ToLowerInvariant();
                if (motionName.Contains("walking") || motionPath.Contains("animation_walking"))
                {
                    errors.Add("VineRelease is incorrectly using a walking animation take.");
                }

                return;
            }

            errors.Add("Generated gorilla animator is missing the VineRelease state.");
        }

        private static void ValidateMainMenu(List<string> errors)
        {
            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            RequireComponent<ArcadeAudioManager>("Main menu audio manager", errors);
            RequireComponent<ArcadeTimeController>("Main menu time manager", errors);
            RequireComponent<MainMenuController>("Main menu controller", errors);
            RequireComponent<ArcadeSettingsMenu>("Main menu settings menu", errors);
            RequireComponent<CanvasScaler>("Main menu canvas scaler", errors);
            RequireSceneObject("Menu_GorillaHero", errors);
            RequireSceneObject("Menu_FartCloud", errors);
            RequireSceneObject("Menu_PaintedJungleBackdrop_3D", errors);
            RequireSceneObject("World_KeyLight_Menu", errors);

            ValidateRenderableVisual("Menu_GorillaHero", errors);
            ValidateRenderableVisual("Menu_FartCloud", errors);
            RequireNoSpriteRenderers("Main menu scene", errors);
            RequireTexturedWorldRenderers("Main menu scene", errors);
        }

        private static void ValidateGameScene(List<string> errors)
        {
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            RequireComponent<ArcadeAudioManager>("Game audio manager", errors);
            RequireComponent<ArcadeTimeController>("Game time manager", errors);
            RequireComponent<GassyGorillaGameManager>("Game manager", errors);
            RequireComponent<GassyScoreManager>("Score manager", errors);
            RequireComponent<DistanceScoreTracker>("Distance tracker", errors);
            RequireComponent<SmoothCameraFollow2D>("Camera follow", errors);
            RequireComponent<FartBarUI>("Fart fuel bar", errors);
            RequireComponent<GassyTutorialPromptController>("Tutorial prompt controller", errors);
            RequireComponent<MilestoneEventManager>("Milestone manager", errors);
            RequireComponent<TextOverlay>("Tutorial text overlay", errors);

            RunChunkDirector runDirector = RequireComponent<RunChunkDirector>("Run chunk director", errors);
            if (runDirector != null)
            {
                runDirector.AppendValidationErrors(errors, 100);
            }

            RequireNoLegacyGameplaySpawners(errors);

            GorillaController gorilla = RequireComponent<GorillaController>("Player gorilla", errors);
            if (gorilla != null)
            {
                ValidatePlayerPolish(gorilla, errors);
            }

            RequireSceneObject("HUD_FartBar", errors);
            RequireSceneObject("TutorialOverlay", errors);
            RequireSceneObject("Director_RunChunks", errors);
            RequireSceneObject("PaintedJungleBackdrop_3D", errors);
            RequireSceneObject("DeathZone", errors);
            RequireSceneObject("World_KeyLight_Game", errors);
            RequireSceneObjectAbsent("Ground_3D", errors);
            RequireSceneObjectAbsent("Distant_MeshyForestDepth_3D", errors);
            RequireSceneObjectAbsent("Foreground_3DDecor", errors);

            RequireNoSpriteRenderers("Game scene", errors);
            RequireTexturedWorldRenderers("Game scene", errors);
        }

        private static void RequireNoLegacyGameplaySpawners(List<string> errors)
        {
            PickupSpawner[] pickupSpawners = UnityEngine.Object.FindObjectsByType<PickupSpawner>(FindObjectsInactive.Include);
            ObstacleSpawner[] obstacleSpawners = UnityEngine.Object.FindObjectsByType<ObstacleSpawner>(FindObjectsInactive.Include);
            VineSwingSpawner[] vineSpawners = UnityEngine.Object.FindObjectsByType<VineSwingSpawner>(FindObjectsInactive.Include);

            if (pickupSpawners.Length > 0 || obstacleSpawners.Length > 0 || vineSpawners.Length > 0)
            {
                errors.Add("Game scene still contains independent collision-critical spawners. Use the authored run chunk director instead.");
            }
        }

        private static void ValidatePlayerPolish(GorillaController gorilla, List<string> errors)
        {
            if (FindChild(gorilla.transform, "Speed Line Burst") == null)
            {
                errors.Add("Player gorilla is missing the boost/release speed line burst.");
            }

            if (FindChild(gorilla.transform, "Fart Cloud Burst") == null)
            {
                errors.Add("Player gorilla is missing the 3D fart cloud burst.");
            }

            if (gorilla.GetComponentInChildren<GorillaModelVisualController>(true) == null)
            {
                errors.Add("Player gorilla is missing the 3D Meshy visual controller.");
            }

            if (FindChild(gorilla.transform, "Visual_Gorilla_3D") == null)
            {
                errors.Add("Player gorilla is missing the 3D model child.");
            }

            LagoonFinishPresentation lagoonFinish = gorilla.GetComponent<LagoonFinishPresentation>();
            if (lagoonFinish == null)
            {
                errors.Add("Player gorilla is missing the lightweight lagoon reflection and impact presentation.");
            }

            Transform reflection = FindChild(gorilla.transform, "Lagoon Reflection 3D");
            if (reflection == null || reflection.GetComponentsInChildren<Renderer>(true).Length < 4)
            {
                errors.Add("Player gorilla lagoon reflection is missing its textured 3D silhouette pieces.");
            }

            Transform impact = FindChild(gorilla.transform, "Lagoon Water Impact FX");
            if (impact == null)
            {
                errors.Add("Player gorilla is missing the lagoon water impact FX root.");
            }
            else
            {
                ParticleSystem[] impactParticles = impact.GetComponentsInChildren<ParticleSystem>(true);
                int maxParticleBudget = 0;
                for (int i = 0; i < impactParticles.Length; i++)
                {
                    ParticleSystem.MainModule main = impactParticles[i].main;
                    maxParticleBudget += main.maxParticles;
                    if (main.loop || !main.useUnscaledTime)
                    {
                        errors.Add("Lagoon impact particles must be finite and run on unscaled game-over time.");
                        break;
                    }
                }

                if (impactParticles.Length != 3 || maxParticleBudget > 64)
                {
                    errors.Add("Lagoon impact must keep exactly three bounded particle systems within the 64-particle mobile budget.");
                }
            }

            if (gorilla.GetComponentsInChildren<UnityEngine.Camera>(true).Length > 0)
            {
                errors.Add("Lagoon reflection must not use an additional camera.");
            }

            ValidateRenderableVisual("Visual_Gorilla_3D", errors);
            ValidateAnimatedGorilla("Visual_Gorilla_3D", errors);
            ValidateRenderableVisual("Fart Cloud Burst", errors);
        }

        private static bool HasMeshyGorilla()
        {
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MeshyGorillaModelPath) != null;
        }

        private static void ValidateRenderableVisual(string objectName, List<string> errors)
        {
            GameObject model = FindSceneObject(objectName);
            if (model == null)
            {
                return;
            }

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                errors.Add(objectName + " has no 3D renderers.");
            }
        }

        private static void ValidateAnimatedGorilla(string objectName, List<string> errors)
        {
            GameObject model = FindSceneObject(objectName);
            if (model == null || !HasMeshyGorilla())
            {
                return;
            }

            Animator animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                errors.Add(objectName + " has no Animator.");
            }
            else if (animator.runtimeAnimatorController == null)
            {
                errors.Add(objectName + " has no generated animator controller assigned.");
            }
        }

        private static void ValidateGeneratedPrefabs(List<string> errors)
        {
            string[] prefabPaths =
            {
                GameRoot + "/Prefabs/Player_GassyGorilla.prefab",
                GameRoot + "/Prefabs/Pickup_Bean.prefab",
                GameRoot + "/Prefabs/Pickup_Burrito.prefab",
                GameRoot + "/Prefabs/Pickup_Soda.prefab",
                GameRoot + "/Prefabs/Pickup_BananaBunch.prefab",
                GameRoot + "/Prefabs/Vine_Swingable.prefab",
                GameRoot + "/Prefabs/Vine_Obstacle.prefab",
                GameRoot + "/Prefabs/Obstacle_TreeTrunk.prefab",
                GameRoot + "/Prefabs/Hazard_SpikyStump.prefab",
                GameRoot + "/Prefabs/Hazard_MudGeyser.prefab",
                GameRoot + "/Prefabs/Hazard_StickySapBlob.prefab"
            };

            for (int i = 0; i < prefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                if (prefab == null)
                {
                    errors.Add("Generated prefab is missing: " + prefabPaths[i]);
                    continue;
                }

                SpriteRenderer[] spriteRenderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
                if (spriteRenderers.Length > 0)
                {
                    errors.Add(prefab.name + " still contains SpriteRenderer components. Gameplay/world visuals must be 3D-only.");
                }

                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                if (prefab.name != "Manager_Game" && renderers.Length == 0 && !prefab.name.StartsWith("Spawner", StringComparison.Ordinal))
                {
                    errors.Add(prefab.name + " has no 3D renderers.");
                }

                ValidateTexturedRenderers(renderers, "Prefab " + prefab.name, errors);
            }

            ValidateTopAnchoredVinePrefab(errors);
        }

        private static void ValidateTopAnchoredVinePrefab(List<string> errors)
        {
            GameObject vine = AssetDatabase.LoadAssetAtPath<GameObject>(GameRoot + "/Prefabs/Vine_Swingable.prefab");
            if (vine == null)
            {
                return;
            }

            Transform pivot = vine.transform.Find("PivotPoint");
            Transform swingRoot = vine.transform.Find("PivotPoint/VineSwingRoot");
            Transform connector = vine.transform.Find("PivotPoint/VineSwingRoot/VineConnector_FromCanopy_3D");
            Transform grabPoint = vine.transform.Find("PivotPoint/VineSwingRoot/GrabPoint");

            if (pivot == null)
            {
                errors.Add("Vine_Swingable has no high canopy PivotPoint.");
                return;
            }

            if (pivot.localPosition.y < 5.8f)
            {
                errors.Add("Vine_Swingable canopy pivot must remain above the highest gameplay camera view.");
            }

            if (swingRoot == null || connector == null || grabPoint == null)
            {
                errors.Add("Vine_Swingable must keep its connector, visible vine, and grab point under the swaying VineSwingRoot.");
            }

            if (swingRoot != null && grabPoint != null && Vector3.Distance(swingRoot.position, grabPoint.position) < 6.5f)
            {
                errors.Add("Vine_Swingable is too short to read as hanging from the upper jungle canopy.");
            }

            VineSwingTrigger trigger = vine.GetComponent<VineSwingTrigger>();
            VineSwingAnimator animator = vine.GetComponent<VineSwingAnimator>();
            if (trigger == null || animator == null)
            {
                errors.Add("Vine_Swingable is missing its magnetic catch or ambient sway component.");
            }
            else if (trigger.PivotPoint != pivot || trigger.GrabPoint != grabPoint)
            {
                errors.Add("Vine_Swingable trigger is not wired to the moving canopy pivot and grab point.");
            }
            else if (!animator.HasAmbientSway)
            {
                errors.Add("Vine_Swingable ambient canopy sway is disabled or is not wired to a visual root.");
            }
        }

        private static void RequireNoSpriteRenderers(string label, List<string> errors)
        {
            SpriteRenderer[] spriteRenderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
            if (spriteRenderers.Length == 0)
            {
                return;
            }

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                errors.Add(label + " contains SpriteRenderer on " + GetHierarchyPath(spriteRenderers[i].transform) + ". Gameplay/world visuals must be 3D-only.");
            }
        }

        private static void RequireTexturedWorldRenderers(string label, List<string> errors)
        {
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            ValidateTexturedRenderers(renderers, label, errors);
        }

        private static void ValidateTexturedRenderers(Renderer[] renderers, string label, List<string> errors)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.GetComponentInParent<Canvas>() != null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    errors.Add(label + " has a 3D renderer with no material at " + GetHierarchyPath(renderer.transform) + ".");
                    continue;
                }

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null)
                    {
                        errors.Add(label + " has a missing 3D material at " + GetHierarchyPath(renderer.transform) + ".");
                    }
                    else if (material.mainTexture == null)
                    {
                        errors.Add(label + " has an untextured 3D material '" + material.name + "' at " + GetHierarchyPath(renderer.transform) + ".");
                    }
                }
            }
        }

        private static T RequireComponent<T>(string label, List<string> errors) where T : Component
        {
            T component = FindSceneComponent<T>();
            if (component == null)
            {
                errors.Add(label + " is missing from the open scene.");
            }

            return component;
        }

        private static void RequireAsset(string path, List<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) == null)
            {
                errors.Add("Required asset is missing: " + path);
            }
        }

        private static void RequireSceneObject(string objectName, List<string> errors)
        {
            if (FindSceneObject(objectName) == null)
            {
                errors.Add("Scene object is missing: " + objectName);
            }
        }

        private static void RequireSceneObjectAbsent(string objectName, List<string> errors)
        {
            if (FindSceneObject(objectName) != null)
            {
                errors.Add("Scene object should have been removed: " + objectName);
            }
        }

        private static T FindSceneComponent<T>() where T : Component
        {
            T[] components = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            return components.Length > 0 ? components[0] : null;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindChild(roots[i].transform, objectName);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChild(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
