using FirstBloom.ArcadeFramework.UI;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FirstBloom.Games.GassyGorilla
{
    public class FartBarUI : MonoBehaviour
    {
        [SerializeField] private GorillaController gorilla;
        [SerializeField] private MeterFillUI meter;
        [SerializeField] private Text titleText;
        [SerializeField] private Image iconImage;
        [SerializeField] private float lowFuelThreshold = 0.25f;
        [SerializeField] private float warningPulseCooldown = 0.35f;
        [SerializeField] private float warningShakePixels = 7f;
        [SerializeField] private Color normalIconColor = Color.white;
        [SerializeField] private Color lowIconColor = new Color(1f, 0.72f, 0.24f, 1f);

        private RectTransform rectTransform;
        private Coroutine pulseRoutine;
        private float lastFuel = -1f;
        private float nextWarningPulseTime;
        private bool wasLowFuel;

        private void Awake()
        {
            if (meter == null)
            {
                meter = GetComponentInChildren<MeterFillUI>();
            }

            rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (gorilla != null)
            {
                gorilla.FuelChanged += HandleFuelChanged;
                gorilla.BoostFailed += HandleBoostFailed;
            }
        }

        private void OnDisable()
        {
            if (gorilla != null)
            {
                gorilla.FuelChanged -= HandleFuelChanged;
                gorilla.BoostFailed -= HandleBoostFailed;
            }
        }

        public void SetGorilla(GorillaController value)
        {
            if (gorilla != null)
            {
                gorilla.FuelChanged -= HandleFuelChanged;
                gorilla.BoostFailed -= HandleBoostFailed;
            }

            gorilla = value;

            if (isActiveAndEnabled && gorilla != null)
            {
                gorilla.FuelChanged += HandleFuelChanged;
                gorilla.BoostFailed += HandleBoostFailed;
                HandleFuelChanged(gorilla.CurrentFuel, gorilla.MaxFuel);
            }
        }

        private void HandleFuelChanged(float current, float max)
        {
            if (meter != null)
            {
                meter.SetValue(current, max);
            }

            if (titleText != null)
            {
                float normalized = max <= 0f ? 0f : current / max;
                bool isLowFuel = normalized <= lowFuelThreshold;
                titleText.text = isLowFuel ? "LOW FUEL" : "FART FUEL";
                titleText.color = isLowFuel ? new Color(1f, 0.72f, 0.24f, 1f) : Color.white;
                if (iconImage != null)
                {
                    iconImage.color = isLowFuel ? lowIconColor : normalIconColor;
                }

                if (isLowFuel && !wasLowFuel && Time.unscaledTime >= nextWarningPulseTime)
                {
                    nextWarningPulseTime = Time.unscaledTime + warningPulseCooldown;
                    PlayPulse(1.1f, true);
                }

                wasLowFuel = isLowFuel;
            }

            if (lastFuel >= 0f && Mathf.Abs(current - lastFuel) > 0.1f)
            {
                float pulseScale = current > lastFuel ? 1.07f : 0.97f;
                PlayPulse(pulseScale, false);
            }

            lastFuel = current;
        }

        private void HandleBoostFailed()
        {
            if (Time.unscaledTime < nextWarningPulseTime)
            {
                return;
            }

            nextWarningPulseTime = Time.unscaledTime + warningPulseCooldown;
            PlayPulse(1.12f, true);
        }

        private void PlayPulse(float pulseScale, bool shake)
        {
            if (rectTransform == null)
            {
                return;
            }

            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
            }

            pulseRoutine = StartCoroutine(PulseRoutine(pulseScale, shake));
        }

        private IEnumerator PulseRoutine(float pulseScale, bool shake)
        {
            Vector3 original = Vector3.one;
            Vector2 originalPosition = rectTransform.anchoredPosition;
            Vector3 target = new Vector3(pulseScale, pulseScale, 1f);
            float halfDuration = 0.08f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                rectTransform.localScale = Vector3.Lerp(original, target, t);
                ApplyWarningShake(originalPosition, t, shake);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                rectTransform.localScale = Vector3.Lerp(target, original, t);
                ApplyWarningShake(originalPosition, 1f - t, shake);
                yield return null;
            }

            rectTransform.localScale = original;
            rectTransform.anchoredPosition = originalPosition;
            pulseRoutine = null;
        }

        private void ApplyWarningShake(Vector2 originalPosition, float strength, bool shake)
        {
            if (!shake)
            {
                return;
            }

            float wobble = Mathf.Sin(Time.unscaledTime * 54f) * warningShakePixels * Mathf.Clamp01(strength);
            rectTransform.anchoredPosition = originalPosition + new Vector2(wobble, 0f);
        }
    }
}
