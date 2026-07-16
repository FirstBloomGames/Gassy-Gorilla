using UnityEngine;
using UnityEngine.UI;

namespace FirstBloom.ArcadeFramework.UI
{
    public class MeterFillUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Image[] segmentImages;
        [SerializeField] private Text valueLabel;
        [SerializeField] private Color normalColor = new Color(0.35f, 0.95f, 0.45f, 1f);
        [SerializeField] private Color lowColor = new Color(1f, 0.28f, 0.16f, 1f);
        [SerializeField] private Color fullColor = new Color(0.35f, 0.9f, 1f, 1f);
        [SerializeField] private Color inactiveSegmentColor = new Color(0.08f, 0.14f, 0.12f, 0.82f);
        [SerializeField] private float lowThreshold = 0.25f;
        [SerializeField] private float smoothSpeed = 14f;
        [SerializeField] private bool showMaximum = true;

        private float targetValue = 1f;
        private float currentValue = 1f;
        private float currentAmount;
        private float maxAmount = 1f;

        public int SegmentCount { get { return segmentImages != null ? segmentImages.Length : 0; } }
        public bool ShowsMaximum { get { return showMaximum; } }

        private void Awake()
        {
            if (fillImage != null)
            {
                currentValue = fillImage.fillAmount;
                targetValue = currentValue;
            }
        }

        private void Update()
        {
            currentValue = Mathf.MoveTowards(currentValue, targetValue, smoothSpeed * Time.unscaledDeltaTime);
            ApplyVisuals(currentValue);
        }

        public void SetNormalized(float normalized)
        {
            targetValue = Mathf.Clamp01(normalized);
        }

        public void SetValue(float current, float max)
        {
            currentAmount = Mathf.Max(0f, current);
            maxAmount = Mathf.Max(0.01f, max);
            targetValue = Mathf.Clamp01(currentAmount / maxAmount);

            if (valueLabel != null)
            {
                int roundedCurrent = Mathf.RoundToInt(currentAmount);
                valueLabel.text = showMaximum
                    ? roundedCurrent + " / " + Mathf.RoundToInt(maxAmount)
                    : roundedCurrent.ToString();
            }
        }

        private void ApplyVisuals(float value)
        {
            Color activeColor = ResolveActiveColor(value);
            if (fillImage != null)
            {
                fillImage.fillAmount = value;
                Color glowColor = activeColor;
                glowColor.a *= 0.28f;
                fillImage.color = glowColor;
            }

            if (segmentImages == null || segmentImages.Length == 0)
            {
                return;
            }

            float scaledValue = value * segmentImages.Length;
            for (int i = 0; i < segmentImages.Length; i++)
            {
                Image segment = segmentImages[i];
                if (segment == null)
                {
                    continue;
                }

                float segmentFill = Mathf.Clamp01(scaledValue - i);
                float easedFill = Mathf.SmoothStep(0f, 1f, segmentFill);
                segment.color = Color.Lerp(inactiveSegmentColor, activeColor, easedFill);
            }
        }

        private Color ResolveActiveColor(float value)
        {
            if (value <= lowThreshold)
            {
                float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 12f) * 0.5f;
                return Color.Lerp(lowColor, Color.white, pulse * 0.18f);
            }

            if (value >= 0.98f)
            {
                float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 4f) * 0.5f;
                return Color.Lerp(fullColor, Color.white, pulse * 0.12f);
            }

            return normalColor;
        }
    }
}
