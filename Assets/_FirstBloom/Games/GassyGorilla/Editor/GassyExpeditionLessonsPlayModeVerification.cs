using System;
using FirstBloom.ArcadeFramework.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FirstBloom.Games.GassyGorilla.EditorTools
{
    [InitializeOnLoad]
    public static class GassyExpeditionLessonsPlayModeVerification
    {
        private const string GameScenePath =
            "Assets/_FirstBloom/Games/GassyGorilla/Scenes/Game.unity";
        private const string CatalogPath =
            "Assets/_FirstBloom/Games/GassyGorilla/ScriptableObjects/GG_ExpeditionCatalog.asset";
        private const string UnlockKey = "GassyGorilla_Expedition_UnlockedIndex";
        private const string StarsPrefix = "GassyGorilla_Expedition_Stars_";
        private const string FailurePrefix =
            "GassyGorilla_Expedition_Failures_";
        private const string VoicePrefix =
            "GassyGorilla_Expedition_Voice_";
        private static readonly string[] VoiceMoments =
            { "opening", "lesson", "success", "hint" };
        private const string StatePrefix = "FirstBloom.GassyExpeditionLessonsVerification.";
        private const string ActiveKey = StatePrefix + "Active";
        private const string IndexKey = StatePrefix + "Index";
        private const string PhaseKey = StatePrefix + "Phase";
        private const string BootstrappedKey = StatePrefix + "Bootstrapped";
        private const string ExitCodeKey = StatePrefix + "ExitCode";

        private static double phaseStartedAt;

        static GassyExpeditionLessonsPlayModeVerification()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.delayCall += ExitBatchModeIfReady;
        }

        [MenuItem("First Bloom/Gassy Gorilla/Verify All Expeditions In Play Mode")]
        public static void Start()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Expedition verification requires Unity to be out of Play Mode.");
            }

            GassyExpeditionCatalog catalog = LoadCatalog();
            Require(
                catalog.Count == GassyExpeditionCatalog.VersionOneExpeditionCount,
                "The runtime verifier requires the complete fifteen-Expedition catalog.");
            Require(
                catalog.ChapterCount == 3,
                "The runtime verifier requires all three Expedition chapters.");

            BackupProgress(catalog);
            GassyExpeditionProgressStore.ResetAll(catalog);
            VerifyLegacyChapterUnlockMigration(catalog);
            GassyExpeditionProgressStore.ResetAll(catalog);
            VerifyAdaptiveFailureHints(catalog);
            GassyExpeditionProgressStore.ResetAll(catalog);
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(IndexKey, 0);
            SessionState.SetInt(PhaseKey, 0);
            SessionState.SetBool(BootstrappedKey, false);
            SessionState.SetInt(ExitCodeKey, -1);
            phaseStartedAt = EditorApplication.timeSinceStartup;

            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            Debug.Log("Gassy Gorilla fifteen-Expedition Play Mode verification started.");
            EditorApplication.isPlaying = true;
        }

        private static void VerifyLegacyChapterUnlockMigration(GassyExpeditionCatalog catalog)
        {
            int legacyFinalIndex = GassyExpeditionCatalog.LevelsPerChapter - 1;
            GassyExpeditionDefinition legacyFinal = catalog.GetByIndex(legacyFinalIndex);
            Require(legacyFinal != null, "Legacy campaign finale is missing.");

            PlayerPrefs.SetInt(UnlockKey, legacyFinalIndex);
            PlayerPrefs.SetInt(StarsPrefix + legacyFinal.ExpeditionId, 3);
            PlayerPrefs.Save();

            int migratedIndex = GassyExpeditionProgressStore.ReconcileUnlocks(catalog);
            Require(
                migratedIndex == GassyExpeditionCatalog.LevelsPerChapter,
                "A completed five-Expedition save did not unlock Dessert Rescue.");
            Require(
                GassyExpeditionProgressStore.IsUnlocked(GassyExpeditionCatalog.LevelsPerChapter),
                "Dessert Rescue remained locked after legacy save migration.");

            GassyExpeditionProgressStore.ResetAll(catalog);
            int secondChapterFinalIndex =
                GassyExpeditionCatalog.LevelsPerChapter * 2 - 1;
            GassyExpeditionDefinition secondChapterFinal =
                catalog.GetByIndex(secondChapterFinalIndex);
            Require(
                secondChapterFinal != null,
                "Existing ten-Expedition campaign finale is missing.");
            PlayerPrefs.SetInt(UnlockKey, secondChapterFinalIndex);
            PlayerPrefs.SetInt(
                StarsPrefix + secondChapterFinal.ExpeditionId,
                3);
            PlayerPrefs.Save();

            int moonlitIndex =
                GassyExpeditionProgressStore.ReconcileUnlocks(catalog);
            Require(
                moonlitIndex == secondChapterFinalIndex + 1,
                "A completed ten-Expedition save did not unlock Moonlit Ruins.");
            Require(
                GassyExpeditionProgressStore.IsUnlocked(moonlitIndex),
                "Bounce By Moonlight remained locked after save migration.");
        }

        private static void VerifyAdaptiveFailureHints(
            GassyExpeditionCatalog catalog)
        {
            GassyExpeditionDefinition definition = catalog.GetByIndex(0);
            Require(
                definition != null &&
                !string.IsNullOrWhiteSpace(definition.FailureHintText),
                "Adaptive hint verification requires authored hint copy.");

            GameObject root =
                new GameObject("QA_ExpeditionNarrationDirector");
            try
            {
                GassyExpeditionNarrationDirector director =
                    root.AddComponent<GassyExpeditionNarrationDirector>();
                director.Configure(definition);
                Require(
                    string.IsNullOrEmpty(
                        director.RecordFailureAndGetAdaptiveHint()),
                    "Adaptive help appeared before the second failure.");
                Require(
                    director.RecordFailureAndGetAdaptiveHint() ==
                        definition.FailureHintText,
                    "Adaptive help did not appear on the second failure.");
                director.ClearFailureCount();
                Require(
                    GassyExpeditionProgressStore.GetFailureCount(
                        definition) == 0,
                    "Adaptive failure count did not clear.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
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
                int expeditionIndex = SessionState.GetInt(IndexKey, 0);
                int phase = SessionState.GetInt(PhaseKey, 0);
                GassyExpeditionCatalog catalog = LoadCatalog();
                GassyExpeditionDefinition expected = catalog.GetByIndex(expeditionIndex);
                GassyGorillaGameManager manager = GassyGorillaGameManager.Instance;

                if (phase == 0)
                {
                    WaitForStory(manager, expected);
                }
                else if (phase == 1)
                {
                    CompleteLesson(manager, expected);
                }
                else
                {
                    VerifyCompletionAndContinue(manager, catalog, expected, expeditionIndex);
                }
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void WaitForStory(
            GassyGorillaGameManager manager,
            GassyExpeditionDefinition expected)
        {
            if ((manager == null || manager.CurrentExpedition != expected) &&
                !SessionState.GetBool(BootstrappedKey, false))
            {
                Require(
                    ArcadeRunSession.SelectFinite(expected.ExpeditionId),
                    "Play Mode could not select the first Expedition.");
                SessionState.SetBool(BootstrappedKey, true);
                phaseStartedAt = EditorApplication.timeSinceStartup;
                SceneManager.LoadScene(GameScenePath);
                return;
            }

            if (manager == null ||
                manager.CurrentState != ArcadeGameState.Ready ||
                manager.CurrentExpedition != expected)
            {
                RequireWithin(15d, "Expedition story scene did not become ready.");
                return;
            }

            Require(manager.IsExpeditionConfigured, "Expedition runtime UI is incomplete.");
            GassyExpeditionThemeDirector themeDirector =
                UnityEngine.Object
                    .FindAnyObjectByType<GassyExpeditionThemeDirector>();
            Require(
                themeDirector != null &&
                themeDirector.IsConfigured &&
                themeDirector.ActiveTheme == expected.Theme,
                expected.DisplayTitle +
                " did not apply its authored Expedition theme.");
            Button replayButton = FindButton("ReplayVoiceButton");
            bool hasOpeningVoice =
                expected.OpeningVoice != null &&
                expected.OpeningVoice.HasClip;
            Require(
                replayButton != null &&
                replayButton.gameObject.activeSelf == hasOpeningVoice,
                expected.DisplayTitle +
                " did not present the correct replay-voice state.");
            Require(
                !hasOpeningVoice ||
                GassyExpeditionProgressStore.HasHeardVoice(
                    expected,
                    "opening"),
                expected.DisplayTitle +
                " did not record its opening voice playback.");
            manager.BeginExpeditionFromStory();
            AdvancePhase(1);
        }

        private static void CompleteLesson(
            GassyGorillaGameManager manager,
            GassyExpeditionDefinition expected)
        {
            if (manager == null || !manager.IsRunActive)
            {
                RequireWithin(15d, "Expedition did not enter its running state.");
                return;
            }

            GassyExpeditionRunController controller =
                UnityEngine.Object.FindAnyObjectByType<GassyExpeditionRunController>();
            GorillaController player =
                UnityEngine.Object.FindAnyObjectByType<GorillaController>();
            Require(controller != null && controller.IsConfigured, "Expedition controller is incomplete.");
            Require(controller.Definition == expected, "Expedition controller loaded the wrong definition.");
            Require(player != null, "Expedition player is missing.");

            ExerciseObjective(controller, player, expected);
            Require(
                controller.IsObjectiveSatisfiedAtFinish(),
                expected.DisplayTitle + " objective did not complete through its runtime contract.");

            manager.ReachExpeditionFinish(controller.FinishLine);
            AdvancePhase(2);
        }

        private static void ExerciseObjective(
            GassyExpeditionRunController controller,
            GorillaController player,
            GassyExpeditionDefinition definition)
        {
            if (definition.ObjectiveType == GassyExpeditionObjectiveType.CompleteInteraction)
            {
                for (int i = 0; i < definition.TargetCount; i++)
                {
                    ExerciseInteraction(player, definition.TargetInteraction);
                }

                return;
            }

            if (definition.ObjectiveType == GassyExpeditionObjectiveType.CompleteInteractionSet)
            {
                ExerciseInteraction(player, GassyInteractionType.ThornDodge);
                ExerciseInteraction(player, GassyInteractionType.GeyserDodge);
                ExerciseInteraction(player, GassyInteractionType.SapEscape);
                ExerciseInteraction(player, GassyInteractionType.UpdraftRide);
                ExerciseInteraction(player, GassyInteractionType.BounceBloom);
                return;
            }

            controller.CompleteObjectiveForQa();
        }

        private static void ExerciseInteraction(
            GorillaController player,
            GassyInteractionType interactionType)
        {
            if (interactionType == GassyInteractionType.SapEscape)
            {
                float fuelBefore = player.CurrentFuel;
                Require(player.TryEnterStickySap(0.52f), "Player could not enter the sticky sap state.");
                Require(player.IsStickySap, "Sticky sap state did not become active.");
                Require(player.TryEscapeStickySap(), "Player could not perform the sap breakout.");
                Require(!player.IsStickySap, "Sticky sap state persisted after breakout.");
                Require(
                    Mathf.Abs(player.CurrentFuel - fuelBefore) < 0.01f,
                    "Sticky sap breakout consumed fart fuel.");
                return;
            }

            if (interactionType == GassyInteractionType.UpdraftRide)
            {
                Require(player.ApplyUpdraft(5.2f), "Player rejected a valid canopy updraft.");
            }
            else if (interactionType == GassyInteractionType.BounceBloom)
            {
                float fuelBefore = player.CurrentFuel;
                Require(
                    player.ApplyBounceBloom(6.1f, 0.9f),
                    "Player rejected a valid bounce bloom launch.");
                Require(
                    player.GetComponent<Rigidbody2D>().linearVelocity.y >= 6f,
                    "Bounce bloom did not apply its authored vertical launch.");
                Require(
                    Mathf.Abs(player.CurrentFuel - fuelBefore) < 0.01f,
                    "Bounce bloom consumed fart fuel.");
            }

            GassyRunEvents.RaiseInteractionCompleted(interactionType);
        }

        private static void VerifyCompletionAndContinue(
            GassyGorillaGameManager manager,
            GassyExpeditionCatalog catalog,
            GassyExpeditionDefinition expected,
            int expeditionIndex)
        {
            if (manager == null || manager.CurrentState != ArcadeGameState.Completed)
            {
                RequireWithin(8d, "Expedition completion state did not arrive.");
                return;
            }

            Require(
                GassyExpeditionProgressStore.GetBestStars(expected.ExpeditionId) >= 1,
                expected.DisplayTitle + " did not persist a completion star.");
            int expectedUnlocked = Mathf.Min(expeditionIndex + 1, catalog.Count - 1);
            Require(
                GassyExpeditionProgressStore.GetHighestUnlockedIndex() == expectedUnlocked,
                expected.DisplayTitle + " did not unlock the correct next Expedition.");

            if (expeditionIndex >= catalog.Count - 1)
            {
                for (int i = 0; i < catalog.Count; i++)
                {
                    GassyExpeditionDefinition definition = catalog.GetByIndex(i);
                    Require(
                        definition != null &&
                        GassyExpeditionProgressStore.GetBestStars(definition.ExpeditionId) >= 1,
                        "The full campaign did not persist every Expedition completion.");
                }

                Debug.Log(
                    "Gassy Gorilla fifteen-Expedition Play Mode verification passed: " +
                    "all objectives, interaction events, sap fuel safety, bounce launch, stars, and unlocks work in sequence.");
                Finish(0);
                return;
            }

            int nextIndex = expeditionIndex + 1;
            SessionState.SetInt(IndexKey, nextIndex);
            AdvancePhase(0);
            manager.PlayNextExpedition();
        }

        private static GassyExpeditionCatalog LoadCatalog()
        {
            GassyExpeditionCatalog catalog =
                AssetDatabase.LoadAssetAtPath<GassyExpeditionCatalog>(CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException("Generated Expedition catalog is missing.");
            }

            return catalog;
        }

        private static Button FindButton(string objectName)
        {
            Button[] buttons =
                UnityEngine.Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null &&
                    buttons[i].gameObject.name == objectName)
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private static void BackupProgress(GassyExpeditionCatalog catalog)
        {
            BackupInt(UnlockKey, "Unlock");
            for (int i = 0; i < catalog.Count; i++)
            {
                GassyExpeditionDefinition definition = catalog.GetByIndex(i);
                if (definition != null)
                {
                    BackupInt(StarsPrefix + definition.ExpeditionId, "Stars." + i);
                    BackupInt(
                        FailurePrefix + definition.ExpeditionId,
                        "Failures." + i);
                    for (int momentIndex = 0;
                        momentIndex < VoiceMoments.Length;
                        momentIndex++)
                    {
                        BackupInt(
                            VoicePrefix + definition.ExpeditionId + "_" +
                                VoiceMoments[momentIndex],
                            "Voice." + i + "." + momentIndex);
                    }
                }
            }
        }

        private static void RestoreProgress()
        {
            GassyExpeditionCatalog catalog = LoadCatalog();
            RestoreInt(UnlockKey, "Unlock");
            for (int i = 0; i < catalog.Count; i++)
            {
                GassyExpeditionDefinition definition = catalog.GetByIndex(i);
                if (definition != null)
                {
                    RestoreInt(StarsPrefix + definition.ExpeditionId, "Stars." + i);
                    RestoreInt(
                        FailurePrefix + definition.ExpeditionId,
                        "Failures." + i);
                    for (int momentIndex = 0;
                        momentIndex < VoiceMoments.Length;
                        momentIndex++)
                    {
                        RestoreInt(
                            VoicePrefix + definition.ExpeditionId + "_" +
                                VoiceMoments[momentIndex],
                            "Voice." + i + "." + momentIndex);
                    }
                }
            }

            PlayerPrefs.Save();
        }

        private static void BackupInt(string playerPrefsKey, string backupKey)
        {
            bool exists = PlayerPrefs.HasKey(playerPrefsKey);
            SessionState.SetBool(StatePrefix + backupKey + ".Exists", exists);
            SessionState.SetInt(
                StatePrefix + backupKey + ".Value",
                exists ? PlayerPrefs.GetInt(playerPrefsKey) : 0);
        }

        private static void RestoreInt(string playerPrefsKey, string backupKey)
        {
            if (SessionState.GetBool(StatePrefix + backupKey + ".Exists", false))
            {
                PlayerPrefs.SetInt(
                    playerPrefsKey,
                    SessionState.GetInt(StatePrefix + backupKey + ".Value", 0));
            }
            else
            {
                PlayerPrefs.DeleteKey(playerPrefsKey);
            }
        }

        private static void AdvancePhase(int phase)
        {
            SessionState.SetInt(PhaseKey, phase);
            phaseStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void Fail(Exception exception)
        {
            Debug.LogError(
                "Gassy Gorilla fifteen-Expedition Play Mode verification failed: " +
                exception);
            Finish(1);
        }

        private static void Finish(int exitCode)
        {
            RestoreProgress();
            ArcadeRunSession.SelectEndless();
            Time.timeScale = 1f;
            SessionState.SetBool(ActiveKey, false);
            SessionState.SetBool(BootstrappedKey, false);
            SessionState.SetInt(ExitCodeKey, exitCode);

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
            else
            {
                ExitBatchModeIfReady();
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                phaseStartedAt = EditorApplication.timeSinceStartup;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                ExitBatchModeIfReady();
            }
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
        }

        private static void RequireWithin(double seconds, string message)
        {
            if (EditorApplication.timeSinceStartup - phaseStartedAt > seconds)
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
    }
}
