using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FirstBloom.ArcadeFramework.UI
{
    public class TextOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Text messageText;
        [SerializeField] private float fadeInDuration = 0.12f;
        [SerializeField] private float fadeOutDuration = 0.25f;

        private Coroutine activeRoutine;

        private void Awake()
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
            }
        }

        public void Show(string message, float holdDuration = 2f)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
            }

            activeRoutine = StartCoroutine(ShowRoutine(Mathf.Max(0.05f, holdDuration)));
        }

        public void HideInstant()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }

        private IEnumerator ShowRoutine(float holdDuration)
        {
            if (group == null)
            {
                yield break;
            }

            group.interactable = false;
            group.blocksRaycasts = false;

            yield return FadeTo(1f, fadeInDuration);
            yield return new WaitForSecondsRealtime(holdDuration);
            yield return FadeTo(0f, fadeOutDuration);
            activeRoutine = null;
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (group == null)
            {
                yield break;
            }

            float startAlpha = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            group.alpha = targetAlpha;
        }
    }
}
