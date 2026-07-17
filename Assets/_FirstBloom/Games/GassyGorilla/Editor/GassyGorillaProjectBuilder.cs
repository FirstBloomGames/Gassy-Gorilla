using System;
using System.Collections.Generic;
using System.IO;
using FirstBloom.ArcadeFramework.Audio;
using FirstBloom.ArcadeFramework.Camera;
using FirstBloom.ArcadeFramework.Core;
using FirstBloom.ArcadeFramework.Scoring;
using FirstBloom.ArcadeFramework.Spawning;
using FirstBloom.ArcadeFramework.UI;
using FirstBloom.ArcadeFramework.VFX;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FirstBloom.Games.GassyGorilla.EditorTools
{
    public static class GassyGorillaProjectBuilder
    {
        private const string FrameworkRoot = "Assets/_FirstBloom/ArcadeFramework";
        private const string GameRoot = "Assets/_FirstBloom/Games/GassyGorilla";
        private const string PrefabRoot = GameRoot + "/Prefabs";
        private const string SpriteRoot = GameRoot + "/Sprites/Generated";
        private const string ProvidedSpriteRoot = GameRoot + "/Sprites/Provided";
        private const string ModelRoot = GameRoot + "/Models";
        private const string MeshyModelRoot = ModelRoot + "/Meshy";
        private const string TextureRoot = GameRoot + "/Textures";
        private const string GeneratedTextureRoot = TextureRoot + "/Generated3D";
        private const string ProceduralTextureRoot = GeneratedTextureRoot + "/Procedural";
        private const string RunChunkRoot = GameRoot + "/ScriptableObjects/RunChunks";
        private const string DifficultyProfilePath = GameRoot + "/ScriptableObjects/GG_RunDifficulty.asset";
        private const string AudioLibraryPath = GameRoot + "/ScriptableObjects/GG_AudioLibrary.asset";
        private const string PaintedJungleTexturePath = GeneratedTextureRoot + "/GG_JungleBackdrop_Painted3D_v1.png";
        private const string MeshyGorillaFolder = MeshyModelRoot + "/Meshy_AI_GG_HeroGorilla_Rigged_biped";
        private const string MeshyGorillaModelPath = MeshyGorillaFolder + "/Meshy_AI_GG_HeroGorilla_Rigged_biped_Character_output.fbx";
        private const string MeshyGorillaAnimationPath = MeshyGorillaFolder + "/Meshy_AI_GG_HeroGorilla_Rigged_biped_Meshy_AI_Meshy_Merged_Animations.fbx";
        private const string MeshyGorillaTexturePath = MeshyGorillaFolder + "/Meshy_AI_GG_HeroGorilla_Rigged_biped_texture_0.png";
        private const string MeshyGorillaMaterialPath = GameRoot + "/Materials/GG_HeroGorilla_Meshy.mat";
        private const string MeshyGorillaAnimatorPath = GameRoot + "/Animations/GG_HeroGorilla.controller";
        private const string CrocodileFolder = ModelRoot + "/Blender/Crocodile";
        private const string CrocodileModelPath = CrocodileFolder + "/GG_Crocodile_Rigged.fbx";
        private const string CrocodileTexturePath = CrocodileFolder + "/GG_Crocodile_Atlas.png";
        private const string CrocodileMaterialPath = GameRoot + "/Materials/GG_Crocodile_Blender.mat";
        private const string CrocodileAnimatorPath = GameRoot + "/Animations/GG_Crocodile.controller";
        private const string VoiceRoot = GameRoot + "/Audio/Voice";
        private const string MainMenuScenePath = GameRoot + "/Scenes/MainMenu.unity";
        private const string GameScenePath = GameRoot + "/Scenes/Game.unity";
        private static Mesh sphereParticleMesh;
        private static Mesh cubeParticleMesh;

        [MenuItem("First Bloom/Gassy Gorilla/Build 1.0 Playable")]
        public static void BuildAll()
        {
            EnsureFolders();
            EnsureProject2DSettings();
            EnsureTags();
            MoveVoiceClipsIntoGameFolder();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            PreparePaintedJungleTexture();
            GassyGorillaAudioAssetGenerator.GenerateProductionAudioAssets();

            SpriteSet sprites = GenerateSprites();
            GorillaModelAssets gorillaModel = PrepareMeshyGorillaAssets();
            CrocodileModelAssets crocodileModel = PrepareCrocodileAssets();
            MeshyGameAssets meshyAssets = PrepareMeshyGameAssets();
            PrefabSet prefabs = BuildPrefabs(sprites, gorillaModel, crocodileModel, meshyAssets);
            RunDifficultyProfile difficultyProfile = BuildRunDifficultyProfile();
            RunChunkSet runChunks = BuildRunChunkDefinitions(prefabs);

            BuildMainMenuScene(sprites, gorillaModel, meshyAssets);
            BuildGameScene(sprites, prefabs, meshyAssets, runChunks, difficultyProfile);
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Gassy Gorilla 1.0 playable framework, prefabs, and scenes have been rebuilt.");
        }

        [MenuItem("First Bloom/Gassy Gorilla/Repair Scene Audio Listeners")]
        public static void RepairSceneAudioListeners()
        {
            EnsureSceneAudioListener(MainMenuScenePath);
            EnsureSceneAudioListener(GameScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Gassy Gorilla scene audio listeners are repaired and ready for validation.");
        }

        [MenuItem("First Bloom/Gassy Gorilla/Rebuild Authored Run Chunks")]
        public static void RebuildRunChunksOnly()
        {
            EnsureFolders();
            PrefabSet prefabs = LoadRunChunkPrefabs();
            RunDifficultyProfile difficultyProfile = BuildRunDifficultyProfile();
            RunChunkSet runChunks = BuildRunChunkDefinitions(prefabs);

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            RunChunkDirector[] directors = UnityEngine.Object.FindObjectsByType<RunChunkDirector>(FindObjectsInactive.Include);
            if (directors.Length == 0)
            {
                throw new InvalidOperationException("Game scene is missing Director_RunChunks. Run Build 1.0 Playable once.");
            }

            GorillaController[] gorillas = UnityEngine.Object.FindObjectsByType<GorillaController>(FindObjectsInactive.Include);
            if (gorillas.Length == 0)
            {
                throw new InvalidOperationException("Game scene is missing Player_GassyGorilla.");
            }

            directors[0].ConfigureContent(gorillas[0].transform, runChunks.All, runChunks.Opening);
            directors[0].ConfigureDifficulty(gorillas[0], difficultyProfile);
            EditorUtility.SetDirty(directors[0]);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Gassy Gorilla authored run chunks were rebuilt without reimporting the full Meshy art library.");
        }

        private static PrefabSet LoadRunChunkPrefabs()
        {
            return new PrefabSet
            {
                Bean = LoadRequiredPrefab("Pickup_Bean"),
                Burrito = LoadRequiredPrefab("Pickup_Burrito"),
                Soda = LoadRequiredPrefab("Pickup_Soda"),
                BananaBunch = LoadRequiredPrefab("Pickup_BananaBunch"),
                SwingableVine = LoadRequiredPrefab("Vine_Swingable"),
                SpikyStumpObstacle = LoadRequiredPrefab("Hazard_SpikyStump"),
                CrocodileAmbush = LoadRequiredPrefab("Hazard_CrocodileAmbush")
            };
        }

        private static GameObject LoadRequiredPrefab(string name)
        {
            string path = PrefabRoot + "/" + name + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException("Required generated prefab is missing: " + path);
            }

            return prefab;
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                FrameworkRoot + "/Scripts/Audio",
                FrameworkRoot + "/Scripts/Camera",
                FrameworkRoot + "/Scripts/Core",
                FrameworkRoot + "/Scripts/Input",
                FrameworkRoot + "/Scripts/Save",
                FrameworkRoot + "/Scripts/Scoring",
                FrameworkRoot + "/Scripts/Spawning",
                FrameworkRoot + "/Scripts/UI",
                FrameworkRoot + "/Scripts/VFX",
                FrameworkRoot + "/Prefabs",
                FrameworkRoot + "/UI",
                FrameworkRoot + "/Audio",
                GameRoot + "/Scripts",
                GameRoot + "/Scenes",
                GameRoot + "/Prefabs",
                GameRoot + "/Sprites/Generated",
                GameRoot + "/Sprites/Provided",
                GameRoot + "/Animations",
                GameRoot + "/Audio/Music",
                GameRoot + "/Audio/SFX",
                GameRoot + "/Audio/Voice",
                GameRoot + "/UI",
                GameRoot + "/Materials",
                GameRoot + "/Models",
                GameRoot + "/Models/Meshy",
                GameRoot + "/Textures",
                GameRoot + "/Textures/Generated3D",
                GameRoot + "/Textures/Generated3D/Procedural",
                GameRoot + "/ScriptableObjects",
                RunChunkRoot,
                GameRoot + "/Editor"
            };

            for (int i = 0; i < folders.Length; i++)
            {
                EnsureAssetFolder(folders[i]);
            }
        }

        private static void EnsureProject2DSettings()
        {
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        }

        private static void EnsureTags()
        {
            AddTagIfMissing("Player");
            AddTagIfMissing("Obstacle");
            AddTagIfMissing("Pickup");
            AddTagIfMissing("Vine");
            AddTagIfMissing("DeathZone");
        }

        private static void MoveVoiceClipsIntoGameFolder()
        {
            MoveAssetIfNeeded("Assets/Audio/voiceline_0_home_for_dinner_heroic.wav", VoiceRoot + "/voiceline_0_home_for_dinner_heroic.wav");
            MoveAssetIfNeeded("Assets/Audio/voiceline_1_shiny_gem_heroic.wav", VoiceRoot + "/voiceline_1_shiny_gem_heroic.wav");
            MoveAssetIfNeeded("Assets/Audio/voiceline_2_perfect_food_heroic.wav", VoiceRoot + "/voiceline_2_perfect_food_heroic.wav");
        }

        private static SpriteSet GenerateSprites()
        {
            SpriteSet set = new SpriteSet();
            set.UiPanel = CreateSoftPanelSprite("ui_panel_clean_v1", 64, 64, new Color32(14, 34, 27, 238), new Color32(54, 111, 72, 232));
            set.FuelBar = CreateFartFuelIconSprite();
            set.FuelSegment = CreateSoftPanelSprite("ui_meter_segment_v1", 32, 16, new Color32(248, 252, 244, 255), new Color32(184, 199, 179, 255));
            AssetDatabase.SaveAssets();
            return set;
        }

        private static void PreparePaintedJungleTexture()
        {
            if (!File.Exists(ToFullPath(PaintedJungleTexturePath)))
            {
                Debug.LogWarning("Painted jungle texture is missing: " + PaintedJungleTexturePath);
                return;
            }

            AssetDatabase.ImportAsset(PaintedJungleTexturePath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(PaintedJungleTexturePath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.npotScale = TextureImporterNPOTScale.ToNearest;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 1;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static GorillaModelAssets PrepareMeshyGorillaAssets()
        {
            bool hasModel = File.Exists(ToFullPath(MeshyGorillaModelPath));
            bool hasAnimations = File.Exists(ToFullPath(MeshyGorillaAnimationPath));
            string vineReleaseAnimationPath = FindMeshyAnimationPath("GG_HeroGorilla_VineRelease", "VineRelease");
            if (!hasModel && !hasAnimations)
            {
                return new GorillaModelAssets();
            }

            ConfigureMeshyTextureImporter(MeshyGorillaTexturePath, true);
            ConfigureMeshyModelImporter(MeshyGorillaModelPath, false);
            ConfigureMeshyModelImporter(MeshyGorillaAnimationPath, true);
            ConfigureMeshyModelImporter(vineReleaseAnimationPath, true);

            Material material = CreateMeshyGorillaMaterial();
            RuntimeAnimatorController controller = CreateMeshyGorillaAnimatorController();
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MeshyGorillaModelPath);
            if (modelPrefab == null)
            {
                modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MeshyGorillaAnimationPath);
            }

            if (modelPrefab != null)
            {
                Debug.Log("Meshy gorilla model wired from " + AssetDatabase.GetAssetPath(modelPrefab));
            }

            return new GorillaModelAssets
            {
                ModelPrefab = modelPrefab,
                Material = material,
                AnimatorController = controller
            };
        }

        private static CrocodileModelAssets PrepareCrocodileAssets()
        {
            if (!File.Exists(ToFullPath(CrocodileModelPath)) || !File.Exists(ToFullPath(CrocodileTexturePath)))
            {
                throw new InvalidOperationException("The Blender crocodile model or texture atlas is missing from " + CrocodileFolder + ".");
            }

            ConfigureCrocodileTextureImporter();
            ConfigureCrocodileModelImporter();

            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrocodileModelPath);
            Material material = CreateCrocodileMaterial();
            RuntimeAnimatorController controller = CreateCrocodileAnimatorController();
            if (modelPrefab == null || material == null || controller == null)
            {
                throw new InvalidOperationException("The Blender crocodile did not import as a complete textured, animated Unity asset.");
            }

            Debug.Log("Blender crocodile ready: one 6.6k-triangle skinned mesh, one atlas material, and three finish clips.");
            return new CrocodileModelAssets
            {
                ModelPrefab = modelPrefab,
                Material = material,
                AnimatorController = controller
            };
        }

        private static void ConfigureCrocodileTextureImporter()
        {
            AssetDatabase.ImportAsset(CrocodileTexturePath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(CrocodileTexturePath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 2;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void ConfigureCrocodileModelImporter()
        {
            AssetDatabase.ImportAsset(CrocodileModelPath, ImportAssetOptions.ForceUpdate);
            ModelImporter importer = AssetImporter.GetAtPath(CrocodileModelPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.preserveHierarchy = true;
            importer.optimizeGameObjects = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Off;

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips != null)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i].name = NormalizeCrocodileClipName(clips[i].name);
                    clips[i].loopTime = clips[i].name == "Idle_Submerged";
                    clips[i].loopPose = clips[i].loopTime;
                }

                importer.clipAnimations = clips;
            }

            importer.SaveAndReimport();
        }

        private static string NormalizeCrocodileClipName(string clipName)
        {
            string[] expected = { "Idle_Submerged", "Lunge_Snap", "Settle_Submerge" };
            for (int i = 0; i < expected.Length; i++)
            {
                if (!string.IsNullOrEmpty(clipName) && clipName.IndexOf(expected[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return expected[i];
                }
            }

            return clipName;
        }

        private static Material CreateCrocodileMaterial()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CrocodileTexturePath);
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (texture == null || shader == null)
            {
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(CrocodileMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, CrocodileMaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.24f);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0.24f);
            }

            material.enableInstancing = true;
            material.renderQueue = -1;
            material.SetOverrideTag("RenderType", "Opaque");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static RuntimeAnimatorController CreateCrocodileAnimatorController()
        {
            AnimationClip idle = LoadCrocodileAnimationClip("Idle_Submerged");
            AnimationClip lunge = LoadCrocodileAnimationClip("Lunge_Snap");
            AnimationClip settle = LoadCrocodileAnimationClip("Settle_Submerge");
            if (idle == null || lunge == null || settle == null)
            {
                throw new InvalidOperationException("GG_Crocodile_Rigged.fbx must contain Idle_Submerged, Lunge_Snap, and Settle_Submerge clips.");
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CrocodileAnimatorPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(CrocodileAnimatorPath);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            ClearAnimatorStateMachine(stateMachine);
            AnimatorState idleState = AddAnimatorState(stateMachine, "Idle_Submerged", idle);
            AddAnimatorState(stateMachine, "Lunge_Snap", lunge);
            AddAnimatorState(stateMachine, "Settle_Submerge", settle);
            stateMachine.defaultState = idleState;
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip LoadCrocodileAnimationClip(string clipName)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(CrocodileModelPath);
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip != null && clip.name.Equals(clipName, StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }

            return null;
        }

        private static MeshyGameAssets PrepareMeshyGameAssets()
        {
            MeshyGameAssets assets = new MeshyGameAssets
            {
                Bean = PrepareGenericMeshyModelWithKey(
                    "GG_Pickup_Bean",
                    "GG_Pickup_Bean_Optimized",
                    "GG_Pickup_Bean"),
                Burrito = PrepareGenericMeshyModelWithKey(
                    "GG_Pickup_Burrito",
                    "GG_Pickup_Burrito_Optimized",
                    "GG_Pickup_Burrito"),
                Soda = PrepareGenericMeshyModel("GG_Pickup_SodaCan", "GG_Pickup_Soda", "GG_Pickup_SodaCan_LP"),
                BananaBunch = PrepareGenericMeshyModel("GG_Pickup_BananaBunch"),
                Vine = PrepareGenericMeshyModelWithKey(
                    "GG_Vine_Medium_Glow",
                    "GG_Vine_Short_Glow_LP",
                    "GG_Vine_Medium_Glow",
                    "GG_Vine_Short_Glow",
                    "GG_Vine_Long_Glow"),
                ThornLog = PrepareGenericMeshyModel("GG_Hazard_ThornLog_LP", "GG_Hazard_ThornLog"),
                SpikyStump = PrepareGenericMeshyModel("GG_Hazard_SpikyStump"),
                MudGeyser = PrepareGenericMeshyModel("GG_Hazard_MudGeyser", "GG_Hazard_MudGeyser_L"),
                StickySapBlob = PrepareGenericMeshyModel("GG_Hazard_StickySapBlob", "GG_Hazard_StickySapBl"),
                ForegroundFernA = PrepareGenericMeshyModel("GG_Foreground_Fern_A"),
                ForegroundFernB = PrepareGenericMeshyModel("GG_Foreground_Fern_B"),
                BroadLeafA = PrepareGenericMeshyModel("GG_Foreground_BroadLeaf_A", "GG_Foreground_BroadLe"),
                RootClusterA = PrepareGenericMeshyModel("GG_Foreground_RootCluster_A", "GG_Foreground_RootClu"),
                GroundEdgeGrassA = PrepareGenericMeshyModel("GG_GroundEdge_GrassChunk_A", "GG_GroundEdge_GrassCh_0709052214"),
                GroundEdgeGrassB = PrepareGenericMeshyModel("GG_GroundEdge_GrassChunk_B", "GG_GroundEdge_GrassCh_0709052226"),
                CanopyClusterA = PrepareGenericMeshyModel("GG_BG_CanopyCluster_A"),
                CanopyClusterB = PrepareGenericMeshyModel("GG_BG_CanopyCluster_B"),
                DistantTreeTrunkA = PrepareGenericMeshyModel("GG_BG_DistantTreeTrunk_A"),
                HangingLeavesA = PrepareGenericMeshyModel("GG_BG_HangingLeaves_A"),
                MenuJunglePlatform = PrepareGenericMeshyModel("GG_Menu_JunglePlatform"),
                MenuFoodPile = PrepareGenericMeshyModel("GG_Menu_FoodPile"),
                MenuFartCloud = PrepareGenericMeshyModel("GG_Menu_ComedyFartCloud")
            };

            Debug.Log("Meshy 3D asset registry ready. Gameplay models found: " + assets.AvailableCount);
            return assets;
        }

        private static ModelVisualAsset PrepareGenericMeshyModel(params string[] keys)
        {
            string key = keys != null && keys.Length > 0 ? keys[0] : "";
            return PrepareGenericMeshyModelWithKey(key, keys);
        }

        private static ModelVisualAsset PrepareGenericMeshyModelWithKey(string key, params string[] searchKeys)
        {
            string assetPath = FindMeshyModelPath(searchKeys);
            if (string.IsNullOrEmpty(assetPath))
            {
                return new ModelVisualAsset { Key = key };
            }

            ConfigureGenericMeshyModelImporter(assetPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Material material = CreateGenericMeshyMaterial(assetPath, key);

            return new ModelVisualAsset
            {
                Key = key,
                ModelPrefab = prefab,
                Material = material,
                AssetPath = assetPath
            };
        }

        private static string FindMeshyModelPath(params string[] keys)
        {
            if (keys == null || keys.Length == 0)
            {
                return null;
            }

            string fullRoot = ToFullPath(MeshyModelRoot);
            if (!Directory.Exists(fullRoot))
            {
                return null;
            }

            string[] files = Directory.GetFiles(fullRoot, "*.*", SearchOption.AllDirectories);
            for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
            {
                string key = keys[keyIndex];
                for (int i = 0; i < files.Length; i++)
                {
                    string extension = Path.GetExtension(files[i]).ToLowerInvariant();
                    if (extension != ".fbx" && extension != ".glb" && extension != ".gltf")
                    {
                        continue;
                    }

                    string fileName = Path.GetFileNameWithoutExtension(files[i]);
                    string directoryName = Path.GetFileName(Path.GetDirectoryName(files[i]));
                    string assetPath = ToAssetPath(files[i]);
                    if (fileName.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        directoryName.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        assetPath.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return assetPath;
                    }
                }
            }

            return null;
        }

        private static string FindMeshyAnimationPath(params string[] keys)
        {
            if (keys == null || keys.Length == 0)
            {
                return null;
            }

            string fullRoot = ToFullPath(MeshyModelRoot);
            if (!Directory.Exists(fullRoot))
            {
                return null;
            }

            string[] files = Directory.GetFiles(fullRoot, "*.*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            string fallback = null;
            for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
            {
                string key = keys[keyIndex];
                for (int i = 0; i < files.Length; i++)
                {
                    string extension = Path.GetExtension(files[i]).ToLowerInvariant();
                    if (extension != ".fbx" && extension != ".glb" && extension != ".gltf")
                    {
                        continue;
                    }

                    string fileName = Path.GetFileNameWithoutExtension(files[i]);
                    string directoryName = Path.GetFileName(Path.GetDirectoryName(files[i]));
                    string assetPath = ToAssetPath(files[i]);
                    bool matchesKey = fileName.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        directoryName.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        assetPath.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!matchesKey)
                    {
                        continue;
                    }

                    if (fallback == null)
                    {
                        fallback = assetPath;
                    }

                    if (fileName.IndexOf("Animation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        fileName.IndexOf("frame_rate", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return assetPath;
                    }
                }
            }

            return fallback;
        }

        private static void ConfigureGenericMeshyModelImporter(string assetPath)
        {
            if (!File.Exists(ToFullPath(assetPath)))
            {
                return;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.preserveHierarchy = true;
            importer.SaveAndReimport();
        }

        private static Material CreateGenericMeshyMaterial(string modelPath, string key)
        {
            string texturePath = FindSiblingTexturePath(modelPath);
            if (string.IsNullOrEmpty(texturePath))
            {
                return null;
            }

            ConfigureMeshyTextureImporter(texturePath, true);
            string normalPath = FindSiblingTexturePathContaining(modelPath, "normal");
            if (!string.IsNullOrEmpty(normalPath))
            {
                ConfigureNormalTextureImporter(normalPath);
            }

            string emissionPath = FindSiblingTexturePathContaining(modelPath, "emit", "emission");
            if (!string.IsNullOrEmpty(emissionPath))
            {
                ConfigureMeshyTextureImporter(emissionPath, true);
            }

            Texture2D mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Texture2D normalTexture = !string.IsNullOrEmpty(normalPath) ? AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath) : null;
            Texture2D emissionTexture = !string.IsNullOrEmpty(emissionPath) ? AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath) : null;
            if (mainTexture == null)
            {
                return null;
            }

            string materialPath = GameRoot + "/Materials/" + key + "_Meshy.mat";
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (shader != null)
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", mainTexture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", mainTexture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            if (normalTexture != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normalTexture);
                material.SetFloat("_BumpScale", 0.72f);
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.DisableKeyword("_NORMALMAP");
            }

            bool shouldGlow = key.IndexOf("Vine", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Pickup", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Sap", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Geyser", StringComparison.OrdinalIgnoreCase) >= 0;
            if (shouldGlow && emissionTexture != null && material.HasProperty("_EmissionMap"))
            {
                material.SetTexture("_EmissionMap", emissionTexture);
                material.SetColor("_EmissionColor", new Color(0.36f, 0.48f, 0.2f, 1f));
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", key.IndexOf("Soda", StringComparison.OrdinalIgnoreCase) >= 0 ? 0.34f : 0.02f);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", key.IndexOf("Soda", StringComparison.OrdinalIgnoreCase) >= 0 ? 0.42f : 0.23f);
            }

            material.enableInstancing = true;
            material.renderQueue = -1;
            material.SetOverrideTag("RenderType", "Opaque");

            EditorUtility.SetDirty(material);
            return material;
        }

        private static string FindSiblingTexturePath(string modelPath)
        {
            string fullModelPath = ToFullPath(modelPath);
            string folder = Path.GetDirectoryName(fullModelPath);
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                return null;
            }

            string[] textureFiles = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < textureFiles.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(textureFiles[i]).ToLowerInvariant();
                if (name.Contains("metallic") || name.Contains("roughness") || name.Contains("normal") || name.Contains("emit") || name.Contains("emission"))
                {
                    continue;
                }

                return ToAssetPath(textureFiles[i]);
            }

            return null;
        }

        private static string FindSiblingTexturePathContaining(string modelPath, params string[] tokens)
        {
            string fullModelPath = ToFullPath(modelPath);
            string folder = Path.GetDirectoryName(fullModelPath);
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder) || tokens == null)
            {
                return null;
            }

            string[] textureFiles = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);
            Array.Sort(textureFiles, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < textureFiles.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(textureFiles[i]);
                for (int tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
                {
                    if (name.IndexOf(tokens[tokenIndex], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return ToAssetPath(textureFiles[i]);
                    }
                }
            }

            return null;
        }

        private static void ConfigureNormalTextureImporter(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(ToFullPath(assetPath)))
            {
                return;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.NormalMap;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 3;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void ConfigureMeshyTextureImporter(string assetPath, bool srgb)
        {
            if (!File.Exists(ToFullPath(assetPath)))
            {
                return;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void ConfigureMeshyModelImporter(string assetPath, bool importAnimation)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            if (!File.Exists(ToFullPath(assetPath)))
            {
                return;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = importAnimation;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.preserveHierarchy = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;

            if (importAnimation)
            {
                importer.animationCompression = ModelImporterAnimationCompression.Optimal;
                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    bool isVineReleaseAsset = assetPath.IndexOf("GG_HeroGorilla_VineRelease", StringComparison.OrdinalIgnoreCase) >= 0;
                    for (int i = 0; i < clips.Length; i++)
                    {
                        if (isVineReleaseAsset)
                        {
                            clips[i].name = i == 0 ? "VineRelease" : "VineRelease_" + (i + 1).ToString();
                            clips[i].loopTime = false;
                        }
                        else
                        {
                            string clipName = clips[i].name.ToLowerInvariant();
                            clips[i].loopTime = clipName.Contains("idle") || clipName.Contains("swing") || clipName.Contains("hang") || clipName.Contains("running") || clipName.Contains("walking");
                        }
                    }

                    importer.clipAnimations = clips;
                }
            }

            importer.SaveAndReimport();
        }

        private static Material CreateMeshyGorillaMaterial()
        {
            Texture2D mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MeshyGorillaTexturePath);
            if (mainTexture == null)
            {
                return null;
            }

            Shader shader = Shader.Find("FirstBloom/GassyGorilla/HeroStylized");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MeshyGorillaMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MeshyGorillaMaterialPath);
            }
            else if (shader != null)
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", mainTexture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", mainTexture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.26f);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0.26f);
            }

            if (material.HasProperty("_ShadowTint"))
            {
                material.SetColor("_ShadowTint", new Color(0.62f, 0.5f, 0.4f, 1f));
            }

            if (material.HasProperty("_KeyTint"))
            {
                material.SetColor("_KeyTint", new Color(1.08f, 1f, 0.88f, 1f));
            }

            if (material.HasProperty("_RimColor"))
            {
                material.SetColor("_RimColor", new Color(0.62f, 0.96f, 0.74f, 1f));
                material.SetFloat("_RimStrength", 0.28f);
                material.SetFloat("_RimPower", 2.35f);
            }

            material.enableInstancing = true;
            material.renderQueue = -1;
            material.SetOverrideTag("RenderType", "Opaque");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static RuntimeAnimatorController CreateMeshyGorillaAnimatorController()
        {
            AnimationClip[] clips = LoadMeshyAnimationClips();
            if (clips.Length == 0)
            {
                return null;
            }

            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(MeshyGorillaAnimatorPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(MeshyGorillaAnimatorPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(MeshyGorillaAnimatorPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            ClearAnimatorStateMachine(stateMachine);

            AnimationClip idle = ChooseAnimationClip(clips, new[] { "idle", "breath", "stand" }, clips[0]);
            AnimationClip boost = ChooseAnimationClip(clips, new[] { "boost", "jump", "fly", "up" }, idle);
            AnimationClip swing = ChooseAnimationClip(clips, new[] { "swing", "hang", "grab", "vine" }, idle);
            AnimationClip run = ChooseAnimationClip(clips, new[] { "running", "run" }, idle);
            AnimationClip walk = ChooseAnimationClip(clips, new[] { "walking", "walk" }, run);
            string dedicatedReleasePath = FindMeshyAnimationPath("GG_HeroGorilla_VineRelease", "VineRelease");
            AnimationClip dedicatedRelease = LoadFirstAnimationClipFromPath(dedicatedReleasePath);
            AnimationClip jumpRelease = ChooseAnimationClip(clips, new[] { "jump_with_arms_open", "jump with arms open", "jump" }, boost);
            bool dedicatedReleaseIsWalking = !string.IsNullOrEmpty(dedicatedReleasePath) &&
                dedicatedReleasePath.IndexOf("walking", StringComparison.OrdinalIgnoreCase) >= 0;
            AnimationClip release = dedicatedRelease != null && !dedicatedReleaseIsWalking ? dedicatedRelease : jumpRelease;
            if (dedicatedReleaseIsWalking)
            {
                Debug.LogWarning("The imported VineRelease FBX contains a walking take. Using Jump_with_Arms_Open from the main gorilla animation set instead.");
            }

            AnimatorState idleState = AddAnimatorState(stateMachine, "Idle", idle);
            AddAnimatorState(stateMachine, "Boost", boost);
            AddAnimatorState(stateMachine, "Swing", swing);
            AddAnimatorState(stateMachine, "VineRelease", release);
            AddAnimatorState(stateMachine, "Run", run);
            AddAnimatorState(stateMachine, "Walk", walk);
            stateMachine.defaultState = idleState;

            string clipNames = "";
            for (int i = 0; i < clips.Length; i++)
            {
                clipNames += (i == 0 ? "" : ", ") + clips[i].name;
            }

            Debug.Log("Meshy gorilla animation controller created from clips: " + clipNames);
            Debug.Log("Meshy gorilla VineRelease state using clip: " + (release != null ? release.name + " from " + AssetDatabase.GetAssetPath(release) : "<none>"));
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip[] LoadMeshyAnimationClips()
        {
            List<AnimationClip> clips = new List<AnimationClip>();
            AddAnimationClipsFromPath(FindMeshyAnimationPath("GG_HeroGorilla_VineRelease", "VineRelease"), clips);
            AddAnimationClipsFromPath(MeshyGorillaAnimationPath, clips);
            AddAnimationClipsFromPath(MeshyGorillaModelPath, clips);
            return clips.ToArray();
        }

        private static void AddAnimationClipsFromPath(string assetPath, List<AnimationClip> clips)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            if (!File.Exists(ToFullPath(assetPath)))
            {
                return;
            }

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip == null || string.IsNullOrEmpty(clip.name) || clip.name.StartsWith("__preview", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool alreadyAdded = false;
                for (int j = 0; j < clips.Count; j++)
                {
                    if (clips[j] == clip || clips[j].name == clip.name)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (!alreadyAdded)
                {
                    clips.Add(clip);
                }
            }
        }

        private static AnimationClip LoadFirstAnimationClipFromPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(ToFullPath(assetPath)))
            {
                return null;
            }

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip != null && !string.IsNullOrEmpty(clip.name) && !clip.name.StartsWith("__preview", StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }

            return null;
        }

        private static AnimationClip ChooseAnimationClip(AnimationClip[] clips, string[] terms, AnimationClip fallback)
        {
            for (int termIndex = 0; termIndex < terms.Length; termIndex++)
            {
                string term = terms[termIndex];
                for (int i = 0; i < clips.Length; i++)
                {
                    string clipName = clips[i].name.ToLowerInvariant();
                    if (clipName.Contains(term))
                    {
                        return clips[i];
                    }
                }
            }

            return fallback;
        }

        private static AnimatorState AddAnimatorState(AnimatorStateMachine stateMachine, string stateName, Motion motion)
        {
            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = motion;
            state.speed = 1f;
            state.writeDefaultValues = true;
            return state;
        }

        private static void ClearAnimatorStateMachine(AnimatorStateMachine stateMachine)
        {
            ChildAnimatorState[] states = stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                stateMachine.RemoveState(states[i].state);
            }
        }

        private static GameObject CreateGorillaModelInstance(GorillaModelAssets assets, string name, Transform parent, Vector3 position, float targetHeight, float targetBottomY, int sortingOrder, out Animator animator)
        {
            animator = null;
            if (assets == null || assets.ModelPrefab == null)
            {
                return null;
            }

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(assets.ModelPrefab);
            if (model == null)
            {
                model = UnityEngine.Object.Instantiate(assets.ModelPrefab);
            }

            model.name = name;
            if (parent != null)
            {
                model.transform.SetParent(parent, false);
                model.transform.localPosition = position;
            }
            else
            {
                model.transform.position = position;
            }

            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            model.transform.localScale = Vector3.one;
            RemoveImportedSceneExtras(model);
            ConfigureGorillaModelRenderers(model, assets.Material, sortingOrder);
            FitModelToTarget(model, parent, position, targetHeight, targetBottomY);

            animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = assets.AnimatorController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            return model;
        }

        private static GameObject CreateCrocodileModelInstance(CrocodileModelAssets assets, Transform parent, out Animator animator)
        {
            return CreateCrocodileModelInstance(
                assets,
                parent,
                "Crocodile Finish 3D",
                1.24f,
                -0.92f,
                9,
                AnimatorUpdateMode.UnscaledTime,
                new Vector3(3.72f, 0f, -0.42f),
                false,
                out animator);
        }

        private static GameObject CreateCrocodileModelInstance(
            CrocodileModelAssets assets,
            Transform parent,
            string rootName,
            float targetHeight,
            float targetBottomY,
            int sortingOrder,
            AnimatorUpdateMode updateMode,
            Vector3 localPosition,
            bool active,
            out Animator animator)
        {
            animator = null;
            if (assets == null || assets.ModelPrefab == null)
            {
                return null;
            }

            GameObject crocodileRoot = new GameObject(rootName);
            crocodileRoot.transform.SetParent(parent, false);

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(assets.ModelPrefab);
            if (model == null)
            {
                model = UnityEngine.Object.Instantiate(assets.ModelPrefab);
            }

            model.name = "Visual_Crocodile_3D";
            model.transform.SetParent(crocodileRoot.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            RemoveImportedSceneExtras(model);
            ConfigureGorillaModelRenderers(model, assets.Material, sortingOrder);
            FitModelToTarget(model, crocodileRoot.transform, Vector3.zero, targetHeight, targetBottomY);

            animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = assets.AnimatorController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = updateMode;

            crocodileRoot.transform.localPosition = localPosition;
            crocodileRoot.SetActive(active);
            return crocodileRoot;
        }

        private static GameObject CreateModelVisualInstance(ModelVisualAsset asset, string name, Transform parent, Vector3 position, float targetHeight, float targetBottomY, int sortingOrder, Quaternion rotation, bool fitToTarget)
        {
            if (!HasModel(asset))
            {
                return null;
            }

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(asset.ModelPrefab);
            if (model == null)
            {
                model = UnityEngine.Object.Instantiate(asset.ModelPrefab);
            }

            model.name = name;
            if (parent != null)
            {
                model.transform.SetParent(parent, false);
                model.transform.localPosition = position;
            }
            else
            {
                model.transform.position = position;
            }

            model.transform.localRotation = rotation;
            model.transform.localScale = Vector3.one;
            RemoveImportedSceneExtras(model);
            ConfigureGorillaModelRenderers(model, asset.Material, sortingOrder);

            Animator[] animators = model.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(animators[i]);
            }

            if (fitToTarget)
            {
                FitModelToTarget(model, parent, position, targetHeight, targetBottomY);
            }

            return model;
        }

        private static bool HasModel(ModelVisualAsset asset)
        {
            return asset != null && asset.ModelPrefab != null;
        }

        private static bool AnyModel(ModelVisualAsset[] assets)
        {
            if (assets == null)
            {
                return false;
            }

            for (int i = 0; i < assets.Length; i++)
            {
                if (HasModel(assets[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static ModelVisualAsset FirstAvailableModel(ModelVisualAsset[] assets, int offset)
        {
            if (assets == null || assets.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < assets.Length; i++)
            {
                ModelVisualAsset asset = assets[(offset + i) % assets.Length];
                if (HasModel(asset))
                {
                    return asset;
                }
            }

            return null;
        }

        private static void RemoveImportedSceneExtras(GameObject model)
        {
            Camera[] cameras = model.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(cameras[i].gameObject);
            }

            Light[] lights = model.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(lights[i].gameObject);
            }
        }

        private static void ConfigureGorillaModelRenderers(GameObject model, Material material, int sortingOrder)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (material != null)
                {
                    Material[] materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0)
                    {
                        renderer.sharedMaterial = material;
                    }
                    else
                    {
                        for (int j = 0; j < materials.Length; j++)
                        {
                            materials[j] = material;
                        }

                        renderer.sharedMaterials = materials;
                    }
                }

                renderer.sortingOrder = sortingOrder;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

                if (sortingOrder <= 1)
                {
                    Color depthTint = sortingOrder <= 0
                        ? new Color(0.54f, 0.7f, 0.67f, 1f)
                        : new Color(0.78f, 0.88f, 0.8f, 1f);
                    MaterialPropertyBlock properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    properties.SetColor("_Color", depthTint);
                    properties.SetColor("_BaseColor", depthTint);
                    renderer.SetPropertyBlock(properties);
                }
            }
        }

        private static void FitModelToTarget(GameObject model, Transform parent, Vector3 desiredPosition, float targetHeight, float targetBottomY)
        {
            Bounds bounds = CalculateRendererBounds(model);
            if (bounds.size.y <= 0.001f)
            {
                return;
            }

            float scale = targetHeight / bounds.size.y;
            model.transform.localScale *= scale;
            bounds = CalculateRendererBounds(model);

            Vector3 anchor = parent != null ? parent.position : desiredPosition;
            float desiredCenterX = anchor.x;
            float desiredBottomY = parent != null ? parent.position.y + targetBottomY : targetBottomY;
            Vector3 correction = new Vector3(desiredCenterX - bounds.center.x, desiredBottomY - bounds.min.y, 0f);
            model.transform.position += correction;
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return bounds;
        }

        private static PrefabSet BuildPrefabs(SpriteSet sprites, GorillaModelAssets gorillaModel, CrocodileModelAssets crocodileModel, MeshyGameAssets meshyAssets)
        {
            PrefabSet prefabs = new PrefabSet();
            prefabs.Player = BuildPlayerPrefab(gorillaModel, crocodileModel);
            prefabs.Bean = BuildPickupPrefab("Pickup_Bean", FoodPickupType.Bean, 24f, meshyAssets.Bean, 0.74f, "Bean");
            prefabs.Burrito = BuildPickupPrefab("Pickup_Burrito", FoodPickupType.Burrito, 44f, meshyAssets.Burrito, 0.82f, "Burrito");
            prefabs.Soda = BuildPickupPrefab("Pickup_Soda", FoodPickupType.Soda, 70f, meshyAssets.Soda, 0.76f, "Soda");
            prefabs.BananaBunch = BuildPickupPrefab("Pickup_BananaBunch", FoodPickupType.BananaBunch, 42f, meshyAssets.BananaBunch, 0.8f, "Banana");
            prefabs.SwingableVine = BuildSwingableVinePrefab(meshyAssets.Vine);
            prefabs.VineObstacle = BuildHazardPrefab("Vine_Obstacle", new Vector2(0.55f, 2.6f), meshyAssets.StickySapBlob, 2.25f, "SapBlob");
            prefabs.TreeTrunkObstacle = BuildHazardPrefab("Obstacle_TreeTrunk", new Vector2(0.75f, 2.4f), meshyAssets.FirstAvailable(meshyAssets.ThornLog, meshyAssets.SpikyStump), 1.85f, "ThornLog");
            prefabs.SpikyStumpObstacle = BuildHazardPrefab("Hazard_SpikyStump", new Vector2(0.9f, 1.55f), meshyAssets.SpikyStump, 1.55f, "ThornLog");
            prefabs.MudGeyserObstacle = BuildHazardPrefab("Hazard_MudGeyser", new Vector2(0.9f, 1.8f), meshyAssets.MudGeyser, 1.8f, "SapBlob");
            prefabs.StickySapObstacle = BuildHazardPrefab("Hazard_StickySapBlob", new Vector2(0.85f, 1.6f), meshyAssets.StickySapBlob, 1.6f, "SapBlob");
            prefabs.CrocodileAmbush = BuildCrocodileAmbushPrefab(crocodileModel);
            prefabs.AudioManager = BuildAudioManagerPrefab();
            prefabs.PickupSpawner = BuildSpawnerPrefab<PickupSpawner>("Spawner_Pickups");
            prefabs.ObstacleSpawner = BuildSpawnerPrefab<ObstacleSpawner>("Spawner_Obstacles");
            prefabs.VineSpawner = BuildSpawnerPrefab<VineSwingSpawner>("Spawner_Vines");
            prefabs.GameManager = BuildPlainPrefab<GassyGorillaGameManager>("Manager_Game");
            prefabs.MilestoneManager = BuildPlainPrefab<MilestoneEventManager>("Manager_Milestones");
            return prefabs;
        }

        private static RunDifficultyProfile BuildRunDifficultyProfile()
        {
            RunDifficultyProfile profile = AssetDatabase.LoadAssetAtPath<RunDifficultyProfile>(DifficultyProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<RunDifficultyProfile>();
                AssetDatabase.CreateAsset(profile, DifficultyProfilePath);
            }

            RunDifficultyStage[] stages =
            {
                DifficultyStage("Welcome", 0f, 1f, 1.45f, 1.25f, 1.1f, 1.15f, 0.78f, 0.5f, 0.9f, 0f),
                DifficultyStage("Groove", 70f, 1.02f, 1.2f, 1.15f, 1.05f, 1.1f, 0.95f, 0.75f, 1f, 0.3f),
                DifficultyStage("Canopy", 150f, 1.05f, 1f, 1.05f, 1f, 1.05f, 1.08f, 1f, 1.08f, 0.55f),
                DifficultyStage("Wild", 260f, 1.08f, 0.85f, 1f, 0.95f, 1f, 1.18f, 1.2f, 1.15f, 0.75f),
                DifficultyStage("Legend", 400f, 1.11f, 0.75f, 0.95f, 0.9f, 0.98f, 1.27f, 1.35f, 1.22f, 0.9f)
            };

            profile.Configure(
                stages,
                550f,
                1.14f,
                90f,
                3,
                4,
                0.3f,
                0.45f,
                2,
                2.3f,
                2f,
                0.35f);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static RunDifficultyStage DifficultyStage(
            string name,
            float startDistance,
            float speedMultiplier,
            float beginner,
            float recovery,
            float fuel,
            float vine,
            float boost,
            float hazard,
            float noVine,
            float predator)
        {
            return new RunDifficultyStage(
                name,
                startDistance,
                speedMultiplier,
                new[]
                {
                    new RunTagWeightMultiplier(RunChunkTag.Beginner, beginner),
                    new RunTagWeightMultiplier(RunChunkTag.Recovery, recovery),
                    new RunTagWeightMultiplier(RunChunkTag.Fuel, fuel),
                    new RunTagWeightMultiplier(RunChunkTag.Vine, vine),
                    new RunTagWeightMultiplier(RunChunkTag.Boost, boost),
                    new RunTagWeightMultiplier(RunChunkTag.Hazard, hazard),
                    new RunTagWeightMultiplier(RunChunkTag.NoVine, noVine),
                    new RunTagWeightMultiplier(RunChunkTag.Predator, predator)
                });
        }

        private static RunChunkSet BuildRunChunkDefinitions(PrefabSet prefabs)
        {
            RunChunkDefinition openingBoost = CreateOrUpdateRunChunk(
                "OpeningBoost",
                RunChunkTag.Beginner | RunChunkTag.Boost | RunChunkTag.Fuel,
                false,
                6.5f,
                0f,
                0,
                0,
                RunChunkTag.None,
                RunChunkTag.None,
                new Vector2(-0.6f, 2.7f),
                new Vector2(0.8f, 3.2f),
                new Vector2(0f, 100f),
                new Vector2(35f, 100f),
                4f,
                new[]
                {
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 2f, 1.45f, -8f),
                    ChunkSpawn(prefabs.Burrito, RunChunkSpawnKind.Pickup, 4.75f, 2.2f, 8f)
                });

            RunChunkDefinition safeVine = CreateOrUpdateRunChunk(
                "SafeVine",
                RunChunkTag.Beginner | RunChunkTag.Vine | RunChunkTag.Recovery,
                true,
                8.5f,
                1.05f,
                0,
                4,
                RunChunkTag.Hazard | RunChunkTag.Predator,
                RunChunkTag.Hazard | RunChunkTag.Predator,
                new Vector2(0.6f, 3.3f),
                new Vector2(0.8f, 3.8f),
                new Vector2(0f, 100f),
                new Vector2(0f, 100f),
                4.5f,
                new[]
                {
                    ChunkSpawn(prefabs.SwingableVine, RunChunkSpawnKind.SwingVine, 3.2f, 3.05f),
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 6.65f, 1.45f, 10f)
                });

            RunChunkDefinition fuelArc = CreateOrUpdateRunChunk(
                "FuelArc",
                RunChunkTag.Fuel | RunChunkTag.Recovery | RunChunkTag.NoVine,
                true,
                7.5f,
                1.2f,
                0,
                4,
                RunChunkTag.None,
                RunChunkTag.None,
                new Vector2(-0.4f, 3.2f),
                new Vector2(0.4f, 3.5f),
                new Vector2(0f, 100f),
                new Vector2(45f, 100f),
                3.8f,
                new[]
                {
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 1.3f, 0.85f, -8f),
                    ChunkSpawn(prefabs.Burrito, RunChunkSpawnKind.Pickup, 3.75f, 2.2f, 4f),
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 6.2f, 1.2f, 9f)
                });

            RunChunkDefinition boostGap = CreateOrUpdateRunChunk(
                "BoostGap",
                RunChunkTag.Boost | RunChunkTag.NoVine,
                true,
                9f,
                0.95f,
                0,
                4,
                RunChunkTag.Hazard,
                RunChunkTag.Hazard,
                new Vector2(0.1f, 3.2f),
                new Vector2(0.6f, 3.4f),
                new Vector2(18f, 100f),
                new Vector2(0f, 100f),
                4.2f,
                new[]
                {
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 2.4f, 0.45f, -7f),
                    ChunkSpawn(prefabs.Soda, RunChunkSpawnKind.Pickup, 6.75f, 2.35f, 8f)
                });

            RunChunkDefinition hazardIntroduction = CreateOrUpdateRunChunk(
                "HazardIntroduction",
                RunChunkTag.Beginner | RunChunkTag.Hazard,
                true,
                8.5f,
                0.85f,
                0,
                2,
                RunChunkTag.Hazard,
                RunChunkTag.Hazard,
                new Vector2(0.6f, 3.2f),
                new Vector2(0.9f, 3.5f),
                new Vector2(18f, 100f),
                new Vector2(0f, 100f),
                4.5f,
                new[]
                {
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 1.55f, 1.35f, -8f),
                    ChunkSpawn(prefabs.SpikyStumpObstacle, RunChunkSpawnKind.Hazard, 4.65f, -0.92f),
                    ChunkSpawn(prefabs.Burrito, RunChunkSpawnKind.Pickup, 6.45f, 2.25f, 8f)
                });

            RunChunkDefinition recovery = CreateOrUpdateRunChunk(
                "Recovery",
                RunChunkTag.Fuel | RunChunkTag.Recovery | RunChunkTag.NoVine,
                true,
                7f,
                1.3f,
                0,
                4,
                RunChunkTag.None,
                RunChunkTag.None,
                new Vector2(-0.4f, 3.5f),
                new Vector2(0.4f, 3.2f),
                new Vector2(0f, 100f),
                new Vector2(50f, 100f),
                3.5f,
                new[]
                {
                    ChunkSpawn(prefabs.BananaBunch, RunChunkSpawnKind.Pickup, 1.75f, 1.1f, -6f),
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 4.9f, 1.45f, 6f)
                });

            RunChunkDefinition crocodileAmbush = CreateOrUpdateRunChunk(
                "CrocodileAmbush",
                RunChunkTag.Hazard | RunChunkTag.NoVine | RunChunkTag.Predator,
                true,
                12.5f,
                0.68f,
                1,
                4,
                RunChunkTag.Hazard | RunChunkTag.Vine | RunChunkTag.Predator,
                RunChunkTag.Hazard | RunChunkTag.Predator,
                new Vector2(-0.2f, 3.3f),
                new Vector2(0.6f, 3.6f),
                new Vector2(17.5f, 100f),
                new Vector2(0f, 100f),
                5.2f,
                new[]
                {
                    ChunkSpawn(prefabs.BananaBunch, RunChunkSpawnKind.Pickup, 0.35f, 1.1f, -5f),
                    ChunkSpawn(prefabs.CrocodileAmbush, RunChunkSpawnKind.Hazard, 8f, -1.61f),
                    ChunkSpawn(prefabs.Burrito, RunChunkSpawnKind.Pickup, 10.8f, 2.35f, 7f)
                });

            RunChunkDefinition highVineArc = CreateOrUpdateRunChunk(
                "HighVineArc",
                RunChunkTag.Vine | RunChunkTag.Fuel,
                true,
                9f,
                1.02f,
                0,
                4,
                RunChunkTag.None,
                RunChunkTag.Hazard | RunChunkTag.Predator,
                new Vector2(-0.2f, 3.6f),
                new Vector2(0.8f, 4.2f),
                new Vector2(0f, 100f),
                new Vector2(25f, 100f),
                4.2f,
                new[]
                {
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 1.35f, 1.35f, -7f),
                    ChunkSpawn(prefabs.SwingableVine, RunChunkSpawnKind.SwingVine, 4.1f, 3.5f),
                    ChunkSpawn(prefabs.Burrito, RunChunkSpawnKind.Pickup, 7.45f, 2.85f, 8f)
                });

            RunChunkDefinition lowVineRescue = CreateOrUpdateRunChunk(
                "LowVineRescue",
                RunChunkTag.Beginner | RunChunkTag.Vine | RunChunkTag.Recovery | RunChunkTag.Fuel,
                true,
                8f,
                1.08f,
                0,
                3,
                RunChunkTag.None,
                RunChunkTag.Hazard | RunChunkTag.Predator,
                new Vector2(-0.5f, 3.4f),
                new Vector2(0.8f, 3.6f),
                new Vector2(0f, 100f),
                new Vector2(45f, 100f),
                3.8f,
                new[]
                {
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 1.1f, 1.05f, -6f),
                    ChunkSpawn(prefabs.SwingableVine, RunChunkSpawnKind.SwingVine, 3.2f, 2.7f),
                    ChunkSpawn(prefabs.BananaBunch, RunChunkSpawnKind.Pickup, 6.25f, 1.6f, 7f)
                });

            RunChunkDefinition vineBoostRelay = CreateOrUpdateRunChunk(
                "VineBoostRelay",
                RunChunkTag.Vine | RunChunkTag.Boost,
                true,
                10f,
                0.92f,
                1,
                4,
                RunChunkTag.Hazard | RunChunkTag.Predator,
                RunChunkTag.Hazard | RunChunkTag.Predator,
                new Vector2(0.2f, 3.6f),
                new Vector2(0.7f, 4f),
                new Vector2(18f, 100f),
                new Vector2(0f, 100f),
                4.5f,
                new[]
                {
                    ChunkSpawn(prefabs.SwingableVine, RunChunkSpawnKind.SwingVine, 2.9f, 3.15f),
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 5.75f, 3.15f, -5f),
                    ChunkSpawn(prefabs.Soda, RunChunkSpawnKind.Pickup, 8.8f, 1.45f, 8f)
                });

            RunChunkDefinition fuelChoiceFork = CreateOrUpdateRunChunk(
                "FuelChoiceFork",
                RunChunkTag.Fuel | RunChunkTag.Recovery | RunChunkTag.NoVine,
                true,
                9f,
                1.12f,
                1,
                4,
                RunChunkTag.None,
                RunChunkTag.None,
                new Vector2(-0.5f, 3.6f),
                new Vector2(0.2f, 3.6f),
                new Vector2(0f, 100f),
                new Vector2(50f, 100f),
                3.7f,
                new[]
                {
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 1.25f, 0.55f, -7f),
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 2.2f, 2.15f, 6f),
                    ChunkSpawn(prefabs.Burrito, RunChunkSpawnKind.Pickup, 4.65f, 3.05f, -5f),
                    ChunkSpawn(prefabs.BananaBunch, RunChunkSpawnKind.Pickup, 7.55f, 1.05f, 7f)
                });

            RunChunkDefinition longBoostGap = CreateOrUpdateRunChunk(
                "LongBoostGap",
                RunChunkTag.Boost | RunChunkTag.NoVine,
                true,
                11.5f,
                0.72f,
                2,
                4,
                RunChunkTag.Hazard | RunChunkTag.Boost,
                RunChunkTag.Hazard | RunChunkTag.Boost,
                new Vector2(0f, 3.5f),
                new Vector2(0.5f, 3.8f),
                new Vector2(36f, 100f),
                new Vector2(0f, 100f),
                4.8f,
                new[]
                {
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 1.85f, 0.65f, -6f),
                    ChunkSpawn(prefabs.Soda, RunChunkSpawnKind.Pickup, 9.85f, 3.15f, 8f)
                });

            RunChunkDefinition thornTimingLane = CreateOrUpdateRunChunk(
                "ThornTimingLane",
                RunChunkTag.Hazard | RunChunkTag.Boost,
                true,
                10f,
                0.8f,
                1,
                4,
                RunChunkTag.Hazard,
                RunChunkTag.Hazard,
                new Vector2(0.2f, 3.6f),
                new Vector2(0.7f, 3.8f),
                new Vector2(18f, 100f),
                new Vector2(0f, 100f),
                4.6f,
                new[]
                {
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 1.6f, 1.05f, -6f),
                    ChunkSpawn(prefabs.SpikyStumpObstacle, RunChunkSpawnKind.Hazard, 5.2f, -0.92f),
                    ChunkSpawn(prefabs.Burrito, RunChunkSpawnKind.Pickup, 7.65f, 2.85f, 7f)
                });

            RunChunkDefinition postPredatorFeast = CreateOrUpdateRunChunk(
                "PostPredatorFeast",
                RunChunkTag.Recovery | RunChunkTag.Fuel | RunChunkTag.Vine,
                true,
                9f,
                1.18f,
                1,
                4,
                RunChunkTag.None,
                RunChunkTag.Hazard | RunChunkTag.Predator,
                new Vector2(-0.4f, 3.8f),
                new Vector2(0.5f, 3.8f),
                new Vector2(0f, 100f),
                new Vector2(55f, 100f),
                4f,
                new[]
                {
                    ChunkSpawn(prefabs.SwingableVine, RunChunkSpawnKind.SwingVine, 2.75f, 2.75f),
                    ChunkSpawn(prefabs.BananaBunch, RunChunkSpawnKind.Pickup, 5.55f, 1.65f, -6f),
                    ChunkSpawn(prefabs.Burrito, RunChunkSpawnKind.Pickup, 7.65f, 2.65f, 7f)
                });

            RunChunkDefinition crocodileBaitLift = CreateOrUpdateRunChunk(
                "CrocodileBaitLift",
                RunChunkTag.Hazard | RunChunkTag.NoVine | RunChunkTag.Predator | RunChunkTag.Boost,
                true,
                13f,
                0.52f,
                3,
                4,
                RunChunkTag.Hazard | RunChunkTag.Vine | RunChunkTag.Predator,
                RunChunkTag.Hazard | RunChunkTag.Predator,
                new Vector2(-0.2f, 3.5f),
                new Vector2(0.8f, 4f),
                new Vector2(35f, 100f),
                new Vector2(0f, 100f),
                5.5f,
                new[]
                {
                    ChunkSpawn(prefabs.Bean, RunChunkSpawnKind.Pickup, 0.55f, 1.05f, -5f),
                    ChunkSpawn(prefabs.Soda, RunChunkSpawnKind.Pickup, 3.35f, 2.75f, 6f),
                    ChunkSpawn(prefabs.CrocodileAmbush, RunChunkSpawnKind.Hazard, 8.75f, -1.61f),
                    ChunkSpawn(prefabs.BananaBunch, RunChunkSpawnKind.Pickup, 11.45f, 3.05f, 7f)
                });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            openingBoost = LoadRunChunk("OpeningBoost");
            safeVine = LoadRunChunk("SafeVine");
            fuelArc = LoadRunChunk("FuelArc");
            boostGap = LoadRunChunk("BoostGap");
            hazardIntroduction = LoadRunChunk("HazardIntroduction");
            recovery = LoadRunChunk("Recovery");
            crocodileAmbush = LoadRunChunk("CrocodileAmbush");
            highVineArc = LoadRunChunk("HighVineArc");
            lowVineRescue = LoadRunChunk("LowVineRescue");
            vineBoostRelay = LoadRunChunk("VineBoostRelay");
            fuelChoiceFork = LoadRunChunk("FuelChoiceFork");
            longBoostGap = LoadRunChunk("LongBoostGap");
            thornTimingLane = LoadRunChunk("ThornTimingLane");
            postPredatorFeast = LoadRunChunk("PostPredatorFeast");
            crocodileBaitLift = LoadRunChunk("CrocodileBaitLift");

            return new RunChunkSet
            {
                All = new[]
                {
                    openingBoost,
                    safeVine,
                    fuelArc,
                    boostGap,
                    hazardIntroduction,
                    recovery,
                    crocodileAmbush,
                    highVineArc,
                    lowVineRescue,
                    vineBoostRelay,
                    fuelChoiceFork,
                    longBoostGap,
                    thornTimingLane,
                    postPredatorFeast,
                    crocodileBaitLift
                },
                Opening = new[] { openingBoost, safeVine, recovery, hazardIntroduction }
            };
        }

        private static RunChunkDefinition CreateOrUpdateRunChunk(
            string id,
            RunChunkTag tags,
            bool allowInMainPool,
            float length,
            float weight,
            int minimumDifficulty,
            int maximumDifficulty,
            RunChunkTag blockedPreviousTags,
            RunChunkTag blockedNextTags,
            Vector2 entryHeightRange,
            Vector2 exitHeightRange,
            Vector2 entryFuelRange,
            Vector2 exitFuelRange,
            float minimumReactionDistance,
            RunChunkSpawn[] spawns)
        {
            string path = RunChunkRoot + "/GG_RunChunk_" + id + ".asset";
            RunChunkDefinition definition = AssetDatabase.LoadAssetAtPath<RunChunkDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<RunChunkDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.Configure(
                id,
                tags,
                allowInMainPool,
                length,
                weight,
                minimumDifficulty,
                maximumDifficulty,
                blockedPreviousTags,
                blockedNextTags,
                entryHeightRange,
                exitHeightRange,
                entryFuelRange,
                exitFuelRange,
                minimumReactionDistance,
                spawns);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static RunChunkDefinition LoadRunChunk(string id)
        {
            string path = RunChunkRoot + "/GG_RunChunk_" + id + ".asset";
            RunChunkDefinition definition = AssetDatabase.LoadAssetAtPath<RunChunkDefinition>(path);
            if (definition == null)
            {
                throw new InvalidOperationException("Unable to reload generated run chunk: " + path);
            }

            return definition;
        }

        private static RunChunkSpawn ChunkSpawn(
            GameObject prefab,
            RunChunkSpawnKind kind,
            float x,
            float y,
            float zRotation = 0f,
            float scale = 1f)
        {
            return new RunChunkSpawn(
                prefab,
                kind,
                new Vector3(x, y, 0f),
                new Vector3(0f, 0f, zRotation),
                Vector3.one * scale);
        }

        private static GameObject BuildPlayerPrefab(GorillaModelAssets gorillaModel, CrocodileModelAssets crocodileModel)
        {
            bool useMeshyGorilla = gorillaModel != null && gorillaModel.ModelPrefab != null;
            GameObject root = new GameObject("Player_GassyGorilla");
            root.tag = "Player";
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 1.5f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.92f, 1.12f);
            collider.offset = new Vector2(0f, -0.04f);

            GameObject visual = new GameObject("Visual_Gorilla");
            visual.transform.SetParent(root.transform, false);

            GameObject fartFxRoot = new GameObject("Fart Boost FX");
            fartFxRoot.transform.SetParent(root.transform, false);

            Material fartCloudMaterial = CreateColorMaterial("GG_FartCloud_3D", new Color(0.66f, 1f, 0.42f, 0.76f), true);
            Material fartCoreMaterial = CreateColorMaterial("GG_FartCoreGlow_3D", new Color(0.98f, 1f, 0.52f, 0.82f), true);
            Material fartRingMaterial = CreateColorMaterial("GG_FartShockRing_3D", new Color(0.78f, 1f, 0.26f, 0.55f), true);
            GameObject cloudBurst = CreatePrimitiveVisual("Fart Cloud Burst", PrimitiveType.Sphere, fartFxRoot.transform, new Vector3(-0.58f, -0.31f, -0.2f), new Vector3(0.72f, 0.38f, 0.44f), fartCloudMaterial, 5);
            Renderer fartCloudRenderer = cloudBurst.GetComponent<Renderer>();
            fartCloudRenderer.enabled = false;

            GameObject coreGlow = CreatePrimitiveVisual("Fart Core Pop", PrimitiveType.Sphere, fartFxRoot.transform, new Vector3(-0.46f, -0.34f, -0.24f), new Vector3(0.34f, 0.2f, 0.24f), fartCoreMaterial, 7);
            Renderer fartCoreRenderer = coreGlow.GetComponent<Renderer>();
            fartCoreRenderer.enabled = false;

            GameObject shockRing = CreatePrimitiveVisual("Fart Shock Ring", PrimitiveType.Sphere, fartFxRoot.transform, new Vector3(-0.52f, -0.32f, -0.26f), new Vector3(0.38f, 0.12f, 0.2f), fartRingMaterial, 6);
            Renderer fartRingRenderer = shockRing.GetComponent<Renderer>();
            fartRingRenderer.enabled = false;

            Renderer[] fartAccentRenderers =
            {
                CreatePrimitiveVisual("Fart Accent Puff A", PrimitiveType.Sphere, fartFxRoot.transform, new Vector3(-0.42f, -0.18f, -0.23f), new Vector3(0.22f, 0.14f, 0.16f), fartCloudMaterial, 6).GetComponent<Renderer>(),
                CreatePrimitiveVisual("Fart Accent Puff B", PrimitiveType.Sphere, fartFxRoot.transform, new Vector3(-0.66f, -0.43f, -0.22f), new Vector3(0.26f, 0.16f, 0.18f), fartCloudMaterial, 6).GetComponent<Renderer>(),
                CreatePrimitiveVisual("Fart Accent Puff C", PrimitiveType.Sphere, fartFxRoot.transform, new Vector3(-0.72f, -0.25f, -0.24f), new Vector3(0.18f, 0.12f, 0.14f), fartCoreMaterial, 7).GetComponent<Renderer>()
            };
            for (int i = 0; i < fartAccentRenderers.Length; i++)
            {
                fartAccentRenderers[i].enabled = false;
            }

            GameObject puff = new GameObject("Fart Gas Plume");
            puff.transform.SetParent(fartFxRoot.transform, false);
            puff.transform.localPosition = new Vector3(-0.5f, -0.34f, -0.05f);
            ParticleSystem particles = puff.AddComponent<ParticleSystem>();
            ConfigureFartParticles(particles);

            GameObject shockwave = new GameObject("Fart Shockwave Burst");
            shockwave.transform.SetParent(fartFxRoot.transform, false);
            shockwave.transform.localPosition = new Vector3(-0.5f, -0.33f, -0.06f);
            ParticleSystem shockwaveParticles = shockwave.AddComponent<ParticleSystem>();
            ConfigureFartShockwaveParticles(shockwaveParticles);

            GameObject spark = new GameObject("Fart Spark Flecks");
            spark.transform.SetParent(fartFxRoot.transform, false);
            spark.transform.localPosition = new Vector3(-0.5f, -0.31f, -0.08f);
            ParticleSystem sparkParticles = spark.AddComponent<ParticleSystem>();
            ConfigureFartSparkParticles(sparkParticles);

            GameObject speedLines = new GameObject("Speed Line Burst");
            speedLines.transform.SetParent(fartFxRoot.transform, false);
            speedLines.transform.localPosition = new Vector3(-0.55f, 0.05f, 0f);
            ParticleSystem speedLineParticles = speedLines.AddComponent<ParticleSystem>();
            ConfigureSpeedLineParticles(speedLineParticles);

            Animator modelAnimator = null;
            GameObject modelRoot = useMeshyGorilla
                ? CreateGorillaModelInstance(gorillaModel, "Visual_Gorilla_3D", visual.transform, new Vector3(0f, 0f, -0.15f), 4.42f, -0.86f, 4, out modelAnimator)
                : CreatePrimitiveGorillaVisual("Visual_Gorilla_3D", visual.transform);

            GorillaController controller = root.AddComponent<GorillaController>();
            SetFloat(controller, "forwardSpeed", 4.85f);
            SetFloat(controller, "fartBoostVelocity", 6.05f);
            SetFloat(controller, "boostForwardKick", 0.72f);
            SetFloat(controller, "boostForwardKickDuration", 0.2f);
            SetFloat(controller, "horizontalCruiseReturn", 2.9f);
            SetFloat(controller, "maxVerticalSpeed", 7.65f);
            SetFloat(controller, "boostCooldown", 0.075f);
            SetFloat(controller, "boostInputBuffer", 0.1f);
            SetFloat(controller, "boostFallRecovery", 0.36f);
            SetFloat(controller, "maxBoostVerticalBonus", 1.15f);
            SetFloat(controller, "fuelDrainPerBoost", 17.5f);
            SetFloat(controller, "swingAngleDegrees", 26f);
            SetFloat(controller, "swingSpeed", 2.35f);
            SetVector2(controller, "vineReleaseVelocity", new Vector2(4.7f, 2.4f));
            SetFloat(controller, "swingEntryArcHeight", 0.15f);
            SetFloat(controller, "swingEntryBlendDuration", 0.14f);
            SetFloat(controller, "swingEntryOvershoot", 0.04f);
            SetFloat(controller, "swingMomentumInheritance", 0.55f);
            SetFloat(controller, "verticalMomentumInheritance", 0.38f);
            SetFloat(controller, "releaseReachForwardBonus", 5.25f);
            SetFloat(controller, "releaseReachLiftBonus", 2.7f);
            SetFloat(controller, "releasePowerExponent", 1.25f);
            SetFloat(controller, "returningReleasePowerScale", 0.58f);
            SetFloat(controller, "maxInheritedSwingSpeed", 8.5f);
            SetFloat(controller, "maxReleaseForwardSpeed", 11.8f);
            SetFloat(controller, "minimumVineReleaseLift", 4f);
            SetFloat(controller, "vineReleaseSafetyDuration", 1f);
            SetFloat(controller, "vineReleaseDangerY", -1.72f);
            SetFloat(controller, "vineReleaseSafetyClearance", 0.35f);
            SetFloat(controller, "swingCameraSmoothingMultiplier", 1.85f);
            SetFloat(controller, "vineSlowMoScale", 0.93f);
            SetFloat(controller, "vineSlowMoDuration", 0.06f);
            SetObject(controller, "visualRoot", visual.transform);
            SetObject(controller, "fartPuff", particles);
            SetObject(controller, "fartShockwaveBurst", shockwaveParticles);
            SetObject(controller, "fartSparkBurst", sparkParticles);
            SetObject(controller, "speedLineBurst", speedLineParticles);
            SetObject(controller, "fartCloudRenderer", fartCloudRenderer);
            SetObject(controller, "fartCoreRenderer", fartCoreRenderer);
            SetObject(controller, "fartRingRenderer", fartRingRenderer);
            SetObjectArray(controller, "fartAccentRenderers", fartAccentRenderers);

            GorillaModelVisualController modelVisual = visual.AddComponent<GorillaModelVisualController>();
            SetObject(modelVisual, "gorilla", controller);
            SetObject(modelVisual, "modelRoot", modelRoot);
            SetObject(modelVisual, "animator", modelAnimator);
            SetObject(modelVisual, "velocitySource", body);
            SetObject(modelVisual, "bodyCollider", collider);
            SetFloat(modelVisual, "crossFadeDuration", 0.065f);
            SetFloat(modelVisual, "boostPoseHold", 0.24f);
            SetFloat(modelVisual, "releasePoseHold", 0.42f);
            SetFloat(modelVisual, "boostAnimationSpeed", 1.3f);
            SetFloat(modelVisual, "releaseAnimationSpeed", 1.5f);
            SetFloat(modelVisual, "swingAnimationSpeed", 0.46f);
            SetFloat(modelVisual, "boostStartTime", 0.1f);
            SetFloat(modelVisual, "swingStartTime", 0.38f);
            SetFloat(modelVisual, "releaseStartTime", 0.2f);
            SetBool(modelVisual, "lockSwingPose", true);
            SetFloat(modelVisual, "swingGripPoseNormalizedTime", 0.38f);
            SetVector2(modelVisual, "gripTargetOffset", Vector2.zero);
            SetFloat(modelVisual, "gripReleasePositionBlendSpeed", 18f);
            SetFloat(modelVisual, "swingLeanDegrees", 0.5f);
            SetFloat(modelVisual, "swingPosePulseDegrees", 1.1f);
            SetFloat(modelVisual, "travelYawDegrees", -42f);
            SetFloat(modelVisual, "boostYawDegrees", -50f);
            SetFloat(modelVisual, "swingYawDegrees", -58f);
            SetFloat(modelVisual, "releaseYawDegrees", -56f);
            SetFloat(modelVisual, "yawBlendSpeed", 9f);

            BuildLagoonFinishPresentation(root, body, modelRoot, crocodileModel);
            return SavePrefab(root, PrefabRoot + "/Player_GassyGorilla.prefab");
        }

        private static void BuildLagoonFinishPresentation(GameObject playerRoot, Rigidbody2D body, GameObject gorillaModelRoot, CrocodileModelAssets crocodileModel)
        {
            Material reflectionMaterial = CreateColorMaterial(
                "GG_LagoonReflection_3D",
                new Color(0.48f, 0.72f, 0.64f, 0.84f),
                true);
            Material rippleMaterial = CreateColorMaterial(
                "GG_LagoonRipple_3D",
                new Color(0.68f, 1f, 0.9f, 0.85f),
                true);

            GameObject reflectionRoot = new GameObject("Lagoon Reflection 3D");
            reflectionRoot.transform.SetParent(playerRoot.transform, false);
            Renderer[] reflectionRenderers =
            {
                CreatePrimitiveVisual("Reflection Body", PrimitiveType.Sphere, reflectionRoot.transform, Vector3.zero, new Vector3(0.68f, 0.18f, 0.18f), reflectionMaterial, 13, false).GetComponent<Renderer>(),
                CreatePrimitiveVisual("Reflection Head", PrimitiveType.Sphere, reflectionRoot.transform, new Vector3(0.23f, 0.04f, -0.01f), new Vector3(0.42f, 0.13f, 0.16f), reflectionMaterial, 13, false).GetComponent<Renderer>(),
                CreatePrimitiveVisual("Reflection Muzzle", PrimitiveType.Sphere, reflectionRoot.transform, new Vector3(0.45f, 0.025f, -0.02f), new Vector3(0.24f, 0.075f, 0.12f), reflectionMaterial, 13, false).GetComponent<Renderer>(),
                CreatePrimitiveVisual("Reflection Forward Arm", PrimitiveType.Sphere, reflectionRoot.transform, new Vector3(0.48f, -0.025f, 0.01f), new Vector3(0.62f, 0.06f, 0.1f), reflectionMaterial, 13, false).GetComponent<Renderer>(),
                CreatePrimitiveVisual("Reflection Rear Arm", PrimitiveType.Sphere, reflectionRoot.transform, new Vector3(-0.42f, -0.015f, 0.015f), new Vector3(0.56f, 0.055f, 0.1f), reflectionMaterial, 13, false).GetComponent<Renderer>(),
                CreatePrimitiveVisual("Reflection Legs", PrimitiveType.Sphere, reflectionRoot.transform, new Vector3(-0.12f, -0.07f, 0.02f), new Vector3(0.58f, 0.07f, 0.11f), reflectionMaterial, 13, false).GetComponent<Renderer>()
            };
            reflectionRenderers[3].transform.localRotation = Quaternion.Euler(0f, 0f, -10f);
            reflectionRenderers[4].transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            reflectionRenderers[5].transform.localRotation = Quaternion.Euler(0f, 0f, -3f);

            GameObject impactRoot = new GameObject("Lagoon Water Impact FX");
            impactRoot.transform.SetParent(playerRoot.transform, false);

            GameObject splashCrown = new GameObject("Splash Crown");
            splashCrown.transform.SetParent(impactRoot.transform, false);
            ParticleSystem splashParticles = splashCrown.AddComponent<ParticleSystem>();
            ConfigureLagoonSplashParticles(splashParticles);

            GameObject droplets = new GameObject("Splash Droplets");
            droplets.transform.SetParent(impactRoot.transform, false);
            ParticleSystem dropletParticles = droplets.AddComponent<ParticleSystem>();
            ConfigureLagoonDropletParticles(dropletParticles);

            GameObject bubbles = new GameObject("Foam Bubbles");
            bubbles.transform.SetParent(impactRoot.transform, false);
            ParticleSystem bubbleParticles = bubbles.AddComponent<ParticleSystem>();
            ConfigureLagoonBubbleParticles(bubbleParticles);

            Transform[] rippleTransforms = new Transform[3];
            Renderer[] rippleRenderers = new Renderer[3];
            for (int i = 0; i < rippleTransforms.Length; i++)
            {
                GameObject ripple = CreatePrimitiveVisual(
                    "Water Ripple " + (i + 1),
                    PrimitiveType.Sphere,
                    impactRoot.transform,
                    new Vector3(0f, 0.01f - i * 0.012f, -0.015f - i * 0.012f),
                    new Vector3(0.48f + i * 0.08f, 0.052f, 0.14f),
                    rippleMaterial,
                    14 + i,
                    false);
                rippleTransforms[i] = ripple.transform;
                rippleRenderers[i] = ripple.GetComponent<Renderer>();
                rippleRenderers[i].enabled = false;
            }

            Animator crocodileAnimator;
            GameObject crocodileRoot = CreateCrocodileModelInstance(crocodileModel, impactRoot.transform, out crocodileAnimator);
            Renderer[] gorillaVisualRenderers = gorillaModelRoot != null
                ? gorillaModelRoot.GetComponentsInChildren<Renderer>(true)
                : Array.Empty<Renderer>();

            LagoonFinishPresentation presentation = playerRoot.AddComponent<LagoonFinishPresentation>();
            SetObject(presentation, "reflectionRoot", reflectionRoot.transform);
            SetObjectArray(presentation, "reflectionRenderers", reflectionRenderers);
            SetObject(presentation, "velocitySource", body);
            SetFloat(presentation, "waterSurfaceY", -1.61f);
            SetFloat(presentation, "reflectionWorldZ", -0.32f);
            SetFloat(presentation, "reflectionDepth", 0.09f);
            SetFloat(presentation, "reflectionVerticalCompression", 0.14f);
            SetFloat(presentation, "reflectionFullAlphaHeight", 0.75f);
            SetFloat(presentation, "reflectionFadeHeight", 4.7f);
            SetFloat(presentation, "reflectionImpactFadeDuration", 0.22f);
            SetObject(presentation, "impactRoot", impactRoot.transform);
            SetObjectArray(presentation, "impactParticles", new[] { splashParticles, dropletParticles, bubbleParticles });
            SetObjectArray(presentation, "rippleTransforms", rippleTransforms);
            SetObjectArray(presentation, "rippleRenderers", rippleRenderers);
            SetFloat(presentation, "impactWorldZ", -0.2f);
            SetFloat(presentation, "rippleDuration", 0.72f);
            SetFloat(presentation, "rippleStagger", 0.1f);
            SetFloat(presentation, "rippleExpansion", 5.2f);
            SetObject(presentation, "crocodileRoot", crocodileRoot);
            SetObject(presentation, "crocodileAnimator", crocodileAnimator);
            SetObjectArray(presentation, "playerVisualRenderers", gorillaVisualRenderers);
            SetFloat(presentation, "crocodileChompDelay", 0.46f);
            SetFloat(presentation, "crocodileSettleDelay", 0.72f);
            SetFloat(presentation, "chompVolume", 0.92f);
        }

        private static GameObject BuildPickupPrefab(string name, FoodPickupType type, float refill, ModelVisualAsset modelAsset, float modelHeight, string primitiveKind)
        {
            GameObject root = new GameObject(name);
            root.tag = "Pickup";
            if (HasModel(modelAsset))
            {
                CreateModelVisualInstance(modelAsset, "Visual_3D", root.transform, Vector3.zero, modelHeight, -0.42f, 3, Quaternion.Euler(0f, 36f, 0f), true);
            }
            else
            {
                CreatePickupPrimitiveVisual(primitiveKind, root.transform);
            }

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.42f;

            FartFuelPickup pickup = root.AddComponent<FartFuelPickup>();
            pickup.Configure(type, refill);
            SetFloat(pickup, "bobHeight", 0.08f);
            SetFloat(pickup, "bobSpeed", 2.7f);
            SetFloat(pickup, "spinDegreesPerSecond", 26f);
            SetFloat(pickup, "attractionRadius", type == FoodPickupType.BananaBunch ? 1.38f : 1.18f);
            SetFloat(pickup, "attractionSpeed", 6.1f);
            SetFloat(pickup, "attractionRampSpeed", 8.2f);
            GameObject sparkle = new GameObject("Collect Sparkle");
            sparkle.transform.SetParent(root.transform, false);
            ParticleSystem sparkleParticles = sparkle.AddComponent<ParticleSystem>();
            ConfigurePickupSparkleParticles(sparkleParticles);
            SetObject(pickup, "collectSparkle", sparkleParticles);
            root.AddComponent<DestroyBehindTarget>();

            return SavePrefab(root, PrefabRoot + "/" + name + ".prefab");
        }

        private static GameObject BuildSwingableVinePrefab(ModelVisualAsset vineModel)
        {
            GameObject root = new GameObject("Vine_Swingable");
            root.tag = "Vine";

            BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2.8f, 3.3f);
            trigger.offset = new Vector2(0f, -1.25f);

            Material branchMaterial = CreateColorMaterial("GG_VineAnchor_Branch_3D", new Color(0.25f, 0.15f, 0.08f, 1f), false);
            Material upperVineMaterial = CreateColorMaterial("GG_VineHighConnector_3D", new Color(0.13f, 0.4f, 0.13f, 1f), false);

            const float canopyPivotHeight = 6.1f;
            GameObject pivot = new GameObject("PivotPoint");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0f, canopyPivotHeight, 0f);

            GameObject swingRoot = new GameObject("VineSwingRoot");
            swingRoot.transform.SetParent(pivot.transform, false);

            CreateCurvedVineConnector("VineConnector_FromCanopy_3D", swingRoot.transform, -0.02f, -5.56f, upperVineMaterial, 2);

            GameObject branch = CreatePrimitiveVisual("HighTreeBranch_3D", PrimitiveType.Cylinder, pivot.transform, new Vector3(0.08f, 0.62f, 0.16f), new Vector3(0.13f, 1.42f, 0.13f), branchMaterial, 1);
            branch.transform.localRotation = Quaternion.Euler(0f, 0f, 86f);

            GameObject visualRoot = new GameObject("VineVisualRoot");
            visualRoot.transform.SetParent(swingRoot.transform, false);
            visualRoot.transform.localPosition = new Vector3(0f, -canopyPivotHeight, 0f);

            bool useModel = HasModel(vineModel);
            if (useModel)
            {
                CreateModelVisualInstance(vineModel, "VineBody_3D", visualRoot.transform, Vector3.zero, 3.72f, -3.08f, 2, Quaternion.Euler(0f, 26f, 0f), true);
            }
            else
            {
                CreatePrimitiveVineVisual(visualRoot.transform);
            }

            GameObject grab = new GameObject("GrabPoint");
            grab.transform.SetParent(swingRoot.transform, false);
            grab.transform.localPosition = new Vector3(0f, -(canopyPivotHeight + 1.42f), 0f);

            Material catchGlow = CreateColorMaterial("GG_VineCatchGlow_3D", new Color(0.54f, 1f, 0.32f, 0.34f), true);
            GameObject assistGlow = CreatePrimitiveVisual("MagneticCatchGlow_3D", PrimitiveType.Sphere, swingRoot.transform, grab.transform.localPosition + new Vector3(0f, 0f, 0.08f), new Vector3(0.78f, 0.42f, 0.08f), catchGlow, 6);
            assistGlow.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            Renderer[] catchCueRenderers = { assistGlow.GetComponent<Renderer>() };

            VineSwingTrigger swingTrigger = root.AddComponent<VineSwingTrigger>();
            SetObject(swingTrigger, "grabPoint", grab.transform);
            SetObject(swingTrigger, "pivotPoint", pivot.transform);
            SetObject(swingTrigger, "visualRoot", visualRoot.transform);
            SetObject(swingTrigger, "releasePowerCue", assistGlow.transform);
            SetObjectArray(swingTrigger, "glowRenderers", catchCueRenderers);
            SetFloat(swingTrigger, "catchRadius", 1.72f);
            SetFloat(swingTrigger, "grabPunchScale", 1.08f);

            VineSwingAnimator animator = root.AddComponent<VineSwingAnimator>();
            SetObject(animator, "vineVisual", swingRoot.transform);
            SetObjectArray(animator, "glowRenderers", catchCueRenderers);
            SetFloat(animator, "swayDegrees", 5.5f);
            SetFloat(animator, "swaySpeed", 0.78f);
            SetFloat(animator, "releaseReturnTime", 0.32f);
            SetFloat(animator, "maxOccupiedDegrees", 28f);
            SetObject(swingTrigger, "swingAnimator", animator);

            root.AddComponent<DestroyBehindTarget>();
            return SavePrefab(root, PrefabRoot + "/Vine_Swingable.prefab");
        }

        private static GameObject BuildCrocodileAmbushPrefab(CrocodileModelAssets crocodileModel)
        {
            if (crocodileModel == null || crocodileModel.ModelPrefab == null)
            {
                throw new InvalidOperationException("The crocodile ambush requires the production Blender crocodile model.");
            }

            GameObject root = new GameObject("Hazard_CrocodileAmbush");
            root.tag = "Obstacle";

            Material warningMaterial = CreateColorMaterial(
                "GG_CrocodileWarningRipple_3D",
                new Color(1f, 0.82f, 0.28f, 0.9f),
                true);

            GameObject warningRoot = new GameObject("Crocodile Warning 3D");
            warningRoot.transform.SetParent(root.transform, false);
            warningRoot.transform.localPosition = new Vector3(-1.95f, 0.07f, -0.24f);

            Transform[] warningRipples = new Transform[3];
            Renderer[] warningRenderers = new Renderer[3];
            for (int i = 0; i < warningRipples.Length; i++)
            {
                GameObject ripple = CreatePrimitiveVisual(
                    "Predator Ripple " + (i + 1),
                    PrimitiveType.Sphere,
                    warningRoot.transform,
                    new Vector3(0f, -i * 0.012f, -i * 0.015f),
                    new Vector3(0.48f + i * 0.09f, 0.05f, 0.15f),
                    warningMaterial,
                    14 + i,
                    false);
                warningRipples[i] = ripple.transform;
                warningRenderers[i] = ripple.GetComponent<Renderer>();
                warningRenderers[i].enabled = false;
            }

            GameObject warningBubbleRoot = new GameObject("Warning Bubbles");
            warningBubbleRoot.transform.SetParent(warningRoot.transform, false);
            ParticleSystem warningBubbles = warningBubbleRoot.AddComponent<ParticleSystem>();
            ConfigureLagoonBubbleParticles(warningBubbles);

            GameObject launchSplashRoot = new GameObject("Ambush Launch Splash");
            launchSplashRoot.transform.SetParent(root.transform, false);
            launchSplashRoot.transform.localPosition = new Vector3(-1.95f, 0.04f, -0.26f);
            ParticleSystem launchSplash = launchSplashRoot.AddComponent<ParticleSystem>();
            ConfigureLagoonSplashParticles(launchSplash);

            GameObject motionRoot = new GameObject("Crocodile Motion Root");
            motionRoot.transform.SetParent(root.transform, false);
            motionRoot.transform.localPosition = new Vector3(0f, -0.18f, 0f);

            Animator crocodileAnimator;
            GameObject visualRoot = CreateCrocodileModelInstance(
                crocodileModel,
                motionRoot.transform,
                "Crocodile Ambush 3D",
                1.05f,
                -0.74f,
                12,
                AnimatorUpdateMode.Normal,
                new Vector3(0f, 0f, -0.38f),
                false,
                out crocodileAnimator);

            GameObject bitePoint = new GameObject("Bite Point");
            bitePoint.transform.SetParent(motionRoot.transform, false);
            bitePoint.transform.localPosition = new Vector3(-2.02f, 0.14f, -0.1f);

            GameObject biteTrigger = new GameObject("Bite Trigger");
            biteTrigger.transform.SetParent(motionRoot.transform, false);
            biteTrigger.transform.localPosition = new Vector3(-2.02f, 0.14f, 0f);
            BoxCollider2D biteCollider = biteTrigger.AddComponent<BoxCollider2D>();
            biteCollider.isTrigger = true;
            biteCollider.size = new Vector2(1.65f, 1.36f);
            biteCollider.enabled = false;

            CrocodileAmbushController controller = root.AddComponent<CrocodileAmbushController>();
            CrocodileAmbushHitbox hitbox = biteTrigger.AddComponent<CrocodileAmbushHitbox>();
            SetObject(hitbox, "controller", controller);
            SetObject(controller, "motionRoot", motionRoot.transform);
            SetObject(controller, "visualRoot", visualRoot);
            SetObject(controller, "animator", crocodileAnimator);
            SetObject(controller, "biteCollider", biteCollider);
            SetObject(controller, "bitePoint", bitePoint.transform);
            SetObject(controller, "warningRoot", warningRoot);
            SetObjectArray(controller, "warningRipples", warningRipples);
            SetObjectArray(controller, "warningRenderers", warningRenderers);
            SetObject(controller, "warningBubbles", warningBubbles);
            SetObject(controller, "launchSplash", launchSplash);
            SetFloat(controller, "activationDistance", 7.35f);
            SetFloat(controller, "minimumLeadDistance", 1.6f);
            SetFloat(controller, "minimumFuel", 17.5f);
            SetFloat(controller, "skipBehindDistance", -0.8f);
            SetFloat(controller, "warningDuration", 0.8f);
            SetFloat(controller, "lungeDuration", 0.84f);
            SetFloat(controller, "biteWindowStart", 0.2f);
            SetFloat(controller, "biteWindowEnd", 0.58f);
            SetFloat(controller, "missSnapNormalizedTime", 0.48f);
            SetFloat(controller, "settleDuration", 0.42f);
            SetFloat(controller, "playerSnapDuration", 0.13f);
            SetVector3(controller, "submergedLocalPosition", new Vector3(0f, -0.18f, 0f));
            SetFloat(controller, "lungeHeight", 2.55f);
            SetFloat(controller, "lungeHorizontalTravel", -0.48f);
            SetFloat(controller, "warningBobHeight", 0.045f);
            SetFloat(controller, "settleDepth", 0.46f);
            SetVector3(controller, "playerBiteOffset", new Vector3(0.08f, 0.08f, 0f));
            SetFloat(controller, "warningSplashVolume", 0.28f);
            SetFloat(controller, "launchSplashVolume", 0.72f);
            SetFloat(controller, "missChompVolume", 0.42f);
            SetFloat(controller, "launchShakeIntensity", 0.14f);
            SetFloat(controller, "launchShakeDuration", 0.24f);

            warningRoot.SetActive(false);
            visualRoot.SetActive(false);
            return SavePrefab(root, PrefabRoot + "/Hazard_CrocodileAmbush.prefab");
        }

        private static GameObject BuildHazardPrefab(string name, Vector2 colliderSize, ModelVisualAsset modelAsset, float modelHeight, string primitiveKind)
        {
            GameObject root = new GameObject(name);
            root.tag = "Obstacle";
            if (HasModel(modelAsset))
            {
                CreateModelVisualInstance(modelAsset, "Visual_3D", root.transform, Vector3.zero, modelHeight, -colliderSize.y * 0.5f, 2, Quaternion.Euler(0f, 34f, 0f), true);
            }
            else
            {
                CreateHazardPrimitiveVisual(primitiveKind, root.transform, colliderSize);
            }

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = colliderSize;

            root.AddComponent<ArcadeHazard>();
            root.AddComponent<DestroyBehindTarget>();
            return SavePrefab(root, PrefabRoot + "/" + name + ".prefab");
        }

        private static GameObject BuildAudioManagerPrefab()
        {
            GameObject root = CreateAudioManagerRoot();
            return SavePrefab(root, FrameworkRoot + "/Prefabs/Manager_Audio.prefab");
        }

        private static GameObject BuildSpawnerPrefab<T>(string name) where T : ArcadeSpawner2D
        {
            GameObject root = new GameObject(name);
            root.AddComponent<T>();
            return SavePrefab(root, PrefabRoot + "/" + name + ".prefab");
        }

        private static GameObject BuildPlainPrefab<T>(string name) where T : Component
        {
            GameObject root = new GameObject(name);
            root.AddComponent<T>();
            return SavePrefab(root, PrefabRoot + "/" + name + ".prefab");
        }

        private static void BuildMainMenuScene(SpriteSet sprites, GorillaModelAssets gorillaModel, MeshyGameAssets meshyAssets)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            UnityEngine.Camera camera = CreateCamera("Main Camera", new Vector3(0f, 0f, -10f), 5.2f, new Color(0.37f, 0.72f, 0.73f, 1f));
            CreateWorldLighting("Menu");
            CreateAudioManagerRoot();
            new GameObject("Manager_Time").AddComponent<ArcadeTimeController>();
            CreateEventSystem();
            CreateMenuBackdrop(camera.transform, meshyAssets);
            CreateMenuHeroArt(gorillaModel, meshyAssets);

            Canvas canvas = CreateCanvas("MainMenuCanvas");
            MainMenuController menu = new GameObject("Manager_MainMenu").AddComponent<MainMenuController>();

            Text title = CreateText("Title", canvas.transform, "GASSY GORILLA", 54, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.38f, 1f));
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(720f, 76f));

            Text best = CreateText("Best Distance", canvas.transform, "Best Distance: 0 m", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            SetRect(best.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -146f), new Vector2(420f, 44f));

            Button playButton = CreateButton("PlayButton", canvas.transform, "PLAY", new Color(0.36f, 0.78f, 0.28f, 1f));
            SetRect(playButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(244f, 64f));
            UnityEventTools.AddPersistentListener(playButton.onClick, menu.Play);

            Button settingsButton = CreateButton("SettingsButton", canvas.transform, "SETTINGS", new Color(0.22f, 0.55f, 0.78f, 1f));
            SetRect(settingsButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(244f, 54f));
            UnityEventTools.AddPersistentListener(settingsButton.onClick, menu.OpenSettings);

            ArcadeSettingsMenu settingsMenu = CreateSettingsPanel(canvas.transform, "SettingsPanel_Menu");
            SetObject(menu, "bestDistanceText", best);
            SetObject(menu, "settingsMenu", settingsMenu);

            Button closeButton = settingsMenu.transform.Find("CloseButton").GetComponent<Button>();
            SetEnum(closeButton.GetComponent<ArcadeButtonFeedback>(), "clickSfx", (int)ArcadeSfxType.UiBack);
            UnityEventTools.AddPersistentListener(closeButton.onClick, menu.CloseSettings);

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void BuildGameScene(
            SpriteSet sprites,
            PrefabSet prefabs,
            MeshyGameAssets meshyAssets,
            RunChunkSet runChunks,
            RunDifficultyProfile difficultyProfile)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            UnityEngine.Camera camera = CreateCamera("Main Camera", new Vector3(2f, 0.35f, -10f), 5.2f, new Color(0.36f, 0.69f, 0.76f, 1f));
            CreateWorldLighting("Game");
            CreateAudioManagerRoot();
            new GameObject("Manager_Time").AddComponent<ArcadeTimeController>();
            CreateEventSystem();

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Player);
            player.name = "Player_GassyGorilla";
            player.transform.position = new Vector3(-2.2f, 1.1f, 0f);
            GorillaController gorilla = player.GetComponent<GorillaController>();

            SmoothCameraFollow2D follow = camera.gameObject.AddComponent<SmoothCameraFollow2D>();
            SetObject(follow, "target", player.transform);
            SetObject(follow, "velocitySource", player.GetComponent<Rigidbody2D>());
            SetVector3(follow, "offset", new Vector3(4f, 0.15f, -10f));
            SetFloat(follow, "smoothTime", 0.2f);
            SetFloat(follow, "minY", -1.1f);
            SetFloat(follow, "maxY", 5.2f);
            SetFloat(follow, "baseOrthographicSize", 5.2f);
            SetFloat(follow, "zoomOutAtSpeed", 0.28f);
            SetFloat(follow, "speedLookaheadX", 0.18f);
            SetFloat(follow, "speedLookaheadY", 0.045f);
            SetFloat(follow, "maxExtraLookahead", 0.96f);
            SetFloat(follow, "actionLookaheadReturnTime", 0.2f);
            SetObject(gorilla, "cameraFollow", follow);

            CreateGameBackdrop(camera.transform, meshyAssets);
            CreateFallDeathZone(camera.transform);

            Canvas canvas = CreateCanvas("GameCanvas");
            CreateHudScrim(canvas.transform);
            Text scoreText = CreateText("DistanceText", canvas.transform, "0 m", 27, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            SetRect(scoreText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(104f, -34f), new Vector2(180f, 46f));

            Text bestHudText = CreateText("BestText", canvas.transform, "Best 0 m", 16, FontStyle.Normal, TextAnchor.MiddleRight, new Color(1f, 0.94f, 0.72f, 1f));
            SetRect(bestHudText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-106f, -32f), new Vector2(188f, 40f));

            FartBarUI fartBar = CreateFartBar(canvas.transform, gorilla, sprites);
            TextOverlay milestoneOverlay = CreateMilestoneOverlay(canvas.transform);
            TextOverlay tutorialOverlay = CreateTutorialOverlay(canvas.transform);
            ArcadeSettingsMenu settingsMenu = CreateSettingsPanel(canvas.transform, "SettingsPanel_Game");
            Button gameSettingsCloseButton = settingsMenu.transform.Find("CloseButton").GetComponent<Button>();
            SetEnum(gameSettingsCloseButton.GetComponent<ArcadeButtonFeedback>(), "clickSfx", (int)ArcadeSfxType.UiBack);
            UnityEventTools.AddPersistentListener(gameSettingsCloseButton.onClick, settingsMenu.Close);

            Button settingsButton = CreateButton("SettingsButton", canvas.transform, "SETTINGS", new Color(0.19f, 0.5f, 0.68f, 1f));
            SetRect(settingsButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-82f, -72f), new Vector2(132f, 38f));
            UnityEventTools.AddPersistentListener(settingsButton.onClick, settingsMenu.Toggle);

            GameObject scoreObject = new GameObject("Manager_Score");
            DistanceScoreTracker distanceTracker = scoreObject.AddComponent<DistanceScoreTracker>();
            SetObject(distanceTracker, "distanceTarget", player.transform);
            GassyScoreManager scoreManager = scoreObject.AddComponent<GassyScoreManager>();
            SetObject(scoreManager, "distanceTracker", distanceTracker);
            SetObject(scoreManager, "scoreText", scoreText);

            GameObject runDirectorObject = new GameObject("Director_RunChunks");
            RunChunkDirector runDirector = runDirectorObject.AddComponent<RunChunkDirector>();
            runDirector.ConfigureContent(player.transform, runChunks.All, runChunks.Opening);
            runDirector.ConfigureDifficulty(gorilla, difficultyProfile);
            EditorUtility.SetDirty(runDirector);
            SetBool(runDirector, "spawning", false);
            SetBool(runDirector, "prewarmOnStart", true);
            SetFloat(runDirector, "firstChunkStartX", 0f);
            SetFloat(runDirector, "spawnAheadDistance", 38f);
            SetFloat(runDirector, "cleanupBehindDistance", 20f);
            SetInt(runDirector, "recentHistoryLength", 2);
            SetFloat(runDirector, "distancePerDifficultyStep", 75f);
            SetInt(runDirector, "maximumDifficulty", 4);
            SetBool(runDirector, "randomizeSeed", true);
            SetInt(runDirector, "fixedSeed", 142857);
            SetBool(runDirector, "logSeed", true);

            GameObject milestoneObject = new GameObject("Manager_Milestones");
            MilestoneEventManager milestones = milestoneObject.AddComponent<MilestoneEventManager>();
            SetObject(milestones, "scoreManager", scoreManager);
            SetObject(milestones, "textOverlay", milestoneOverlay);
            ConfigureMilestones(milestones);

            GameObject tutorialObject = new GameObject("Manager_TutorialPrompts");
            GassyTutorialPromptController tutorial = tutorialObject.AddComponent<GassyTutorialPromptController>();
            SetObject(tutorial, "gorilla", gorilla);
            SetObject(tutorial, "overlay", tutorialOverlay);
            SetFloat(tutorial, "openingDelay", 1.35f);

            CanvasGroupPanel gameOverPanel = CreateGameOverPanel(canvas.transform, out Text currentDistanceText, out Text bestDistanceText, out Button retryButton, out Button mainMenuButton);

            GameObject gameManagerObject = new GameObject("Manager_Game");
            GassyGorillaGameManager gameManager = gameManagerObject.AddComponent<GassyGorillaGameManager>();
            SetObject(gameManager, "player", gorilla);
            SetObject(gameManager, "scoreManager", scoreManager);
            SetObjectArray(gameManager, "spawners", Array.Empty<ArcadeSpawner2D>());
            SetObject(gameManager, "runChunkDirector", runDirector);
            SetObject(gameManager, "cameraFollow", follow);
            SetObject(gameManager, "sceneCamera", camera);
            SetObject(gameManager, "lagoonFinishPresentation", gorilla.GetComponent<LagoonFinishPresentation>());
            SetObject(gameManager, "tutorialPrompt", tutorial);
            SetFloat(gameManager, "deathY", -1.72f);
            SetFloat(gameManager, "gameOverRestY", -1.72f);
            SetFloat(gameManager, "lagoonResultRevealDelay", 1.02f);
            SetFloat(gameManager, "crocodileAmbushResultRevealDelay", 0.9f);
            SetFloat(gameManager, "hazardResultRevealDelay", 0.08f);
            SetFloat(gameManager, "introDuration", 1.15f);
            SetFloat(gameManager, "introStartZoom", 2.85f);
            SetVector3(gameManager, "introStartOffset", new Vector3(-0.4f, -0.28f, -10f));
            SetVector3(gameManager, "introEndOffset", new Vector3(4f, 0.15f, -10f));
            SetFloat(gameManager, "outroDuration", 0.78f);
            SetFloat(gameManager, "outroZoom", 3.25f);
            SetVector3(gameManager, "outroOffset", new Vector3(1.2f, 0.24f, -10f));
            SetObject(gameManager, "gameOverPanel", gameOverPanel);
            SetObject(gameManager, "currentDistanceText", currentDistanceText);
            SetObject(gameManager, "bestDistanceText", bestDistanceText);
            SetObject(gameManager, "hudBestDistanceText", bestHudText);

            GameObject audioDirectorObject = new GameObject("Director_Audio");
            GassyGorillaAudioDirector audioDirector = audioDirectorObject.AddComponent<GassyGorillaAudioDirector>();
            SetObject(audioDirector, "runDirector", runDirector);
            SetObject(audioDirector, "gorilla", gorilla);
            SetObject(audioDirector, "gameManager", gameManager);

            UnityEventTools.AddPersistentListener(retryButton.onClick, gameManager.RestartRun);
            SetEnum(mainMenuButton.GetComponent<ArcadeButtonFeedback>(), "clickSfx", (int)ArcadeSfxType.UiBack);
            UnityEventTools.AddPersistentListener(mainMenuButton.onClick, gameManager.ReturnToMainMenu);

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
        }

        private static void CreateOpeningPlayfield(PrefabSet prefabs)
        {
            PlaceOpeningPrefab(prefabs.Bean, "Opening_Bean_01", new Vector3(1.8f, 1.65f, 0f), Quaternion.Euler(0f, 0f, -10f));
            PlaceOpeningPrefab(prefabs.Burrito, "Opening_Burrito_01", new Vector3(4.55f, 2.25f, 0f), Quaternion.Euler(0f, 0f, 8f));
            PlaceOpeningPrefab(prefabs.SwingableVine, "Opening_Vine_01", new Vector3(8.4f, 2.85f, 0f), Quaternion.identity);
            PlaceOpeningPrefab(prefabs.Bean, "Opening_Bean_02", new Vector3(11.4f, 1.1f, 0f), Quaternion.Euler(0f, 0f, 12f));
            PlaceOpeningPrefab(prefabs.BananaBunch, "Opening_BananaBunch_01", new Vector3(13.6f, 2.05f, 0f), Quaternion.Euler(0f, 0f, -6f));
        }

        private static void PlaceOpeningPrefab(GameObject prefab, string name, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.position = position;
            instance.transform.rotation = rotation;
        }

        private static UnityEngine.Camera CreateCamera(string name, Vector3 position, float orthographicSize, Color background)
        {
            GameObject cameraObject = new GameObject(name);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = position;
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.backgroundColor = background;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            camera.depthTextureMode = DepthTextureMode.None;
            return camera;
        }

        private static void EnsureSceneAudioListener(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            UnityEngine.Camera[] cameras = UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(FindObjectsInactive.Include);
            UnityEngine.Camera mainCamera = null;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i].CompareTag("MainCamera"))
                {
                    mainCamera = cameras[i];
                    break;
                }
            }

            if (mainCamera == null)
            {
                throw new InvalidOperationException(scenePath + " has no camera tagged MainCamera.");
            }

            AudioListener[] listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            if (listeners.Length > 1)
            {
                throw new InvalidOperationException(scenePath + " has multiple AudioListeners. Resolve the duplicate before repairing audio output.");
            }

            AudioListener listener = listeners.Length == 1
                ? listeners[0]
                : mainCamera.gameObject.AddComponent<AudioListener>();
            listener.enabled = true;
            EditorUtility.SetDirty(listener);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static GameObject CreateAudioManagerRoot()
        {
            GameObject root = new GameObject("Manager_Audio");
            ArcadeAudioManager manager = root.AddComponent<ArcadeAudioManager>();

            AudioSource music = CreateAudioSourceChild(root.transform, "Music Source", true);
            AudioSource intensityMusic = CreateAudioSourceChild(root.transform, "Intensity Music Source", true);
            AudioSource ambience = CreateAudioSourceChild(root.transform, "Ambience Source", true);
            AudioSource sfx = CreateAudioSourceChild(root.transform, "SFX Source", false);
            AudioSource voice = CreateAudioSourceChild(root.transform, "Voice Source", false);
            ArcadeAudioLibrary library = AssetDatabase.LoadAssetAtPath<ArcadeAudioLibrary>(AudioLibraryPath);
            if (library == null)
            {
                throw new InvalidOperationException("Production audio library is missing. Generate production audio before building scenes.");
            }

            SetObject(manager, "audioLibrary", library);
            SetObject(manager, "musicSource", music);
            SetObject(manager, "intensityMusicSource", intensityMusic);
            SetObject(manager, "ambienceSource", ambience);
            SetObject(manager, "sfxSource", sfx);
            SetObject(manager, "voiceSource", voice);
            SetObject(manager, "musicClip", library.BaseMusic);
            SetBool(manager, "generatePlaceholderMusic", false);
            SetInt(manager, "sfxVoiceCount", 8);
            return root;
        }

        private static AudioSource CreateAudioSourceChild(Transform parent, string name, bool loop)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            return source;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject canvasObject = new GameObject(name);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateWorldLighting(string sceneKey)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.78f, 0.72f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.29f, 0.48f, 0.38f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.17f, 0.13f, 1f);
            RenderSettings.ambientIntensity = 0.82f;
            RenderSettings.fog = false;

            GameObject keyObject = new GameObject("World_KeyLight_" + sceneKey);
            Light key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1f, 0.91f, 0.68f, 1f);
            key.intensity = 1.08f;
            key.shadows = LightShadows.None;
            key.renderMode = LightRenderMode.ForcePixel;
            keyObject.transform.rotation = Quaternion.Euler(38f, -32f, -18f);

            GameObject fillObject = new GameObject("World_FillLight_" + sceneKey);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.38f, 0.72f, 0.74f, 1f);
            fill.intensity = 0.32f;
            fill.shadows = LightShadows.None;
            fill.renderMode = LightRenderMode.ForceVertex;
            fillObject.transform.rotation = Quaternion.Euler(-24f, 145f, 12f);
        }

        private static void CreatePaintedJungleBackdrop(string name, Transform cameraTransform, float parallax, Color tint)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PaintedJungleTexturePath);
            if (texture == null)
            {
                CreateSkyLayer3D(cameraTransform);
                return;
            }

            const float tileWidth = 18.62f;
            GameObject root = new GameObject(name);
            root.transform.position = new Vector3(-tileWidth, 0.12f, 7.1f);
            ParallaxBand2D band = root.AddComponent<ParallaxBand2D>();
            SetObject(band, "target", cameraTransform);
            SetFloat(band, "parallaxX", parallax);
            SetFloat(band, "parallaxY", 1f);
            SetFloat(band, "tileWidth", tileWidth);

            Material regular = CreatePaintedBackdropMaterial(name + "_Regular", texture, tint, false);
            Material mirrored = CreatePaintedBackdropMaterial(name + "_Mirrored", texture, tint, true);
            Transform[] tiles = new Transform[3];
            for (int i = 0; i < tiles.Length; i++)
            {
                GameObject tile = CreatePrimitiveVisual(
                    "JungleTexturePanel_" + (i + 1),
                    PrimitiveType.Cube,
                    root.transform,
                    new Vector3(i * tileWidth, 0f, 0f),
                    new Vector3(tileWidth + 0.035f, 12.1f, 0.09f),
                    i % 2 == 0 ? regular : mirrored,
                    -10,
                    false);
                tiles[i] = tile.transform;
            }

            SetObjectArray(band, "tiles", tiles);
        }

        private static Material CreatePaintedBackdropMaterial(string key, Texture2D texture, Color tint, bool mirrored)
        {
            string materialPath = GameRoot + "/Materials/" + key + ".mat";
            Shader shader = Shader.Find("Unlit/Texture");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (shader != null)
            {
                material.shader = shader;
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", mirrored ? new Vector2(-1f, 1f) : Vector2.one);
                material.SetTextureOffset("_MainTex", mirrored ? new Vector2(1f, 0f) : Vector2.zero);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tint);
            }

            material.enableInstancing = true;
            material.renderQueue = -1;
            material.SetOverrideTag("RenderType", "Opaque");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateBackdropDepthVeil(string name, Transform cameraTransform, Color color)
        {
            GameObject root = new GameObject(name);
            root.transform.position = new Vector3(0f, 0.15f, 5.65f);
            FollowTargetX follow = root.AddComponent<FollowTargetX>();
            SetObject(follow, "target", cameraTransform);
            SetBool(follow, "followY", true);

            Material material = CreateColorMaterial(name + "_MistedTexture", color, true);
            GameObject veil = CreatePrimitiveVisual("AnimatedMistSurface_3D", PrimitiveType.Cube, root.transform, Vector3.zero, new Vector3(40f, 12.8f, 0.04f), material, -8);
            AddAmbientSway(veil, 0.24f, 0.035f, 0f, 0.008f, 0.09f, 0.8f);
        }

        private static void CreateMenuBackdrop(Transform cameraTransform, MeshyGameAssets meshyAssets)
        {
            CreatePaintedJungleBackdrop("Menu_PaintedJungleBackdrop_3D", cameraTransform, 0.018f, new Color(0.64f, 0.79f, 0.73f, 1f));
            CreateBackdropDepthVeil("Menu_JungleDepthVeil_3D", cameraTransform, new Color(0.2f, 0.48f, 0.42f, 0.25f));
        }

        private static void CreateGameBackdrop(Transform cameraTransform, MeshyGameAssets meshyAssets)
        {
            CreatePaintedJungleBackdrop("PaintedJungleBackdrop_3D", cameraTransform, 0.032f, new Color(0.54f, 0.7f, 0.67f, 1f));
            CreateBackdropDepthVeil("JungleDepthVeil_3D", cameraTransform, new Color(0.14f, 0.42f, 0.39f, 0.34f));
            CreateJungleLagoon(cameraTransform);
        }

        private static void CreateJungleLagoon(Transform cameraTransform)
        {
            GameObject root = new GameObject("JungleLagoon_3D");
            root.transform.position = new Vector3(0f, -2.43f, -0.42f);
            FollowTargetX follow = root.AddComponent<FollowTargetX>();
            SetObject(follow, "target", cameraTransform);

            Material deepWater = CreateColorMaterial("GG_LagoonDeepWater_3D", new Color(0.025f, 0.23f, 0.27f, 0.94f), true);
            Material surfaceWater = CreateColorMaterial("GG_LagoonSurface_3D", new Color(0.18f, 0.68f, 0.58f, 0.52f), true);
            Material highlight = CreateColorMaterial("GG_LagoonHighlight_3D", new Color(0.54f, 0.96f, 0.72f, 0.22f), true);

            CreatePrimitiveVisual("LagoonBody_3D", PrimitiveType.Cube, root.transform, new Vector3(0f, -3f, 0f), new Vector3(42f, 7.7f, 0.16f), deepWater, 10);
            CreatePrimitiveVisual("LagoonSurface_3D", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.82f, -0.03f), new Vector3(42f, 0.09f, 0.18f), surfaceWater, 11);
            CreatePrimitiveVisual("LagoonHighlight_3D", PrimitiveType.Cube, root.transform, new Vector3(-0.32f, 0.885f, -0.05f), new Vector3(42f, 0.02f, 0.19f), highlight, 12);
        }

        private static void CreateMenuHeroArt(GorillaModelAssets gorillaModel, MeshyGameAssets meshyAssets)
        {
            if (HasModel(meshyAssets.RootClusterA))
            {
                CreateModelVisualInstance(meshyAssets.RootClusterA, "Menu_GorillaPerch_3D", null, new Vector3(-3.32f, -1.62f, 0.18f), 1.18f, -2.12f, 2, Quaternion.Euler(0f, 28f, 0f), true);
            }

            if (HasModel(meshyAssets.MenuFartCloud))
            {
                CreateModelVisualInstance(meshyAssets.MenuFartCloud, "Menu_FartCloud", null, new Vector3(-4.15f, -1.15f, -0.12f), 0.95f, -1.46f, 2, Quaternion.Euler(0f, 35f, 0f), true);
            }
            else
            {
                CreatePrimitiveFartCloud("Menu_FartCloud", null, new Vector3(-4.15f, -1.15f, -0.12f), 0.95f, 2);
            }

            Animator menuAnimator;
            GameObject menuModel = CreateGorillaModelInstance(gorillaModel, "Menu_GorillaHero", null, new Vector3(-3.32f, -1.2f, -0.12f), 5.05f, -2.14f, 4, out menuAnimator);
            if (menuModel != null)
            {
                if (menuAnimator != null && menuAnimator.runtimeAnimatorController != null && menuAnimator.HasState(0, Animator.StringToHash("Idle")))
                {
                    menuAnimator.Play("Idle");
                }
            }
            else
            {
                CreatePrimitiveGorillaVisual("Menu_GorillaHero", null).transform.position = new Vector3(-3.35f, -1.28f, -0.12f);
            }

            if (HasModel(meshyAssets.MenuFoodPile))
            {
                CreateModelVisualInstance(meshyAssets.MenuFoodPile, "Menu_FoodPile", null, new Vector3(3.78f, -1.3f, -0.12f), 1.15f, -1.78f, 3, Quaternion.Euler(0f, 35f, 0f), true);
            }
            else
            {
                CreateMenuFoodVisual("Menu_Bean", meshyAssets.Bean, new Vector3(3.35f, -1.28f, -0.12f), 0.82f, -1.7f, "Bean");
                CreateMenuFoodVisual("Menu_Burrito", meshyAssets.Burrito, new Vector3(4.12f, -0.68f, -0.12f), 0.88f, -1.1f, "Burrito");
            }
        }

        private static void CreateSkyLayer(SpriteSet sprites, Transform cameraTransform)
        {
            if (sprites.SkyGradient != null)
            {
                GameObject sky = CreateSpriteChild("Sky_Gradient", null, sprites.SkyGradient, -6, Color.white);
                sky.transform.position = new Vector3(0f, 0.3f, 0f);
                sky.transform.localScale = new Vector3(30f, 3.2f, 1f);
                FollowTargetX followSky = sky.AddComponent<FollowTargetX>();
                SetObject(followSky, "target", cameraTransform);
            }

            if (sprites.MistBand != null)
            {
                GameObject mist = CreateSpriteChild("Low_Mist", null, sprites.MistBand, 0, new Color(1f, 1f, 1f, 0.74f));
                mist.transform.position = new Vector3(0f, -1.55f, 0f);
                mist.transform.localScale = new Vector3(22f, 1.7f, 1f);
                FollowTargetX followMist = mist.AddComponent<FollowTargetX>();
                SetObject(followMist, "target", cameraTransform);
            }
        }

        private static void CreateParallaxBand(string name, Sprite sprite, Transform cameraTransform, float startX, float y, float parallax, float tileWidth, int sortingOrder, Color tint)
        {
            GameObject root = new GameObject(name);
            root.transform.position = new Vector3(startX, y, 2f);
            ParallaxBand2D band = root.AddComponent<ParallaxBand2D>();
            SetObject(band, "target", cameraTransform);
            SetFloat(band, "parallaxX", parallax);
            SetFloat(band, "tileWidth", tileWidth);

            Transform[] tiles = new Transform[4];
            for (int i = 0; i < tiles.Length; i++)
            {
                GameObject tile = CreateSpriteChild("Tile_" + (i + 1), root.transform, sprite, sortingOrder, tint);
                tile.transform.localPosition = new Vector3(i * tileWidth, 0f, 0f);
                tile.transform.localScale = new Vector3(4.4f, 2.25f, 1f);
                tiles[i] = tile.transform;
            }

            SetObjectArray(band, "tiles", tiles);
        }

        private static void CreatePlantParallaxBand(string name, SpriteSet sprites, Transform cameraTransform, float y, float parallax, float tileWidth, int sortingOrder, Color tint, float scale)
        {
            Sprite[] plantSprites =
            {
                sprites.PlantLeft,
                sprites.PlantMiddle,
                sprites.PlantRight
            };

            if (plantSprites[0] == null && plantSprites[1] == null && plantSprites[2] == null)
            {
                return;
            }

            GameObject root = new GameObject(name);
            root.transform.position = new Vector3(-tileWidth, y, 1f);
            ParallaxBand2D band = root.AddComponent<ParallaxBand2D>();
            SetObject(band, "target", cameraTransform);
            SetFloat(band, "parallaxX", parallax);
            SetFloat(band, "tileWidth", tileWidth);

            Transform[] tiles = new Transform[5];
            for (int i = 0; i < tiles.Length; i++)
            {
                GameObject tile = new GameObject("Tile_" + (i + 1));
                tile.transform.SetParent(root.transform, false);
                tile.transform.localPosition = new Vector3(i * tileWidth, 0f, 0f);

                for (int j = 0; j < 3; j++)
                {
                    Sprite plantSprite = plantSprites[(i + j) % plantSprites.Length];
                    if (plantSprite == null)
                    {
                        plantSprite = sprites.BackgroundTree;
                    }

                    GameObject plant = CreateSpriteChild("Plant_" + (j + 1), tile.transform, plantSprite, sortingOrder, tint);
                    plant.transform.localPosition = new Vector3(-2.85f + j * 2.85f, Mathf.Sin((i + j) * 1.7f) * 0.12f, 0f);
                    float size = scale * (0.9f + ((i + j) % 3) * 0.08f);
                    plant.transform.localScale = new Vector3(size, size, 1f);
                }

                tiles[i] = tile.transform;
            }

            SetObjectArray(band, "tiles", tiles);
        }

        private static void CreatePlantDecor(string name, SpriteSet sprites, Transform cameraTransform, float y)
        {
            if (sprites.PlantLeft == null && sprites.PlantRight == null)
            {
                return;
            }

            GameObject root = new GameObject(name);
            root.transform.position = new Vector3(0f, y, 0f);
            FollowTargetX follow = root.AddComponent<FollowTargetX>();
            SetObject(follow, "target", cameraTransform);

            Sprite[] plantSprites =
            {
                sprites.PlantLeft != null ? sprites.PlantLeft : sprites.BackgroundTree,
                sprites.PlantMiddle != null ? sprites.PlantMiddle : sprites.BackgroundTree,
                sprites.PlantRight != null ? sprites.PlantRight : sprites.BackgroundTree,
                sprites.PlantLeft != null ? sprites.PlantLeft : sprites.BackgroundTree
            };

            float[] xPositions = { -7.6f, -3.1f, 2.2f, 7.1f };
            float[] scales = { 0.76f, 0.68f, 0.7f, 0.72f };
            for (int i = 0; i < plantSprites.Length; i++)
            {
                GameObject plant = CreateSpriteChild("Plant_" + (i + 1), root.transform, plantSprites[i], 1, Color.white);
                plant.transform.localPosition = new Vector3(xPositions[i], 0f, 0f);
                plant.transform.localScale = Vector3.one * scales[i];
            }
        }

        private static void CreateMeshyDecorBand(string name, Transform cameraTransform, float y, ModelVisualAsset[] assets, float targetHeight, float parallax, int sortingOrder)
        {
            GameObject root = new GameObject(name);
            root.transform.position = new Vector3(0f, y, 0f);
            FollowTargetX follow = root.AddComponent<FollowTargetX>();
            SetObject(follow, "target", cameraTransform);

            float[] xPositions = { -6.8f, 0f, 6.65f };
            for (int i = 0; i < xPositions.Length; i++)
            {
                ModelVisualAsset asset = FirstAvailableModel(assets, i);
                GameObject holder = new GameObject("Decor3D_" + (i + 1));
                holder.transform.SetParent(root.transform, false);
                holder.transform.localPosition = new Vector3(xPositions[i], Mathf.Sin(i * 1.7f) * 0.035f, -0.12f);
                if (HasModel(asset))
                {
                    CreateModelVisualInstance(asset, "Visual", holder.transform, Vector3.zero, targetHeight * (0.92f + (i % 3) * 0.08f), -0.48f, sortingOrder, Quaternion.Euler(0f, 28f + i * 7f, 0f), true);
                }
                else
                {
                    CreatePrimitivePlantVisual("Visual", holder.transform, targetHeight * (0.92f + (i % 3) * 0.08f), sortingOrder, i);
                }

            }
        }

        private static void CreateMenuFoodVisual(string name, ModelVisualAsset modelAsset, Vector3 position, float modelHeight, float bottomY, string primitiveKind)
        {
            if (HasModel(modelAsset))
            {
                CreateModelVisualInstance(modelAsset, name, null, position, modelHeight, bottomY, 3, Quaternion.Euler(0f, 38f, 0f), true);
                return;
            }

            GameObject item = new GameObject(name);
            item.transform.position = position;
            CreatePickupPrimitiveVisual(primitiveKind, item.transform);
        }

        private static void CreateFallDeathZone(Transform cameraTransform)
        {
            GameObject death = new GameObject("DeathZone");
            death.tag = "DeathZone";
            death.transform.position = new Vector3(0f, -5.45f, 0f);
            BoxCollider2D collider = death.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(34f, 1.2f);
            death.AddComponent<ArcadeHazard>();
            FollowTargetX followDeath = death.AddComponent<FollowTargetX>();
            SetObject(followDeath, "target", cameraTransform);
        }

        private static void CreateAmbientMotes(Transform cameraTransform)
        {
            GameObject root = new GameObject("Ambient_GlowMotes");
            root.transform.SetParent(cameraTransform, false);
            root.transform.localPosition = new Vector3(0f, 0f, 8f);

            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.duration = 7f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 1f, 0.5f, 0.18f), new Color(0.55f, 1f, 0.68f, 0.28f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 90;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 8f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12.5f, 7.4f, 0.1f);

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, -0.02f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.015f, 0.08f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            ConfigureParticleMeshRenderer(renderer, PrimitiveType.Sphere, "GG_AmbientMote_Particle3D", new Color(0.76f, 1f, 0.48f, 0.72f), 6);
        }

        private static void CreateHudScrim(Transform parent)
        {
            GameObject root = new GameObject("HUD_TopScrim");
            root.transform.SetParent(parent, false);
            Image image = root.AddComponent<Image>();
            image.color = new Color(0.015f, 0.09f, 0.075f, 0.3f);
            image.raycastTarget = false;
            SetRect(image.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -38f), new Vector2(0f, 76f));
        }

        private static FartBarUI CreateFartBar(Transform parent, GorillaController gorilla, SpriteSet sprites)
        {
            GameObject root = new GameObject("HUD_FartBar");
            RectTransform rect = root.AddComponent<RectTransform>();
            root.transform.SetParent(parent, false);
            SetRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -43f), new Vector2(408f, 62f));

            Image back = root.AddComponent<Image>();
            back.sprite = UiPanelSprite();
            back.type = Image.Type.Sliced;
            back.color = new Color(0.025f, 0.04f, 0.035f, 0.96f);
            back.raycastTarget = false;
            Shadow shadow = root.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.48f);
            shadow.effectDistance = new Vector2(0f, -5f);
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(0.76f, 0.5f, 0.16f, 0.78f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);

            GameObject railObject = new GameObject("BrassTopRail");
            railObject.transform.SetParent(root.transform, false);
            Image rail = railObject.AddComponent<Image>();
            rail.sprite = sprites.FuelSegment;
            rail.type = Image.Type.Sliced;
            rail.color = new Color(0.86f, 0.62f, 0.2f, 0.92f);
            rail.raycastTarget = false;
            SetRect(rail.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -3f), new Vector2(390f, 4f));

            GameObject iconWellObject = new GameObject("FuelIconWell");
            iconWellObject.transform.SetParent(root.transform, false);
            Image iconWell = iconWellObject.AddComponent<Image>();
            iconWell.sprite = UiPanelSprite();
            iconWell.type = Image.Type.Sliced;
            iconWell.color = new Color(0.14f, 0.085f, 0.035f, 0.98f);
            iconWell.raycastTarget = false;
            SetRect(iconWell.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-171f, -1f), new Vector2(50f, 50f));

            GameObject iconObject = new GameObject("FuelCloudIcon");
            iconObject.transform.SetParent(iconWellObject.transform, false);
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = sprites.FuelBar;
            icon.preserveAspect = true;
            icon.color = Color.white;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(42f, 42f));

            Text title = CreateText("Label", root.transform, "FART FUEL", 14, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.96f, 0.98f, 0.9f, 1f));
            title.raycastTarget = false;
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-72f, 14f), new Vector2(132f, 20f));

            Text value = CreateText("Value", root.transform, "100", 17, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.78f, 0.98f, 1f, 1f));
            value.raycastTarget = false;
            SetRect(value.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(165f, 14f), new Vector2(52f, 20f));

            GameObject trackObject = new GameObject("FuelGlassTrack");
            trackObject.transform.SetParent(root.transform, false);
            Image track = trackObject.AddComponent<Image>();
            track.sprite = UiPanelSprite();
            track.type = Image.Type.Sliced;
            track.color = new Color(0.008f, 0.02f, 0.018f, 0.98f);
            track.raycastTarget = false;
            SetRect(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(29f, -13f), new Vector2(328f, 21f));

            GameObject fillObject = new GameObject("FuelGlowFill");
            fillObject.transform.SetParent(trackObject.transform, false);
            Image fill = fillObject.AddComponent<Image>();
            fill.sprite = sprites.FuelSegment;
            fill.color = new Color(0.48f, 0.92f, 1f, 0.24f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.raycastTarget = false;
            SetRect(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-8f, -8f));

            const int segmentCount = 10;
            const float innerTrackWidth = 316f;
            const float segmentGap = 3f;
            float segmentWidth = (innerTrackWidth - segmentGap * (segmentCount - 1)) / segmentCount;
            float firstSegmentX = -innerTrackWidth * 0.5f + segmentWidth * 0.5f;
            Image[] segments = new Image[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                GameObject segmentObject = new GameObject("FuelSegment_" + (i + 1).ToString("00"));
                segmentObject.transform.SetParent(trackObject.transform, false);
                Image segment = segmentObject.AddComponent<Image>();
                segment.sprite = sprites.FuelSegment;
                segment.type = Image.Type.Sliced;
                segment.color = new Color(0.48f, 0.92f, 1f, 1f);
                segment.raycastTarget = false;
                float x = firstSegmentX + i * (segmentWidth + segmentGap);
                SetRect(segment.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(segmentWidth, 13f));
                segments[i] = segment;
            }

            GameObject glassHighlightObject = new GameObject("GlassHighlight");
            glassHighlightObject.transform.SetParent(trackObject.transform, false);
            Image glassHighlight = glassHighlightObject.AddComponent<Image>();
            glassHighlight.color = new Color(0.72f, 1f, 0.9f, 0.16f);
            glassHighlight.raycastTarget = false;
            SetRect(glassHighlight.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6.5f), new Vector2(312f, 1.5f));

            MeterFillUI meter = root.AddComponent<MeterFillUI>();
            SetObject(meter, "fillImage", fill);
            SetObjectArray(meter, "segmentImages", segments);
            SetObject(meter, "valueLabel", value);
            SetColor(meter, "normalColor", new Color(0.34f, 0.96f, 0.38f, 1f));
            SetColor(meter, "lowColor", new Color(1f, 0.3f, 0.12f, 1f));
            SetColor(meter, "fullColor", new Color(0.48f, 0.92f, 1f, 1f));
            SetColor(meter, "inactiveSegmentColor", new Color(0.08f, 0.14f, 0.12f, 0.82f));
            SetFloat(meter, "lowThreshold", 0.26f);
            SetFloat(meter, "smoothSpeed", 18f);
            SetBool(meter, "showMaximum", false);

            FartBarUI fartBar = root.AddComponent<FartBarUI>();
            SetObject(fartBar, "gorilla", gorilla);
            SetObject(fartBar, "meter", meter);
            SetObject(fartBar, "titleText", title);
            SetObject(fartBar, "iconImage", icon);
            SetFloat(fartBar, "lowFuelThreshold", 0.26f);
            SetColor(fartBar, "normalIconColor", Color.white);
            SetColor(fartBar, "lowIconColor", new Color(1f, 0.72f, 0.24f, 1f));
            return fartBar;
        }

        private static TextOverlay CreateMilestoneOverlay(Transform parent)
        {
            GameObject root = new GameObject("MilestoneOverlay");
            root.transform.SetParent(parent, false);
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            RectTransform rect = root.AddComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), Vector2.zero, new Vector2(620f, 62f));

            Text text = CreateText("MilestoneText", root.transform, "", 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.48f, 1f));
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = 27;
            SetRect(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            TextOverlay overlay = root.AddComponent<TextOverlay>();
            SetObject(overlay, "group", group);
            SetObject(overlay, "messageText", text);
            return overlay;
        }

        private static TextOverlay CreateTutorialOverlay(Transform parent)
        {
            GameObject root = new GameObject("TutorialOverlay");
            root.transform.SetParent(parent, false);
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            RectTransform rect = root.AddComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(420f, 50f));

            Image backing = root.AddComponent<Image>();
            backing.sprite = UiPanelSprite();
            backing.type = Image.Type.Sliced;
            backing.color = new Color(0.035f, 0.11f, 0.09f, 0.68f);

            Text text = CreateText("TutorialText", root.transform, "", 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.56f, 1f));
            SetRect(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-22f, -8f));

            TextOverlay overlay = root.AddComponent<TextOverlay>();
            SetObject(overlay, "group", group);
            SetObject(overlay, "messageText", text);
            SetFloat(overlay, "fadeInDuration", 0.1f);
            SetFloat(overlay, "fadeOutDuration", 0.22f);
            return overlay;
        }

        private static CanvasGroupPanel CreateGameOverPanel(Transform parent, out Text currentDistanceText, out Text bestDistanceText, out Button retryButton, out Button mainMenuButton)
        {
            GameObject panel = new GameObject("UI_GameOverPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 326f));
            Image image = panel.AddComponent<Image>();
            image.sprite = UiPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.045f, 0.1f, 0.08f, 0.95f);
            Shadow shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
            shadow.effectDistance = new Vector2(0f, -8f);
            CanvasGroup group = panel.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            CanvasGroupPanel panelController = panel.AddComponent<CanvasGroupPanel>();
            SetObject(panelController, "canvasGroup", group);

            Text title = CreateText("Title", panel.transform, "RUN OVER", 38, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.35f, 1f));
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(380f, 52f));

            Text distanceLabel = CreateText("DistanceLabel", panel.transform, "DISTANCE", 16, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.76f));
            SetRect(distanceLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(240f, 28f));

            currentDistanceText = CreateText("CurrentDistance", panel.transform, "0 m", 32, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            SetRect(currentDistanceText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(250f, 44f));

            bestDistanceText = CreateText("BestDistance", panel.transform, "BEST  0 m", 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.75f, 1f, 0.74f, 1f));
            SetRect(bestDistanceText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(280f, 34f));

            retryButton = CreateButton("RetryButton", panel.transform, "RETRY", new Color(0.36f, 0.78f, 0.28f, 1f));
            SetRect(retryButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 78f), new Vector2(210f, 52f));

            mainMenuButton = CreateButton("MainMenuButton", panel.transform, "MAIN MENU", new Color(0.22f, 0.55f, 0.78f, 1f));
            SetRect(mainMenuButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(210f, 46f));

            return panelController;
        }

        private static ArcadeSettingsMenu CreateSettingsPanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 430f));
            Image image = panel.AddComponent<Image>();
            image.sprite = UiPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.07f, 0.12f, 0.11f, 0.96f);
            Shadow shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
            shadow.effectDistance = new Vector2(0f, -8f);
            CanvasGroup group = panel.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            ArcadeSettingsMenu menu = panel.AddComponent<ArcadeSettingsMenu>();

            Text title = CreateText("Title", panel.transform, "SETTINGS", 36, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.35f, 1f));
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(360f, 54f));

            Slider master = CreateSlider("MasterVolume", panel.transform, "MASTER", new Vector2(0f, -110f));
            Slider music = CreateSlider("MusicVolume", panel.transform, "MUSIC", new Vector2(0f, -172f));
            Slider sfx = CreateSlider("SfxVolume", panel.transform, "SFX", new Vector2(0f, -234f));
            Slider voice = CreateSlider("VoiceVolume", panel.transform, "VOICE", new Vector2(0f, -296f));

            Button close = CreateButton("CloseButton", panel.transform, "CLOSE", new Color(0.36f, 0.78f, 0.28f, 1f));
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(210f, 52f));

            SetObject(menu, "panelGroup", group);
            SetObject(menu, "masterSlider", master);
            SetObject(menu, "musicSlider", music);
            SetObject(menu, "sfxSlider", sfx);
            SetObject(menu, "voiceSlider", voice);
            return menu;
        }

        private static Slider CreateSlider(string name, Transform parent, string label, Vector2 anchoredPosition)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            SetRect(rootRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredPosition, new Vector2(360f, 52f));

            Text labelText = CreateText("Label", root.transform, label, 16, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            SetRect(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48f, 0f), new Vector2(96f, 38f));

            GameObject sliderObject = new GameObject("Slider");
            sliderObject.transform.SetParent(root.transform, false);
            RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
            SetRect(sliderRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(54f, 0f), new Vector2(-126f, 20f));

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            GameObject background = new GameObject("Background");
            background.transform.SetParent(sliderObject.transform, false);
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.02f, 0.05f, 0.05f, 1f);
            SetRect(backgroundImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 12f));

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(sliderObject.transform, false);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.44f, 0.88f, 0.44f, 1f);
            SetRect(fillImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 12f));

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(sliderObject.transform, false);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(1f, 0.93f, 0.42f, 1f);
            SetRect(handleImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(26f, 26f));

            slider.fillRect = fillImage.rectTransform;
            slider.handleRect = handleImage.rectTransform;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle style, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text textComponent = textObject.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = DefaultFont();
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = style;
            textComponent.alignment = alignment;
            textComponent.color = color;
            textComponent.raycastTarget = false;
            textComponent.resizeTextForBestFit = true;
            textComponent.resizeTextMinSize = Mathf.Max(10, fontSize - 12);
            textComponent.resizeTextMaxSize = fontSize;
            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.08f, 0.05f, 0.58f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return textComponent;
        }

        private static Button CreateButton(string name, Transform parent, string text, Color color)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = UiPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            Shadow shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.36f);
            shadow.effectDistance = new Vector2(0f, -5f);
            Button button = buttonObject.AddComponent<Button>();
            buttonObject.AddComponent<ArcadeButtonFeedback>();

            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.14f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            Text label = CreateText("Text", buttonObject.transform, text, 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            return button;
        }

        private static void ConfigureSpawner(ArcadeSpawner2D spawner, Transform source, GameObject[] prefabs, float ahead, float minY, float maxY, float startAfter, float minInterval, float maxInterval, float chance)
        {
            SetObject(spawner, "distanceSource", source);
            SetObjectArray(spawner, "prefabs", prefabs);
            SetFloat(spawner, "spawnAheadDistance", ahead);
            SetFloat(spawner, "minY", minY);
            SetFloat(spawner, "maxY", maxY);
            SetFloat(spawner, "startAfterDistance", startAfter);
            SetFloat(spawner, "minIntervalDistance", minInterval);
            SetFloat(spawner, "maxIntervalDistance", maxInterval);
            SetFloat(spawner, "spawnChance", chance);
        }

        private static void ConfigureMilestones(MilestoneEventManager milestones)
        {
            AudioClip home = LoadVoice("voiceline_0_home_for_dinner_heroic.wav");
            AudioClip gem = LoadVoice("voiceline_1_shiny_gem_heroic.wav");
            AudioClip food = LoadVoice("voiceline_2_perfect_food_heroic.wav");

            SerializedObject serialized = new SerializedObject(milestones);
            SerializedProperty array = serialized.FindProperty("milestones");
            array.arraySize = 3;
            ConfigureMilestone(array.GetArrayElementAtIndex(0), 25f, "I've gotta get home for dinner!", home);
            ConfigureMilestone(array.GetArrayElementAtIndex(1), 75f, "Where did I leave that shiny gem?", gem);
            ConfigureMilestone(array.GetArrayElementAtIndex(2), 125f, "I must find the perfect food...", food);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureMilestone(SerializedProperty property, float distance, string line, AudioClip clip)
        {
            property.FindPropertyRelative("distance").floatValue = distance;
            property.FindPropertyRelative("line").stringValue = line;
            property.FindPropertyRelative("voiceClip").objectReferenceValue = clip;
        }

        private static AudioClip LoadVoice(string fileName)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(VoiceRoot + "/" + fileName);
            if (clip != null)
            {
                return clip;
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + fileName);
        }

        private static void CreateSkyLayer3D(Transform cameraTransform)
        {
            Camera camera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
            if (camera != null)
            {
                camera.backgroundColor = new Color(0.34f, 0.72f, 0.88f, 1f);
            }

            GameObject root = new GameObject("Sky_3D");
            root.transform.position = new Vector3(0f, 0f, 8f);
            if (cameraTransform != null)
            {
                FollowTargetX follow = root.AddComponent<FollowTargetX>();
                SetObject(follow, "target", cameraTransform);
            }

            Material upperMist = CreateColorMaterial("GG_Sky_UpperMist_3D", new Color(0.72f, 0.95f, 0.9f, 0.28f), true);
            Material lowMist = CreateColorMaterial("GG_Sky_LowMist_3D", new Color(0.47f, 0.78f, 0.62f, 0.22f), true);
            Material warmHaze = CreateColorMaterial("GG_Sky_WarmHaze_3D", new Color(1f, 0.86f, 0.42f, 0.12f), true);
            GameObject upper = CreatePrimitiveVisual("UpperMistBand_3D", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.75f, 0f), new Vector3(38f, 1.35f, 0.05f), upperMist, -6);
            GameObject lower = CreatePrimitiveVisual("LowerMistBand_3D", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.65f, 0.02f), new Vector3(38f, 1.05f, 0.05f), lowMist, -5);
            GameObject haze = CreatePrimitiveVisual("WarmHazeBand_3D", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.58f, 0.03f), new Vector3(38f, 0.74f, 0.05f), warmHaze, -5);
            AddAmbientSway(upper, 0.12f, 0.015f, 0f, 0.006f, 0.12f, 0.3f);
            AddAmbientSway(lower, 0.18f, 0.018f, 0f, 0.008f, 0.16f, 1.9f);
            AddAmbientSway(haze, 0.1f, 0.012f, 0f, 0.004f, 0.1f, 3.2f);
        }

        private static void CreatePrimitiveJungleParallaxBand(string name, Transform cameraTransform, float y, float parallax, float tileWidth, int sortingOrder, float scale, float saturation)
        {
            GameObject root = new GameObject(name);
            root.transform.position = new Vector3(-tileWidth, y, 1f);
            ParallaxBand2D band = root.AddComponent<ParallaxBand2D>();
            SetObject(band, "target", cameraTransform);
            SetFloat(band, "parallaxX", parallax);
            SetFloat(band, "tileWidth", tileWidth);

            Transform[] tiles = new Transform[5];
            for (int i = 0; i < tiles.Length; i++)
            {
                GameObject tile = new GameObject("Tile_" + (i + 1));
                tile.transform.SetParent(root.transform, false);
                tile.transform.localPosition = new Vector3(i * tileWidth, 0f, 0f);

                for (int j = 0; j < 3; j++)
                {
                    GameObject plant = y > 1.5f
                        ? CreatePrimitiveCanopyVisual("Canopy3D_" + (j + 1), tile.transform, scale * (1.05f + ((i + j) % 3) * 0.12f), sortingOrder, i + j)
                        : CreatePrimitivePlantVisual("Plant3D_" + (j + 1), tile.transform, scale * (0.9f + ((i + j) % 3) * 0.08f), sortingOrder, i + j);
                    plant.transform.localPosition = new Vector3(-2.85f + j * 2.85f, Mathf.Sin((i + j) * 1.7f) * 0.12f, -0.08f);
                    plant.transform.localScale *= Mathf.Lerp(0.78f, 1f, saturation);
                    AddAmbientSway(plant, 0.025f, 0.015f, y > 1.5f ? 1.3f : 2f, 0.01f, 0.62f + j * 0.09f, (i * 3f + j) * 0.71f);
                }

                tiles[i] = tile.transform;
            }

            SetObjectArray(band, "tiles", tiles);
        }

        private static void CreateAnimatedMeshyForestBand(string name, Transform cameraTransform, float y, float parallax, float tileWidth, int sortingOrder, ModelVisualAsset[] assets, float targetHeight, float targetBottomY)
        {
            GameObject root = new GameObject(name);
            root.transform.position = new Vector3(-tileWidth, y, 0.62f);
            ParallaxBand2D band = root.AddComponent<ParallaxBand2D>();
            SetObject(band, "target", cameraTransform);
            SetFloat(band, "parallaxX", parallax);
            SetFloat(band, "tileWidth", tileWidth);

            Transform[] tiles = new Transform[5];
            for (int i = 0; i < tiles.Length; i++)
            {
                GameObject tile = new GameObject("Tile_" + (i + 1));
                tile.transform.SetParent(root.transform, false);
                tile.transform.localPosition = new Vector3(i * tileWidth, 0f, 0f);

                for (int j = 0; j < 3; j++)
                {
                    GameObject holder = new GameObject("ForestAsset_" + (j + 1));
                    holder.transform.SetParent(tile.transform, false);
                    holder.transform.localPosition = new Vector3(-3.15f + j * 3.15f + ((i + j) % 2 == 0 ? -0.15f : 0.12f), Mathf.Sin((i + j) * 1.43f) * 0.07f, -0.1f);

                    ModelVisualAsset asset = FirstAvailableModel(assets, i + j);
                    float height = targetHeight * (0.82f + ((i + j) % 4) * 0.09f);
                    if (HasModel(asset))
                    {
                        CreateModelVisualInstance(asset, "Visual_3D", holder.transform, Vector3.zero, height, targetBottomY, sortingOrder, Quaternion.Euler(0f, 24f + (i + j) * 7f, 0f), true);
                    }
                    else if (y > 0.5f)
                    {
                        CreatePrimitiveCanopyVisual("Visual_3D", holder.transform, height, sortingOrder, i + j);
                    }
                    else
                    {
                        CreatePrimitivePlantVisual("Visual_3D", holder.transform, height, sortingOrder, i + j);
                    }

                    AddAmbientSway(holder, 0.006f, 0.004f, 0.32f + ((i + j) % 3) * 0.08f, 0.0025f, 0.2f + j * 0.025f, (i * 3f + j) * 0.79f);
                }

                tiles[i] = tile.transform;
            }

            SetObjectArray(band, "tiles", tiles);
        }

        private static void CreateFilledJungleWall(string name, Transform cameraTransform, float y, float parallax, float tileWidth, int sortingOrder, float scale)
        {
            GameObject root = new GameObject(name);
            root.transform.position = new Vector3(-tileWidth, y, 0.86f);
            ParallaxBand2D band = root.AddComponent<ParallaxBand2D>();
            SetObject(band, "target", cameraTransform);
            SetFloat(band, "parallaxX", parallax);
            SetFloat(band, "tileWidth", tileWidth);

            Material deepLeaf = CreateColorMaterial(name + "_DeepLeaf", new Color(0.035f, 0.18f, 0.105f, 1f), false);
            Material midLeaf = CreateColorMaterial(name + "_MidLeaf", new Color(0.075f, 0.32f, 0.145f, 1f), false);
            Material lightLeaf = CreateColorMaterial(name + "_LightLeaf", new Color(0.14f, 0.48f, 0.22f, 1f), false);
            Material trunk = CreateColorMaterial(name + "_Trunk", new Color(0.13f, 0.075f, 0.045f, 1f), false);
            Material hangingVine = CreateColorMaterial(name + "_HangingVine", new Color(0.055f, 0.25f, 0.085f, 1f), false);

            Transform[] tiles = new Transform[5];
            for (int i = 0; i < tiles.Length; i++)
            {
                GameObject tile = new GameObject("Tile_" + (i + 1));
                tile.transform.SetParent(root.transform, false);
                tile.transform.localPosition = new Vector3(i * tileWidth, 0f, 0f);

                for (int t = 0; t < 3; t++)
                {
                    float trunkX = -3.7f + t * 3.05f + (i % 2 == 0 ? 0.15f : -0.2f);
                    GameObject trunkObject = CreatePrimitiveVisual("BackgroundTrunk_" + (t + 1), PrimitiveType.Cylinder, tile.transform, new Vector3(trunkX, -0.78f, 0.02f), new Vector3(0.1f, 2.25f * scale, 0.1f), trunk, sortingOrder);
                    trunkObject.transform.localRotation = Quaternion.Euler(0f, 0f, -5f + ((i + t) % 3) * 5f);
                    AddAmbientSway(trunkObject, 0.012f, 0.006f, 0.7f, 0f, 0.28f + t * 0.04f, i + t * 0.5f);
                }

                for (int j = 0; j < 9; j++)
                {
                    float x = -4.2f + j * 1.05f + ((i + j) % 2 == 0 ? -0.16f : 0.13f);
                    float leafY = -1.55f + (j % 5) * 0.84f + Mathf.Sin((i + j) * 1.31f) * 0.14f;
                    float size = scale * (0.82f + ((i + j) % 4) * 0.1f);
                    Material leaf = j % 3 == 0 ? lightLeaf : (j % 3 == 1 ? midLeaf : deepLeaf);
                    GameObject leafMass = CreatePrimitiveVisual("FilledLeafMass_" + (j + 1), PrimitiveType.Sphere, tile.transform, new Vector3(x, leafY, -0.04f), new Vector3(0.82f, 0.46f, 0.2f) * size, leaf, sortingOrder);
                    leafMass.transform.localRotation = Quaternion.Euler(0f, 0f, -22f + ((i + j) % 7) * 7f);
                    AddAmbientSway(leafMass, 0.026f, 0.018f, 1.25f + (j % 3) * 0.4f, 0.01f, 0.34f + (j % 4) * 0.055f, (i * 9f + j) * 0.47f);
                }

                for (int v = 0; v < 4; v++)
                {
                    float vineX = -3.45f + v * 2.3f + ((i + v) % 2 == 0 ? 0.18f : -0.12f);
                    float drop = 1.25f + ((i + v) % 3) * 0.35f;
                    GameObject vineObject = CreatePrimitiveVisual("FilledWallHangingVine_" + (v + 1), PrimitiveType.Cylinder, tile.transform, new Vector3(vineX, 1.42f - drop * 0.5f, -0.1f), new Vector3(0.035f, drop * scale, 0.035f), hangingVine, sortingOrder + 1);
                    vineObject.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((i + v) * 0.9f) * 7f);
                    AddAmbientSway(vineObject, 0.05f, 0.008f, 3.5f, 0f, 0.44f + v * 0.08f, (i * 4f + v) * 0.66f);
                }

                tiles[i] = tile.transform;
            }

            SetObjectArray(band, "tiles", tiles);
        }

        private static void CreateHangingCanopyParallaxBand(string name, Transform cameraTransform, float y, float parallax, float tileWidth, int sortingOrder, float scale)
        {
            GameObject root = new GameObject(name);
            root.transform.position = new Vector3(-tileWidth, y, 0.75f);
            ParallaxBand2D band = root.AddComponent<ParallaxBand2D>();
            SetObject(band, "target", cameraTransform);
            SetFloat(band, "parallaxX", parallax);
            SetFloat(band, "tileWidth", tileWidth);

            Material leaf = CreateColorMaterial(name + "_LeafMaterial", new Color(0.08f, 0.28f, 0.14f, 1f), false);
            Material leafHighlight = CreateColorMaterial(name + "_LeafHighlightMaterial", new Color(0.14f, 0.44f, 0.2f, 1f), false);
            Material branch = CreateColorMaterial(name + "_BranchMaterial", new Color(0.18f, 0.1f, 0.06f, 1f), false);
            Material vine = CreateColorMaterial(name + "_VineMaterial", new Color(0.08f, 0.31f, 0.11f, 1f), false);

            Transform[] tiles = new Transform[5];
            for (int i = 0; i < tiles.Length; i++)
            {
                GameObject tile = new GameObject("Tile_" + (i + 1));
                tile.transform.SetParent(root.transform, false);
                tile.transform.localPosition = new Vector3(i * tileWidth, 0f, 0f);

                GameObject limb = CreatePrimitiveVisual("CanopyBranch_" + (i + 1), PrimitiveType.Cylinder, tile.transform, new Vector3(0f, 0.22f, 0.05f), new Vector3(0.11f, 2.55f * scale, 0.11f), branch, sortingOrder);
                limb.transform.localRotation = Quaternion.Euler(0f, 0f, 84f + (i % 2 == 0 ? -4f : 5f));

                for (int j = 0; j < 5; j++)
                {
                    float x = -3.55f + j * 1.75f + ((i + j) % 2 == 0 ? -0.18f : 0.14f);
                    float drop = 0.72f + ((i + j) % 4) * 0.22f;
                    GameObject hangingVine = CreatePrimitiveVisual("BackgroundHangingVine_" + (j + 1), PrimitiveType.Cylinder, tile.transform, new Vector3(x, -drop * 0.5f, -0.02f), new Vector3(0.035f, drop * scale, 0.035f), vine, sortingOrder);
                    hangingVine.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((i + j) * 1.2f) * 5f);
                    AddAmbientSway(hangingVine, 0.045f, 0.006f, 3.4f, 0f, 0.68f + j * 0.1f, (i * 5f + j) * 0.62f);

                    GameObject leafMass = CreatePrimitiveVisual("CanopyLeafMass_" + (j + 1), PrimitiveType.Sphere, tile.transform, new Vector3(x + 0.15f, 0.08f + Mathf.Sin((i + j) * 1.5f) * 0.08f, 0f), new Vector3(0.58f, 0.24f, 0.16f) * scale, j % 2 == 0 ? leaf : leafHighlight, sortingOrder);
                    leafMass.transform.localRotation = Quaternion.Euler(0f, 0f, -14f + j * 7f);
                    AddAmbientSway(leafMass, 0.025f, 0.018f, 1.8f, 0.014f, 0.54f + j * 0.06f, (i * 5f + j) * 0.77f);
                }

                tiles[i] = tile.transform;
            }

            SetObjectArray(band, "tiles", tiles);
        }

        private static GameObject CreateCurvedVineConnector(string name, Transform parent, float startY, float endY, Material material, int sortingOrder)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);

            const int pointCount = 10;
            Vector3[] points = new Vector3[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (pointCount - 1f);
                float y = Mathf.Lerp(startY, endY, t);
                float x = Mathf.Sin(t * Mathf.PI * 2.1f + 0.35f) * (0.08f + t * 0.07f)
                    + Mathf.Sin(t * Mathf.PI * 0.82f) * 0.06f;
                points[i] = new Vector3(x, y, 0.05f);
            }

            LineRenderer line = root.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = pointCount;
            line.SetPositions(points);
            line.startWidth = 0.1f;
            line.endWidth = 0.082f;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.TransformZ;
            line.textureMode = LineTextureMode.Stretch;
            line.sharedMaterial = material;
            line.sortingOrder = sortingOrder;

            return root;
        }

        private static GameObject CreateCanopyAttachmentVisual(string name, Transform parent, ModelVisualAsset modelAsset, Vector3 localPosition, float targetHeight, int sortingOrder, Material fallbackMaterial, int variant)
        {
            GameObject holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = localPosition;

            if (HasModel(modelAsset))
            {
                CreateModelVisualInstance(
                    modelAsset,
                    "TexturedModel_3D",
                    holder.transform,
                    Vector3.zero,
                    targetHeight,
                    0f,
                    sortingOrder,
                    Quaternion.Euler(0f, 22f + variant * 27f, -9f + variant * 6f),
                    true);
            }
            else
            {
                GameObject fallback = CreatePrimitiveCanopyVisual("TexturedFallback_3D", holder.transform, targetHeight, sortingOrder, variant);
                Renderer[] renderers = fallback.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].sharedMaterial = fallbackMaterial;
                }
            }

            return holder;
        }

        private static GameObject CreatePrimitivePlantVisual(string name, Transform parent, float targetHeight, int sortingOrder, int variant)
        {
            GameObject root = new GameObject(name);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            Material leaf = CreateColorMaterial("GG_Primitive_Leaf_" + (variant % 3), Color.Lerp(new Color(0.13f, 0.43f, 0.21f, 1f), new Color(0.42f, 0.78f, 0.28f, 1f), (variant % 3) * 0.28f), false);
            Material stem = CreateColorMaterial("GG_Primitive_Stem_3D", new Color(0.14f, 0.32f, 0.16f, 1f), false);

            float h = Mathf.Max(0.35f, targetHeight);
            GameObject stemObject = CreatePrimitiveVisual("Stem", PrimitiveType.Cylinder, root.transform, new Vector3(0f, h * 0.18f, 0.08f), new Vector3(0.05f, h * 0.28f, 0.05f), stem, sortingOrder);
            stemObject.transform.localRotation = Quaternion.Euler(0f, 0f, -8f + (variant % 3) * 8f);

            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                GameObject blade = CreatePrimitiveVisual("Leaf_" + (i + 1), PrimitiveType.Sphere, root.transform, new Vector3(side * (0.22f + i * 0.05f), h * (0.28f + i * 0.12f), 0f), new Vector3(0.32f, 0.11f, 0.08f) * h, leaf, sortingOrder);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, side * (24f + i * 10f));
            }

            return root;
        }

        private static GameObject CreatePrimitiveCanopyVisual(string name, Transform parent, float targetHeight, int sortingOrder, int variant)
        {
            GameObject root = new GameObject(name);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            Material leaf = CreateColorMaterial("GG_Primitive_Canopy_" + (variant % 3), Color.Lerp(new Color(0.08f, 0.32f, 0.2f, 1f), new Color(0.24f, 0.55f, 0.28f, 1f), (variant % 3) * 0.28f), false);
            float h = Mathf.Max(0.55f, targetHeight);
            for (int i = 0; i < 5; i++)
            {
                float x = -0.62f + i * 0.31f;
                float y = Mathf.Sin((variant + i) * 1.1f) * 0.08f;
                GameObject blob = CreatePrimitiveVisual("LeafMass_" + (i + 1), PrimitiveType.Sphere, root.transform, new Vector3(x, y, 0f), new Vector3(0.34f, 0.22f, 0.12f) * h, leaf, sortingOrder);
                blob.transform.localRotation = Quaternion.Euler(0f, 0f, -18f + i * 9f);
            }

            return root;
        }

        private static GameObject CreatePrimitiveGorillaVisual(string name, Transform parent)
        {
            GameObject root = new GameObject(name);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
                root.transform.localPosition = new Vector3(0f, 0f, -0.15f);
            }

            Material fur = CreateColorMaterial("GG_Gorilla_Primitive_Fur_3D", new Color(0.38f, 0.2f, 0.1f, 1f), false);
            Material tan = CreateColorMaterial("GG_Gorilla_Primitive_Tan_3D", new Color(0.78f, 0.55f, 0.34f, 1f), false);
            Material dark = CreateColorMaterial("GG_Gorilla_Primitive_Dark_3D", new Color(0.08f, 0.05f, 0.04f, 1f), false);

            CreatePrimitiveVisual("Body", PrimitiveType.Sphere, root.transform, new Vector3(0f, -0.18f, 0f), new Vector3(0.58f, 0.86f, 0.42f), fur, 4);
            CreatePrimitiveVisual("Belly", PrimitiveType.Sphere, root.transform, new Vector3(0.08f, -0.25f, -0.18f), new Vector3(0.35f, 0.48f, 0.08f), tan, 5);
            CreatePrimitiveVisual("Head", PrimitiveType.Sphere, root.transform, new Vector3(0.05f, 0.68f, 0f), new Vector3(0.44f, 0.38f, 0.35f), fur, 4);
            CreatePrimitiveVisual("Muzzle", PrimitiveType.Sphere, root.transform, new Vector3(0.18f, 0.58f, -0.2f), new Vector3(0.24f, 0.16f, 0.1f), tan, 5);
            CreatePrimitiveVisual("Eye", PrimitiveType.Sphere, root.transform, new Vector3(0.16f, 0.76f, -0.27f), new Vector3(0.05f, 0.05f, 0.04f), dark, 6);
            CreatePrimitiveVisual("Arm_Back", PrimitiveType.Capsule, root.transform, new Vector3(-0.4f, -0.18f, 0.08f), new Vector3(0.17f, 0.58f, 0.17f), fur, 3).transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            CreatePrimitiveVisual("Arm_Front", PrimitiveType.Capsule, root.transform, new Vector3(0.46f, -0.12f, -0.08f), new Vector3(0.17f, 0.62f, 0.17f), fur, 5).transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            CreatePrimitiveVisual("Foot", PrimitiveType.Sphere, root.transform, new Vector3(0.18f, -1.02f, -0.08f), new Vector3(0.32f, 0.12f, 0.18f), tan, 5);
            root.transform.localScale = new Vector3(1.35f, 1.35f, 1.35f);
            return root;
        }

        private static GameObject CreatePrimitiveFartCloud(string name, Transform parent, Vector3 position, float height, int sortingOrder)
        {
            GameObject root = new GameObject(name);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
                root.transform.localPosition = position;
            }
            else
            {
                root.transform.position = position;
            }

            Material cloud = CreateColorMaterial("GG_FartCloud_3D", new Color(0.68f, 1f, 0.47f, 0.72f), true);
            CreatePrimitiveVisual("Puff_A", PrimitiveType.Sphere, root.transform, new Vector3(-0.2f, 0f, 0f), new Vector3(0.36f, 0.25f, 0.28f) * height, cloud, sortingOrder);
            CreatePrimitiveVisual("Puff_B", PrimitiveType.Sphere, root.transform, new Vector3(0.1f, 0.08f, -0.02f), new Vector3(0.42f, 0.3f, 0.3f) * height, cloud, sortingOrder);
            CreatePrimitiveVisual("Puff_C", PrimitiveType.Sphere, root.transform, new Vector3(0.42f, -0.03f, 0.02f), new Vector3(0.28f, 0.2f, 0.24f) * height, cloud, sortingOrder);
            return root;
        }

        private static GameObject CreatePickupPrimitiveVisual(string primitiveKind, Transform parent)
        {
            GameObject root = new GameObject("Visual_3D");
            root.transform.SetParent(parent, false);
            string kind = primitiveKind != null ? primitiveKind : "";

            if (kind == "Burrito")
            {
                Material wrap = CreateColorMaterial("GG_Primitive_Burrito_Wrap_3D", new Color(0.88f, 0.64f, 0.32f, 1f), false);
                Material filling = CreateColorMaterial("GG_Primitive_Burrito_Filling_3D", new Color(0.24f, 0.74f, 0.22f, 1f), false);
                GameObject roll = CreatePrimitiveVisual("BurritoRoll", PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(0.24f, 0.62f, 0.24f), wrap, 3);
                roll.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                CreatePrimitiveVisual("Filling", PrimitiveType.Sphere, root.transform, new Vector3(0.44f, 0f, -0.02f), new Vector3(0.18f, 0.18f, 0.08f), filling, 4);
            }
            else if (kind == "Banana")
            {
                Material peel = CreateColorMaterial("GG_Primitive_Banana_3D", new Color(1f, 0.84f, 0.18f, 1f), false);
                Material tip = CreateColorMaterial("GG_Primitive_Banana_Tip_3D", new Color(0.36f, 0.22f, 0.08f, 1f), false);
                for (int i = 0; i < 3; i++)
                {
                    GameObject banana = CreatePrimitiveVisual("Banana_" + (i + 1), PrimitiveType.Capsule, root.transform, new Vector3((i - 1) * 0.18f, Mathf.Abs(i - 1) * 0.04f, -0.03f), new Vector3(0.08f, 0.34f, 0.08f), peel, 3);
                    banana.transform.localRotation = Quaternion.Euler(0f, 0f, -24f + i * 24f);
                }

                CreatePrimitiveVisual("Stem", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.3f, -0.05f), new Vector3(0.08f, 0.05f, 0.05f), tip, 4);
            }
            else if (kind == "Soda")
            {
                Material can = CreateColorMaterial("GG_Primitive_Soda_Can_3D", new Color(0.2f, 0.65f, 0.95f, 1f), false);
                Material label = CreateColorMaterial("GG_Primitive_Soda_Label_3D", new Color(1f, 0.9f, 0.25f, 1f), false);
                CreatePrimitiveVisual("Can", PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(0.22f, 0.46f, 0.22f), can, 3);
                CreatePrimitiveVisual("Label", PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, -0.22f), new Vector3(0.36f, 0.2f, 0.04f), label, 4);
            }
            else
            {
                Material bean = CreateColorMaterial("GG_Primitive_Bean_3D", new Color(0.48f, 0.25f, 0.12f, 1f), false);
                Material shine = CreateColorMaterial("GG_Primitive_Bean_Shine_3D", new Color(0.9f, 0.72f, 0.35f, 1f), false);
                GameObject beanBody = CreatePrimitiveVisual("BeanBody", PrimitiveType.Sphere, root.transform, Vector3.zero, new Vector3(0.42f, 0.28f, 0.25f), bean, 3);
                beanBody.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
                CreatePrimitiveVisual("BeanShine", PrimitiveType.Sphere, root.transform, new Vector3(-0.12f, 0.08f, -0.18f), new Vector3(0.11f, 0.04f, 0.03f), shine, 4);
            }

            return root;
        }

        private static void CreateHazardPrimitiveVisual(string primitiveKind, Transform parent, Vector2 colliderSize)
        {
            string kind = primitiveKind != null ? primitiveKind : "";
            if (kind == "SapBlob")
            {
                Material sap = CreateColorMaterial("GG_Primitive_SapBlob_3D", new Color(0.48f, 0.94f, 0.2f, 0.82f), true);
                CreatePrimitiveVisual("SapBlob_A", PrimitiveType.Sphere, parent, new Vector3(0f, -0.78f, 0f), new Vector3(0.34f, 0.62f, 0.28f), sap, 2);
                CreatePrimitiveVisual("SapBlob_B", PrimitiveType.Sphere, parent, new Vector3(0.08f, -1.38f, -0.04f), new Vector3(0.24f, 0.34f, 0.24f), sap, 3);
                return;
            }

            Material bark = CreateColorMaterial("GG_Primitive_ThornLog_Bark_3D", new Color(0.42f, 0.22f, 0.1f, 1f), false);
            Material thorn = CreateColorMaterial("GG_Primitive_ThornLog_Thorns_3D", new Color(0.98f, 0.83f, 0.43f, 1f), false);
            GameObject log = CreatePrimitiveVisual("Log", PrimitiveType.Cylinder, parent, new Vector3(0f, -colliderSize.y * 0.5f, 0f), new Vector3(0.35f, colliderSize.y * 0.28f, 0.35f), bark, 2);
            log.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            for (int i = 0; i < 4; i++)
            {
                GameObject spike = CreatePrimitiveVisual("Thorn_" + (i + 1), PrimitiveType.Cube, parent, new Vector3(-0.42f + i * 0.28f, -0.58f - (i % 2) * 0.34f, -0.2f), new Vector3(0.08f, 0.28f, 0.08f), thorn, 4);
                spike.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
        }

        private static Renderer[] CreatePrimitiveVineVisual(Transform parent)
        {
            Material vine = CreateColorMaterial("GG_Primitive_Vine_3D", new Color(0.13f, 0.42f, 0.15f, 1f), false);
            Material glow = CreateColorMaterial("GG_Primitive_Vine_Glow_3D", new Color(0.5f, 1f, 0.22f, 0.86f), true);
            GameObject body = CreatePrimitiveVisual("VineBody_3D", PrimitiveType.Cylinder, parent, new Vector3(0f, -1.34f, 0f), new Vector3(0.16f, 1.72f, 0.16f), vine, 2);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, -4f);

            Renderer[] glows = new Renderer[3];
            for (int i = 0; i < glows.Length; i++)
            {
                GameObject leaf = CreatePrimitiveVisual("GlowCluster_" + (i + 1), PrimitiveType.Sphere, parent, new Vector3((i - 1) * 0.26f, -1.35f + Mathf.Abs(i - 1) * 0.14f, -0.18f), new Vector3(0.22f, 0.12f, 0.07f), glow, 5);
                leaf.transform.localRotation = Quaternion.Euler(0f, 0f, -22f + i * 22f);
                glows[i] = leaf.GetComponent<Renderer>();
            }

            return glows;
        }

        private static GameObject CreatePrimitiveVisual(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, int sortingOrder, bool tileTexture = true)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            if (parent != null)
            {
                primitive.transform.SetParent(parent, false);
                primitive.transform.localPosition = localPosition;
            }
            else
            {
                primitive.transform.position = localPosition;
            }

            primitive.transform.localScale = localScale;
            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.sortingOrder = sortingOrder;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

                if (tileTexture && material != null && material.mainTexture != null)
                {
                    Vector2 tiling;
                    if (type == PrimitiveType.Cube)
                    {
                        tiling = new Vector2(Mathf.Max(1f, Mathf.Abs(localScale.x) * 0.55f), Mathf.Max(1f, Mathf.Abs(localScale.y) * 0.55f));
                    }
                    else if (type == PrimitiveType.Cylinder || type == PrimitiveType.Capsule)
                    {
                        tiling = new Vector2(Mathf.Max(1f, Mathf.Abs(localScale.x) * 2f), Mathf.Max(1f, Mathf.Abs(localScale.y) * 0.72f));
                    }
                    else
                    {
                        tiling = new Vector2(1.35f, 1.35f);
                    }

                    MaterialPropertyBlock properties = new MaterialPropertyBlock();
                    properties.SetVector("_MainTex_ST", new Vector4(tiling.x, tiling.y, 0f, 0f));
                    properties.SetVector("_BaseMap_ST", new Vector4(tiling.x, tiling.y, 0f, 0f));
                    renderer.SetPropertyBlock(properties);
                }
            }

            return primitive;
        }

        private static Material CreateColorMaterial(string key, Color color, bool transparent)
        {
            string materialPath = GameRoot + "/Materials/" + key + ".mat";
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (shader != null)
            {
                material.shader = shader;
            }

            Texture2D surfaceTexture = CreateProceduralSurfaceTexture(key, color, transparent);
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", surfaceTexture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", surfaceTexture);
                material.SetTextureScale("_MainTex", new Vector2(1.6f, 1.6f));
            }

            Color materialTint = new Color(1f, 1f, 1f, color.a);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", materialTint);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", materialTint);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            if (material.HasProperty("_Glossiness"))
            {
                string lowerKey = key.ToLowerInvariant();
                float gloss = lowerKey.Contains("sap") ? 0.58f : (lowerKey.Contains("mist") || lowerKey.Contains("haze") || lowerKey.Contains("cloud") ? 0.05f : 0.18f);
                material.SetFloat("_Glossiness", gloss);
            }

            if (transparent)
            {
                if (material.HasProperty("_Mode"))
                {
                    material.SetFloat("_Mode", 2f);
                }

                if (material.HasProperty("_SrcBlend"))
                {
                    material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                }

                if (material.HasProperty("_DstBlend"))
                {
                    material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                }

                material.renderQueue = (int)RenderQueue.Transparent;
                material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            else
            {
                if (material.HasProperty("_Mode"))
                {
                    material.SetFloat("_Mode", 0f);
                }

                if (material.HasProperty("_SrcBlend"))
                {
                    material.SetFloat("_SrcBlend", (float)BlendMode.One);
                }

                if (material.HasProperty("_DstBlend"))
                {
                    material.SetFloat("_DstBlend", (float)BlendMode.Zero);
                }

                material.renderQueue = -1;
                material.SetFloat("_ZWrite", 1f);
                material.SetOverrideTag("RenderType", "Opaque");
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D CreateProceduralSurfaceTexture(string key, Color baseColor, bool transparent)
        {
            const int size = 96;
            string texturePath = ProceduralTextureRoot + "/" + SanitizeAssetName(key) + "_Surface.asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
                texture.name = SanitizeAssetName(key) + "_Surface";
                AssetDatabase.CreateAsset(texture, texturePath);
            }
            else if (texture.width != size || texture.height != size)
            {
                texture.Reinitialize(size, size, TextureFormat.RGBA32, true);
            }

            string lowerKey = key.ToLowerInvariant();
            int seed = StableStringHash(key);
            float offsetX = (seed & 255) * 0.037f;
            float offsetY = ((seed >> 8) & 255) * 0.041f;
            bool foliage = lowerKey.Contains("leaf") || lowerKey.Contains("grass") || lowerKey.Contains("canopy");
            bool bark = lowerKey.Contains("trunk") || lowerKey.Contains("branch") || lowerKey.Contains("bark") || lowerKey.Contains("dirt") || lowerKey.Contains("root") || lowerKey.Contains("wrap");
            bool vine = lowerKey.Contains("vine") || lowerKey.Contains("stem");
            bool vapor = lowerKey.Contains("mist") || lowerKey.Contains("haze") || lowerKey.Contains("cloud") || lowerKey.Contains("glow") || lowerKey.Contains("ring") || lowerKey.Contains("sap");

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = y / (float)(size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float broadNoise = Mathf.PerlinNoise(offsetX + u * 3.4f, offsetY + v * 3.4f);
                    float fineNoise = Mathf.PerlinNoise(offsetX * 1.7f + u * 11.5f, offsetY * 1.3f + v * 11.5f);
                    float pattern = broadNoise * 0.7f + fineNoise * 0.3f;

                    if (foliage)
                    {
                        float centerVein = Mathf.Exp(-Mathf.Abs(u - 0.5f) * 28f);
                        float sideVeins = Mathf.Pow(Mathf.Abs(Mathf.Sin((v * 12f + Mathf.Abs(u - 0.5f) * 8f) * Mathf.PI)), 12f);
                        pattern = Mathf.Clamp01(pattern * 0.72f + centerVein * 0.2f + sideVeins * 0.08f);
                    }
                    else if (bark)
                    {
                        float ridges = Mathf.PerlinNoise(offsetX + u * 8f, offsetY + v * 1.35f);
                        float grain = Mathf.Abs(Mathf.Sin((u * 9f + ridges * 2.2f) * Mathf.PI));
                        pattern = Mathf.Clamp01(pattern * 0.46f + grain * 0.42f + fineNoise * 0.12f);
                    }
                    else if (vine)
                    {
                        float twist = 0.5f + 0.5f * Mathf.Sin((u * 8f + v * 3.2f + broadNoise) * Mathf.PI);
                        pattern = Mathf.Clamp01(pattern * 0.5f + twist * 0.5f);
                    }
                    else if (vapor)
                    {
                        float soft = Mathf.SmoothStep(0.2f, 0.86f, broadNoise * 0.8f + fineNoise * 0.2f);
                        pattern = Mathf.Lerp(0.28f, 1f, soft);
                    }

                    float brightness = Mathf.Lerp(0.68f, 1.22f, pattern);
                    Color pixel = new Color(
                        Mathf.Clamp01(baseColor.r * brightness),
                        Mathf.Clamp01(baseColor.g * brightness),
                        Mathf.Clamp01(baseColor.b * brightness),
                        transparent ? Mathf.Lerp(0.42f, 1f, pattern) : 1f);
                    pixels[y * size + x] = pixel;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 3;
            EditorUtility.SetDirty(texture);
            return texture;
        }

        private static int StableStringHash(string value)
        {
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }

        private static string SanitizeAssetName(string value)
        {
            char[] characters = value.ToCharArray();
            for (int i = 0; i < characters.Length; i++)
            {
                if (!char.IsLetterOrDigit(characters[i]) && characters[i] != '_' && characters[i] != '-')
                {
                    characters[i] = '_';
                }
            }

            return new string(characters);
        }

        private static GameObject CreateSpriteChild(string name, Transform parent, Sprite sprite, int sortingOrder, Color color)
        {
            GameObject child = new GameObject(name);
            if (parent != null)
            {
                child.transform.SetParent(parent, false);
            }

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = color;
            return child;
        }

        private static void ConfigureParticleMeshRenderer(ParticleSystemRenderer renderer, PrimitiveType meshType, string materialKey, Color color, int sortingOrder)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = GetParticleMesh(meshType);
            renderer.sharedMaterial = CreateParticleMaterial(materialKey, color);
            renderer.enableGPUInstancing = true;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Mesh GetParticleMesh(PrimitiveType meshType)
        {
            if (meshType == PrimitiveType.Sphere && sphereParticleMesh != null)
            {
                return sphereParticleMesh;
            }

            if (meshType == PrimitiveType.Cube && cubeParticleMesh != null)
            {
                return cubeParticleMesh;
            }

            GameObject temporary = GameObject.CreatePrimitive(meshType);
            Mesh mesh = temporary.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(temporary);
            if (meshType == PrimitiveType.Sphere)
            {
                sphereParticleMesh = mesh;
            }
            else if (meshType == PrimitiveType.Cube)
            {
                cubeParticleMesh = mesh;
            }

            return mesh;
        }

        private static Material CreateParticleMaterial(string key, Color color)
        {
            string materialPath = GameRoot + "/Materials/" + key + ".mat";
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (shader != null)
            {
                material.shader = shader;
            }

            Texture2D texture = CreateProceduralSurfaceTexture(key, color, true);
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 2f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureFartParticles(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.48f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.48f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.58f, 1f, 0.36f, 0.88f), new Color(0.98f, 1f, 0.66f, 0.78f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 26) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.16f;
            shape.rotation = new Vector3(0f, 90f, 0f);

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-3.8f, -1.4f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.35f, 0.45f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.96f, 1f, 0.5f), 0f), new GradientColorKey(new Color(0.45f, 1f, 0.35f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 0.45f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            ConfigureParticleMeshRenderer(renderer, PrimitiveType.Sphere, "GG_FartCloud_Particle3D", new Color(0.62f, 1f, 0.34f, 0.76f), 7);
        }

        private static void ConfigureFartShockwaveParticles(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.18f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.24f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 1f, 0.33f, 0.78f), new Color(0.46f, 1f, 0.78f, 0.58f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.18f;

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-2.8f, -0.9f);
            velocity.y = new ParticleSystem.MinMaxCurve(-1.0f, 1.0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            ConfigureParticleMeshRenderer(renderer, PrimitiveType.Sphere, "GG_FartShock_Particle3D", new Color(0.82f, 1f, 0.3f, 0.68f), 8);
        }

        private static void ConfigureFartSparkParticles(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.26f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.96f, 0.34f, 0.94f), new Color(0.62f, 1f, 0.4f, 0.78f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-4.2f, -1.1f);
            velocity.y = new ParticleSystem.MinMaxCurve(-1.1f, 1.2f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            ConfigureParticleMeshRenderer(renderer, PrimitiveType.Cube, "GG_FartSpark_Particle3D", new Color(1f, 0.94f, 0.26f, 0.86f), 9);
        }

        private static void ConfigureLagoonSplashParticles(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.useUnscaledTime = true;
            main.duration = 0.28f;
            main.maxParticles = 30;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.78f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.4f, 6.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.11f, 0.26f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.76f, 1f, 0.92f, 0.9f),
                new Color(0.2f, 0.82f, 0.76f, 0.82f));
            main.gravityModifier = 0.75f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 38f;
            shape.radius = 0.36f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.82f, 1f, 0.94f), 0f), new GradientColorKey(new Color(0.2f, 0.72f, 0.7f), 1f) },
                new[] { new GradientAlphaKey(0.92f, 0f), new GradientAlphaKey(0.52f, 0.55f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            ConfigureParticleMeshRenderer(renderer, PrimitiveType.Sphere, "GG_LagoonSplash_Particle3D", new Color(0.55f, 1f, 0.9f, 0.86f), 16);
        }

        private static void ConfigureLagoonDropletParticles(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.useUnscaledTime = true;
            main.duration = 0.24f;
            main.maxParticles = 20;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.44f, 0.86f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4.4f, 7.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.92f, 1f, 0.96f, 0.96f),
                new Color(0.34f, 0.9f, 0.84f, 0.8f));
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.035f, 20) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 52f;
            shape.radius = 0.24f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.28f, 0.78f, 0.78f), 1f) },
                new[] { new GradientAlphaKey(0.94f, 0f), new GradientAlphaKey(0.68f, 0.66f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            ConfigureParticleMeshRenderer(renderer, PrimitiveType.Sphere, "GG_LagoonDroplet_Particle3D", new Color(0.8f, 1f, 0.95f, 0.9f), 17);
        }

        private static void ConfigureLagoonBubbleParticles(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.useUnscaledTime = true;
            main.duration = 0.32f;
            main.maxParticles = 14;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.48f, 0.88f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.065f, 0.17f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.86f, 1f, 0.9f, 0.78f),
                new Color(0.34f, 0.86f, 0.7f, 0.62f));
            main.gravityModifier = -0.06f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.08f, 12) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.48f;

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.12f, 0.62f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.78f, 1f, 0.88f), 0f), new GradientColorKey(new Color(0.24f, 0.7f, 0.6f), 1f) },
                new[] { new GradientAlphaKey(0.72f, 0f), new GradientAlphaKey(0.42f, 0.58f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            ConfigureParticleMeshRenderer(renderer, PrimitiveType.Sphere, "GG_LagoonBubble_Particle3D", new Color(0.62f, 1f, 0.82f, 0.72f), 15);
        }

        private static void ConfigurePickupSparkleParticles(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.22f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.55f, 1.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.94f, 0.35f, 0.92f), new Color(0.52f, 1f, 0.44f, 0.82f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.18f;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            ConfigureParticleMeshRenderer(renderer, PrimitiveType.Sphere, "GG_PickupSparkle_Particle3D", new Color(1f, 0.92f, 0.24f, 0.86f), 6);
        }

        private static void ConfigureSpeedLineParticles(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.19f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(7.2f, 11.5f);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.035f, 0.075f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.035f, 0.075f);
            main.startSizeZ = new ParticleSystem.MinMaxCurve(0.55f, 1.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.9f, 1f, 0.64f, 0.72f), new Color(0.45f, 0.98f, 1f, 0.48f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 26) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.18f, 1.35f, 0.1f);

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-6.4f, -2.8f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.75f, 0.75f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            ConfigureParticleMeshRenderer(renderer, PrimitiveType.Cube, "GG_SpeedLine_Particle3D", new Color(0.72f, 1f, 0.7f, 0.68f), 8);
            renderer.alignment = ParticleSystemRenderSpace.Velocity;
        }

        private static void ApplyProvidedArtwork(SpriteSet set)
        {
            const string sheetPath = "Assets/Sprites/TransGorillaSheet.png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath) == null)
            {
                return;
            }

            Sprite gorillaIdle = CropProvidedSprite("provided_gorilla_idle", 24, 162, 248, 352, 100f);
            Sprite gorillaBoost = CropProvidedSprite("provided_gorilla_boost", 282, 160, 332, 354, 100f);
            Sprite gorillaSwing = CropProvidedSprite("provided_gorilla_swing", 610, 166, 228, 350, 100f, true, true);
            Sprite bean = CropProvidedSprite("provided_pickup_bean", 34, 568, 188, 152, 100f);
            Sprite burrito = CropProvidedSprite("provided_pickup_burrito", 228, 562, 262, 150, 100f);
            Sprite vine = CropProvidedSprite("provided_vine_glowing", 842, 56, 174, 474, 100f);
            Sprite fartCloud = CropProvidedSprite("provided_fart_cloud", 284, 320, 150, 138, 100f);
            Sprite fuelBar = CropProvidedSprite("provided_fuel_bar", 462, 548, 478, 128, 100f);
            Sprite plantLeft = CropProvidedSprite("provided_plant_left", 22, 738, 260, 168, 100f);
            Sprite plantMiddle = CropProvidedSprite("provided_plant_middle", 322, 724, 250, 168, 100f);
            Sprite plantRight = CropProvidedSprite("provided_plant_right", 590, 710, 395, 190, 100f);
            Sprite ground = CropProvidedSprite("provided_ground_strip", 26, 930, 968, 94, 100f);

            if (gorillaIdle != null)
            {
                set.Gorilla = gorillaIdle;
                set.GorillaBoost = gorillaBoost != null ? gorillaBoost : gorillaIdle;
                set.GorillaSwing = gorillaSwing != null ? gorillaSwing : gorillaIdle;
                set.ProvidedArtworkAvailable = true;
            }

            if (bean != null)
            {
                set.Bean = bean;
            }

            if (burrito != null)
            {
                set.Burrito = burrito;
            }

            if (vine != null)
            {
                set.Vine = vine;
            }

            if (fartCloud != null)
            {
                set.FartCloud = fartCloud;
            }

            if (fuelBar != null)
            {
                set.FuelBar = fuelBar;
            }

            if (plantLeft != null)
            {
                set.PlantLeft = plantLeft;
            }

            if (plantMiddle != null)
            {
                set.PlantMiddle = plantMiddle;
            }

            if (plantRight != null)
            {
                set.PlantRight = plantRight;
            }

            if (ground != null)
            {
                set.Ground = ground;
            }
        }

        private static Sprite CropProvidedSprite(string name, int left, int top, int width, int height, float pixelsPerUnit, bool keepLargestAlphaIsland = false, bool clearTopRightCorner = false)
        {
            const string sheetPath = "Assets/Sprites/TransGorillaSheet.png";
            string assetPath = ProvidedSpriteRoot + "/" + name + ".png";
            string fullPath = ToFullPath(assetPath);
            string sourceFullPath = ToFullPath(sheetPath);

            if (!File.Exists(sourceFullPath))
            {
                return null;
            }

            byte[] sourceBytes = File.ReadAllBytes(sourceFullPath);
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(sourceBytes))
            {
                UnityEngine.Object.DestroyImmediate(source);
                return null;
            }

            int clampedLeft = Mathf.Clamp(left, 0, source.width - 1);
            int clampedTop = Mathf.Clamp(top, 0, source.height - 1);
            int clampedWidth = Mathf.Clamp(width, 1, source.width - clampedLeft);
            int clampedHeight = Mathf.Clamp(height, 1, source.height - clampedTop);
            int bottom = Mathf.Clamp(source.height - clampedTop - clampedHeight, 0, source.height - clampedHeight);

            Texture2D crop = new Texture2D(clampedWidth, clampedHeight, TextureFormat.RGBA32, false);
            Color[] pixels = source.GetPixels(clampedLeft, bottom, clampedWidth, clampedHeight);
            crop.SetPixels(pixels);
            crop.Apply();

            if (keepLargestAlphaIsland)
            {
                KeepLargestAlphaIsland(crop);
            }

            if (clearTopRightCorner)
            {
                ClearAlphaRect(crop, crop.width - 34, crop.height - 40, 34, 40);
            }

            EnsureAssetFolder(ProvidedSpriteRoot);
            File.WriteAllBytes(fullPath, crop.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(crop);

            TextureImporter existingImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (existingImporter != null)
            {
                ConfigureProvidedSpriteImporter(existingImporter, pixelsPerUnit);
                AssetDatabase.WriteImportSettingsIfDirty(assetPath);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (importer != null)
            {
                ConfigureProvidedSpriteImporter(importer, pixelsPerUnit);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void ConfigureProvidedSpriteImporter(TextureImporter importer, float pixelsPerUnit)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }

        private static void KeepLargestAlphaIsland(Texture2D texture)
        {
            const byte alphaThreshold = 12;
            int width = texture.width;
            int height = texture.height;
            Color32[] pixels = texture.GetPixels32();
            int[] componentByPixel = new int[pixels.Length];

            for (int i = 0; i < componentByPixel.Length; i++)
            {
                componentByPixel[i] = -1;
            }

            int bestComponent = -1;
            int bestSize = 0;
            int component = 0;
            Stack<int> pending = new Stack<int>();

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a <= alphaThreshold || componentByPixel[i] >= 0)
                {
                    continue;
                }

                int count = 0;
                componentByPixel[i] = component;
                pending.Push(i);

                while (pending.Count > 0)
                {
                    int index = pending.Pop();
                    count++;
                    int x = index % width;
                    int y = index / width;

                    AddAlphaNeighbor(index - 1, x > 0);
                    AddAlphaNeighbor(index + 1, x < width - 1);
                    AddAlphaNeighbor(index - width, y > 0);
                    AddAlphaNeighbor(index + width, y < height - 1);
                }

                if (count > bestSize)
                {
                    bestSize = count;
                    bestComponent = component;
                }

                component++;

                void AddAlphaNeighbor(int neighbor, bool inBounds)
                {
                    if (!inBounds || componentByPixel[neighbor] >= 0 || pixels[neighbor].a <= alphaThreshold)
                    {
                        return;
                    }

                    componentByPixel[neighbor] = component;
                    pending.Push(neighbor);
                }
            }

            if (bestComponent < 0)
            {
                return;
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > alphaThreshold && componentByPixel[i] != bestComponent)
                {
                    pixels[i] = new Color32(0, 0, 0, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        private static void ClearAlphaRect(Texture2D texture, int x, int y, int width, int height)
        {
            int startX = Mathf.Clamp(x, 0, texture.width);
            int startY = Mathf.Clamp(y, 0, texture.height);
            int endX = Mathf.Clamp(x + width, 0, texture.width);
            int endY = Mathf.Clamp(y + height, 0, texture.height);
            Color32[] pixels = texture.GetPixels32();

            for (int py = startY; py < endY; py++)
            {
                int row = py * texture.width;
                for (int px = startX; px < endX; px++)
                {
                    pixels[row + px] = new Color32(0, 0, 0, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        private static Sprite CreateEllipseSprite(string name, int width, int height, Color32 edge, Color32 center)
        {
            return CreateSprite(name, width, height, (x, y, w, h) =>
            {
                float nx = (x - w * 0.5f) / (w * 0.5f);
                float ny = (y - h * 0.5f) / (h * 0.5f);
                float d = nx * nx + ny * ny;
                if (d > 1f)
                {
                    return Transparent();
                }

                return Color32.Lerp(center, edge, Mathf.Clamp01(d));
            });
        }

        private static Sprite CreateFartFuelIconSprite()
        {
            return CreateSprite("gg_fart_fuel_icon_v1", 96, 96, (x, y, w, h) =>
            {
                Vector2 point = new Vector2((x + 0.5f) / w, (y + 0.5f) / h);
                bool inside =
                    Vector2.Distance(point, new Vector2(0.48f, 0.48f)) <= 0.235f ||
                    Vector2.Distance(point, new Vector2(0.29f, 0.46f)) <= 0.17f ||
                    Vector2.Distance(point, new Vector2(0.67f, 0.47f)) <= 0.18f ||
                    Vector2.Distance(point, new Vector2(0.5f, 0.66f)) <= 0.18f ||
                    Vector2.Distance(point, new Vector2(0.25f, 0.29f)) <= 0.085f ||
                    Vector2.Distance(point, new Vector2(0.14f, 0.2f)) <= 0.045f;

                bool insideOutline =
                    Vector2.Distance(point, new Vector2(0.48f, 0.48f)) <= 0.275f ||
                    Vector2.Distance(point, new Vector2(0.29f, 0.46f)) <= 0.21f ||
                    Vector2.Distance(point, new Vector2(0.67f, 0.47f)) <= 0.22f ||
                    Vector2.Distance(point, new Vector2(0.5f, 0.66f)) <= 0.22f ||
                    Vector2.Distance(point, new Vector2(0.25f, 0.29f)) <= 0.125f ||
                    Vector2.Distance(point, new Vector2(0.14f, 0.2f)) <= 0.085f;

                if (!insideOutline)
                {
                    return Transparent();
                }

                if (!inside)
                {
                    return new Color32(20, 46, 28, 255);
                }

                Color gasColor = Color.Lerp(
                    new Color(0.32f, 0.9f, 0.32f, 1f),
                    new Color(0.88f, 1f, 0.36f, 1f),
                    Mathf.Clamp01(point.y));
                float highlight = 1f - Mathf.Clamp01(Vector2.Distance(point, new Vector2(0.43f, 0.68f)) / 0.22f);
                gasColor = Color.Lerp(gasColor, Color.white, highlight * 0.34f);
                return (Color32)gasColor;
            });
        }

        private static Sprite CreateRoundedRectSprite(string name, int width, int height, Color32 fill, Color32 accent)
        {
            return CreateSprite(name, width, height, (x, y, w, h) =>
            {
                float border = Mathf.Min(w, h) * 0.18f;
                bool cornerX = x < border || x > w - border;
                bool cornerY = y < border || y > h - border;
                if (cornerX && cornerY)
                {
                    float cx = x < border ? border : w - border;
                    float cy = y < border ? border : h - border;
                    if (Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) > border)
                    {
                        return Transparent();
                    }
                }

                if (Mathf.Abs(y - h * 0.58f) < h * 0.08f || Mathf.Abs(x - w * 0.35f) < w * 0.04f)
                {
                    return accent;
                }

                return fill;
            });
        }

        private static Sprite CreateSoftPanelSprite(string name, int width, int height, Color32 fill, Color32 edge)
        {
            Sprite sprite = CreateSprite(name, width, height, (x, y, w, h) =>
            {
                float radius = Mathf.Min(w, h) * 0.14f;
                float px = Mathf.Min(x, w - 1 - x);
                float py = Mathf.Min(y, h - 1 - y);

                if (px < radius && py < radius)
                {
                    Vector2 corner = new Vector2(radius, radius);
                    Vector2 point = new Vector2(px, py);
                    if (Vector2.Distance(point, corner) > radius)
                    {
                        return Transparent();
                    }
                }

                float edgeDistance = Mathf.Min(px, py);
                float edgeT = Mathf.Clamp01(edgeDistance / 8f);
                Color color = Color.Lerp(edge, fill, edgeT);
                float topLight = Mathf.Clamp01((y / (float)(h - 1) - 0.45f) * 1.6f);
                color = Color.Lerp(color, Color.white, topLight * 0.08f);
                return (Color32)color;
            });

            string assetPath = SpriteRoot + "/" + name + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                float border = Mathf.Round(Mathf.Min(width, height) * 0.16f);
                importer.spriteBorder = new Vector4(border, border, border, border);
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }

            return sprite;
        }

        private static Sprite CreateSprite(string name, int width, int height, Func<int, int, int, int, Color32> colorFunc)
        {
            string assetPath = SpriteRoot + "/" + name + ".png";
            string fullPath = ToFullPath(assetPath);

            if (!File.Exists(fullPath))
            {
                Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color32[] pixels = new Color32[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        pixels[y * width + x] = colorFunc(x, y, width, height);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(fullPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(assetPath);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64f;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Color32 Transparent()
        {
            return new Color32(0, 0, 0, 0);
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            EnsureAssetFolder(Path.GetDirectoryName(path).Replace("\\", "/"));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static Font DefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }

        private static Sprite UiPanelSprite()
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + "/ui_panel_clean_v1.png");
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetPath).Replace("\\", "/");
            string folderName = Path.GetFileName(assetPath);
            EnsureAssetFolder(parent);
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void MoveAssetIfNeeded(string from, string to)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(to) != null)
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(from) == null)
            {
                return;
            }

            EnsureAssetFolder(Path.GetDirectoryName(to).Replace("\\", "/"));
            string error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning("Could not move asset from " + from + " to " + to + ": " + error);
            }
        }

        private static string ToFullPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }

        private static string ToAssetPath(string fullPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string relative = fullPath.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
        }

        private static void AddTagIfMissing(string tag)
        {
            UnityEngine.Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            SerializedObject tagManager = new SerializedObject(tagManagerAsset);
            SerializedProperty tags = tagManager.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    return;
                }
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AmbientSway2D AddAmbientSway(GameObject target, float positionX, float positionY, float rotation, float scale, float speed, float phase)
        {
            if (target == null)
            {
                return null;
            }

            AmbientSway2D sway = target.AddComponent<AmbientSway2D>();
            SetFloat(sway, "positionAmplitudeX", positionX);
            SetFloat(sway, "positionAmplitudeY", positionY);
            SetFloat(sway, "rotationAmplitudeDegrees", rotation);
            SetFloat(sway, "scaleAmplitude", scale);
            SetFloat(sway, "speed", speed);
            SetFloat(sway, "phase", phase);
            return sway;
        }

        private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetObjectArray<T>(UnityEngine.Object target, string propertyName, T[] values) where T : UnityEngine.Object
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.arraySize = values != null ? values.Length : 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetColor(UnityEngine.Object target, string propertyName, Color value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.colorValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetBool(UnityEngine.Object target, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetInt(UnityEngine.Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetEnum(UnityEngine.Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetVector3(UnityEngine.Object target, string propertyName, Vector3 value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.vector3Value = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetVector2(UnityEngine.Object target, string propertyName, Vector2 value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.vector2Value = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private sealed class SpriteSet
        {
            public bool ProvidedArtworkAvailable;
            public Sprite Gorilla;
            public Sprite GorillaBoost;
            public Sprite GorillaSwing;
            public Sprite Bean;
            public Sprite Burrito;
            public Sprite Soda;
            public Sprite Vine;
            public Sprite GlowLeaf;
            public Sprite Trunk;
            public Sprite ThickVine;
            public Sprite Ground;
            public Sprite Cloud;
            public Sprite SkyGradient;
            public Sprite MistBand;
            public Sprite FartCloud;
            public Sprite FuelBar;
            public Sprite FuelSegment;
            public Sprite PlantLeft;
            public Sprite PlantMiddle;
            public Sprite PlantRight;
            public Sprite BackgroundHill;
            public Sprite BackgroundTree;
            public Sprite UiPanel;
        }

        private sealed class GorillaModelAssets
        {
            public GameObject ModelPrefab;
            public Material Material;
            public RuntimeAnimatorController AnimatorController;
        }

        private sealed class CrocodileModelAssets
        {
            public GameObject ModelPrefab;
            public Material Material;
            public RuntimeAnimatorController AnimatorController;
        }

        private sealed class ModelVisualAsset
        {
            public string Key;
            public string AssetPath;
            public GameObject ModelPrefab;
            public Material Material;
        }

        private sealed class MeshyGameAssets
        {
            public ModelVisualAsset Bean;
            public ModelVisualAsset Burrito;
            public ModelVisualAsset Soda;
            public ModelVisualAsset BananaBunch;
            public ModelVisualAsset Vine;
            public ModelVisualAsset ThornLog;
            public ModelVisualAsset SpikyStump;
            public ModelVisualAsset MudGeyser;
            public ModelVisualAsset StickySapBlob;
            public ModelVisualAsset ForegroundFernA;
            public ModelVisualAsset ForegroundFernB;
            public ModelVisualAsset BroadLeafA;
            public ModelVisualAsset RootClusterA;
            public ModelVisualAsset GroundEdgeGrassA;
            public ModelVisualAsset GroundEdgeGrassB;
            public ModelVisualAsset CanopyClusterA;
            public ModelVisualAsset CanopyClusterB;
            public ModelVisualAsset DistantTreeTrunkA;
            public ModelVisualAsset HangingLeavesA;
            public ModelVisualAsset MenuJunglePlatform;
            public ModelVisualAsset MenuFoodPile;
            public ModelVisualAsset MenuFartCloud;

            public int AvailableCount
            {
                get
                {
                    int count = 0;
                    CountIfAvailable(Bean, ref count);
                    CountIfAvailable(Burrito, ref count);
                    CountIfAvailable(Soda, ref count);
                    CountIfAvailable(BananaBunch, ref count);
                    CountIfAvailable(Vine, ref count);
                    CountIfAvailable(ThornLog, ref count);
                    CountIfAvailable(SpikyStump, ref count);
                    CountIfAvailable(MudGeyser, ref count);
                    CountIfAvailable(StickySapBlob, ref count);
                    CountIfAvailable(ForegroundFernA, ref count);
                    CountIfAvailable(ForegroundFernB, ref count);
                    CountIfAvailable(BroadLeafA, ref count);
                    CountIfAvailable(RootClusterA, ref count);
                    CountIfAvailable(GroundEdgeGrassA, ref count);
                    CountIfAvailable(GroundEdgeGrassB, ref count);
                    CountIfAvailable(CanopyClusterA, ref count);
                    CountIfAvailable(CanopyClusterB, ref count);
                    CountIfAvailable(DistantTreeTrunkA, ref count);
                    CountIfAvailable(HangingLeavesA, ref count);
                    CountIfAvailable(MenuJunglePlatform, ref count);
                    CountIfAvailable(MenuFoodPile, ref count);
                    CountIfAvailable(MenuFartCloud, ref count);
                    return count;
                }
            }

            public ModelVisualAsset FirstAvailable(params ModelVisualAsset[] assets)
            {
                if (assets == null)
                {
                    return null;
                }

                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] != null && assets[i].ModelPrefab != null)
                    {
                        return assets[i];
                    }
                }

                return null;
            }

            private static void CountIfAvailable(ModelVisualAsset asset, ref int count)
            {
                if (asset != null && asset.ModelPrefab != null)
                {
                    count++;
                }
            }
        }

        private sealed class PrefabSet
        {
            public GameObject Player;
            public GameObject Bean;
            public GameObject Burrito;
            public GameObject Soda;
            public GameObject BananaBunch;
            public GameObject SwingableVine;
            public GameObject VineObstacle;
            public GameObject TreeTrunkObstacle;
            public GameObject SpikyStumpObstacle;
            public GameObject MudGeyserObstacle;
            public GameObject StickySapObstacle;
            public GameObject CrocodileAmbush;
            public GameObject AudioManager;
            public GameObject PickupSpawner;
            public GameObject ObstacleSpawner;
            public GameObject VineSpawner;
            public GameObject GameManager;
            public GameObject MilestoneManager;
        }

        private sealed class RunChunkSet
        {
            public RunChunkDefinition[] All;
            public RunChunkDefinition[] Opening;
        }
    }
}
