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
        [SerializeField] private bool startClosed = true;

        private bool initialized;

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
        }

        public void Open()
        {
            SyncFromAudio();
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
    }
}
