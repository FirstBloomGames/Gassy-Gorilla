using System.Collections;
using FirstBloom.ArcadeFramework.Accessibility;
using UnityEngine;

namespace FirstBloom.ArcadeFramework.VFX
{
    [DefaultExecutionOrder(-90)]
    public class ArcadeTimeController : MonoBehaviour
    {
        private float baseFixedDeltaTime;
        private Coroutine slowMotionRoutine;
        private bool hardPaused;

        public static ArcadeTimeController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            baseFixedDeltaTime = Time.fixedDeltaTime;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlaySlowMotion(float timeScale, float duration)
        {
            if (ArcadeAccessibilitySettings.ReducedMotion)
            {
                if (!hardPaused)
                {
                    SetScale(1f);
                }

                return;
            }

            if (slowMotionRoutine != null)
            {
                StopCoroutine(slowMotionRoutine);
            }

            slowMotionRoutine = StartCoroutine(SlowMotionRoutine(Mathf.Clamp(timeScale, 0.05f, 1f), Mathf.Max(0f, duration)));
        }

        public void SetHardPaused(bool paused)
        {
            hardPaused = paused;

            if (slowMotionRoutine != null)
            {
                StopCoroutine(slowMotionRoutine);
                slowMotionRoutine = null;
            }

            SetScale(paused ? 0f : 1f);
        }

        public void ResetTimeScale()
        {
            hardPaused = false;
            if (slowMotionRoutine != null)
            {
                StopCoroutine(slowMotionRoutine);
                slowMotionRoutine = null;
            }

            SetScale(1f);
        }

        private IEnumerator SlowMotionRoutine(float timeScale, float duration)
        {
            if (hardPaused)
            {
                yield break;
            }

            SetScale(timeScale);
            yield return new WaitForSecondsRealtime(duration);

            if (!hardPaused)
            {
                SetScale(1f);
            }

            slowMotionRoutine = null;
        }

        private void SetScale(float scale)
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = baseFixedDeltaTime * Mathf.Max(0.01f, scale);
        }
    }
}
