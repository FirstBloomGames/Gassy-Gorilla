using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstBloom.ArcadeFramework.Audio
{
    public enum ArcadeSfxVoiceLimitMode
    {
        ReplaceOldest,
        RejectNewest
    }

    [Serializable]
    public sealed class ArcadeSfxEntry
    {
        [SerializeField] private ArcadeSfxType type;
        [SerializeField] private AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1.5f)] [SerializeField] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.98f, 1.02f);
        [SerializeField] private bool loop;
        [Min(0)] [SerializeField] private int maximumSimultaneousVoices;
        [Min(0f)] [SerializeField] private float minimumRetriggerInterval;
        [SerializeField] private ArcadeSfxVoiceLimitMode voiceLimitMode = ArcadeSfxVoiceLimitMode.ReplaceOldest;
        [Min(-1)] [SerializeField] private int rareClipIndex = -1;
        [Min(0)] [SerializeField] private int rareClipCooldownPlays;

        public ArcadeSfxType Type { get { return type; } }
        public AudioClip[] Clips { get { return clips; } }
        public float Volume { get { return Mathf.Clamp(volume, 0f, 1.5f); } }
        public Vector2 PitchRange
        {
            get
            {
                return pitchRange.x <= pitchRange.y
                    ? pitchRange
                    : new Vector2(pitchRange.y, pitchRange.x);
            }
        }
        public bool Loop { get { return loop; } }
        public int MaximumSimultaneousVoices { get { return Mathf.Max(0, maximumSimultaneousVoices); } }
        public float MinimumRetriggerInterval { get { return Mathf.Max(0f, minimumRetriggerInterval); } }
        public ArcadeSfxVoiceLimitMode VoiceLimitMode { get { return voiceLimitMode; } }
        public int RareClipIndex { get { return rareClipIndex; } }
        public int RareClipCooldownPlays { get { return Mathf.Max(0, rareClipCooldownPlays); } }

        public ArcadeSfxEntry(
            ArcadeSfxType type,
            AudioClip[] clips,
            float volume,
            Vector2 pitchRange,
            bool loop = false,
            int maximumSimultaneousVoices = 0,
            float minimumRetriggerInterval = 0f,
            ArcadeSfxVoiceLimitMode voiceLimitMode = ArcadeSfxVoiceLimitMode.ReplaceOldest,
            int rareClipIndex = -1,
            int rareClipCooldownPlays = 0)
        {
            this.type = type;
            this.clips = clips ?? Array.Empty<AudioClip>();
            this.volume = Mathf.Clamp(volume, 0f, 1.5f);
            this.pitchRange = pitchRange;
            this.loop = loop;
            this.maximumSimultaneousVoices = Mathf.Max(0, maximumSimultaneousVoices);
            this.minimumRetriggerInterval = Mathf.Max(0f, minimumRetriggerInterval);
            this.voiceLimitMode = voiceLimitMode;
            this.rareClipIndex = rareClipIndex;
            this.rareClipCooldownPlays = Mathf.Max(0, rareClipCooldownPlays);
        }
    }

    [CreateAssetMenu(fileName = "ArcadeAudioLibrary", menuName = "First Bloom/Arcade Framework/Audio Library")]
    public sealed class ArcadeAudioLibrary : ScriptableObject
    {
        [Header("Synchronized Music")]
        [SerializeField] private AudioClip baseMusic;
        [SerializeField] private AudioClip intensityMusic;
        [SerializeField] private AudioClip ambience;
        [Range(0f, 1f)] [SerializeField] private float baseMusicGain = 0.82f;
        [Range(0f, 1f)] [SerializeField] private float intensityMusicGain = 0.72f;
        [Range(0f, 1f)] [SerializeField] private float ambienceGain = 0.2f;

        [Header("Sound Effects")]
        [SerializeField] private ArcadeSfxEntry[] soundEffects = Array.Empty<ArcadeSfxEntry>();

        public AudioClip BaseMusic { get { return baseMusic; } }
        public AudioClip IntensityMusic { get { return intensityMusic; } }
        public AudioClip Ambience { get { return ambience; } }
        public float BaseMusicGain { get { return Mathf.Clamp01(baseMusicGain); } }
        public float IntensityMusicGain { get { return Mathf.Clamp01(intensityMusicGain); } }
        public float AmbienceGain { get { return Mathf.Clamp01(ambienceGain); } }
        public ArcadeSfxEntry[] SoundEffects { get { return soundEffects; } }

        public bool TryGetEntry(ArcadeSfxType type, out ArcadeSfxEntry entry)
        {
            if (soundEffects != null)
            {
                for (int i = 0; i < soundEffects.Length; i++)
                {
                    ArcadeSfxEntry candidate = soundEffects[i];
                    if (candidate != null && candidate.Type == type)
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }

        public void Configure(
            AudioClip baseLayer,
            AudioClip intensityLayer,
            AudioClip ambienceLayer,
            float baseGain,
            float intensityGain,
            float ambienceVolume,
            ArcadeSfxEntry[] entries)
        {
            baseMusic = baseLayer;
            intensityMusic = intensityLayer;
            ambience = ambienceLayer;
            baseMusicGain = Mathf.Clamp01(baseGain);
            intensityMusicGain = Mathf.Clamp01(intensityGain);
            ambienceGain = Mathf.Clamp01(ambienceVolume);
            soundEffects = entries ?? Array.Empty<ArcadeSfxEntry>();
        }

        public void AppendValidationErrors(List<string> errors, bool requireEverySfxType)
        {
            if (baseMusic == null)
            {
                errors.Add(name + " has no base music clip.");
            }

            if (intensityMusic == null)
            {
                errors.Add(name + " has no intensity music clip.");
            }

            if (ambience == null)
            {
                errors.Add(name + " has no ambience clip.");
            }

            HashSet<ArcadeSfxType> foundTypes = new HashSet<ArcadeSfxType>();
            if (soundEffects == null)
            {
                errors.Add(name + " has a null sound-effect list.");
                return;
            }

            for (int i = 0; i < soundEffects.Length; i++)
            {
                ArcadeSfxEntry entry = soundEffects[i];
                if (entry == null)
                {
                    errors.Add(name + " has a missing sound-effect entry at index " + i + ".");
                    continue;
                }

                if (!foundTypes.Add(entry.Type))
                {
                    errors.Add(name + " defines " + entry.Type + " more than once.");
                }

                AudioClip[] clips = entry.Clips;
                if (clips == null || clips.Length == 0)
                {
                    errors.Add(name + " has no clips for " + entry.Type + ".");
                    continue;
                }

                for (int clipIndex = 0; clipIndex < clips.Length; clipIndex++)
                {
                    if (clips[clipIndex] == null)
                    {
                        errors.Add(name + " has a missing " + entry.Type + " clip at index " + clipIndex + ".");
                    }
                }

                if (entry.RareClipIndex >= clips.Length)
                {
                    errors.Add(name + " has an out-of-range rare clip index for " + entry.Type + ".");
                }
                else if (entry.RareClipIndex >= 0 && entry.RareClipCooldownPlays <= 0)
                {
                    errors.Add(name + " must configure a positive rare clip cooldown for " + entry.Type + ".");
                }
            }

            if (!requireEverySfxType)
            {
                return;
            }

            Array values = Enum.GetValues(typeof(ArcadeSfxType));
            for (int i = 0; i < values.Length; i++)
            {
                ArcadeSfxType type = (ArcadeSfxType)values.GetValue(i);
                if (!foundTypes.Contains(type))
                {
                    errors.Add(name + " has no production cue for " + type + ".");
                }
            }
        }
    }
}
