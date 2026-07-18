using UnityEngine;

namespace FirstBloom.ArcadeFramework.Save
{
    public static class ArcadeProgressStore
    {
        public static int GetInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }

        public static bool SetIntIfGreater(string key, int value, int defaultValue = 0)
        {
            int current = GetInt(key, defaultValue);
            if (value <= current)
            {
                return false;
            }

            SetInt(key, value);
            return true;
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;
        }

        public static void SetBool(string key, bool value)
        {
            SetInt(key, value ? 1 : 0);
        }

        public static void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
