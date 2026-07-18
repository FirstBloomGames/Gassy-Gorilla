using FirstBloom.ArcadeFramework.Accessibility;
using FirstBloom.ArcadeFramework.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace FirstBloom.ArcadeFramework.UI
{
    public class ArcadeSettingsMenu : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider voiceSlider;
        [SerializeField] private Toggle reducedMotionToggle;
        [SerializeField] private Toggle hapticsToggle;
        [SerializeField] private bool startClosed = true;

        private bool initialized;

        public bool IsVisible
        {
            get { return panelGroup != null && panelGroup.alpha > 0.5f; }
        }

        public bool HasAccessibilityControls
        {
            get { return reducedMotionToggle != null && hapticsToggle != null; }
        }

        private void Awake()
        {
            if (panelGroup == null)
            {
                panelGroup = GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            BindSliders();
            SyncFromAudio();
            SyncFromAccessibility();

            if (startClosed)
            {
                CloseInstant();
            }
        }

        private void BindSliders()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            if (masterSlider != null)
            {
                masterSlider.onValueChanged.AddListener(SetMasterVolume);
            }

            if (musicSlider != null)
            {
                musicSlider.onValueChanged.AddListener(SetMusicVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.AddListener(SetSfxVolume);
            }

            if (voiceSlider != null)
            {
                voiceSlider.onValueChanged.AddListener(SetVoiceVolume);
            }

            if (reducedMotionToggle != null)
            {
                reducedMotionToggle.onValueChanged.AddListener(SetReducedMotion);
            }

            if (hapticsToggle != null)
            {
                hapticsToggle.onValueChanged.AddListener(SetHapticsEnabled);
            }
        }

        public void Open()
        {
            SyncFromAudio();
            SyncFromAccessibility();
            SetVisible(true);
        }

        public void Close()
        {
            SetVisible(false);
        }

        public void Toggle()
        {
            bool currentlyVisible = panelGroup != null && panelGroup.alpha > 0.5f;
            SetVisible(!currentlyVisible);
        }

        private void CloseInstant()
        {
            SetVisible(false);
        }

        private void SyncFromAudio()
        {
            ArcadeAudioManager audioManager = ArcadeAudioManager.Instance;
            if (audioManager == null)
            {
                return;
            }

            if (masterSlider != null)
            {
                masterSlider.SetValueWithoutNotify(audioManager.MasterVolume);
            }

            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(audioManager.MusicVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(audioManager.SfxVolume);
            }

            if (voiceSlider != null)
            {
                voiceSlider.SetValueWithoutNotify(audioManager.VoiceVolume);
            }
        }

        private void SyncFromAccessibility()
        {
            if (reducedMotionToggle != null)
            {
                SyncToggle(
                    reducedMotionToggle,
                    ArcadeAccessibilitySettings.ReducedMotion);
            }

            if (hapticsToggle != null)
            {
                SyncToggle(
                    hapticsToggle,
                    ArcadeAccessibilitySettings.HapticsEnabled);
            }
        }

        private static void SyncToggle(Toggle toggle, bool value)
        {
            toggle.SetIsOnWithoutNotify(value);
            ArcadeToggleVisual visual = toggle.GetComponent<ArcadeToggleVisual>();
            if (visual != null)
            {
                visual.Refresh();
            }
        }

        private void SetVisible(bool visible)
        {
            if (panelGroup == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            panelGroup.alpha = visible ? 1f : 0f;
            panelGroup.interactable = visible;
            panelGroup.blocksRaycasts = visible;
        }

        private void SetMasterVolume(float value)
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.SetMasterVolume(value);
            }
        }

        private void SetMusicVolume(float value)
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.SetMusicVolume(value);
            }
        }

        private void SetSfxVolume(float value)
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.SetSfxVolume(value);
            }
        }

        private void SetVoiceVolume(float value)
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.SetVoiceVolume(value);
            }
        }

        private static void SetReducedMotion(bool value)
        {
            ArcadeAccessibilitySettings.SetReducedMotion(value);
        }

        private static void SetHapticsEnabled(bool value)
        {
            ArcadeAccessibilitySettings.SetHapticsEnabled(value);
        }
    }
}
