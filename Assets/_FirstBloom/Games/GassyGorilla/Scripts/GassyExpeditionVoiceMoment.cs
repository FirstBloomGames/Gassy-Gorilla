using System;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [Serializable]
    public sealed class GassyExpeditionVoiceMoment
    {
        [SerializeField] private AudioClip clip;
        [TextArea(1, 3)] [SerializeField] private string subtitle;
        [Range(0f, 1f)] [SerializeField] private float volume = 0.9f;
        [Min(0.8f)] [SerializeField] private float textDuration = 2.4f;

        public AudioClip Clip { get { return clip; } }
        public string Subtitle { get { return subtitle; } }
        public float Volume { get { return volume; } }
        public float TextDuration { get { return textDuration; } }
        public bool HasClip { get { return clip != null; } }

        public void Configure(
            AudioClip audioClip,
            string caption,
            float volumeScale = 0.9f,
            float fallbackDuration = 2.4f)
        {
            clip = audioClip;
            subtitle = caption;
            volume = Mathf.Clamp01(volumeScale);
            textDuration = Mathf.Max(0.8f, fallbackDuration);
        }
    }
}
