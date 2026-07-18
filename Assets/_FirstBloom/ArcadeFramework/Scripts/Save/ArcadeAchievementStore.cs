using UnityEngine;

namespace FirstBloom.ArcadeFramework.Save
{
    public static class ArcadeAchievementStore
    {
        private const string Prefix = "FirstBloom_Achievement_";

        public static int GetProgress(string scope, string achievementId)
        {
            return PlayerPrefs.GetInt(BuildKey(scope, achievementId, "Progress"), 0);
        }

        public static bool SetProgressIfGreater(string scope, string achievementId, int value)
        {
            int clampedValue = Mathf.Max(0, value);
            string key = BuildKey(scope, achievementId, "Progress");
            int previous = PlayerPrefs.GetInt(key, 0);
            if (clampedValue <= previous)
            {
                return false;
            }

            PlayerPrefs.SetInt(key, clampedValue);
            PlayerPrefs.Save();
            return true;
        }

        public static int AddProgress(string scope, string achievementId, int amount)
        {
            int next = Mathf.Max(0, GetProgress(scope, achievementId) + amount);
            SetProgressIfGreater(scope, achievementId, next);
            return next;
        }

        public static bool IsUnlocked(string scope, string achievementId)
        {
            return PlayerPrefs.GetInt(BuildKey(scope, achievementId, "Unlocked"), 0) != 0;
        }

        public static bool TryUnlock(string scope, string achievementId)
        {
            string key = BuildKey(scope, achievementId, "Unlocked");
            if (PlayerPrefs.GetInt(key, 0) != 0)
            {
                return false;
            }

            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            return true;
        }

        private static string BuildKey(string scope, string achievementId, string suffix)
        {
            return Prefix + scope + "_" + achievementId + "_" + suffix;
        }
    }
}
