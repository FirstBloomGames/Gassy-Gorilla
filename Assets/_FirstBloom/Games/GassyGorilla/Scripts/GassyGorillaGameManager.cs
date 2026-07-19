using System;
using System.Collections;
using FirstBloom.ArcadeFramework.Accessibility;
using FirstBloom.ArcadeFramework.Audio;
using FirstBloom.ArcadeFramework.Camera;
using FirstBloom.ArcadeFramework.Core;
using FirstBloom.ArcadeFramework.Input;
using FirstBloom.ArcadeFramework.Save;
using FirstBloom.ArcadeFramework.Spawning;
using FirstBloom.ArcadeFramework.UI;
using FirstBloom.ArcadeFramework.VFX;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FirstBloom.Games.GassyGorilla
{
    [DefaultExecutionOrder(-100)]
    public class GassyGorillaGameManager : ArcadeGameStateController
    {
        public const string BestDistanceKey = "GassyGorilla_BestDistance";

        [Header("Scene")]
        [SerializeField] private string gameSceneName = "Game";
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Run References")]
        [SerializeField] private GorillaController player;
        [SerializeField] private GassyScoreManager scoreManager;
        [SerializeField] private ArcadeSpawner2D[] spawners;
        [SerializeField] private RunChunkDirector runChunkDirector;
        [SerializeField] private SmoothCameraFollow2D cameraFollow;
        [SerializeField] private UnityEngine.Camera sceneCamera;
        [SerializeField] private float deathY = -1.72f;
        [SerializeField] private float gameOverRestY = -1.72f;

        [Header("Lagoon Finish")]
        [SerializeField] private LagoonFinishPresentation lagoonFinishPresentation;
        [SerializeField] private GassyTutorialPromptController tutorialPrompt;
        [SerializeField] private float lagoonResultRevealDelay = 1.02f;
        [SerializeField] private float crocodileAmbushResultRevealDelay = 0.9f;
        [SerializeField] private float hazardResultRevealDelay = 0.08f;

        [Header("Camera Beats")]
        [SerializeField] private bool playCameraIntro = true;
        [SerializeField] private float introDuration = 1.15f;
        [SerializeField] private float introStartZoom = 2.85f;
        [SerializeField] private Vector3 introStartOffset = new Vector3(-0.4f, -0.28f, -10f);
        [SerializeField] private Vector3 introEndOffset = new Vector3(4f, 0.15f, -10f);
        [SerializeField] private float outroDuration = 0.78f;
        [SerializeField] private float outroZoom = 3.25f;
        [SerializeField] private Vector3 outroOffset = new Vector3(1.2f, 0.24f, -10f);

        [Header("UI")]
        [SerializeField] private CanvasGroupPanel gameOverPanel;
        [SerializeField] private Text currentDistanceText;
        [SerializeField] private Text bestDistanceText;
        [SerializeField] private Text hudBestDistanceText;
        [SerializeField] private Text gameOverTitleText;
        [SerializeField] private Text gameOverReasonText;

        [Header("Pause")]
        [SerializeField] private ArcadePausePanel pausePanel;
        [SerializeField] private ArcadeSettingsMenu settingsMenu;

        [Header("Expedition Run")]
        [SerializeField] private GassyExpeditionCatalog expeditionCatalog;
        [SerializeField] private GassyExpeditionRunController expeditionRunController;
        [SerializeField] private CanvasGroupPanel expeditionStoryPanel;
        [SerializeField] private Text expeditionStoryTitleText;
        [SerializeField] private Text expeditionStoryBodyText;
        [SerializeField] private Text expeditionStoryObjectiveText;
        [SerializeField] private Text expeditionStoryLessonText;
        [SerializeField] private CanvasGroupPanel expeditionSuccessPanel;
        [SerializeField] private Text expeditionSuccessTitleText;
        [SerializeField] private Text expeditionSuccessObjectiveText;
        [SerializeField] private Text expeditionSuccessStarsText;
        [SerializeField] private Text expeditionSuccessStoryText;
        [SerializeField] private Button expeditionNextButton;
        [SerializeField] private float expeditionSuccessRevealDelay = 0.72f;

        public static GassyGorillaGameManager Instance { get; private set; }

        public bool IsRunActive { get { return CurrentState == ArcadeGameState.Running; } }
        public bool IsVineQaMode { get; private set; }
        public bool IsExpedition { get { return currentExpedition != null; } }
        public GassyExpeditionDefinition CurrentExpedition { get { return currentExpedition; } }
        public bool IsPaused { get { return CurrentState == ArcadeGameState.Paused; } }
        public bool IsPauseConfigured
        {
            get
            {
                return pausePanel != null &&
                    pausePanel.IsConfigured &&
                    settingsMenu != null &&
                    settingsMenu.HasAccessibilityControls;
            }
        }
        public bool IsExpeditionConfigured
        {
            get
            {
                return expeditionCatalog != null &&
                    expeditionCatalog.Count == GassyExpeditionCatalog.VersionOneExpeditionCount &&
                    expeditionRunController != null && expeditionRunController.IsConfigured &&
                    expeditionStoryPanel != null &&
                    expeditionStoryTitleText != null &&
                    expeditionStoryBodyText != null &&
                    expeditionStoryObjectiveText != null &&
                    expeditionStoryLessonText != null &&
                    expeditionSuccessPanel != null &&
                    expeditionSuccessTitleText != null &&
                    expeditionSuccessObjectiveText != null &&
                    expeditionSuccessStarsText != null &&
                    expeditionSuccessStoryText != null &&
                    expeditionNextButton != null;
            }
        }

        private Coroutine introRoutine;
        private Coroutine outroRoutine;
        private Coroutine expeditionSuccessRoutine;
        private bool crocodileQaMode;
        private float nextCrocodileQaSafetyRefresh;
        private GassyExpeditionDefinition currentExpedition;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            ApplyWebQaRunSelection();
            ResolveRunSelection();
        }

        private void Start()
        {
            if (ArcadeTimeController.Instance != null)
            {
                ArcadeTimeController.Instance.ResetTimeScale();
            }
            else
            {
                Time.timeScale = 1f;
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.SetPauseMixActive(false);
            }

            if (pausePanel != null)
            {
                pausePanel.Hide();
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.Hide();
            }

            if (expeditionStoryPanel != null)
            {
                expeditionStoryPanel.Hide();
            }

            if (expeditionSuccessPanel != null)
            {
                expeditionSuccessPanel.Hide();
            }

            if (tutorialPrompt == null)
            {
                tutorialPrompt = FindAnyObjectByType<GassyTutorialPromptController>();
            }

            if (scoreManager != null)
            {
                scoreManager.ResetScore();
                scoreManager.SetRunning(false);
            }

            if (player != null)
            {
                if (lagoonFinishPresentation == null)
                {
                    lagoonFinishPresentation = player.GetComponent<LagoonFinishPresentation>();
                }

                player.PrepareForIntro();
            }

            SetSpawnersActive(false);
            ConfigureSelectedRun();
            UpdateBestDistanceText();
            SetState(ArcadeGameState.Ready);
            ApplyWebQaConfiguration();

            if (IsExpedition)
            {
                ConfigureExpeditionStoryUi();
                if (expeditionRunController != null)
                {
                    expeditionRunController.SetHudVisible(false);
                }

                if (tutorialPrompt != null)
                {
                    tutorialPrompt.PauseForStory();
                }

                if (expeditionStoryPanel != null)
                {
                    expeditionStoryPanel.Show();
                }

                if (ShouldAutoStartQaExpedition())
                {
                    StartCoroutine(AutoStartExpeditionQaRoutine());
                }

                return;
            }

            if (playCameraIntro && GetSceneCamera() != null && player != null)
            {
                introRoutine = StartCoroutine(CameraIntroRoutine());
            }
            else
            {
                BeginRun();
            }
        }

        private void Update()
        {
            if (OneTouchInput.WasPausePressedThisFrame())
            {
                TogglePause();
            }

            if (crocodileQaMode && Time.unscaledTime >= nextCrocodileQaSafetyRefresh)
            {
                nextCrocodileQaSafetyRefresh = Time.unscaledTime + 0.75f;
                DisableStandardHazardsForQa();
            }

            if (!IsRunActive || player == null)
            {
                return;
            }

            if (player.transform.position.y <= deathY)
            {
                if (crocodileQaMode)
                {
                    player.RecoverForCrocodileQa(deathY + 2.1f);
                }
                else
                {
                    GameOver("Fell into the jungle.");
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                PauseRun();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PauseRun();
            }
        }

        public void TogglePause()
        {
            if (CurrentState == ArcadeGameState.Running)
            {
                PauseRun();
                return;
            }

            if (CurrentState != ArcadeGameState.Paused)
            {
                return;
            }

            if (settingsMenu != null && settingsMenu.IsVisible)
            {
                CloseSettingsToPause();
            }
            else
            {
                ResumeRun();
            }
        }

        public void PauseRun()
        {
            if (CurrentState != ArcadeGameState.Running)
            {
                return;
            }

            SetState(ArcadeGameState.Paused);
            if (scoreManager != null)
            {
                scoreManager.SetRunning(false);
            }

            if (settingsMenu != null)
            {
                settingsMenu.Close();
            }

            if (tutorialPrompt != null)
            {
                tutorialPrompt.PauseForSystemMenu();
            }

            if (pausePanel != null)
            {
                pausePanel.Show();
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.NotifyUserGesture();
                ArcadeAudioManager.Instance.SetPauseMixActive(true);
            }

            ArcadeHaptics.Play(ArcadeHapticType.Light);
            SetPausedTime(true);
        }

        public void ResumeRun()
        {
            if (CurrentState != ArcadeGameState.Paused)
            {
                return;
            }

            if (settingsMenu != null)
            {
                settingsMenu.Close();
            }

            if (pausePanel != null)
            {
                pausePanel.Hide();
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.NotifyUserGesture();
                ArcadeAudioManager.Instance.SetPauseMixActive(false);
            }

            SetPausedTime(false);
            SetState(ArcadeGameState.Running);
            if (scoreManager != null)
            {
                scoreManager.SetRunning(true);
            }

            if (tutorialPrompt != null)
            {
                tutorialPrompt.ResumeAfterSystemMenu();
            }

            ArcadeHaptics.Play(ArcadeHapticType.Light);
        }

        public void OpenSettingsFromPause()
        {
            if (CurrentState != ArcadeGameState.Paused || settingsMenu == null)
            {
                return;
            }

            if (pausePanel != null)
            {
                pausePanel.Hide();
            }

            settingsMenu.Open();
        }

        public void CloseSettingsToPause()
        {
            if (settingsMenu != null)
            {
                settingsMenu.Close();
            }

            if (CurrentState == ArcadeGameState.Paused && pausePanel != null)
            {
                pausePanel.Show();
            }
        }

        public void ReachExpeditionFinish(ExpeditionFinishLine finishLine)
        {
            if (!IsExpedition || !IsRunActive || expeditionRunController == null)
            {
                return;
            }

            if (finishLine != null)
            {
                finishLine.MarkReached();
            }

            if (expeditionRunController.IsObjectiveSatisfiedAtFinish())
            {
                CompleteExpedition();
            }
            else
            {
                GameOverInternal(
                    "Objective incomplete: " + expeditionRunController.GetProgressSummary() + ".",
                    null);
            }
        }

        private void CompleteExpedition()
        {
            if (!IsExpedition || CurrentState == ArcadeGameState.Completed)
            {
                return;
            }

            SetState(ArcadeGameState.Completed);
            SetSpawnersActive(false);

            if (tutorialPrompt != null)
            {
                tutorialPrompt.HideForGameOver();
            }

            if (expeditionRunController != null)
            {
                expeditionRunController.SetHudVisible(false);
            }

            if (player != null)
            {
                player.SetInputEnabled(false);
                player.StopForGameOver();
            }

            if (scoreManager != null)
            {
                scoreManager.SetRunning(false);
            }

            float finishFuel = player != null ? player.CurrentFuel : 0f;
            int stars = currentExpedition.CalculateStars(finishFuel);
            int catalogCount = expeditionCatalog != null ? expeditionCatalog.Count : 0;
            GassyExpeditionProgressStore.Complete(currentExpedition, stars, catalogCount);
            GassyBadgeService.Reconcile(expeditionCatalog, true);

            if (expeditionSuccessTitleText != null)
            {
                expeditionSuccessTitleText.text = "EXPEDITION COMPLETE";
            }

            if (expeditionSuccessObjectiveText != null)
            {
                expeditionSuccessObjectiveText.text = currentExpedition.DisplayTitle.ToUpperInvariant() +
                    "\n" + expeditionRunController.GetProgressSummary();
            }

            if (expeditionSuccessStarsText != null)
            {
                expeditionSuccessStarsText.text = new string('*', stars) + "\n" + stars + " / 3 STARS";
            }

            if (expeditionSuccessStoryText != null)
            {
                expeditionSuccessStoryText.text = currentExpedition.SuccessStory;
            }

            if (expeditionNextButton != null)
            {
                int currentIndex = expeditionCatalog != null
                    ? expeditionCatalog.IndexOf(currentExpedition)
                    : -1;
                GassyExpeditionDefinition next = expeditionCatalog != null
                    ? expeditionCatalog.GetByIndex(currentIndex + 1)
                    : null;
                Text nextLabel = expeditionNextButton.GetComponentInChildren<Text>();
                if (nextLabel != null)
                {
                    nextLabel.text = next == null
                        ? "STORY COMPLETE"
                        : (next.ChapterIndex != currentExpedition.ChapterIndex
                            ? "NEXT CHAPTER"
                            : "NEXT EXPEDITION");
                }
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.Milestone, 0.9f);
            }

            if (cameraFollow != null)
            {
                cameraFollow.Shake(0.16f, 0.34f);
            }

            ArcadeHaptics.Play(ArcadeHapticType.Success);

            if (expeditionSuccessRoutine != null)
            {
                StopCoroutine(expeditionSuccessRoutine);
            }

            expeditionSuccessRoutine = StartCoroutine(ExpeditionSuccessRoutine(stars));

            if (ArcadeTimeController.Instance != null)
            {
                ArcadeTimeController.Instance.SetHardPaused(true);
            }
            else
            {
                Time.timeScale = 0f;
            }
        }

        private IEnumerator ExpeditionSuccessRoutine(int stars)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, expeditionSuccessRevealDelay));
            if (expeditionSuccessPanel != null)
            {
                expeditionSuccessPanel.Show();
            }

            Debug.Log(
                "[GG_EXPEDITION] complete id=" + currentExpedition.ExpeditionId +
                " stars=" + stars +
                " progress='" + expeditionRunController.GetProgressSummary() + "'.",
                this);
            expeditionSuccessRoutine = null;
        }

        public void PlayNextExpedition()
        {
            if (!IsExpedition || expeditionCatalog == null)
            {
                ReturnToMainMenu();
                return;
            }

            int currentIndex = expeditionCatalog.IndexOf(currentExpedition);
            GassyExpeditionDefinition next = expeditionCatalog.GetByIndex(currentIndex + 1);
            if (next == null)
            {
                ReturnToMainMenu();
                return;
            }

            ArcadeRunSession.SelectFinite(next.ExpeditionId);
            ResetTimeAndLoad(gameSceneName);
        }

        public void ReturnToExpeditionSelect()
        {
            ResetTimeAndLoad(mainMenuSceneName);
        }

        public void GameOver(string reason)
        {
            if (crocodileQaMode)
            {
                return;
            }

            GameOverInternal(reason, null);
        }

        public void ConfigureCrocodileQa(string mode)
        {
            crocodileQaMode = true;
            nextCrocodileQaSafetyRefresh = 0f;
            DisableStandardHazardsForQa();

            if (player != null)
            {
                player.ConfigureCrocodileQa(mode);
            }
        }

        private void ApplyWebQaConfiguration()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string absoluteUrl = Application.absoluteURL;
            bool vineQa = absoluteUrl.IndexOf("qa-vine", StringComparison.OrdinalIgnoreCase) >= 0;
            bool crocodileHitQa = absoluteUrl.IndexOf("qa-croc-hit", StringComparison.OrdinalIgnoreCase) >= 0;
            bool crocodileQa = crocodileHitQa ||
                absoluteUrl.IndexOf("qa-croc", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!vineQa && !crocodileQa)
            {
                return;
            }

            RunChunkDirector runDirector = FindAnyObjectByType<RunChunkDirector>();
            if (vineQa)
            {
                IsVineQaMode = true;
                if (runDirector != null)
                {
                    runDirector.ConfigureOpeningForQa("LowVineRescue");
                }

                return;
            }

            if (runDirector != null)
            {
                runDirector.ConfigureSeedForQa("6");
            }

            ConfigureCrocodileQa(crocodileHitQa ? "hit" : "dodge");
