using System;
using FirstBloom.ArcadeFramework.Accessibility;
using FirstBloom.ArcadeFramework.Audio;
using FirstBloom.ArcadeFramework.Core;
using FirstBloom.ArcadeFramework.Save;
using FirstBloom.ArcadeFramework.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstBloom.Games.GassyGorilla.EditorTools
{
    [InitializeOnLoad]
    public static class GassyCommercialFoundationPlayModeVerification
    {
        private const string MainMenuScenePath =
            "Assets/_FirstBloom/Games/GassyGorilla/Scenes/MainMenu.unity";
        private const string GameScenePath =
            "Assets/_FirstBloom/Games/GassyGorilla/Scenes/Game.unity";
        private const string ActiveKey = "FirstBloom.GassyCommercialVerification.Active";
        private const string StageKey = "FirstBloom.GassyCommercialVerification.Stage";
        private const string ExitCodeKey = "FirstBloom.GassyCommercialVerification.ExitCode";
        private const string OriginalReducedMotionKey =
            "FirstBloom.GassyCommercialVerification.OriginalReducedMotion";
        private const string OriginalHapticsKey =
            "FirstBloom.GassyCommercialVerification.OriginalHaptics";
        private const string OriginalSubtitlesKey =
            "FirstBloom.GassyCommercialVerification.OriginalSubtitles";
        private const string AchievementScopeKey =
            "FirstBloom.GassyCommercialVerification.AchievementScope";

        private static double stageStartedAt;
        private static bool exitRequested;

        static GassyCommercialFoundationPlayModeVerification()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.delayCall += ExitBatchModeIfReady;
        }

        [MenuItem("First Bloom/Gassy Gorilla/Verify Commercial Foundation In Play Mode")]
        public static void Start()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Commercial foundation verification requires Unity to be out of Play Mode.");
            }

            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetInt(ExitCodeKey, -1);
            SessionState.SetBool(
                OriginalReducedMotionKey,
                ArcadeAccessibilitySettings.ReducedMotion);
            SessionState.SetBool(
                OriginalHapticsKey,
                ArcadeAccessibilitySettings.HapticsEnabled);
            SessionState.SetBool(
                OriginalSubtitlesKey,
                ArcadeAccessibilitySettings.SubtitlesEnabled);
            SessionState.SetString(AchievementScopeKey, string.Empty);
            stageStartedAt = EditorApplication.timeSinceStartup;
            exitRequested = false;

            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            Debug.Log("Gassy Gorilla commercial foundation Play Mode verification started.");
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(ActiveKey, false) ||
                !EditorApplication.isPlaying ||
                EditorApplication.isCompiling)
            {
                return;
            }

            try
            {
                switch (SessionState.GetInt(StageKey, 0))
                {
                    case 0:
                        VerifyMainMenuAndOpenBadges();
                        break;
                    case 1:
                        VerifyBadgesAndOpenSettings();
                        break;
                    case 2:
                        VerifyMenuSettingsAndLoadGame();
                        break;
                    case 3:
                        WaitForRunAndPause();
                        break;
                    case 4:
                        VerifyPauseAndOpenSettings();
                        break;
                    case 5:
                        VerifyPausedSettingsAndReturn();
                        break;
                    case 6:
                        VerifyPauseReturnAndResume();
                        break;
                    case 7:
                        VerifyResumeAndPersistence();
                        break;
                }
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void VerifyMainMenuAndOpenBadges()
        {
            if (!HasWaited(0.45d))
            {
                return;
            }

            MainMenuController menu = FindSceneComponent<MainMenuController>();
            ArcadeSettingsMenu settings = FindSceneComponent<ArcadeSettingsMenu>();
            Require(menu != null, "Main menu controller did not initialize.");
            Require(menu.IsExpeditionUiConfigured, "Main menu Expedition UI is incomplete.");
            Require(menu.IsBadgeUiConfigured, "Main menu Jungle Badge UI is incomplete.");
            Require(
                settings != null && settings.HasAccessibilityControls,
                "Main menu accessibility settings are incomplete.");

            menu.OpenBadges();
            AdvanceTo(1);
        }

        private static void VerifyBadgesAndOpenSettings()
        {
            if (!HasWaited(0.35d))
            {
                return;
            }

            CanvasGroupPanel badgePanel =
                FindSceneObject("UI_JungleBadgesPanel").GetComponent<CanvasGroupPanel>();
            Require(
                badgePanel != null && badgePanel.IsVisible,
                "Jungle Badge panel did not open.");
            Require(GassyBadgeService.Count == 8, "Jungle Badge launch catalog is not eight badges.");

            MainMenuController menu = FindSceneComponent<MainMenuController>();
            ArcadeSettingsMenu settings = FindSceneComponent<ArcadeSettingsMenu>();
            menu.CloseBadges();
            settings.Open();
            AdvanceTo(2);
        }

        private static void VerifyMenuSettingsAndLoadGame()
        {
            if (!HasWaited(0.35d))
            {
                return;
            }

            ArcadeSettingsMenu settings = FindSceneComponent<ArcadeSettingsMenu>();
            Require(settings != null && settings.IsVisible, "Main menu settings did not open.");

            ArcadeAccessibilitySettings.SetReducedMotion(true);
            ArcadeAccessibilitySettings.SetHapticsEnabled(false);
            ArcadeAccessibilitySettings.SetSubtitlesEnabled(false);
            ArcadeAccessibilitySettings.Reload();
            Require(
                ArcadeAccessibilitySettings.ReducedMotion,
                "Reduced Motion did not persist.");
            Require(
                !ArcadeAccessibilitySettings.HapticsEnabled,
                "Haptics preference did not persist.");
            Require(
                !ArcadeAccessibilitySettings.SubtitlesEnabled,
                "Subtitles preference did not persist.");

            settings.Open();
            ArcadeToggleVisual[] toggleVisuals =
                UnityEngine.Object.FindObjectsByType<ArcadeToggleVisual>(
                    FindObjectsInactive.Include);
            Require(toggleVisuals.Length == 3, "Menu accessibility switches are missing.");
            for (int i = 0; i < toggleVisuals.Length; i++)
            {
                Require(
                    toggleVisuals[i].IsSynchronized,
                    "A menu accessibility switch does not match its persisted value.");
            }

            settings.Close();
            ArcadeRunSession.SelectEndless();
            SceneManager.LoadScene(GameScenePath);
            AdvanceTo(3);
        }

        private static void WaitForRunAndPause()
        {
            GassyGorillaGameManager manager = GassyGorillaGameManager.Instance;
            if (manager == null || !manager.IsRunActive)
            {
                RequireWithin(15d, "Endless Run did not enter its running state.");
                return;
            }

            Require(manager.IsPauseConfigured, "Game pause flow is incomplete.");
            Require(
                FindSceneComponent<GassyBadgeTracker>() != null &&
                FindSceneComponent<GassyBadgeTracker>().IsConfigured,
                "Jungle Badge tracker did not initialize.");
            Require(
                FindSceneComponent<GassyFeedbackDirector>() != null &&
                FindSceneComponent<GassyFeedbackDirector>().IsConfigured,
                "Haptic feedback director did not initialize.");

            manager.PauseRun();
            AdvanceTo(4);
        }

        private static void VerifyPauseAndOpenSettings()
        {
            if (!HasWaited(0.35d))
            {
                return;
            }

            GassyGorillaGameManager manager = GassyGorillaGameManager.Instance;
            ArcadePausePanel pausePanel = FindSceneComponent<ArcadePausePanel>();
            Require(manager != null && manager.IsPaused, "Pause did not enter the paused state.");
            Require(Mathf.Approximately(Time.timeScale, 0f), "Pause did not stop simulation time.");
            Require(
                pausePanel != null && pausePanel.IsVisible,
                "Pause panel did not open using unscaled time.");
            Require(
                ArcadeAudioManager.Instance != null &&
                ArcadeAudioManager.Instance.PauseMixActive,
                "Pause mix did not engage.");

            manager.OpenSettingsFromPause();
            AdvanceTo(5);
        }

        private static void VerifyPausedSettingsAndReturn()
        {
            if (!HasWaited(0.35d))
            {
                return;
            }

            ArcadeSettingsMenu settings = FindSceneComponent<ArcadeSettingsMenu>();
            ArcadePausePanel pausePanel = FindSceneComponent<ArcadePausePanel>();
            Require(settings != null && settings.IsVisible, "Paused settings did not open.");
            Require(
                pausePanel != null && !pausePanel.IsVisible,
                "Pause panel remained active behind settings.");

            GassyGorillaGameManager.Instance.CloseSettingsToPause();
            AdvanceTo(6);
        }

        private static void VerifyPauseReturnAndResume()
        {
            if (!HasWaited(0.35d))
            {
                return;
            }

            ArcadeSettingsMenu settings = FindSceneComponent<ArcadeSettingsMenu>();
            ArcadePausePanel pausePanel = FindSceneComponent<ArcadePausePanel>();
            Require(
                settings != null && !settings.IsVisible,
                "Paused settings did not close.");
            Require(
                pausePanel != null && pausePanel.IsVisible,
                "Settings did not return to the pause panel.");

            GassyGorillaGameManager.Instance.ResumeRun();
            AdvanceTo(7);
        }

        private static void VerifyResumeAndPersistence()
        {
            if (!HasWaited(0.35d))
            {
                return;
            }

            GassyGorillaGameManager manager = GassyGorillaGameManager.Instance;
            Require(manager != null && manager.IsRunActive, "Resume did not restore the run.");
            Require(Mathf.Approximately(Time.timeScale, 1f), "Resume did not restore simulation time.");
            Require(
                ArcadeAudioManager.Instance != null &&
                !ArcadeAudioManager.Instance.PauseMixActive,
                "Resume did not clear the pause mix.");

            string scope = "CommercialVerification_" + DateTime.UtcNow.Ticks;
            const string achievementId = "persistence";
            SessionState.SetString(AchievementScopeKey, scope);
            Require(
                ArcadeAchievementStore.GetProgress(scope, achievementId) == 0,
                "New achievement progress did not begin at zero.");
            Require(
                ArcadeAchievementStore.SetProgressIfGreater(scope, achievementId, 5),
                "Achievement progress did not advance.");
            Require(
                !ArcadeAchievementStore.SetProgressIfGreater(scope, achievementId, 3),
                "Achievement progress regressed.");
            Require(
                ArcadeAchievementStore.GetProgress(scope, achievementId) == 5,
                "Achievement progress was not monotonic.");
            Require(
                ArcadeAchievementStore.TryUnlock(scope, achievementId) &&
                ArcadeAchievementStore.IsUnlocked(scope, achievementId),
                "Achievement unlock did not persist.");
            Require(
                !ArcadeAchievementStore.TryUnlock(scope, achievementId),
                "Achievement unlock was not idempotent.");

            Debug.Log(
                "Gassy Gorilla commercial foundation Play Mode verification passed: " +
                "badges, motion, haptics, subtitle persistence, pause/settings return, audio pause mix, " +
                "resume, and achievement persistence are working.");
            Finish(0);
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                stageStartedAt = EditorApplication.timeSinceStartup;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                ExitBatchModeIfReady();
            }
        }

        private static void Fail(Exception exception)
        {
            Debug.LogError(
                "Gassy Gorilla commercial foundation Play Mode verification failed: " +
                exception);
            Finish(1);
        }

        private static void Finish(int exitCode)
        {
            RestorePreferences();
            DeleteVerificationAchievement();
            Time.timeScale = 1f;
            SessionState.SetBool(ActiveKey, false);
            SessionState.SetInt(ExitCodeKey, exitCode);
            exitRequested = true;

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
            else
            {
                ExitBatchModeIfReady();
            }
        }

        private static void RestorePreferences()
        {
            ArcadeAccessibilitySettings.SetReducedMotion(
                SessionState.GetBool(OriginalReducedMotionKey, false));
            ArcadeAccessibilitySettings.SetHapticsEnabled(
                SessionState.GetBool(OriginalHapticsKey, true));
            ArcadeAccessibilitySettings.SetSubtitlesEnabled(
                SessionState.GetBool(OriginalSubtitlesKey, true));
            ArcadeAccessibilitySettings.Reload();
        }

        private static void DeleteVerificationAchievement()
        {
            string scope = SessionState.GetString(AchievementScopeKey, string.Empty);
            if (string.IsNullOrWhiteSpace(scope))
            {
                return;
            }

            const string prefix = "FirstBloom_Achievement_";
            const string achievementId = "persistence";
            PlayerPrefs.DeleteKey(prefix + scope + "_" + achievementId + "_Progress");
            PlayerPrefs.DeleteKey(prefix + scope + "_" + achievementId + "_Unlocked");
            PlayerPrefs.Save();
            SessionState.SetString(AchievementScopeKey, string.Empty);
        }

        private static void ExitBatchModeIfReady()
        {
            int exitCode = SessionState.GetInt(ExitCodeKey, -1);
            if (exitCode < 0 ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isPlaying)
            {
                return;
            }

            SessionState.SetInt(ExitCodeKey, -1);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
            else if (exitRequested || exitCode == 0)
            {
                Debug.Log(
                    exitCode == 0
                        ? "Commercial foundation verification completed successfully."
                        : "Commercial foundation verification failed.");
            }
        }

        private static void AdvanceTo(int stage)
        {
            SessionState.SetInt(StageKey, stage);
            stageStartedAt = EditorApplication.timeSinceStartup;
        }

        private static bool HasWaited(double seconds)
        {
            return EditorApplication.timeSinceStartup - stageStartedAt >= seconds;
        }

        private static void RequireWithin(double seconds, string message)
        {
            if (EditorApplication.timeSinceStartup - stageStartedAt > seconds)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static T FindSceneComponent<T>() where T : Component
        {
            T[] components =
                UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
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

            throw new InvalidOperationException(
                "Scene object is missing during Play Mode verification: " + objectName);
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
    }
}
