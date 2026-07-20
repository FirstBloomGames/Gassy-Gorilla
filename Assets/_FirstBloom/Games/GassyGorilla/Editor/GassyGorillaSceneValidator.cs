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
        private const string CrocodileModelPath = GameRoot + "/Models/Blender/Crocodile/GG_Crocodile_Rigged.fbx";
        private const string CrocodileTexturePath = GameRoot + "/Models/Blender/Crocodile/GG_Crocodile_Atlas.png";
        private const string CrocodileMaterialPath = GameRoot + "/Materials/GG_Crocodile_Blender.mat";
        private const string CrocodileAnimatorPath = GameRoot + "/Animations/GG_Crocodile.controller";
        private const string CrocodileAmbushPrefabPath = GameRoot + "/Prefabs/Hazard_CrocodileAmbush.prefab";
        private const string CrocodileAmbushChunkPath = GameRoot + "/ScriptableObjects/RunChunks/GG_RunChunk_CrocodileAmbush.asset";
        private const string SwingableVinePrefabPath = GameRoot + "/Prefabs/Vine_Swingable.prefab";
        private const string MudGeyserPrefabPath = GameRoot + "/Prefabs/Hazard_MudGeyser.prefab";
        private const string StickySapPrefabPath = GameRoot + "/Prefabs/Hazard_StickySapBlob.prefab";
        private const string CanopyUpdraftPrefabPath = GameRoot + "/Prefabs/Interaction_CanopyUpdraft.prefab";
        private const string BounceBloomPrefabPath = GameRoot + "/Prefabs/Interaction_BounceBloom.prefab";
        private const string RunChunkFolder = GameRoot + "/ScriptableObjects/RunChunks";
        private const string DifficultyProfilePath = GameRoot + "/ScriptableObjects/GG_RunDifficulty.asset";
        private const string AudioLibraryPath = GameRoot + "/ScriptableObjects/GG_AudioLibrary.asset";
        private const string ExpeditionCatalogPath = GameRoot + "/ScriptableObjects/GG_ExpeditionCatalog.asset";
        private const string VoiceRoot = GameRoot + "/Audio/Voice";
        private const string IosHapticsPluginPath = "Assets/_FirstBloom/ArcadeFramework/Plugins/iOS/FirstBloomHaptics.mm";
        private const string TintedBackdropShaderPath = "Assets/_FirstBloom/ArcadeFramework/Shaders/ArcadeUnlitTintedTexture.shader";
        private const float MinimumAuthoredVineGrabY = 1.25f;

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

            Debug.Log("Gassy Gorilla scene validation passed. Endless Run, fifteen finite story Expeditions across three chapters, adaptive narration, lesson interactions, pause, accessibility, Jungle Badges, authored routes, textured 3D world art, audio, camera, and game loop are wired.");
        }

        private static void ValidateRequiredAssets(List<string> errors)
        {
            RequireAsset(MainMenuScenePath, errors);
            RequireAsset(GameScenePath, errors);
            RequireAsset(PaintedJungleTexturePath, errors);
            RequireAsset(CrocodileAmbushPrefabPath, errors);
            RequireAsset(CrocodileAmbushChunkPath, errors);
            RequireAsset(BounceBloomPrefabPath, errors);
            RequireAsset(DifficultyProfilePath, errors);
            RequireAsset(AudioLibraryPath, errors);
            RequireAsset(ExpeditionCatalogPath, errors);
            RequireAsset(IosHapticsPluginPath, errors);
            RequireAsset(TintedBackdropShaderPath, errors);
            ValidateExpeditionCatalog(errors);
            ValidateBadgeContract(errors);
            ValidateCrocodileAssets(errors);
            ValidateProductionAudio(errors);

            if (HasMeshyGorilla())
            {
                RequireAsset(MeshyGorillaAnimatorPath, errors);
                ValidateVineReleaseAnimation(errors);
            }
        }

        private static void ValidateProductionAudio(List<string> errors)
        {
            ValidateVoiceImporters(errors);
            ArcadeAudioLibrary library = AssetDatabase.LoadAssetAtPath<ArcadeAudioLibrary>(AudioLibraryPath);
            if (library == null)
            {
                return;
            }

            library.AppendValidationErrors(errors, true);
            ValidateComedicSfxPolicies(library, errors);
            AudioClip baseMusic = library.BaseMusic;
            AudioClip intensityMusic = library.IntensityMusic;
            AudioClip ambience = library.Ambience;
            if (baseMusic != null && intensityMusic != null)
            {
                if (baseMusic.frequency != intensityMusic.frequency ||
                    baseMusic.channels != intensityMusic.channels ||
                    Mathf.Abs(baseMusic.length - intensityMusic.length) > 0.02f)
                {
                    errors.Add("Base and intensity music stems must have matching sample rate, channels, and duration.");
                }
            }

            if (baseMusic != null && ambience != null && Mathf.Abs(baseMusic.length - ambience.length) > 0.02f)
            {
                errors.Add("Jungle ambience must match the synchronized music-loop duration.");
            }

            ValidateClipPeak(baseMusic, 0.505f, "Base music", errors);
            ValidateClipPeak(intensityMusic, 0.505f, "Intensity music", errors);
            ValidateClipPeak(ambience, 0.505f, "Jungle ambience", errors);

            HashSet<AudioClip> checkedClips = new HashSet<AudioClip>();
            ArcadeSfxEntry[] entries = library.SoundEffects;
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                ArcadeSfxEntry entry = entries[i];
                if (entry == null || entry.Clips == null)
                {
                    continue;
                }

                for (int clipIndex = 0; clipIndex < entry.Clips.Length; clipIndex++)
                {
                    AudioClip clip = entry.Clips[clipIndex];
                    if (clip != null && checkedClips.Add(clip))
                    {
                        ValidateClipPeak(clip, 0.505f, entry.Type + " SFX", errors);
                        ValidateSfxImporter(clip, errors);
                    }
                }
            }
        }

        private static void ValidateComedicSfxPolicies(ArcadeAudioLibrary library, List<string> errors)
        {
            ValidateSfxFamilyCount(library, ArcadeSfxType.Boost, 6, errors);
            ValidateSfxFamilyCount(library, ArcadeSfxType.BoostFailed, 3, errors);
            ValidateSfxFamilyCount(library, ArcadeSfxType.Pickup, 4, errors);
            ValidateSfxFamilyCount(library, ArcadeSfxType.GeyserWarning, 2, errors);
            ValidateSfxFamilyCount(library, ArcadeSfxType.GeyserBurst, 2, errors);
            ValidateSfxFamilyCount(library, ArcadeSfxType.SapCatch, 2, errors);
            ValidateSfxFamilyCount(library, ArcadeSfxType.SapPop, 3, errors);
            ValidateSfxFamilyCount(library, ArcadeSfxType.Updraft, 2, errors);

            if (library.TryGetEntry(ArcadeSfxType.Boost, out ArcadeSfxEntry boost))
            {
                if (boost.Volume > 0.63f)
                {
                    errors.Add("Boost audio gain must remain at or below 0.63.");
                }

                if (boost.RareClipIndex != 5 || boost.RareClipCooldownPlays < 8)
                {
                    errors.Add("Boost audio must reserve variant 6 as the heroic clip with an eight-play cooldown.");
                }
            }

            if (library.TryGetEntry(ArcadeSfxType.Pickup, out ArcadeSfxEntry pickup))
            {
                if (pickup.Volume > 0.21f)
                {
                    errors.Add("Pickup audio gain must remain at or below 0.21.");
                }

                if (pickup.MaximumSimultaneousVoices != 1 ||
                    pickup.VoiceLimitMode != ArcadeSfxVoiceLimitMode.ReplaceOldest)
                {
                    errors.Add("Pickup audio must cap at one voice and replace the previous pickup.");
                }

                if (pickup.MinimumRetriggerInterval < 0.055f)
                {
                    errors.Add("Pickup audio must consolidate dense chains with at least a 0.055-second retrigger guard.");
                }
            }

            ValidateSfxFamilyGain(library, ArcadeSfxType.VineSwing, 0.33f, errors);
            ValidateSfxFamilyGain(library, ArcadeSfxType.UiClick, 0.35f, errors);
            ValidateSfxFamilyGain(library, ArcadeSfxType.UiBack, 0.33f, errors);
            ValidateSfxFamilyGain(library, ArcadeSfxType.UiError, 0.37f, errors);
            ValidateSfxFamilyGain(library, ArcadeSfxType.GeyserWarning, 0.41f, errors);
            ValidateSfxFamilyGain(library, ArcadeSfxType.GeyserBurst, 0.57f, errors);
            ValidateSfxFamilyGain(library, ArcadeSfxType.SapCatch, 0.41f, errors);
            ValidateSfxFamilyGain(library, ArcadeSfxType.SapPop, 0.49f, errors);
            ValidateSfxFamilyGain(library, ArcadeSfxType.Updraft, 0.45f, errors);

            ArcadeSfxEntry[] entries = library.SoundEffects;
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i] != null && entries[i].Volume > 0.73f)
                    {
                        errors.Add(entries[i].Type + " audio gain exceeds the calibrated 0.73 family ceiling.");
                    }
                }
            }
        }

        private static void ValidateSfxFamilyGain(
            ArcadeAudioLibrary library,
            ArcadeSfxType type,
            float maximumGain,
            List<string> errors)
        {
            if (library.TryGetEntry(type, out ArcadeSfxEntry entry) && entry.Volume > maximumGain)
            {
                errors.Add(type + " audio gain must remain at or below " + maximumGain.ToString("F2") + ".");
            }
        }

        private static void ValidateSfxFamilyCount(
            ArcadeAudioLibrary library,
            ArcadeSfxType type,
            int expectedCount,
            List<string> errors)
        {
            if (!library.TryGetEntry(type, out ArcadeSfxEntry entry) ||
                entry.Clips == null ||
                entry.Clips.Length != expectedCount)
            {
                errors.Add(type + " audio must contain exactly " + expectedCount + " production variants.");
            }
        }

        private static void ValidateSfxImporter(AudioClip clip, List<string> errors)
        {
            string path = AssetDatabase.GetAssetPath(clip);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (clip.channels != 1 || importer == null || !importer.forceToMono)
            {
                errors.Add("Production SFX must import as mono: " + path + ".");
            }

            if (importer == null || !importer.defaultSampleSettings.preloadAudioData)
            {
                errors.Add("Production SFX must preload for reliable WebGL playback: " + path + ".");
            }
        }

        private static void ValidateVoiceImporters(List<string> errors)
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { VoiceRoot });
            if (guids.Length == 0)
            {
                errors.Add("Production milestone voice clips are missing.");
                return;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null || !importer.defaultSampleSettings.preloadAudioData)
                {
                    errors.Add("Voice clip must preload for reliable WebGL playback: " + path + ".");
                }
            }
        }

        private static void ValidateAudioManager(
            ArcadeAudioManager manager,
            string sceneLabel,
            List<string> errors)
        {
            if (manager == null)
            {
                return;
            }

            if (manager.AudioLibrary == null)
            {
                errors.Add(sceneLabel + " audio manager has no production audio library.");
            }

            if (manager.UsesGeneratedPlaceholderMusic)
            {
                errors.Add(sceneLabel + " audio manager still permits generated placeholder music.");
            }

            if (manager.SfxMixHeadroom > 0.73f)
            {
                errors.Add(sceneLabel + " SFX mix headroom must remain at or below 0.73.");
            }

            if (manager.SfxVolume > 0.71f)
            {
                errors.Add(sceneLabel + " default SFX slider must remain at or below 0.71.");
            }
        }

        private static void ValidateClipPeak(
            AudioClip clip,
            float maximumPeak,
            string label,
            List<string> errors)
        {
            if (clip == null)
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(clip);
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string fullPath = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                errors.Add(label + " source master is missing: " + clip.name + ".");
                return;
            }

            byte[] bytes = File.ReadAllBytes(fullPath);
            int dataOffset = FindWaveDataOffset(bytes, out int dataLength);
            if (dataOffset < 0 || dataLength < 2)
            {
                errors.Add(label + " is not a readable PCM WAV master: " + clip.name + ".");
                return;
            }

            int end = Mathf.Min(bytes.Length, dataOffset + dataLength);
            int peakSample = 0;
            for (int offset = dataOffset; offset + 1 < end; offset += 2)
            {
                short sample = BitConverter.ToInt16(bytes, offset);
                peakSample = Mathf.Max(peakSample, Mathf.Abs((int)sample));
            }

            float peak = peakSample / (float)short.MaxValue;
            if (peak > maximumPeak)
            {
                errors.Add(label + " exceeds its headroom budget at " + peak.ToString("F3") + ": " + clip.name + ".");
            }
        }

        private static int FindWaveDataOffset(byte[] bytes, out int dataLength)
        {
            dataLength = 0;
            if (bytes == null || bytes.Length < 44 ||
                bytes[0] != 'R' || bytes[1] != 'I' || bytes[2] != 'F' || bytes[3] != 'F' ||
                bytes[8] != 'W' || bytes[9] != 'A' || bytes[10] != 'V' || bytes[11] != 'E')
            {
                return -1;
            }

            int offset = 12;
            while (offset + 8 <= bytes.Length)
            {
                int chunkLength = BitConverter.ToInt32(bytes, offset + 4);
                if (chunkLength < 0)
                {
                    return -1;
                }

                if (bytes[offset] == 'd' && bytes[offset + 1] == 'a' &&
                    bytes[offset + 2] == 't' && bytes[offset + 3] == 'a')
                {
                    dataLength = Mathf.Min(chunkLength, bytes.Length - offset - 8);
                    return offset + 8;
                }

                offset += 8 + chunkLength + (chunkLength & 1);
            }

            return -1;
        }

        private static void ValidateCrocodileAssets(List<string> errors)
        {
            RequireAsset(CrocodileModelPath, errors);
            RequireAsset(CrocodileTexturePath, errors);
            RequireAsset(CrocodileMaterialPath, errors);
            RequireAsset(CrocodileAnimatorPath, errors);

            string[] expectedClips = { "Idle_Submerged", "Lunge_Snap", "Settle_Submerge" };
            HashSet<string> clipNames = new HashSet<string>(StringComparer.Ordinal);
            UnityEngine.Object[] importedAssets = AssetDatabase.LoadAllAssetsAtPath(CrocodileModelPath);
            for (int i = 0; i < importedAssets.Length; i++)
            {
                AnimationClip clip = importedAssets[i] as AnimationClip;
                if (clip != null && !clip.name.StartsWith("__preview", StringComparison.OrdinalIgnoreCase))
                {
                    clipNames.Add(clip.name);
                }
            }

            for (int i = 0; i < expectedClips.Length; i++)
            {
                if (!clipNames.Contains(expectedClips[i]))
                {
                    errors.Add("Blender crocodile is missing imported clip " + expectedClips[i] + ".");
                }
            }

            GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(CrocodileModelPath);
            if (sourceModel != null)
            {
                SkinnedMeshRenderer[] renderers = sourceModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (renderers.Length != 1 || renderers[0].sharedMesh == null)
                {
                    errors.Add("Blender crocodile must import as exactly one skinned mesh renderer.");
                }
                else
                {
                    int triangles = renderers[0].sharedMesh.triangles.Length / 3;
                    if (triangles > 7000)
                    {
                        errors.Add("Blender crocodile exceeds its 7,000-triangle mobile budget: " + triangles + ".");
                    }

                    if (renderers[0].sharedMesh.subMeshCount != 1)
                    {
                        errors.Add("Blender crocodile must keep one atlas-backed submesh for one finish draw call.");
                    }
                }
            }

            ValidateCrocodileAnimatorController(errors);
        }

        private static void ValidateCrocodileAnimatorController(List<string> errors)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CrocodileAnimatorPath);
            if (controller == null || controller.layers.Length == 0)
            {
                return;
            }

            HashSet<string> expectedStates = new HashSet<string>(StringComparer.Ordinal)
            {
                "Idle_Submerged",
                "Lunge_Snap",
                "Settle_Submerge"
            };
            ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state != null && expectedStates.Remove(state.name) && (state.motion == null || state.motion.name != state.name))
                {
                    errors.Add("Crocodile animator state " + state.name + " is not wired to its matching imported clip.");
                }
            }

            foreach (string missingState in expectedStates)
            {
                errors.Add("Crocodile animator is missing state " + missingState + ".");
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

            ValidateSceneAudioListener("Main menu", errors);
            ArcadeAudioManager audioManager = RequireComponent<ArcadeAudioManager>("Main menu audio manager", errors);
            ValidateAudioManager(audioManager, "Main menu", errors);
            RequireComponent<ArcadeTimeController>("Main menu time manager", errors);
            MainMenuController menuController = RequireComponent<MainMenuController>("Main menu controller", errors);
            ArcadeSettingsMenu settingsMenu =
                RequireComponent<ArcadeSettingsMenu>("Main menu settings menu", errors);
            RequireComponent<CanvasScaler>("Main menu canvas scaler", errors);
            RequireSceneObject("Menu_GorillaHero", errors);
            RequireSceneObject("Menu_FartCloud", errors);
            RequireSceneObject("Menu_PaintedJungleBackdrop_3D", errors);
            RequireSceneObject("World_KeyLight_Menu", errors);
            RequireSceneObject("UI_MainMenuActions", errors);
            RequireSceneObject("EndlessRunButton", errors);
            RequireSceneObject("ExpeditionsButton", errors);
            RequireSceneObject("UI_ExpeditionSelectPanel", errors);
            RequireSceneObject("StartExpeditionButton", errors);
            RequireSceneObject("ExpeditionChapter", errors);
            RequireSceneObject("PreviousChapterButton", errors);
            RequireSceneObject("NextChapterButton", errors);
            RequireSceneObject("ExpeditionLesson", errors);
            RequireSceneObject("BadgesButton", errors);
            RequireSceneObject("UI_JungleBadgesPanel", errors);
            for (int i = 1; i <= 5; i++)
            {
                RequireSceneObject("ExpeditionButton_" + i, errors);
            }

            if (menuController != null && !menuController.IsExpeditionUiConfigured)
            {
                errors.Add("Main menu Expedition selector is not fully wired to its fifteen-level, three-chapter catalog.");
            }

            if (menuController != null && !menuController.IsBadgeUiConfigured)
            {
                errors.Add("Main menu Jungle Badges panel is not fully wired.");
            }

            if (settingsMenu != null && !settingsMenu.HasAccessibilityControls)
            {
                errors.Add("Main menu settings must expose Reduced Motion, Haptics, and Subtitles.");
            }

            ValidateToggleVisuals("Main menu", errors);

            for (int i = 1; i <= GassyBadgeService.Count; i++)
            {
                RequireSceneObject("Badge_" + i.ToString("D2"), errors);
            }

            ValidateRenderableVisual("Menu_GorillaHero", errors);
            ValidateRenderableVisual("Menu_FartCloud", errors);
            RequireNoSpriteRenderers("Main menu scene", errors);
            RequireTexturedWorldRenderers("Main menu scene", errors);
        }

        private static void ValidateGameScene(List<string> errors)
        {
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            ValidateSceneAudioListener("Game", errors);
            ArcadeAudioManager audioManager = RequireComponent<ArcadeAudioManager>("Game audio manager", errors);
            ValidateAudioManager(audioManager, "Game", errors);
            RequireComponent<ArcadeTimeController>("Game time manager", errors);
            GassyGorillaGameManager gameManager = RequireComponent<GassyGorillaGameManager>("Game manager", errors);
            RequireComponent<GassyScoreManager>("Score manager", errors);
            RequireComponent<DistanceScoreTracker>("Distance tracker", errors);
            RequireComponent<SmoothCameraFollow2D>("Camera follow", errors);
            RequireComponent<FartBarUI>("Fart fuel bar", errors);
            RequireComponent<GassyTutorialPromptController>("Tutorial prompt controller", errors);
            RequireComponent<MilestoneEventManager>("Milestone manager", errors);
            RequireComponent<GassyGorillaAudioDirector>("Gassy Gorilla audio director", errors);
            GassyFeedbackDirector feedbackDirector =
                RequireComponent<GassyFeedbackDirector>("Haptic feedback director", errors);
            GassyBadgeTracker badgeTracker =
                RequireComponent<GassyBadgeTracker>("Jungle Badge tracker", errors);
            ArcadePausePanel pausePanel =
                RequireComponent<ArcadePausePanel>("Pause panel", errors);
            ArcadeSettingsMenu settingsMenu =
                RequireComponent<ArcadeSettingsMenu>("Game settings menu", errors);
            RequireComponent<TextOverlay>("Tutorial text overlay", errors);
            GassyExpeditionRunController expeditionRun =
                RequireComponent<GassyExpeditionRunController>("Expedition run controller", errors);
            ExpeditionFinishLine finishLine =
                RequireComponent<ExpeditionFinishLine>("Expedition finish line", errors);

            if (gameManager != null && !gameManager.IsExpeditionConfigured)
            {
                errors.Add("Game manager does not have the complete Expedition story, objective, and success flow wired.");
            }

            if (gameManager != null && !gameManager.IsPauseConfigured)
            {
                errors.Add("Game manager does not have the complete pause and settings return flow wired.");
            }

            if (pausePanel != null && !pausePanel.IsConfigured)
            {
                errors.Add("Pause panel is missing its CanvasGroup transition controller.");
            }

            if (settingsMenu != null && !settingsMenu.HasAccessibilityControls)
            {
                errors.Add("Game settings must expose Reduced Motion, Haptics, and Subtitles.");
            }

            ValidateToggleVisuals("Game", errors);

            if (feedbackDirector != null && !feedbackDirector.IsConfigured)
            {
                errors.Add("Haptic feedback director is not connected to the gorilla.");
            }

            if (badgeTracker != null && !badgeTracker.IsConfigured)
            {
                errors.Add("Jungle Badge tracker is missing gameplay events, catalog, or toast wiring.");
            }

            if (expeditionRun != null && !expeditionRun.IsConfigured)
            {
                errors.Add("Expedition run controller is missing player, HUD, objective, remaining-distance, or finish-line wiring.");
            }

            if (finishLine != null && !finishLine.IsConfigured)
            {
                errors.Add("Expedition finish line is missing its trigger, pulse root, or glow renderers.");
            }

            RunChunkDirector runDirector = RequireComponent<RunChunkDirector>("Run chunk director", errors);
            if (runDirector != null)
            {
                runDirector.AppendValidationErrors(errors, 5000);
                ValidateCrocodileAmbushChunk(runDirector, errors);
            }

            RequireNoLegacyGameplaySpawners(errors);

            GorillaController gorilla = RequireComponent<GorillaController>("Player gorilla", errors);
            if (gorilla != null)
            {
                ValidatePlayerPolish(gorilla, errors);
                ValidateVineReleaseContract(gorilla, errors);
            }

            ValidateFuelHud(errors);

            RequireSceneObject("HUD_FartBar", errors);
            RequireSceneObject("TutorialOverlay", errors);
            RequireSceneObject("Director_RunChunks", errors);
            RequireSceneObject("Director_Audio", errors);
            RequireSceneObject("PaintedJungleBackdrop_3D", errors);
            RequireSceneObject("DeathZone", errors);
            RequireSceneObject("World_KeyLight_Game", errors);
            RequireSceneObject("Manager_ExpeditionRun", errors);
            RequireSceneObject("UI_ExpeditionHUD", errors);
            RequireSceneObject("UI_ExpeditionCoach", errors);
            RequireSceneObject("UI_ExpeditionStoryPanel", errors);
            RequireSceneObject("Lesson", errors);
            RequireSceneObject("UI_ExpeditionSuccessPanel", errors);
            RequireSceneObject("ExpeditionFinishLine_3D", errors);
            RequireSceneObject("PauseButton", errors);
            RequireSceneObject("UI_PausePanel", errors);
            RequireSceneObject("PauseContent", errors);
            RequireSceneObject("BadgeToast", errors);
            RequireSceneObject("Director_Feedback", errors);
            RequireSceneObject("Manager_JungleBadges", errors);
            RequireSceneObject("Director_ExpeditionTheme", errors);
            RequireSceneObject("Director_ExpeditionNarration", errors);
            RequireSceneObject("UI_Subtitles", errors);
            RequireSceneObject("ReplayVoiceButton", errors);
            RequireSceneObjectAbsent("Ground_3D", errors);
            RequireSceneObjectAbsent("Distant_MeshyForestDepth_3D", errors);
            RequireSceneObjectAbsent("Foreground_3DDecor", errors);

            RequireNoSpriteRenderers("Game scene", errors);
            ValidateRenderableVisual("ExpeditionFinishLine_3D", errors);
            RequireTexturedWorldRenderers("Game scene", errors);
        }

        private static void ValidateExpeditionCatalog(List<string> errors)
        {
            GassyExpeditionCatalog catalog =
                AssetDatabase.LoadAssetAtPath<GassyExpeditionCatalog>(ExpeditionCatalogPath);
            if (catalog != null)
            {
                catalog.AppendValidationErrors(errors);
            }
        }

        private static void ValidateBadgeContract(List<string> errors)
        {
            GassyBadgeDefinition[] definitions = GassyBadgeService.Definitions;
            if (definitions == null || definitions.Length != 8)
            {
                errors.Add("Jungle Badges must contain exactly eight launch definitions.");
                return;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < definitions.Length; i++)
            {
                GassyBadgeDefinition definition = definitions[i];
                if (definition == null)
                {
                    errors.Add("Jungle Badge definition " + i + " is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.Id) || !ids.Add(definition.Id))
                {
                    errors.Add("Jungle Badge IDs must be non-empty and unique.");
                }

                if (string.IsNullOrWhiteSpace(definition.DisplayTitle) ||
                    string.IsNullOrWhiteSpace(definition.Description) ||
                    definition.Target <= 0)
                {
                    errors.Add("Jungle Badge " + definition.Id + " is missing release-ready copy or a valid target.");
                }
            }

            RequireBadge(definitions, "first-blast", GassyBadgeMetric.SuccessfulBoosts, 1, errors);
            RequireBadge(definitions, "vine-time", GassyBadgeMetric.VineReleases, 10, errors);
            RequireBadge(definitions, "bean-counter", GassyBadgeMetric.FoodPickups, 50, errors);
            RequireBadge(definitions, "swamp-smarts", GassyBadgeMetric.CrocodileDodges, 5, errors);
            RequireBadge(definitions, "hundred-meter-hero", GassyBadgeMetric.EndlessDistance, 100, errors);
            RequireBadge(definitions, "jungle-legend", GassyBadgeMetric.EndlessDistance, 500, errors);
            RequireBadge(definitions, "star-collector", GassyBadgeMetric.ExpeditionStars, 10, errors);
            RequireBadge(definitions, "home-for-dinner", GassyBadgeMetric.CompletedExpeditions, 5, errors);
        }

        private static void RequireBadge(
            GassyBadgeDefinition[] definitions,
            string id,
            GassyBadgeMetric metric,
            int target,
            List<string> errors)
        {
            for (int i = 0; i < definitions.Length; i++)
            {
                GassyBadgeDefinition definition = definitions[i];
                if (definition != null && definition.Id == id)
                {
                    if (definition.Metric != metric || definition.Target != target)
                    {
                        errors.Add("Jungle Badge " + id + " does not match its launch metric and target.");
                    }

                    return;
                }
            }

            errors.Add("Jungle Badge contract is missing " + id + ".");
        }

        private static void ValidateToggleVisuals(string sceneLabel, List<string> errors)
        {
            ArcadeToggleVisual[] visuals =
                UnityEngine.Object.FindObjectsByType<ArcadeToggleVisual>(FindObjectsInactive.Include);
            if (visuals.Length != 3)
            {
                errors.Add(sceneLabel + " scene must contain exactly three accessible switch visuals.");
                return;
            }

            for (int i = 0; i < visuals.Length; i++)
            {
                if (!visuals[i].IsConfigured)
                {
                    errors.Add(sceneLabel + " accessibility switch visual is not fully wired.");
                }
            }
        }

        private static void ValidateSceneAudioListener(string sceneLabel, List<string> errors)
        {
            AudioListener[] listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            if (listeners.Length != 1)
            {
                errors.Add(sceneLabel + " scene must contain exactly one AudioListener; found " + listeners.Length + ".");
                return;
            }

            AudioListener listener = listeners[0];
            if (!listener.isActiveAndEnabled)
            {
                errors.Add(sceneLabel + " AudioListener must be active and enabled.");
            }

            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
            {
                errors.Add(sceneLabel + " scene has no active camera tagged MainCamera.");
            }
            else if (listener.gameObject != mainCamera.gameObject)
            {
                errors.Add(sceneLabel + " AudioListener must be attached to the MainCamera.");
            }
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
            else
            {
                Transform gorillaModel = FindChild(gorilla.transform, "Visual_Gorilla_3D");
                if (Mathf.Abs(Mathf.DeltaAngle(gorillaModel.localEulerAngles.y, 180f)) > 1f)
                {
                    errors.Add("Player gorilla imported model must keep its established 180-degree travel-facing correction.");
                }
            }

            LagoonFinishPresentation lagoonFinish = gorilla.GetComponent<LagoonFinishPresentation>();
            if (lagoonFinish == null)
            {
                errors.Add("Player gorilla is missing the lightweight lagoon reflection and impact presentation.");
            }
            else if (!lagoonFinish.HasCrocodileFinish)
            {
                errors.Add("Player gorilla lagoon finish is missing its rigged Blender crocodile.");
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

            Transform crocodile = FindChild(gorilla.transform, "Crocodile Finish 3D");
            if (crocodile == null)
            {
                errors.Add("Player gorilla is missing the inactive crocodile finish object.");
            }
            else
            {
                if (crocodile.gameObject.activeSelf)
                {
                    errors.Add("Crocodile finish must remain inactive until a lagoon fall.");
                }

                SkinnedMeshRenderer[] crocodileRenderers = crocodile.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (crocodileRenderers.Length != 1 || crocodileRenderers[0].sharedMaterials.Length != 1)
                {
                    errors.Add("Crocodile finish must use exactly one skinned renderer and one atlas material.");
                }

                Animator crocodileAnimator = crocodile.GetComponentInChildren<Animator>(true);
                if (crocodileAnimator == null || crocodileAnimator.runtimeAnimatorController == null)
                {
                    errors.Add("Crocodile finish has no generated animator controller.");
                }
                else
                {
                    if (crocodileAnimator.applyRootMotion)
                    {
                        errors.Add("Crocodile finish must not apply root motion.");
                    }

                    if (crocodileAnimator.updateMode != AnimatorUpdateMode.UnscaledTime)
                    {
                        errors.Add("Crocodile finish must animate on unscaled game-over time.");
                    }
                }

                Transform crocodileModel = FindChild(crocodile, "Visual_Crocodile_3D");
                if (crocodileModel == null || Quaternion.Angle(crocodileModel.localRotation, Quaternion.identity) > 1f)
                {
                    errors.Add("Crocodile finish must keep the FBX's native Unity-facing axis so its lunge travels toward the gorilla.");
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

        private static void ValidateVineReleaseContract(GorillaController gorilla, List<string> errors)
        {
            if (gorilla.VineReleaseSafetyDuration < 0.95f || gorilla.VineReleaseSafetyClearance < 0.3f)
            {
                errors.Add("Vine release safety must preserve at least one second of flight with 0.3 units of danger-line clearance.");
            }

            if (gorilla.MinimumVineReleaseLift < 3.9f)
            {
                errors.Add("Vine release baseline lift is below the production comfort floor.");
            }

            float duration = gorilla.VineReleaseSafetyDuration;
            float requiredLift = gorilla.CalculateMinimumSafeVineReleaseLift(MinimumAuthoredVineGrabY);
            float appliedLift = Mathf.Min(
                gorilla.MaximumVerticalSpeed,
                Mathf.Max(gorilla.MinimumVineReleaseLift, requiredLift));
            float gravityAcceleration = Mathf.Abs(Physics2D.gravity.y * gorilla.GravityScale);
            float projectedY = MinimumAuthoredVineGrabY + appliedLift * duration -
                0.5f * gravityAcceleration * duration * duration;
            float promisedY = gorilla.VineReleaseDangerY + gorilla.VineReleaseSafetyClearance;
            if (projectedY + 0.01f < promisedY)
            {
                errors.Add("Vine release tuning fails the one-second ballistic survival contract at the minimum authored grab height.");
            }

            GameObject vinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwingableVinePrefabPath);
            VineSwingTrigger vineTrigger = vinePrefab != null ? vinePrefab.GetComponent<VineSwingTrigger>() : null;
            if (vineTrigger == null)
            {
                return;
            }

            float grabOffsetY = vinePrefab.transform.InverseTransformPoint(vineTrigger.GrabPoint.position).y;
            string[] chunkGuids = AssetDatabase.FindAssets("t:RunChunkDefinition", new[] { RunChunkFolder });
            RunChunkTag blockedAfterVine = RunChunkTag.Hazard | RunChunkTag.Predator;
            for (int i = 0; i < chunkGuids.Length; i++)
            {
                string chunkPath = AssetDatabase.GUIDToAssetPath(chunkGuids[i]);
                RunChunkDefinition definition = AssetDatabase.LoadAssetAtPath<RunChunkDefinition>(chunkPath);
                if (definition == null || !definition.AllowInMainPool || (definition.Tags & RunChunkTag.Vine) == 0)
                {
                    continue;
                }

                if ((definition.BlockedNextTags & blockedAfterVine) != blockedAfterVine)
                {
                    errors.Add(definition.ChunkId + " must reserve its next route beat from hazards and predators.");
                }

                RunChunkSpawn[] spawns = definition.Spawns;
                for (int spawnIndex = 0; spawnIndex < spawns.Length; spawnIndex++)
                {
                    RunChunkSpawn spawn = spawns[spawnIndex];
                    if (spawn == null || spawn.Kind != RunChunkSpawnKind.SwingVine)
                    {
                        continue;
                    }

                    float authoredGrabY = spawn.LocalPosition.y + grabOffsetY;
                    if (authoredGrabY + 0.01f < MinimumAuthoredVineGrabY)
                    {
                        errors.Add(definition.ChunkId + " places a vine grab at " + authoredGrabY.ToString("0.00") +
                            " Y; the production floor is " + MinimumAuthoredVineGrabY.ToString("0.00") + ".");
                    }
                }
            }
        }

        private static void ValidateFuelHud(List<string> errors)
        {
            FartBarUI fartBar = FindSceneComponent<FartBarUI>();
            if (fartBar == null)
            {
                return;
            }

            MeterFillUI meter = fartBar.GetComponent<MeterFillUI>();
            if (meter == null || meter.SegmentCount != 10 || meter.ShowsMaximum)
            {
                errors.Add("Fart fuel HUD must use ten wired canister segments and the compact current-value readout.");
            }

            string[] requiredChildren =
            {
                "FuelIconWell",
                "FuelCloudIcon",
                "FuelGlassTrack",
                "Label",
                "Value"
            };
            for (int i = 0; i < requiredChildren.Length; i++)
            {
                if (FindChild(fartBar.transform, requiredChildren[i]) == null)
                {
                    errors.Add("Fart fuel HUD is missing its " + requiredChildren[i] + " visual lane.");
                }
            }

            RectTransform rect = fartBar.GetComponent<RectTransform>();
            if (rect == null || rect.sizeDelta.x < 390f || rect.sizeDelta.y < 56f)
            {
                errors.Add("Fart fuel HUD no longer has the stable release dimensions required for landscape phones.");
            }
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
                MudGeyserPrefabPath,
                StickySapPrefabPath,
                CanopyUpdraftPrefabPath,
                BounceBloomPrefabPath,
                CrocodileAmbushPrefabPath
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
            ValidateCrocodileAmbushPrefab(errors);
            ValidateLessonInteractionPrefabs(errors);
        }

        private static void ValidateLessonInteractionPrefabs(List<string> errors)
        {
            GameObject stump = AssetDatabase.LoadAssetAtPath<GameObject>(
                GameRoot + "/Prefabs/Hazard_SpikyStump.prefab");
            if (stump != null)
            {
                GassyInteractionMarker marker = stump.GetComponent<GassyInteractionMarker>();
                GassyHazardPassReporter reporter = stump.GetComponent<GassyHazardPassReporter>();
                if (marker == null ||
                    marker.InteractionType != GassyInteractionType.ThornDodge ||
                    reporter == null ||
                    !reporter.IsConfigured)
                {
                    errors.Add("Thorn stump must report a successful pass as the Thorn Dodge lesson.");
                }
            }

            GameObject geyser = AssetDatabase.LoadAssetAtPath<GameObject>(MudGeyserPrefabPath);
            if (geyser != null)
            {
                GassyMudGeyserController controller =
                    geyser.GetComponent<GassyMudGeyserController>();
                if (controller == null || !controller.IsConfigured)
                {
                    errors.Add("Mud geyser is missing its warning, eruption, hitbox, or lesson wiring.");
                }
                else
                {
                    if (controller.WarningDuration < 0.8f ||
                        controller.ActivationDistance < 4.5f ||
                        controller.EruptionDuration < 0.45f)
                    {
                        errors.Add("Mud geyser no longer guarantees its readable reaction window.");
                    }

                    if (controller.EruptionHitbox == null ||
                        !controller.EruptionHitbox.isTrigger)
                    {
                        errors.Add("Mud geyser eruption must use a trigger hitbox.");
                    }
                }
            }

            GameObject sap = AssetDatabase.LoadAssetAtPath<GameObject>(StickySapPrefabPath);
            if (sap != null)
            {
                GassyStickySapTrap trap = sap.GetComponent<GassyStickySapTrap>();
                if (trap == null || !trap.IsConfigured ||
                    sap.GetComponentInChildren<ArcadeHazard>(true) != null)
                {
                    errors.Add("Sticky sap must be a configured, recoverable trigger with no fatal hazard component.");
                }
                else
                {
                    BoxCollider2D sapTrigger = trap.Trigger as BoxCollider2D;
                    if (sapTrigger == null ||
                        sapTrigger.size.x < 2.5f ||
                        sapTrigger.size.y < 1.15f ||
                        trap.SupportRoot == null ||
                        trap.CatchAnchor == null ||
                        trap.StrandCount < 5 ||
                        trap.SupportRoot.Find("RootedBranchShelf_3D") == null)
                    {
                        errors.Add("Sticky sap must provide a broad supported catch bed, magnetic anchor, and at least five elastic strands.");
                    }

                    Transform sapSurface =
                        sap.transform.Find("StickySapSurface_3D");
                    Bounds sapBounds;
                    if (sapSurface == null ||
                        sapSurface.Find("AmberSapPoolCore_3D") == null ||
                        sapSurface.Find("AmberPuddleLobe_3") == null ||
                        sapSurface.Find("SapBubble_2") == null ||
                        sapSurface.Find("HangingSapDrip_1") == null ||
                        !TryCalculateVisualBounds(
                            sapSurface.gameObject,
                            out sapBounds) ||
                        sapBounds.size.x < 2.9f ||
                        sapBounds.size.y < 0.45f ||
                        sapBounds.size.y > 1.15f)
                    {
                        errors.Add("Sticky sap must remain a low, wide, bubbling 3D amber bed with visible drips instead of a floating dot or mushroom stack.");
                    }
                }
            }
            GameObject updraft = AssetDatabase.LoadAssetAtPath<GameObject>(
                CanopyUpdraftPrefabPath);
            if (updraft != null)
            {
                GassyCanopyUpdraft current = updraft.GetComponent<GassyCanopyUpdraft>();
                if (current == null || !current.IsConfigured ||
                    current.LeafCount < 5 ||
                    current.LeafCount > 8 ||
                    current.LiftVelocity < 4f ||
                    current.LiftVelocity > 6.2f)
                {
                    errors.Add("Canopy updraft is missing its bounded lift, trigger, glow, or mobile leaf budget.");
                }
            }

            GameObject bounceBloom = AssetDatabase.LoadAssetAtPath<GameObject>(
                BounceBloomPrefabPath);
            if (bounceBloom != null)
            {
                GassyBounceBloom bloom =
                    bounceBloom.GetComponent<GassyBounceBloom>();
                if (bloom == null || !bloom.IsConfigured)
                {
                    errors.Add("Moonleaf is missing its supported platform, staged compression, contact anchor, burst, or lesson wiring.");
                }
                else
                {
                    BoxCollider2D bloomTrigger =
                        bloom.Trigger as BoxCollider2D;
                    if (bloomTrigger == null ||
                        bloomTrigger.size.x < 3.4f ||
                        bloomTrigger.size.y < 1.35f ||
                        bloom.SupportRoot == null ||
                        bloom.SpringRoot == null ||
                        bloom.SpringRoot.localPosition.y < 0.4f ||
                        bloom.SupportRoot.localPosition.y >=
                            bloom.SpringRoot.localPosition.y ||
                        bloom.SpringRoot.Find("GiantMoonleafBlade_3D") == null ||
                        bloom.SpringRoot.Find("MoonleafCentralVein_3D") == null ||
                        bloom.SpringRoot.Find(
                            "LaunchLeaf_1/TexturedSpringLobe_3D") == null ||
                        bloom.SpringRoot.Find(
                            "LaunchLeaf_2/TexturedSpringLobe_3D") == null ||
                        bloom.SpringRoot.Find(
                            "LaunchLeaf_3/TexturedSpringLobe_3D") == null ||
                        bloom.ContactAnchor == null ||
                        bloom.ContactAnchor.parent != bloom.SpringRoot ||
                        bloom.ContactAnchor.localPosition.y < 0.5f ||
                        bloom.CompressionDuration < 0.12f ||
                        bloom.CompressionDuration > 0.22f ||
                        bloom.LiftVelocity < 6.5f ||
                        bloom.LiftVelocity > 7.6f ||
                        bloom.ForwardKick < 0.8f ||
                        bloom.ForwardKick > 1.8f)
                    {
                        errors.Add("Moonleaf must keep its raised hero-scale landing zone, readable squash beat, and bounded premium rebound.");
                    }

                    Bounds leafBounds;
                    if (bloom.SpringRoot == null ||
                        !TryCalculateVisualBounds(
                            bloom.SpringRoot.gameObject,
                            out leafBounds) ||
                        leafBounds.size.x < 3.2f ||
                        leafBounds.size.y < 0.55f)
                    {
                        errors.Add("Moonleaf foliage must remain a giant readable 3D platform rather than a cluster of tiny dots.");
                    }
                }

                RunChunkDefinition bloomChunk =
                    AssetDatabase.LoadAssetAtPath<RunChunkDefinition>(
                        RunChunkFolder + "/GG_RunChunk_BounceBloom.asset");
                bool hasRaisedMoonleaf = false;
                if (bloomChunk != null)
                {
                    RunChunkSpawn[] spawns = bloomChunk.Spawns;
                    for (int i = 0; i < spawns.Length; i++)
                    {
                        RunChunkSpawn spawn = spawns[i];
                        if (spawn != null &&
                            spawn.Prefab == bounceBloom &&
                            spawn.LocalPosition.y >= 0.55f)
                        {
                            hasRaisedMoonleaf = true;
                            break;
                        }
                    }
                }

                if (!hasRaisedMoonleaf)
                {
                    errors.Add(
                        "Bounce Bloom route must raise the moonleaf clearly above the waterline.");
                }
            }
        }

        private static bool TryCalculateVisualBounds(
            GameObject root,
            out Bounds bounds)
        {
            bounds = new Bounds();
            if (root == null)
            {
                return false;
            }

            bool initialized = false;
            Matrix4x4 worldToRoot = root.transform.worldToLocalMatrix;
            MeshFilter[] meshFilters =
                root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter filter = meshFilters[i];
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                Matrix4x4 toRoot =
                    worldToRoot * filter.transform.localToWorldMatrix;
                EncapsulateTransformedBounds(
                    filter.sharedMesh.bounds,
                    toRoot,
                    ref bounds,
                    ref initialized);
            }

            SkinnedMeshRenderer[] skinnedRenderers =
                root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinnedRenderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = skinnedRenderers[i];
                if (renderer == null || renderer.sharedMesh == null)
                {
                    continue;
                }

                Matrix4x4 toRoot =
                    worldToRoot * renderer.transform.localToWorldMatrix;
                EncapsulateTransformedBounds(
                    renderer.localBounds,
                    toRoot,
                    ref bounds,
                    ref initialized);
            }

            return initialized;
        }

        private static void EncapsulateTransformedBounds(
            Bounds source,
            Matrix4x4 matrix,
            ref Bounds destination,
            ref bool initialized)
        {
            Vector3 min = source.min;
            Vector3 max = source.max;
            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 corner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 point = matrix.MultiplyPoint3x4(corner);
                        if (!initialized)
                        {
                            destination = new Bounds(point, Vector3.zero);
                            initialized = true;
                        }
                        else
                        {
                            destination.Encapsulate(point);
                        }
                    }
                }
            }
        }
        private static void ValidateCrocodileAmbushChunk(RunChunkDirector runDirector, List<string> errors)
        {
            RunChunkDefinition definition = AssetDatabase.LoadAssetAtPath<RunChunkDefinition>(CrocodileAmbushChunkPath);
            if (definition == null)
            {
                return;
            }

            RunChunkTag requiredTags = RunChunkTag.Hazard | RunChunkTag.NoVine | RunChunkTag.Predator;
            if ((definition.Tags & requiredTags) != requiredTags || !definition.AllowInMainPool)
            {
                errors.Add("Crocodile ambush chunk must be a main-pool hazard, predator, and vine-free beat.");
            }

            if (definition.MinimumReactionDistance < 5f || definition.EntryFuelRange.x < 17f)
            {
                errors.Add("Crocodile ambush chunk must guarantee readable lead distance and at least one boost of entry fuel.");
            }

            int pickupCount = 0;
            int predatorCount = 0;
            RunChunkSpawn[] spawns = definition.Spawns;
            for (int i = 0; i < spawns.Length; i++)
            {
                RunChunkSpawn spawn = spawns[i];
                if (spawn == null || spawn.Prefab == null)
                {
                    continue;
                }

                if (spawn.Kind == RunChunkSpawnKind.Pickup)
                {
                    pickupCount++;
                }

                if (spawn.Prefab.name == "Hazard_CrocodileAmbush")
                {
                    predatorCount++;
                }
            }

            if (predatorCount != 1 || pickupCount < 2)
            {
                errors.Add("Crocodile ambush chunk must contain one crocodile plus approach and recovery food.");
            }

            RunChunkDefinition[] opening = runDirector.OpeningSequence;
            for (int i = 0; i < opening.Length; i++)
            {
                if (opening[i] != null && (opening[i].Tags & RunChunkTag.Predator) != 0)
                {
                    errors.Add("The controlled opening sequence must not contain a crocodile predator beat.");
                    break;
                }
            }
        }

        private static void ValidateCrocodileAmbushPrefab(List<string> errors)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrocodileAmbushPrefabPath);
            if (prefab == null)
            {
                return;
            }

            CrocodileAmbushController controller = prefab.GetComponent<CrocodileAmbushController>();
            if (controller == null || !controller.IsConfigured)
            {
                errors.Add("Crocodile ambush prefab is missing its complete warning, motion, bite, or animation wiring.");
                return;
            }

            Animator animator = controller.Animator;
            if (animator.runtimeAnimatorController == null || animator.applyRootMotion || animator.updateMode != AnimatorUpdateMode.Normal)
            {
                errors.Add("Crocodile ambush animator must use the generated controller, normal gameplay time, and no root motion.");
            }

            SkinnedMeshRenderer[] skinnedRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skinnedRenderers.Length != 1 || skinnedRenderers[0].sharedMaterials.Length != 1)
            {
                errors.Add("Crocodile ambush must use one atlas-backed skinned renderer.");
            }

            Transform crocodileModel = FindChild(prefab.transform, "Visual_Crocodile_3D");
            if (crocodileModel == null || Quaternion.Angle(crocodileModel.localRotation, Quaternion.identity) > 1f)
            {
                errors.Add("Crocodile ambush must keep the FBX native facing axis so the head attacks the incoming gorilla.");
            }

            Collider2D biteCollider = controller.BiteCollider;
            if (biteCollider == null || !biteCollider.isTrigger || biteCollider.enabled)
            {
                errors.Add("Crocodile bite collider must be a disabled trigger outside its authored bite window.");
            }

            if (controller.WarningRippleCount != 3 || controller.WarningDuration < 0.65f || controller.WarningDuration > 1.05f)
            {
                errors.Add("Crocodile warning must keep three readable ripples and a 0.65 to 1.05 second telegraph.");
            }

            if (controller.BiteWindowStart < 0.12f || controller.BiteWindowEnd <= controller.BiteWindowStart || controller.BiteWindowEnd > 0.7f)
            {
                errors.Add("Crocodile bite window must stay narrow, ordered, and inside the readable middle of the lunge.");
            }

            const float expectedForwardSpeed = 4.85f;
            float remainingLeadAtBite = controller.ActivationDistance -
                expectedForwardSpeed * (controller.WarningDuration + controller.LungeDuration * controller.BiteWindowStart);
            if (remainingLeadAtBite < controller.MinimumLeadDistance)
            {
                errors.Add("Crocodile telegraph timing no longer leaves the promised minimum lead distance at bite-window start.");
            }

            if (controller.MinimumFuel < 17f)
            {
                errors.Add("Crocodile ambush must require at least one production boost of fuel.");
            }

            ParticleSystem[] particles = prefab.GetComponentsInChildren<ParticleSystem>(true);
            int particleBudget = 0;
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.MainModule main = particles[i].main;
                particleBudget += main.maxParticles;
                if (main.loop)
                {
                    errors.Add("Crocodile warning and launch particles must be finite.");
                    break;
                }
            }

            if (particles.Length != 2 || particleBudget > 48)
            {
                errors.Add("Crocodile ambush must keep two finite particle systems within its 48-particle mobile budget.");
            }
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
