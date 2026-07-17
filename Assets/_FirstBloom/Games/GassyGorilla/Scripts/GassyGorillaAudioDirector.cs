using System.Collections;
using FirstBloom.ArcadeFramework.Audio;
using FirstBloom.ArcadeFramework.Core;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public sealed class GassyGorillaAudioDirector : MonoBehaviour
    {
        [Header("Run References")]
        [SerializeField] private RunChunkDirector runDirector;
        [SerializeField] private GorillaController gorilla;
        [SerializeField] private GassyGorillaGameManager gameManager;

        [Header("Vine Motion")]
        [Range(0f, 1f)] [SerializeField] private float swingBaseVolume = 0.1f;
        [Range(0f, 1f)] [SerializeField] private float swingSpeedVolume = 0.08f;
        [SerializeField] private Vector2 swingPitchRange = new Vector2(0.88f, 1.08f);
        [Min(0.1f)] [SerializeField] private float fullSwingSpeed = 7f;

        [Header("Result")]
        [Min(0f)] [SerializeField] private float gameOverStingDelay = 0.28f;
        [Range(0f, 1f)] [SerializeField] private float gameOverStingVolume = 0.72f;
        [Range(0f, 1f)] [SerializeField] private float gameOverMusicIntensity = 0.12f;

        private Coroutine gameOverStingRoutine;

        private void OnEnable()
        {
            if (runDirector != null)
            {
                runDirector.DifficultyChanged += HandleDifficultyChanged;
            }

            if (gorilla != null)
            {
                gorilla.VineGrabbed += HandleVineGrabbed;
                gorilla.VineReleased += HandleVineReleased;
            }

            if (gameManager != null)
            {
                gameManager.StateChanged += HandleGameStateChanged;
            }
        }

        private void Start()
        {
            ArcadeAudioManager audio = ArcadeAudioManager.Instance;
            if (audio != null)
            {
                audio.SetMusicIntensity(runDirector != null ? runDirector.CurrentIntensity : 0f);
            }
        }

        private void Update()
        {
            if (gorilla == null || !gorilla.IsSwinging || ArcadeAudioManager.Instance == null)
            {
                return;
            }

            float speed = gorilla.CurrentSwingVelocity.magnitude;
            float normalizedSpeed = Mathf.Clamp01(speed / Mathf.Max(0.1f, fullSwingSpeed));
            float volume = swingBaseVolume + normalizedSpeed * swingSpeedVolume;
            float pitch = Mathf.Lerp(swingPitchRange.x, swingPitchRange.y, normalizedSpeed);
            ArcadeAudioManager.Instance.SetSfxLoopParameters(ArcadeSfxType.VineSwing, volume, pitch);
        }

        private void OnDisable()
        {
            if (runDirector != null)
            {
                runDirector.DifficultyChanged -= HandleDifficultyChanged;
            }

            if (gorilla != null)
            {
                gorilla.VineGrabbed -= HandleVineGrabbed;
                gorilla.VineReleased -= HandleVineReleased;
            }

            if (gameManager != null)
            {
                gameManager.StateChanged -= HandleGameStateChanged;
            }

            if (gameOverStingRoutine != null)
            {
                StopCoroutine(gameOverStingRoutine);
                gameOverStingRoutine = null;
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.StopSfxLoop(ArcadeSfxType.VineSwing, 0f);
            }
        }

        private void HandleDifficultyChanged(float intensity, int stage)
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.SetMusicIntensity(intensity);
            }
        }

        private void HandleVineGrabbed()
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.StartSfxLoop(ArcadeSfxType.VineSwing, swingBaseVolume);
            }
        }

        private void HandleVineReleased()
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.StopSfxLoop(ArcadeSfxType.VineSwing, 0.1f);
            }
        }

        private void HandleGameStateChanged(ArcadeGameState state)
        {
            ArcadeAudioManager audio = ArcadeAudioManager.Instance;
            if (audio == null)
            {
                return;
            }

            if (state == ArcadeGameState.GameOver)
            {
                audio.StopSfxLoop(ArcadeSfxType.VineSwing, 0.05f);
                audio.SetMusicIntensity(gameOverMusicIntensity);
                if (gameOverStingRoutine != null)
                {
                    StopCoroutine(gameOverStingRoutine);
                }

                gameOverStingRoutine = StartCoroutine(PlayGameOverSting());
            }
            else if (state == ArcadeGameState.Ready)
            {
                audio.SetMusicIntensity(0f);
            }
        }

        private IEnumerator PlayGameOverSting()
        {
            yield return new WaitForSecondsRealtime(gameOverStingDelay);
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.GameOver, gameOverStingVolume);
            }

            gameOverStingRoutine = null;
        }
    }
}
