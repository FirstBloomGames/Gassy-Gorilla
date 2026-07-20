using System;
using UnityEngine;

namespace FirstBloom.ArcadeFramework.Accessibility
{
    public static class ArcadeAccessibilitySettings
    {
        private const string ReducedMotionKey = "FirstBloom_ReducedMotion";
        private const string HapticsEnabledKey = "FirstBloom_HapticsEnabled";
        private const string SubtitlesEnabledKey = "FirstBloom_SubtitlesEnabled";

        private static bool loaded;
        private static bool reducedMotion;
        private static bool hapticsEnabled;
        private static bool subtitlesEnabled;

        public static event Action SettingsChanged;

        public static bool ReducedMotion
        {
            get
            {
                EnsureLoaded();
                return reducedMotion;
            }
        }

        public static bool HapticsEnabled
        {
            get
            {
                EnsureLoaded();
                return hapticsEnabled;
            }
        }

        public static bool SubtitlesEnabled
        {
            get
            {
                EnsureLoaded();
                return subtitlesEnabled;
            }
        }

        public static void SetReducedMotion(bool value)
        {
            EnsureLoaded();
            if (reducedMotion == value)
            {
                return;
            }

            reducedMotion = value;
            PlayerPrefs.SetInt(ReducedMotionKey, value ? 1 : 0);
            SaveAndNotify();
        }

        public static void SetHapticsEnabled(bool value)
        {
            EnsureLoaded();
            if (hapticsEnabled == value)
            {
                return;
            }

            hapticsEnabled = value;
            PlayerPrefs.SetInt(HapticsEnabledKey, value ? 1 : 0);
            SaveAndNotify();
        }

        public static void SetSubtitlesEnabled(bool value)
        {
            EnsureLoaded();
            if (subtitlesEnabled == value)
            {
                return;
            }

            subtitlesEnabled = value;
            PlayerPrefs.SetInt(SubtitlesEnabledKey, value ? 1 : 0);
            SaveAndNotify();
        }

        public static void Reload()
        {
            loaded = false;
            EnsureLoaded();
            NotifyChanged();
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            reducedMotion = PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0;
            hapticsEnabled = PlayerPrefs.GetInt(HapticsEnabledKey, 1) != 0;
            subtitlesEnabled =
                PlayerPrefs.GetInt(SubtitlesEnabledKey, 1) != 0;
        }

        private static void SaveAndNotify()
        {
            PlayerPrefs.Save();
            NotifyChanged();
        }

        private static void NotifyChanged()
        {
            if (SettingsChanged != null)
            {
                SettingsChanged.Invoke();
            }
        }
    }
}
