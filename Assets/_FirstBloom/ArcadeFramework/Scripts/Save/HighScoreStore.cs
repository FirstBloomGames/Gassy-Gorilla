using UnityEngine;

namespace FirstBloom.ArcadeFramework.Save
{
    public static class HighScoreStore
    {
        public static float GetBestDistance(string key)
        {
            return PlayerPrefs.GetFloat(key, 0f);
        }

        public static bool TrySaveBestDistance(string key, float distance)
        {
            float currentBest = GetBestDistance(key);
            if (distance <= currentBest)
            {
                return false;
            }

            PlayerPrefs.SetFloat(key, distance);
            PlayerPrefs.Save();
            return true;
        }

        public static void ResetBestDistance(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
