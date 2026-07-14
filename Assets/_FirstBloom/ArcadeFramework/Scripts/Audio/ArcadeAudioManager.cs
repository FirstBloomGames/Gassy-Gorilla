using System.Collections.Generic;
using UnityEngine;

namespace FirstBloom.ArcadeFramework.Audio
{
    [DefaultExecutionOrder(-100)]
    public class ArcadeAudioManager : MonoBehaviour
    {
        private const string MasterVolumeKey = "FirstBloom_MasterVolume";
        private const string MusicVolumeKey = "FirstBloom_MusicVolume";
        private const string SfxVolumeKey = "FirstBloom_SfxVolume";
        private const string VoiceVolumeKey = "FirstBloom_VoiceVolume";

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource voiceSource;

        [Header("Music")]
        [SerializeField] private AudioClip musicClip;
        [SerializeField] private bool playMusicOnStart = true;
        [SerializeField] private bool generatePlaceholderMusic = true;

        [Header("Volumes")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float voiceVolume = 1f;

        private readonly Dictionary<ArcadeSfxType, AudioClip> generatedSfx = new Dictionary<ArcadeSfxType, AudioClip>();

        public static ArcadeAudioManager Instance { get; private set; }

        public float MasterVolume { get { return masterVolume; } }
        public float MusicVolume { get { return musicVolume; } }
        public float SfxVolume { get { return sfxVolume; } }
        public float VoiceVolume { get { return voiceVolume; } }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureSources();
            LoadSettings();
            ApplyVolumes();

            if (musicClip == null && generatePlaceholderMusic)
            {
                musicClip = CreatePlaceholderMusic();
            }
        }

        private void Start()
        {
            if (playMusicOnStart)
            {
                PlayMusic(musicClip);
            }
        }

        private void EnsureSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }

            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
            }

            musicSource.loop = true;
            musicSource.playOnAwake = false;
            sfxSource.playOnAwake = false;
            voiceSource.playOnAwake = false;
        }

        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, masterVolume);
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
            voiceVolume = PlayerPrefs.GetFloat(VoiceVolumeKey, voiceVolume);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null)
            {
                return;
            }

            if (musicSource.clip == clip && musicSource.isPlaying)
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitchJitter = 0.04f)
        {
            if (clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.pitch = Random.Range(1f - pitchJitter, 1f + pitchJitter);
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
            sfxSource.pitch = 1f;
        }

        public void PlaySfx(ArcadeSfxType type, float volumeScale = 1f)
        {
            PlaySfx(GetGeneratedSfx(type), volumeScale, 0.02f);
        }

        public void PlayVoice(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || voiceSource == null)
            {
                return;
            }

            voiceSource.Stop();
            voiceSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            ApplyVolumesAndSave();
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            ApplyVolumesAndSave();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            ApplyVolumesAndSave();
        }

        public void SetVoiceVolume(float value)
        {
            voiceVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VoiceVolumeKey, voiceVolume);
            ApplyVolumesAndSave();
        }

        private void ApplyVolumesAndSave()
        {
            ApplyVolumes();
            PlayerPrefs.Save();
        }

        private void ApplyVolumes()
        {
            if (musicSource != null)
            {
                musicSource.volume = masterVolume * musicVolume;
            }

            if (sfxSource != null)
            {
                sfxSource.volume = masterVolume * sfxVolume;
            }

            if (voiceSource != null)
            {
                voiceSource.volume = masterVolume * voiceVolume;
            }
        }

        private AudioClip GetGeneratedSfx(ArcadeSfxType type)
        {
            AudioClip clip;
            if (generatedSfx.TryGetValue(type, out clip))
            {
                return clip;
            }

            clip = CreateSfx(type);
            generatedSfx.Add(type, clip);
            return clip;
        }

        private static AudioClip CreateSfx(ArcadeSfxType type)
        {
            int sampleRate = 22050;
            float duration = 0.18f;
            float startFrequency = 180f;
            float endFrequency = 420f;
            float noise = 0.02f;

            if (type == ArcadeSfxType.Pickup)
            {
                duration = 0.14f;
                startFrequency = 720f;
                endFrequency = 1120f;
                noise = 0.005f;
            }
            else if (type == ArcadeSfxType.VineGrab)
            {
                duration = 0.22f;
                startFrequency = 380f;
                endFrequency = 620f;
                noise = 0.01f;
            }
            else if (type == ArcadeSfxType.VineRelease)
            {
                duration = 0.2f;
                startFrequency = 620f;
                endFrequency = 300f;
                noise = 0.01f;
            }
            else if (type == ArcadeSfxType.Crash)
            {
                duration = 0.3f;
                startFrequency = 160f;
                endFrequency = 70f;
                noise = 0.12f;
            }
            else if (type == ArcadeSfxType.UiClick)
            {
                duration = 0.08f;
                startFrequency = 600f;
                endFrequency = 740f;
                noise = 0f;
            }
            else if (type == ArcadeSfxType.Splash)
            {
                duration = 0.42f;
                startFrequency = 230f;
                endFrequency = 82f;
                noise = 0.2f;
            }
            else if (type == ArcadeSfxType.Chomp)
            {
                duration = 0.24f;
                startFrequency = 185f;
                endFrequency = 58f;
                noise = 0.11f;
            }

            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                phase += frequency * Mathf.PI * 2f / sampleRate;
                float envelope = Mathf.Sin(Mathf.Clamp01(1f - t) * Mathf.PI * 0.5f);
                float tone = Mathf.Sin(phase) * 0.35f;
                float hiss = Random.Range(-noise, noise);
                data[i] = (tone + hiss) * envelope;
            }

            AudioClip clip = AudioClip.Create("Generated_" + type, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreatePlaceholderMusic()
        {
            int sampleRate = 22050;
            int seconds = 8;
            int samples = sampleRate * seconds;
            float[] data = new float[samples];
            float[] notes = { 196f, 247f, 294f, 330f, 392f, 330f, 294f, 247f };

            for (int i = 0; i < samples; i++)
            {
                float time = i / (float)sampleRate;
                int noteIndex = Mathf.FloorToInt(time * 2f) % notes.Length;
                float note = notes[noteIndex];
                float lead = Mathf.Sin(time * note * Mathf.PI * 2f) * 0.045f;
                float pad = Mathf.Sin(time * note * 0.5f * Mathf.PI * 2f) * 0.025f;
                data[i] = lead + pad;
            }

            AudioClip clip = AudioClip.Create("Generated_FirstBloom_JungleLoop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
