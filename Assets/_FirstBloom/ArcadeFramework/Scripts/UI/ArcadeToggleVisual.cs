using UnityEngine;
using UnityEngine.UI;

namespace FirstBloom.ArcadeFramework.UI
{
    [RequireComponent(typeof(Toggle))]
    public sealed class ArcadeToggleVisual : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private Image track;
        [SerializeField] private RectTransform knob;
        [SerializeField] private Text stateText;
        [SerializeField] private float knobOffset = 14f;
        [SerializeField] private Color offTrackColor = new Color(0.055f, 0.14f, 0.12f, 1f);
        [SerializeField] private Color onTrackColor = new Color(0.25f, 0.68f, 0.28f, 1f);

        public bool IsConfigured
        {
            get
            {
                return toggle != null &&
                    track != null &&
                    knob != null &&
                    stateText != null;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return IsConfigured &&
                    stateText.text == (toggle.isOn ? "ON" : "OFF") &&
                    (toggle.isOn ? knob.anchoredPosition.x > 0f : knob.anchoredPosition.x < 0f);
            }
        }

        private void Awake()
        {
            if (toggle == null)
            {
                toggle = GetComponent<Toggle>();
            }

            Apply(toggle != null && toggle.isOn);
        }

        private void OnEnable()
        {
            if (toggle == null)
            {
                toggle = GetComponent<Toggle>();
            }

            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(Apply);
                Apply(toggle.isOn);
            }
        }

        private void OnDisable()
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(Apply);
            }
        }

        public void Refresh()
        {
            if (toggle == null)
            {
                toggle = GetComponent<Toggle>();
            }

            Apply(toggle != null && toggle.isOn);
        }

        private void Apply(bool isOn)
        {
            if (track != null)
            {
                track.color = isOn ? onTrackColor : offTrackColor;
            }

            if (knob != null)
            {
                Vector2 position = knob.anchoredPosition;
                position.x = isOn ? knobOffset : -knobOffset;
                knob.anchoredPosition = position;
            }

            if (stateText != null)
            {
                stateText.text = isOn ? "ON" : "OFF";
                stateText.alignment = isOn
                    ? TextAnchor.MiddleLeft
                    : TextAnchor.MiddleRight;
            }
        }
    }
}