#endif
        }

        private void ApplyWebQaRunSelection()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string requested = GetQueryValue(Application.absoluteURL, "qa-expedition");
            if (string.IsNullOrWhiteSpace(requested) || expeditionCatalog == null)
            {
                return;
            }

            GassyExpeditionDefinition definition = expeditionCatalog.FindById(requested);
            int oneBasedIndex;
            if (definition == null && int.TryParse(requested, out oneBasedIndex))
            {
                definition = expeditionCatalog.GetByIndex(oneBasedIndex - 1);
            }

            if (definition != null)
            {
                ArcadeRunSession.SelectFinite(definition.ExpeditionId);
            }
#endif
        }

        private void ResolveRunSelection()
        {
            currentExpedition = null;
            if (ArcadeRunSession.Mode != ArcadeRunMode.Finite || expeditionCatalog == null)
            {
                return;
            }

            currentExpedition = expeditionCatalog.FindById(ArcadeRunSession.ContentId);
            if (currentExpedition == null)
            {
                Debug.LogWarning(
                    "Unknown Expedition '" + ArcadeRunSession.ContentId + "'. Falling back to Endless Run.",
                    this);
                ArcadeRunSession.SelectEndless();
            }
        }

        private void ConfigureSelectedRun()
        {
            if (IsExpedition)
            {
                if (runChunkDirector != null)
                {
                    runChunkDirector.ConfigureFiniteRoute(currentExpedition.Route);
                }

                float finishWorldX = runChunkDirector != null
                    ? runChunkDirector.ConfiguredFiniteRouteEndX - currentExpedition.FinishInset
                    : currentExpedition.RouteLength - currentExpedition.FinishInset;
                if (expeditionRunController != null)
                {
                    expeditionRunController.Configure(currentExpedition, finishWorldX);
                }

                if (hudBestDistanceText != null)
                {
                    hudBestDistanceText.gameObject.SetActive(false);
                }
            }
            else
            {
                if (expeditionRunController != null)
                {
                    expeditionRunController.ConfigureEndless();
                }

                if (hudBestDistanceText != null)
                {
                    hudBestDistanceText.gameObject.SetActive(true);
                }
            }
        }

        private void ConfigureExpeditionStoryUi()
        {
            if (currentExpedition == null)
            {
                return;
            }

            if (expeditionStoryTitleText != null)
            {
                expeditionStoryTitleText.text = currentExpedition.DisplayTitle.ToUpperInvariant();
            }

            if (expeditionStoryBodyText != null)
            {
                expeditionStoryBodyText.text = currentExpedition.OpeningStory;
            }

            if (expeditionStoryObjectiveText != null)
            {
                expeditionStoryObjectiveText.text = "OBJECTIVE  " + currentExpedition.ObjectiveText;
            }

            if (expeditionStoryLessonText != null)
            {
                expeditionStoryLessonText.text = "LESSON  " + currentExpedition.LessonText;
            }
        }

        public void BeginExpeditionFromStory()
        {
            if (!IsExpedition || CurrentState != ArcadeGameState.Ready)
            {
                return;
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.NotifyUserGesture();
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.UiClick);
            }

            if (expeditionStoryPanel != null)
            {
                expeditionStoryPanel.Hide();
            }

            if (expeditionRunController != null)
            {
                expeditionRunController.SetHudVisible(true);
            }

            if (tutorialPrompt != null)
            {
                tutorialPrompt.BeginForRun();
            }

            if (playCameraIntro && GetSceneCamera() != null && player != null)
            {
                introRoutine = StartCoroutine(CameraIntroRoutine());
            }
            else
            {
                BeginRun();
            }
        }

        private bool ShouldAutoStartQaExpedition()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return IsExpedition &&
                Application.absoluteURL.IndexOf("qa-expedition-auto", StringComparison.OrdinalIgnoreCase) >= 0;
