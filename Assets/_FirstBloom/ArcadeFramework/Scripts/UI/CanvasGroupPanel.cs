using System.Collections;
using UnityEngine;

namespace FirstBloom.ArcadeFramework.UI
{
    public class CanvasGroupPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool visibleOnStart;
        [SerializeField] private float fadeDuration = 0.16f;
        [SerializeField] private float hiddenScale = 0.94f;
        [SerializeField] private Transform scaleRoot;

        private Coroutine transitionRoutine;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (scaleRoot == null)
            {
                scaleRoot = transform;
            }

            SetVisibleInstant(visibleOnStart);
        }

        public void Show()
        {
            SetVisible(true, true);
        }

        public void Hide()
        {
            SetVisible(false, true);
        }

        public void SetVisible(bool visible)
        {
            SetVisible(visible, false);
        }

        private void SetVisible(bool visible, bool animated)
        {
            if (canvasGroup == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            if (!animated || fadeDuration <= 0f)
            {
                SetVisibleInstant(visible);
                return;
            }

            transitionRoutine = StartCoroutine(TransitionRoutine(visible));
        }

        private IEnumerator TransitionRoutine(bool visible)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float startAlpha = canvasGroup.alpha;
            float targetAlpha = visible ? 1f : 0f;
            Vector3 startScale = scaleRoot != null ? scaleRoot.localScale : Vector3.one;
            Vector3 targetScale = Vector3.one * (visible ? 1f : hiddenScale);
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
                if (scaleRoot != null)
                {
                    scaleRoot.localScale = Vector3.Lerp(startScale, targetScale, eased);
                }

                yield return null;
            }

            SetVisibleInstant(visible);
            transitionRoutine = null;
        }

        private void SetVisibleInstant(bool visible)
        {
            if (canvasGroup == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            if (scaleRoot != null)
            {
                scaleRoot.localScale = Vector3.one * (visible ? 1f : hiddenScale);
            }
        }
    }
}
