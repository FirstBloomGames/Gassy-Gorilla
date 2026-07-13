using System.Collections;
using FirstBloom.ArcadeFramework.Audio;
using FirstBloom.ArcadeFramework.Camera;
using FirstBloom.ArcadeFramework.Core;
using FirstBloom.ArcadeFramework.Save;
using FirstBloom.ArcadeFramework.Spawning;
using FirstBloom.ArcadeFramework.UI;
using FirstBloom.ArcadeFramework.VFX;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FirstBloom.Games.GassyGorilla
{
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
        [SerializeField] private float deathY = -3.45f;
        [SerializeField] private float gameOverRestY = -2.82f;

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

        public static GassyGorillaGameManager Instance { get; private set; }

        public bool IsRunActive { get { return CurrentState == ArcadeGameState.Running; } }

        private Coroutine introRoutine;
        private Coroutine outroRoutine;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
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

            if (gameOverPanel != null)
            {
                gameOverPanel.Hide();
            }

            if (scoreManager != null)
            {
                scoreManager.ResetScore();
                scoreManager.SetRunning(false);
            }

            if (player != null)
            {
                player.PrepareForIntro();
            }

            SetSpawnersActive(false);
            UpdateBestDistanceText();
            SetState(ArcadeGameState.Ready);

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
            if (!IsRunActive || player == null)
            {
                return;
            }

            if (player.transform.position.y <= deathY)
            {
                GameOver("Fell into the jungle.");
            }
        }

        public void GameOver(string reason)
        {
            if (CurrentState == ArcadeGameState.GameOver)
            {
                return;
            }

            SetState(ArcadeGameState.GameOver);
            SetSpawnersActive(false);

            if (player != null)
            {
                player.SetInputEnabled(false);
                player.StopForGameOver(gameOverRestY);
            }

            if (scoreManager != null)
            {
                scoreManager.SetRunning(false);
            }

            float distance = scoreManager != null ? scoreManager.Distance : 0f;
            HighScoreStore.TrySaveBestDistance(BestDistanceKey, distance);

            if (currentDistanceText != null)
            {
                currentDistanceText.text = Mathf.FloorToInt(distance) + " m";
            }

            UpdateBestDistanceText();

            if (gameOverPanel != null)
            {
                gameOverPanel.Show();
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.Crash);
            }

            if (cameraFollow != null)
            {
                cameraFollow.Shake(0.32f, 0.45f);
            }

            if (outroRoutine != null)
            {
                StopCoroutine(outroRoutine);
            }

            outroRoutine = StartCoroutine(CameraOutroRoutine());

            if (ArcadeTimeController.Instance != null)
            {
                ArcadeTimeController.Instance.SetHardPaused(true);
            }
            else
            {
                Time.timeScale = 0f;
            }
        }

        private IEnumerator CameraIntroRoutine()
        {
            UnityEngine.Camera activeCamera = GetSceneCamera();
            if (activeCamera == null || player == null)
            {
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

        private IEnumerator CameraOutroRoutine()
        {
            UnityEngine.Camera activeCamera = GetSceneCamera();
            if (activeCamera == null || player == null)
            {
                yield break;
            }

            if (cameraFollow != null)
            {
                cameraFollow.enabled = false;
            }

            Vector3 startPosition = activeCamera.transform.position;
            Vector3 targetPosition = player.transform.position + outroOffset;
            targetPosition.z = startPosition.z;
            float startZoom = activeCamera.orthographicSize;
            float duration = Mathf.Max(0.1f, outroDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float bonkShake = Mathf.Sin(t * Mathf.PI * 12f) * (1f - t) * 0.09f;
                Vector3 shake = new Vector3(bonkShake, -Mathf.Abs(bonkShake) * 0.65f, 0f);

                activeCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, eased) + shake;
                activeCamera.orthographicSize = Mathf.Lerp(startZoom, outroZoom, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            activeCamera.transform.position = targetPosition;
            activeCamera.orthographicSize = outroZoom;
            outroRoutine = null;
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
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.UiClick);
            }

            ResetTimeAndLoad(gameSceneName);
        }

        public void ReturnToMainMenu()
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.UiClick);
            }

            ResetTimeAndLoad(mainMenuSceneName);
        }

        private void ResetTimeAndLoad(string sceneName)
        {
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
            float best = HighScoreStore.GetBestDistance(BestDistanceKey);
            string value = Mathf.FloorToInt(best) + " m";

            if (bestDistanceText != null)
            {
                bestDistanceText.text = value;
            }

            if (hudBestDistanceText != null)
            {
                hudBestDistanceText.text = "Best " + value;
            }
        }
    }
}
