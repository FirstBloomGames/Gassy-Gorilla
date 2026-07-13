using UnityEngine;
using UnityEngine.UI;

namespace FirstBloom.ArcadeFramework.UI
{
    public class MeterFillUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Text valueLabel;
        [SerializeField] private Color normalColor = new Color(0.35f, 0.95f, 0.45f, 1f);
        [SerializeField] private Color lowColor = new Color(1f, 0.28f, 0.16f, 1f);
        [SerializeField] private Color fullColor = new Color(0.35f, 0.9f, 1f, 1f);
        [SerializeField] private float lowThreshold = 0.25f;
        [SerializeField] private float smoothSpeed = 14f;

        private float targetValue = 1f;
        private float currentValue = 1f;
        private float currentAmount;
        private float maxAmount = 1f;

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
                valueLabel.text = Mathf.RoundToInt(currentAmount) + " / " + Mathf.RoundToInt(maxAmount);
            }
        }

        private void ApplyVisuals(float value)
        {
            if (fillImage == null)
            {
                return;
            }

            fillImage.fillAmount = value;

            if (value <= lowThreshold)
            {
                float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 12f) * 0.5f;
                fillImage.color = Color.Lerp(lowColor, Color.white, pulse * 0.18f);
            }
            else if (value >= 0.98f)
            {
                float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 4f) * 0.5f;
                fillImage.color = Color.Lerp(fullColor, Color.white, pulse * 0.12f);
            }
            else
            {
                fillImage.color = normalColor;
            }
        }
    }
}
