using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstBloom.ArcadeFramework.Audio
{
    [DefaultExecutionOrder(-100)]
    public class ArcadeAudioManager : MonoBehaviour
    {
        private const string MasterVolumeKey = "FirstBloom_MasterVolume";
        private const string MusicVolumeKey = "FirstBloom_MusicVolume";
        private const string SfxVolumeKey = "FirstBloom_SfxVolume";
        private const string VoiceVolumeKey = "FirstBloom_VoiceVolume";

        [Header("Library")]
        [SerializeField] private ArcadeAudioLibrary audioLibrary;

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource intensityMusicSource;
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource voiceSource;

        [Header("Music")]
        [SerializeField] private AudioClip musicClip;
        [SerializeField] private bool playMusicOnStart = true;
        [SerializeField] private bool generatePlaceholderMusic = true;
        [Min(0.01f)] [SerializeField] private float musicIntensityResponse = 1.6f;
        [Range(0f, 1f)] [SerializeField] private float voiceDuckMultiplier = 0.42f;
        [Min(0.01f)] [SerializeField] private float voiceDuckResponse = 8f;

        [Header("Sound Effects")]
        [Range(4, 16)] [SerializeField] private int sfxVoiceCount = 8;
        [Range(0f, 1f)] [SerializeField] private float sfxMixHeadroom = 0.72f;

        [Header("Volumes")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float voiceVolume = 1f;

        private readonly Dictionary<ArcadeSfxType, AudioClip> generatedSfx = new Dictionary<ArcadeSfxType, AudioClip>();
        private readonly Dictionary<ArcadeSfxType, int> lastClipIndices = new Dictionary<ArcadeSfxType, int>();
        private readonly Dictionary<ArcadeSfxType, float> lastSfxPlayTimes = new Dictionary<ArcadeSfxType, float>();
        private readonly Dictionary<ArcadeSfxType, int> playsSinceRareClip = new Dictionary<ArcadeSfxType, int>();
        private readonly Dictionary<ArcadeSfxType, LoopVoice> loopVoices = new Dictionary<ArcadeSfxType, LoopVoice>();
        private readonly List<VoiceSlot> sfxVoices = new List<VoiceSlot>();
        private int sfxVoiceCursor;
        private float targetMusicIntensity;
        private float currentMusicIntensity;
        private float currentMusicDuck = 1f;
        private float voiceGain = 1f;
        private bool musicRequested;
        private bool audioUnlocked;
        private bool audioQaEnabled;
        private bool audioRemixQaEnabled;

        public static ArcadeAudioManager Instance { get; private set; }

        public float MasterVolume { get { return masterVolume; } }
        public float MusicVolume { get { return musicVolume; } }
        public float SfxVolume { get { return sfxVolume; } }
        public float SfxMixHeadroom { get { return sfxMixHeadroom; } }
        public float VoiceVolume { get { return voiceVolume; } }
        public float MusicIntensity { get { return currentMusicIntensity; } }
        public ArcadeAudioLibrary AudioLibrary { get { return audioLibrary; } }
        public bool UsesGeneratedPlaceholderMusic { get { return generatePlaceholderMusic; } }

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
            EnsureSfxVoicePool();
            LoadSettings();
            ApplyVolumes();

#if UNITY_WEBGL && !UNITY_EDITOR
            audioUnlocked = false;
            audioQaEnabled = Application.absoluteURL.Contains("qa-audio");
            audioRemixQaEnabled = Application.absoluteURL.Contains("qa-audio-remix");
#else
            audioUnlocked = true;
#endif

            if (audioLibrary == null && musicClip == null && generatePlaceholderMusic)
            {
                musicClip = CreatePlaceholderMusic();
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (playMusicOnStart)
            {
                RequestMusicStart();
            }
        }

        private void Update()
        {
            currentMusicIntensity = Mathf.MoveTowards(
                currentMusicIntensity,
                targetMusicIntensity,
                musicIntensityResponse * Time.unscaledDeltaTime);

            float targetDuck = voiceSource != null && voiceSource.isPlaying ? voiceDuckMultiplier : 1f;
            currentMusicDuck = Mathf.MoveTowards(
                currentMusicDuck,
                targetDuck,
                voiceDuckResponse * Time.unscaledDeltaTime);
            ApplyMusicVolumes();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void NotifyUserGesture()
        {
            if (audioUnlocked)
            {
                return;
            }

            audioUnlocked = true;
            AudioListener.pause = false;
            TryStartMusicSet();
            if (audioQaEnabled)
            {
                StartCoroutine(LogAudioQaAfterUnlock());
            }

            if (audioRemixQaEnabled)
            {
                StartCoroutine(RunAudioRemixQa());
            }
        }

        public void RequestMusicStart()
        {
            musicRequested = true;
            TryStartMusicSet();
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            musicClip = clip;
            musicRequested = true;
            if (musicSource != null && musicSource.clip != clip)
            {
                musicSource.Stop();
            }

            TryStartMusicSet();
        }

        public void SetMusicIntensity(float intensity)
        {
            targetMusicIntensity = Mathf.Clamp01(intensity);
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitchJitter = 0.04f)
        {
            if (clip == null || !audioUnlocked)
            {
                return;
            }

            float pitch = Random.Range(1f - pitchJitter, 1f + pitchJitter);
            PlaySfxClip(clip, Mathf.Clamp01(volumeScale), pitch);
        }

        public void PlaySfx(ArcadeSfxType type, float volumeScale = 1f)
        {
            PlaySfx(type, volumeScale, 1f);
        }

        public void PlaySfx(ArcadeSfxType type, float volumeScale, float pitchScale)
        {
            if (!audioUnlocked)
            {
                return;
            }

            if (audioLibrary != null && audioLibrary.TryGetEntry(type, out ArcadeSfxEntry entry))
            {
                if (!CanRetrigger(type, entry))
                {
                    return;
                }

                if (!TrySelectLibraryClip(type, entry, out AudioClip clip, out int clipIndex))
                {
                    return;
                }

                Vector2 pitchRange = entry.PitchRange;
                float pitch = Random.Range(pitchRange.x, pitchRange.y) * Mathf.Clamp(pitchScale, 0.5f, 2f);
                float gain = Mathf.Clamp(volumeScale * entry.Volume, 0f, 1.5f);
                if (PlaySfxClip(type, entry, clip, clipIndex, gain, pitch))
                {
                    RecordSfxPlayback(type, entry, clipIndex, gain, pitch);
                }

                return;
            }

            PlaySfxClip(GetGeneratedSfx(type), Mathf.Clamp01(volumeScale), Mathf.Clamp(pitchScale, 0.5f, 2f));
        }

        public void StartSfxLoop(ArcadeSfxType type, float volumeScale = 1f)
        {
            if (!audioUnlocked ||
                audioLibrary == null ||
                !audioLibrary.TryGetEntry(type, out ArcadeSfxEntry entry) ||
                !TrySelectLibraryClip(type, entry, out AudioClip clip, out int _))
            {
                return;
            }

            if (!loopVoices.TryGetValue(type, out LoopVoice loopVoice) || loopVoice.Source == null)
            {
                GameObject sourceObject = new GameObject("Loop_" + type);
                sourceObject.transform.SetParent(transform, false);
                AudioSource source = sourceObject.AddComponent<AudioSource>();
                ConfigureSource(source, true);
                loopVoice = new LoopVoice(source);
                loopVoices[type] = loopVoice;
            }

            if (loopVoice.StopRoutine != null)
            {
                StopCoroutine(loopVoice.StopRoutine);
                loopVoice.StopRoutine = null;
            }

            loopVoice.Gain = Mathf.Clamp(volumeScale * entry.Volume, 0f, 1.5f);
            loopVoice.Source.clip = clip;
            loopVoice.Source.loop = true;
            Vector2 pitchRange = entry.PitchRange;
            loopVoice.Source.pitch = Random.Range(pitchRange.x, pitchRange.y);
            loopVoice.Source.volume = GetSfxOutputVolume(loopVoice.Gain);
            if (!loopVoice.Source.isPlaying)
            {
                loopVoice.Source.Play();
            }
        }

        public void SetSfxLoopParameters(ArcadeSfxType type, float volumeScale, float pitch)
        {
            if (!loopVoices.TryGetValue(type, out LoopVoice loopVoice) || loopVoice.Source == null)
            {
                return;
            }

            loopVoice.Gain = Mathf.Clamp(volumeScale, 0f, 1.5f);
            loopVoice.Source.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            loopVoice.Source.volume = GetSfxOutputVolume(loopVoice.Gain);
        }

        public void StopSfxLoop(ArcadeSfxType type, float fadeDuration = 0.08f)
        {
            if (!loopVoices.TryGetValue(type, out LoopVoice loopVoice) || loopVoice.Source == null)
            {
                return;
            }

            if (loopVoice.StopRoutine != null)
            {
                StopCoroutine(loopVoice.StopRoutine);
            }

            if (fadeDuration <= 0f)
            {
                loopVoice.Source.Stop();
                loopVoice.StopRoutine = null;
                return;
            }

            loopVoice.StopRoutine = StartCoroutine(FadeAndStopLoop(loopVoice, fadeDuration));
        }

        public void StopAllSfxLoops(float fadeDuration = 0f)
        {
            List<ArcadeSfxType> types = new List<ArcadeSfxType>(loopVoices.Keys);
            for (int i = 0; i < types.Count; i++)
            {
                StopSfxLoop(types[i], fadeDuration);
            }
        }

        public void PlayVoice(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || voiceSource == null || !audioUnlocked)
            {
                return;
            }

            voiceGain = Mathf.Clamp01(volumeScale);
            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.pitch = 1f;
            voiceSource.volume = masterVolume * voiceVolume * voiceGain;
            voiceSource.Play();
            LogAudioQaPlayback("voice", voiceSource, clip);
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

        private void EnsureSources()
        {
            musicSource = EnsureSource(musicSource, "Music_Base", true);
            intensityMusicSource = EnsureSource(intensityMusicSource, "Music_Intensity", true);
            ambienceSource = EnsureSource(ambienceSource, "Music_Ambience", true);
            sfxSource = EnsureSource(sfxSource, "SFX_Voice_00", false);
            voiceSource = EnsureSource(voiceSource, "Voice", false);
        }

        private AudioSource EnsureSource(AudioSource source, string childName, bool loop)
        {
            if (source == null)
            {
                GameObject sourceObject = new GameObject(childName);
                sourceObject.transform.SetParent(transform, false);
                source = sourceObject.AddComponent<AudioSource>();
            }

            ConfigureSource(source, loop);
            return source;
        }

        private static void ConfigureSource(AudioSource source, bool loop)
        {
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
        }

        private void EnsureSfxVoicePool()
        {
            sfxVoices.Clear();
            sfxVoices.Add(new VoiceSlot(sfxSource));
            int count = Mathf.Clamp(sfxVoiceCount, 4, 16);
            for (int i = 1; i < count; i++)
            {
                GameObject sourceObject = new GameObject("SFX_Voice_" + i.ToString("D2"));
                sourceObject.transform.SetParent(transform, false);
                AudioSource source = sourceObject.AddComponent<AudioSource>();
                ConfigureSource(source, false);
                sfxVoices.Add(new VoiceSlot(source));
            }
        }

        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, masterVolume);
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
            voiceVolume = PlayerPrefs.GetFloat(VoiceVolumeKey, voiceVolume);
        }

        private void ApplyVolumesAndSave()
        {
            ApplyVolumes();
            PlayerPrefs.Save();
        }

        private void ApplyVolumes()
        {
            ApplyMusicVolumes();

            for (int i = 0; i < sfxVoices.Count; i++)
            {
                VoiceSlot slot = sfxVoices[i];
                if (slot.Source != null)
                {
                    slot.Source.volume = GetSfxOutputVolume(slot.Gain);
                }
            }

            foreach (LoopVoice loopVoice in loopVoices.Values)
            {
                if (loopVoice.Source != null)
                {
                    loopVoice.Source.volume = GetSfxOutputVolume(loopVoice.Gain);
                }
            }

            if (voiceSource != null)
            {
                voiceSource.volume = masterVolume * voiceVolume * voiceGain;
            }
        }

        private void ApplyMusicVolumes()
        {
            float baseGain = audioLibrary != null ? audioLibrary.BaseMusicGain : 1f;
            float intensityGain = audioLibrary != null ? audioLibrary.IntensityMusicGain : 0f;
            float ambienceGain = audioLibrary != null ? audioLibrary.AmbienceGain : 0f;
            float busVolume = masterVolume * musicVolume * currentMusicDuck;

            if (musicSource != null)
            {
                musicSource.volume = busVolume * baseGain;
            }

            if (intensityMusicSource != null)
            {
                intensityMusicSource.volume = busVolume * intensityGain * currentMusicIntensity;
            }

            if (ambienceSource != null)
            {
                ambienceSource.volume = busVolume * ambienceGain;
            }
        }

        private void TryStartMusicSet()
        {
            if (!musicRequested || !audioUnlocked || musicSource == null)
            {
                return;
            }

            AudioClip baseClip = audioLibrary != null && audioLibrary.BaseMusic != null
                ? audioLibrary.BaseMusic
                : musicClip;
            AudioClip intensityClip = audioLibrary != null ? audioLibrary.IntensityMusic : null;
            AudioClip ambienceClip = audioLibrary != null ? audioLibrary.Ambience : null;
            if (baseClip == null)
            {
                return;
            }

            if (musicSource.isPlaying && musicSource.clip == baseClip)
            {
                return;
            }

            musicSource.Stop();
            intensityMusicSource.Stop();
            ambienceSource.Stop();
            musicSource.clip = baseClip;
            intensityMusicSource.clip = intensityClip;
            ambienceSource.clip = ambienceClip;
            musicSource.loop = true;
            intensityMusicSource.loop = true;
            ambienceSource.loop = true;
            ApplyMusicVolumes();

            double startTime = AudioSettings.dspTime + 0.08d;
            musicSource.PlayScheduled(startTime);
            if (intensityClip != null)
            {
                intensityMusicSource.PlayScheduled(startTime);
            }

            if (ambienceClip != null)
            {
                ambienceSource.PlayScheduled(startTime);
            }
        }

        private bool CanRetrigger(ArcadeSfxType type, ArcadeSfxEntry entry)
        {
            float interval = entry != null ? entry.MinimumRetriggerInterval : 0f;
            return interval <= 0f ||
                !lastSfxPlayTimes.TryGetValue(type, out float lastPlayTime) ||
                Time.unscaledTime - lastPlayTime >= interval;
        }

        private bool TrySelectLibraryClip(
            ArcadeSfxType type,
            ArcadeSfxEntry entry,
            out AudioClip clip,
            out int selectedIndex)
        {
            clip = null;
            selectedIndex = -1;
            AudioClip[] clips = entry.Clips;
            if (clips == null || clips.Length == 0)
            {
                return false;
            }

            int rareIndex = entry.RareClipIndex >= 0 && entry.RareClipIndex < clips.Length
                ? entry.RareClipIndex
                : -1;
            int playsSinceRare = rareIndex >= 0 && playsSinceRareClip.TryGetValue(type, out int trackedPlays)
                ? trackedPlays
                : entry.RareClipCooldownPlays;
            bool rareAllowed = rareIndex < 0 ||
                entry.RareClipCooldownPlays <= 0 ||
                playsSinceRare >= entry.RareClipCooldownPlays;
            int excludedIndex = rareAllowed ? -1 : rareIndex;
            int eligibleCount = clips.Length - (excludedIndex >= 0 ? 1 : 0);
            if (eligibleCount <= 0)
            {
                return false;
            }

            int selectedOrdinal = Random.Range(0, eligibleCount);
            selectedIndex = GetEligibleClipIndex(selectedOrdinal, excludedIndex);
            if (eligibleCount > 1 &&
                lastClipIndices.TryGetValue(type, out int previousIndex) &&
                selectedIndex == previousIndex)
            {
                int previousOrdinal = GetEligibleClipOrdinal(previousIndex, excludedIndex);
                int offset = Random.Range(1, eligibleCount);
                selectedOrdinal = (previousOrdinal + offset) % eligibleCount;
                selectedIndex = GetEligibleClipIndex(selectedOrdinal, excludedIndex);
            }

            lastClipIndices[type] = selectedIndex;
            clip = clips[selectedIndex];
            return clip != null;
        }

        private static int GetEligibleClipIndex(int ordinal, int excludedIndex)
        {
            return excludedIndex >= 0 && ordinal >= excludedIndex ? ordinal + 1 : ordinal;
        }

        private static int GetEligibleClipOrdinal(int clipIndex, int excludedIndex)
        {
            return excludedIndex >= 0 && clipIndex > excludedIndex ? clipIndex - 1 : clipIndex;
        }

        private bool PlaySfxClip(
            ArcadeSfxType type,
            ArcadeSfxEntry entry,
            AudioClip clip,
            int clipIndex,
            float gain,
            float pitch)
        {
            VoiceSlot selected = SelectSfxVoice(type, entry);
            if (selected == null)
            {
                return false;
            }

            PlaySfxOnVoice(selected, clip, gain, pitch, true, type, clipIndex);
            return true;
        }

        private void PlaySfxClip(AudioClip clip, float gain, float pitch)
        {
            if (clip == null || sfxVoices.Count == 0)
            {
                return;
            }

            VoiceSlot selected = null;
            for (int i = 0; i < sfxVoices.Count; i++)
            {
                int index = (sfxVoiceCursor + i) % sfxVoices.Count;
                VoiceSlot candidate = sfxVoices[index];
                if (candidate.Source != null && !candidate.Source.isPlaying)
                {
                    selected = candidate;
                    sfxVoiceCursor = (index + 1) % sfxVoices.Count;
                    break;
                }
            }

            if (selected == null)
            {
                selected = sfxVoices[sfxVoiceCursor];
                sfxVoiceCursor = (sfxVoiceCursor + 1) % sfxVoices.Count;
            }

            if (selected.Source == null)
            {
                return;
            }

            PlaySfxOnVoice(selected, clip, gain, pitch, false, default, -1);
        }

        private VoiceSlot SelectSfxVoice(ArcadeSfxType type, ArcadeSfxEntry entry)
        {
            int maximumVoices = entry != null ? entry.MaximumSimultaneousVoices : 0;
            if (maximumVoices > 0)
            {
                int activeFamilyVoices = 0;
                VoiceSlot oldestFamilyVoice = null;
                for (int i = 0; i < sfxVoices.Count; i++)
                {
                    VoiceSlot slot = sfxVoices[i];
                    if (slot.Source == null || !slot.Source.isPlaying || !slot.HasType || slot.Type != type)
                    {
                        continue;
                    }

                    activeFamilyVoices++;
                    if (oldestFamilyVoice == null || slot.StartedAt < oldestFamilyVoice.StartedAt)
                    {
                        oldestFamilyVoice = slot;
                    }
                }

                if (activeFamilyVoices >= maximumVoices)
                {
                    return entry.VoiceLimitMode == ArcadeSfxVoiceLimitMode.ReplaceOldest
                        ? oldestFamilyVoice
                        : null;
                }
            }

            for (int i = 0; i < sfxVoices.Count; i++)
            {
                int index = (sfxVoiceCursor + i) % sfxVoices.Count;
                VoiceSlot candidate = sfxVoices[index];
                if (candidate.Source != null && !candidate.Source.isPlaying)
                {
                    sfxVoiceCursor = (index + 1) % sfxVoices.Count;
                    return candidate;
                }
            }

            VoiceSlot selected = sfxVoices[sfxVoiceCursor];
            sfxVoiceCursor = (sfxVoiceCursor + 1) % sfxVoices.Count;
            return selected;
        }

        private void PlaySfxOnVoice(
            VoiceSlot selected,
            AudioClip clip,
            float gain,
            float pitch,
            bool hasType,
            ArcadeSfxType type,
            int clipIndex)
        {
            if (selected == null || selected.Source == null || clip == null)
            {
                return;
            }

            selected.Source.Stop();
            selected.Source.clip = clip;
            selected.Source.loop = false;
            selected.Source.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            selected.Gain = Mathf.Clamp(gain, 0f, 1.5f);
            selected.HasType = hasType;
            selected.Type = type;
            selected.ClipIndex = clipIndex;
            selected.StartedAt = Time.unscaledTime;
            selected.Source.volume = GetSfxOutputVolume(selected.Gain);
            selected.Source.Play();
            LogAudioQaPlayback("sfx", selected.Source, clip);
        }

        private void RecordSfxPlayback(
            ArcadeSfxType type,
            ArcadeSfxEntry entry,
            int clipIndex,
            float gain,
            float pitch)
        {
            lastSfxPlayTimes[type] = Time.unscaledTime;
            if (entry.RareClipIndex >= 0 && entry.RareClipIndex < entry.Clips.Length)
            {
                int playsSinceRare = playsSinceRareClip.TryGetValue(type, out int trackedPlays)
                    ? trackedPlays
                    : entry.RareClipCooldownPlays;
                playsSinceRareClip[type] = clipIndex == entry.RareClipIndex
                    ? 0
                    : Mathf.Min(playsSinceRare + 1, entry.RareClipCooldownPlays);
            }

            LogSfxQaPolicy(type, entry, clipIndex, gain, pitch);
        }

        private void LogSfxQaPolicy(
            ArcadeSfxType type,
            ArcadeSfxEntry entry,
            int clipIndex,
            float gain,
            float pitch)
        {
            if (!audioQaEnabled)
            {
                return;
            }

            int rareCooldownRemaining = 0;
            if (entry.RareClipIndex >= 0 && entry.RareClipCooldownPlays > 0)
            {
                int playsSinceRare = playsSinceRareClip.TryGetValue(type, out int trackedPlays)
                    ? trackedPlays
                    : entry.RareClipCooldownPlays;
                rareCooldownRemaining = Mathf.Max(0, entry.RareClipCooldownPlays - playsSinceRare);
            }

            Debug.Log(
                "[GG_AUDIO_QA] sfxType=" + type +
                " variant=" + (clipIndex + 1) + "/" + entry.Clips.Length +
                " activeFamilyVoices=" + CountActiveSfxVoices(type) +
                " familyGain=" + gain.ToString("F2") +
                " mixHeadroom=" + sfxMixHeadroom.ToString("F2") +
                " slider=" + sfxVolume.ToString("F2") +
                " effectiveGain=" + GetSfxOutputVolume(gain).ToString("F2") +
                " pitch=" + pitch.ToString("F2") +
                " rareCooldownRemaining=" + rareCooldownRemaining);
        }

        private int CountActiveSfxVoices(ArcadeSfxType type)
        {
            int count = 0;
            for (int i = 0; i < sfxVoices.Count; i++)
            {
                VoiceSlot slot = sfxVoices[i];
                if (slot.Source != null && slot.Source.isPlaying && slot.HasType && slot.Type == type)
                {
                    count++;
                }
            }

            return count;
        }

        private IEnumerator LogAudioQaAfterUnlock()
        {
            yield return new WaitForSecondsRealtime(0.25f);

            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            Debug.Log(
                "[GG_AUDIO_QA] unlocked=" + audioUnlocked +
                " listeners=" + listeners.Length +
                " listenerPaused=" + AudioListener.pause +
                " master=" + masterVolume.ToString("F2") +
                " music=" + musicVolume.ToString("F2") +
                " sfx=" + sfxVolume.ToString("F2") +
                " voice=" + voiceVolume.ToString("F2") +
                " base=" + DescribeAudioSource(musicSource) +
                " intensity=" + DescribeAudioSource(intensityMusicSource) +
                " ambience=" + DescribeAudioSource(ambienceSource));
        }

        private IEnumerator RunAudioRemixQa()
        {
            yield return new WaitForSecondsRealtime(0.35f);
            Debug.Log("[GG_AUDIO_REMIX_QA] begin");

            float[] pickupPitches = { 1f, 1.05946f, 1.12246f, 1.18921f, 1.25992f };
            for (int i = 0; i < 8; i++)
            {
                PlaySfx(ArcadeSfxType.Pickup, 1f, pickupPitches[Mathf.Min(i, pickupPitches.Length - 1)]);
                yield return new WaitForSecondsRealtime(0.04f);
            }

            yield return new WaitForSecondsRealtime(0.3f);
            Random.State previousRandomState = Random.state;
            Random.InitState(20260717);
            for (int i = 0; i < 48; i++)
            {
                PlaySfx(ArcadeSfxType.Boost);
                yield return new WaitForSecondsRealtime(0.055f);
            }

            Random.state = previousRandomState;
            yield return new WaitForSecondsRealtime(0.35f);
            for (int i = 0; i < 6; i++)
            {
                PlaySfx(ArcadeSfxType.BoostFailed);
                yield return new WaitForSecondsRealtime(0.075f);
            }

            yield return new WaitForSecondsRealtime(0.45f);
            Debug.Log("[GG_AUDIO_REMIX_QA] complete");
        }

        private void LogAudioQaPlayback(string channel, AudioSource source, AudioClip clip)
        {
            if (!audioQaEnabled || source == null || clip == null)
            {
                return;
            }

            Debug.Log(
                "[GG_AUDIO_QA] channel=" + channel +
                " clip=" + clip.name +
                " load=" + clip.loadState +
                " playing=" + source.isPlaying +
                " volume=" + source.volume.ToString("F2"));
        }

        private static string DescribeAudioSource(AudioSource source)
        {
            if (source == null)
            {
                return "missing";
            }

            string clipDescription = source.clip == null
                ? "none"
                : source.clip.name + ":" + source.clip.loadState;
            return clipDescription + ":playing=" + source.isPlaying + ":volume=" + source.volume.ToString("F2");
        }

        private IEnumerator FadeAndStopLoop(LoopVoice loopVoice, float duration)
        {
            float startGain = loopVoice.Gain;
            float elapsed = 0f;
            while (elapsed < duration && loopVoice.Source != null)
            {
                elapsed += Time.unscaledDeltaTime;
                loopVoice.Gain = Mathf.Lerp(startGain, 0f, Mathf.Clamp01(elapsed / duration));
                loopVoice.Source.volume = GetSfxOutputVolume(loopVoice.Gain);
                yield return null;
            }

            if (loopVoice.Source != null)
            {
                loopVoice.Source.Stop();
            }

            loopVoice.Gain = 0f;
            loopVoice.StopRoutine = null;
        }

        private float GetSfxOutputVolume(float gain)
        {
            return masterVolume * sfxVolume * sfxMixHeadroom * gain;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StopAllSfxLoops(0f);
            SetMusicIntensity(0f);
        }

        private AudioClip GetGeneratedSfx(ArcadeSfxType type)
        {
            if (generatedSfx.TryGetValue(type, out AudioClip clip))
            {
                return clip;
            }

            clip = CreateSfx(type);
            generatedSfx.Add(type, clip);
            return clip;
        }

        private static AudioClip CreateSfx(ArcadeSfxType type)
        {
            const int sampleRate = 22050;
            float duration = 0.18f;
            float startFrequency = 180f;
            float endFrequency = 420f;
            float noise = 0.02f;

            switch (type)
            {
                case ArcadeSfxType.Pickup:
                case ArcadeSfxType.Milestone:
                    duration = 0.16f;
                    startFrequency = 720f;
                    endFrequency = 1120f;
                    noise = 0.005f;
                    break;
                case ArcadeSfxType.VineGrab:
                    duration = 0.22f;
                    startFrequency = 380f;
                    endFrequency = 620f;
                    noise = 0.01f;
                    break;
                case ArcadeSfxType.VineRelease:
                    duration = 0.2f;
                    startFrequency = 620f;
                    endFrequency = 300f;
                    noise = 0.01f;
                    break;
                case ArcadeSfxType.Crash:
                case ArcadeSfxType.GameOver:
                    duration = 0.3f;
                    startFrequency = 160f;
                    endFrequency = 70f;
                    noise = 0.12f;
                    break;
                case ArcadeSfxType.UiClick:
                case ArcadeSfxType.UiBack:
                case ArcadeSfxType.UiError:
                    duration = 0.08f;
                    startFrequency = 600f;
                    endFrequency = 740f;
                    noise = 0f;
                    break;
                case ArcadeSfxType.Splash:
                case ArcadeSfxType.CrocodileWarning:
                    duration = 0.42f;
                    startFrequency = 230f;
                    endFrequency = 82f;
                    noise = 0.2f;
                    break;
                case ArcadeSfxType.Chomp:
                    duration = 0.24f;
                    startFrequency = 185f;
                    endFrequency = 58f;
                    noise = 0.11f;
                    break;
                case ArcadeSfxType.BoostFailed:
                    duration = 0.16f;
                    startFrequency = 120f;
                    endFrequency = 55f;
                    noise = 0.08f;
                    break;
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

            AudioClip generated = AudioClip.Create("Generated_" + type, samples, 1, sampleRate, false);
            generated.SetData(data, 0);
            return generated;
        }

        private static AudioClip CreatePlaceholderMusic()
        {
            const int sampleRate = 22050;
            const int seconds = 8;
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

            AudioClip generated = AudioClip.Create("Generated_FirstBloom_JungleLoop", samples, 1, sampleRate, false);
            generated.SetData(data, 0);
            return generated;
        }

        private sealed class VoiceSlot
        {
            public readonly AudioSource Source;
            public float Gain = 1f;
            public bool HasType;
            public ArcadeSfxType Type;
            public int ClipIndex = -1;
            public float StartedAt;

            public VoiceSlot(AudioSource source)
            {
                Source = source;
            }
        }

        private sealed class LoopVoice
        {
            public readonly AudioSource Source;
            public float Gain = 1f;
            public Coroutine StopRoutine;

            public LoopVoice(AudioSource source)
            {
                Source = source;
            }
        }
    }
}