#else
            return false;
#endif
        }

        private IEnumerator AutoStartExpeditionQaRoutine()
        {
            yield return new WaitForSecondsRealtime(0.25f);
            BeginExpeditionFromStory();

            if (!ShouldAutoCompleteQaExpedition())
            {
                yield break;
            }

            float timeoutAt = Time.realtimeSinceStartup + 5f;
            while (CurrentState != ArcadeGameState.Running &&
                Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            if (CurrentState != ArcadeGameState.Running || expeditionRunController == null)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.25f);
            expeditionRunController.CompleteObjectiveForQa();
            ReachExpeditionFinish(expeditionRunController.FinishLine);
        }

        private bool ShouldAutoCompleteQaExpedition()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return IsExpedition &&
                Application.absoluteURL.IndexOf("qa-expedition-complete", StringComparison.OrdinalIgnoreCase) >= 0;
#else
            return false;
#endif
        }

        private static string GetQueryValue(string url, string key)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            int queryStart = url.IndexOf('?');
            if (queryStart < 0 || queryStart >= url.Length - 1)
            {
                return string.Empty;
            }

            string[] pairs = url.Substring(queryStart + 1).Split('&');
            for (int i = 0; i < pairs.Length; i++)
            {
                string[] parts = pairs[i].Split(new[] { '=' }, 2);
                if (parts.Length > 0 &&
                    string.Equals(parts[0], key, StringComparison.OrdinalIgnoreCase))
                {
                    return parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "1";
                }
            }

            return string.Empty;
        }

        private static void DisableStandardHazardsForQa()
        {
            ArcadeHazard[] standardHazards = FindObjectsByType<ArcadeHazard>(FindObjectsInactive.Include);
            for (int i = 0; i < standardHazards.Length; i++)
            {
                Collider2D[] colliders = standardHazards[i].GetComponentsInChildren<Collider2D>(true);
                for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                {
                    colliders[colliderIndex].enabled = false;
                }
            }
        }

        public bool GameOverFromCrocodileAmbush(CrocodileAmbushController ambush)
        {
            if (ambush == null)
            {
                return false;
            }

            return GameOverInternal("Caught by a lagoon crocodile.", ambush);
        }

        private bool GameOverInternal(string reason, CrocodileAmbushController crocodileAmbush)
        {
            if (CurrentState == ArcadeGameState.GameOver ||
                CurrentState == ArcadeGameState.Completed)
            {
                return false;
            }

            if (IsVineQaMode && player != null)
            {
                Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
                Vector2 velocity = playerBody != null ? playerBody.linearVelocity : Vector2.zero;
                Debug.Log("[GG_VINE_QA] Game over reason='" + reason + "' position=" + player.transform.position +
                    " velocity=" + velocity + " swinging=" + player.IsSwinging + ".", this);
            }

            SetState(ArcadeGameState.GameOver);
            SetSpawnersActive(false);

            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = IsExpedition ? "EXPEDITION FAILED" : "RUN OVER";
            }

            if (gameOverReasonText != null)
            {
                gameOverReasonText.text = reason;
            }

            if (tutorialPrompt != null)
            {
                tutorialPrompt.HideForGameOver();
            }

            if (expeditionRunController != null)
            {
                expeditionRunController.SetHudVisible(false);
            }

            bool isCrocodileAmbush = crocodileAmbush != null;
            bool lagoonFall = !isCrocodileAmbush && player != null && player.transform.position.y <= deathY + 0.05f;
            if (lagoonFall && lagoonFinishPresentation != null)
            {
                lagoonFinishPresentation.PlayWaterImpact(player.transform.position);
            }

            if (player != null)
            {
                player.SetInputEnabled(false);
                player.StopForGameOver(gameOverRestY);
            }

            if (isCrocodileAmbush && player != null)
            {
                crocodileAmbush.ConfirmSuccessfulBite(player);
            }

            if (scoreManager != null)
            {
                scoreManager.SetRunning(false);
            }

            float distance = scoreManager != null ? scoreManager.Distance : 0f;
            if (!IsExpedition)
            {
                HighScoreStore.TrySaveBestDistance(BestDistanceKey, distance);
            }

            if (currentDistanceText != null)
            {
                currentDistanceText.text = Mathf.FloorToInt(distance) + " m";
            }

            UpdateBestDistanceText();

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeSfxType sfx = isCrocodileAmbush
                    ? ArcadeSfxType.Chomp
                    : (lagoonFall ? ArcadeSfxType.Splash : ArcadeSfxType.Crash);
                ArcadeAudioManager.Instance.PlaySfx(sfx, isCrocodileAmbush ? 0.96f : 1f);
            }

            if (cameraFollow != null)
            {
                float shakeIntensity = isCrocodileAmbush ? 0.42f : (lagoonFall ? 0.24f : 0.32f);
                float shakeDuration = isCrocodileAmbush ? 0.5f : (lagoonFall ? 0.34f : 0.45f);
                cameraFollow.Shake(shakeIntensity, shakeDuration);
            }

            ArcadeHaptics.Play(ArcadeHapticType.Failure);

            if (outroRoutine != null)
            {
                StopCoroutine(outroRoutine);
            }

            float resultRevealDelay = isCrocodileAmbush
                ? crocodileAmbushResultRevealDelay
                : (lagoonFall ? lagoonResultRevealDelay : hazardResultRevealDelay);
            Transform outroFocus = isCrocodileAmbush && crocodileAmbush.BitePoint != null
                ? crocodileAmbush.BitePoint
                : (player != null ? player.transform : null);
            float outroShake = isCrocodileAmbush ? 0.15f : (lagoonFall ? 0.07f : 0.09f);
            outroRoutine = StartCoroutine(CameraOutroRoutine(resultRevealDelay, outroFocus, outroShake));

            if (ArcadeTimeController.Instance != null)
            {
                ArcadeTimeController.Instance.SetHardPaused(true);
            }
            else
            {
                Time.timeScale = 0f;
            }

            return true;
        }

        private IEnumerator CameraIntroRoutine()
        {
            UnityEngine.Camera activeCamera = GetSceneCamera();
            if (activeCamera == null || player == null)
            {
                BeginRun();
                yield break;
            }

            if (ArcadeAccessibilitySettings.ReducedMotion)
            {
                if (cameraFollow != null)
                {
                    cameraFollow.SnapToTarget();
                }
                else
                {
                    Vector3 stablePosition = player.transform.position + introEndOffset;
                    stablePosition.z = activeCamera.transform.position.z;
                    activeCamera.transform.position = stablePosition;
                }

                introRoutine = null;
                BeginRun();
                yield break;
            }

            bool followWasEnabled = cameraFollow != null && cameraFollow.enabled;
            if (cameraFollow != null)
            {
                cameraFollow.enabled = false;
            }

            Transform playerTransform = player.transform;
            Vector3 endPosition = playerTransform.position + introEndOffset;
            Vector3 startPosition = playerTransform.position + introStartOffset;
            float endZoom = activeCamera.orthographicSize;
            activeCamera.transform.position = startPosition;
            activeCamera.orthographicSize = introStartZoom;

            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, introDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t < 0.38f
                    ? Mathf.SmoothStep(0f, 0.2f, t / 0.38f)
                    : Mathf.SmoothStep(0.2f, 1f, (t - 0.38f) / 0.62f);
                float wobble = Mathf.Sin(t * Mathf.PI * 10f) * (1f - t) * 0.08f;
                Vector3 comicJiggle = new Vector3(wobble * 0.5f, -Mathf.Abs(wobble), 0f);

                activeCamera.transform.position = Vector3.Lerp(startPosition, endPosition, eased) + comicJiggle;
                activeCamera.orthographicSize = Mathf.Lerp(introStartZoom, endZoom, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            if (cameraFollow != null)
            {
                cameraFollow.enabled = followWasEnabled;
                cameraFollow.SnapToTarget();
            }
            else
            {
                activeCamera.transform.position = endPosition;
                activeCamera.orthographicSize = endZoom;
            }

            introRoutine = null;
            BeginRun();
        }

        private IEnumerator CameraOutroRoutine(float resultRevealDelay, Transform focusTarget, float impactShakeAmplitude)
        {
            UnityEngine.Camera activeCamera = GetSceneCamera();
            if (activeCamera == null || player == null)
            {
                yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, resultRevealDelay));
                ShowGameOverPanel();
                outroRoutine = null;
                yield break;
            }

            if (ArcadeAccessibilitySettings.ReducedMotion)
            {
                if (cameraFollow != null)
                {
                    cameraFollow.enabled = false;
                }

                Transform reducedFocus = focusTarget != null ? focusTarget : player.transform;
                Vector3 stablePosition = reducedFocus.position + outroOffset;
                stablePosition.z = activeCamera.transform.position.z;
                activeCamera.transform.position = stablePosition;
                yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, resultRevealDelay));
                ShowGameOverPanel();
                outroRoutine = null;
                yield break;
            }

            if (cameraFollow != null)
            {
                cameraFollow.enabled = false;
            }

            Vector3 startPosition = activeCamera.transform.position;
            Transform activeFocus = focusTarget != null ? focusTarget : player.transform;
            Vector3 targetPosition = activeFocus.position + outroOffset;
            targetPosition.z = startPosition.z;
            float startZoom = activeCamera.orthographicSize;
            float duration = Mathf.Max(0.1f, outroDuration);
            float elapsed = 0f;
            bool resultShown = false;

            float totalDuration = Mathf.Max(duration, Mathf.Max(0f, resultRevealDelay));
            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (!resultShown && elapsed >= Mathf.Max(0f, resultRevealDelay))
                {
                    ShowGameOverPanel();
                    resultShown = true;
                }

                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                if (activeFocus != null)
                {
                    targetPosition = activeFocus.position + outroOffset;
                    targetPosition.z = startPosition.z;
                }

                float bonkShake = Mathf.Sin(t * Mathf.PI * 12f) * (1f - t) * impactShakeAmplitude;
                Vector3 shake = new Vector3(bonkShake, -Mathf.Abs(bonkShake) * 0.65f, 0f);

                activeCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, eased) + shake;
                activeCamera.orthographicSize = Mathf.Lerp(startZoom, outroZoom, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            if (activeFocus != null)
            {
                targetPosition = activeFocus.position + outroOffset;
                targetPosition.z = startPosition.z;
            }

            activeCamera.transform.position = targetPosition;
            activeCamera.orthographicSize = outroZoom;
            if (!resultShown)
            {
                ShowGameOverPanel();
            }

            outroRoutine = null;
        }

        private void ShowGameOverPanel()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.Show();
            }
        }

        private void BeginRun()
        {
            SetState(ArcadeGameState.Running);

            if (player != null)
            {
                player.BeginRun();
            }

            if (scoreManager != null)
            {
                scoreManager.SetRunning(true);
            }

            SetSpawnersActive(true);

            if (IsExpedition && expeditionRunController != null)
            {
                expeditionRunController.BeginRunLesson();
            }
        }

        private UnityEngine.Camera GetSceneCamera()
        {
            if (sceneCamera == null)
            {
                sceneCamera = UnityEngine.Camera.main;
            }

            return sceneCamera;
        }

        public void RestartRun()
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.NotifyUserGesture();
            }

            ResetTimeAndLoad(gameSceneName);
        }

        public void ReturnToMainMenu()
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.NotifyUserGesture();
            }

            ResetTimeAndLoad(mainMenuSceneName);
        }

        private void ResetTimeAndLoad(string sceneName)
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.SetPauseMixActive(false);
            }

            if (ArcadeTimeController.Instance != null)
            {
                ArcadeTimeController.Instance.ResetTimeScale();
            }
            else
            {
                Time.timeScale = 1f;
            }

            SceneManager.LoadScene(sceneName);
        }

        private static void SetPausedTime(bool paused)
        {
            if (ArcadeTimeController.Instance != null)
            {
                ArcadeTimeController.Instance.SetHardPaused(paused);
            }
            else
            {
                Time.timeScale = paused ? 0f : 1f;
            }
        }

        private void SetSpawnersActive(bool active)
        {
            if (runChunkDirector != null)
            {
                runChunkDirector.SetSpawning(active);
            }

            if (spawners == null)
            {
                return;
            }

            for (int i = 0; i < spawners.Length; i++)
            {
                if (spawners[i] != null)
                {
                    spawners[i].SetSpawning(active);
                }
            }
        }

        private void UpdateBestDistanceText()
        {
            if (IsExpedition)
            {
                if (bestDistanceText != null)
                {
                    bestDistanceText.text = expeditionRunController != null
                        ? expeditionRunController.GetProgressSummary()
                        : currentExpedition.ObjectiveText;
                }

                if (hudBestDistanceText != null)
                {
                    hudBestDistanceText.gameObject.SetActive(false);
                }

                return;
            }

            float best = HighScoreStore.GetBestDistance(BestDistanceKey);
            string value = Mathf.FloorToInt(best) + " m";

            if (bestDistanceText != null)
            {
                bestDistanceText.text = "BEST  " + value;
            }

            if (hudBestDistanceText != null)
            {
                hudBestDistanceText.text = "Best " + value;
            }
        }
    }
}
