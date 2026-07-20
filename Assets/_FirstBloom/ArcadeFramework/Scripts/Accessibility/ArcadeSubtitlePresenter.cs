using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FirstBloom.ArcadeFramework.Accessibility
{
    public sealed class ArcadeSubtitlePresenter : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Text subtitleText;
        [Min(0f)] [SerializeField] private float fadeDuration = 0.12f;

        private Coroutine presentationRoutine;

        public bool IsConfigured
        {
            get { return group != null && subtitleText != null; }
        }

        private void Awake()
        {
            HideImmediate();
        }

        private void OnEnable()
        {
            ArcadeAccessibilitySettings.SettingsChanged +=
                HandleSettingsChanged;
        }

        private void OnDisable()
        {
            ArcadeAccessibilitySettings.SettingsChanged -=
                HandleSettingsChanged;
            StopPresentation();
        }

        public void Show(string text, float duration)
        {
            StopPresentation();
            if (!ArcadeAccessibilitySettings.SubtitlesEnabled ||
                string.IsNullOrWhiteSpace(text) ||
                group == null ||
                subtitleText == null)
            {
                HideImmediate();
                return;
            }

            subtitleText.text = text;
            gameObject.SetActive(true);
            presentationRoutine =
                StartCoroutine(PresentationRoutine(Mathf.Max(0.8f, duration)));
        }

        public void Hide()
        {
            StopPresentation();
            HideImmediate();
        }

        private IEnumerator PresentationRoutine(float duration)
        {
            float transition = ArcadeAccessibilitySettings.ReducedMotion
                ? 0f
                : fadeDuration;
            yield return Fade(0f, 1f, transition);
            yield return new WaitForSecondsRealtime(duration);
            yield return Fade(1f, 0f, transition);
            HideImmediate();
            presentationRoutine = null;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                group.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(
                    from,
                    to,
                    Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            group.alpha = to;
        }

        private void HandleSettingsChanged()
        {
            if (!ArcadeAccessibilitySettings.SubtitlesEnabled)
            {
                Hide();
            }
        }

        private void StopPresentation()
        {
            if (presentationRoutine == null)
            {
                return;
            }

            StopCoroutine(presentationRoutine);
            presentationRoutine = null;
        }

        private void HideImmediate()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }
    }
}
