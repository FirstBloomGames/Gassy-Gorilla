using FirstBloom.ArcadeFramework.Audio;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstBloom.ArcadeFramework.UI
{
    public class ArcadeButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private float hoverScale = 1.035f;
        [SerializeField] private float pressedScale = 0.965f;
        [SerializeField] private float smoothSpeed = 18f;
        [SerializeField] private bool playClickSfx = true;

        private float desiredScale = 1f;

        private void Awake()
        {
            if (target == null)
            {
                target = transform as RectTransform;
            }
        }

        private void OnDisable()
        {
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
            if (playClickSfx && ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.UiClick);
            }
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
    }
}
