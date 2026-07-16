using System.Collections;
using FirstBloom.ArcadeFramework.Audio;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstBloom.ArcadeFramework.UI
{
    public class ArcadeButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private float hoverScale = 1.035f;
        [SerializeField] private float pressedScale = 0.965f;
        [SerializeField] private float smoothSpeed = 18f;
        [SerializeField] private bool playClickSfx = true;
        [SerializeField] private ArcadeSfxType clickSfx = ArcadeSfxType.UiClick;

        private float desiredScale = 1f;
        private Coroutine submitReleaseRoutine;

        private void Awake()
        {
            if (target == null)
            {
                target = transform as RectTransform;
            }
        }

        private void OnDisable()
        {
            if (submitReleaseRoutine != null)
            {
                StopCoroutine(submitReleaseRoutine);
                submitReleaseRoutine = null;
            }

            desiredScale = 1f;
            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            Vector3 scale = Vector3.one * desiredScale;
            target.localScale = Vector3.Lerp(target.localScale, scale, smoothSpeed * Time.unscaledDeltaTime);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            desiredScale = pressedScale;
            PlayClickFeedback();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            desiredScale = hoverScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            desiredScale = hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            desiredScale = 1f;
        }

        public void OnSelect(BaseEventData eventData)
        {
            desiredScale = hoverScale;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            desiredScale = 1f;
        }

        public void OnSubmit(BaseEventData eventData)
        {
            desiredScale = pressedScale;
            PlayClickFeedback();

            if (submitReleaseRoutine != null)
            {
                StopCoroutine(submitReleaseRoutine);
            }

            submitReleaseRoutine = StartCoroutine(ReleaseSubmitPress());
        }

        private IEnumerator ReleaseSubmitPress()
        {
            yield return new WaitForSecondsRealtime(0.08f);
            desiredScale = hoverScale;
            submitReleaseRoutine = null;
        }

        private void PlayClickFeedback()
        {
            if (ArcadeAudioManager.Instance == null)
            {
                return;
            }

            ArcadeAudioManager.Instance.NotifyUserGesture();
            if (playClickSfx)
            {
                ArcadeAudioManager.Instance.PlaySfx(clickSfx);
            }
        }
    }
}
