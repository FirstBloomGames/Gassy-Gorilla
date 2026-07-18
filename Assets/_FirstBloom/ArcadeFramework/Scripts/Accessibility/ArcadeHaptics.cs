using System.Runtime.InteropServices;

namespace FirstBloom.ArcadeFramework.Accessibility
{
    public static class ArcadeHaptics
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal", EntryPoint = "FirstBloom_PlayHaptic")]
        private static extern void PlayNativeHaptic(int hapticType);
#endif

        public static void Play(ArcadeHapticType hapticType)
        {
            if (!ArcadeAccessibilitySettings.HapticsEnabled)
            {
                return;
            }

#if UNITY_IOS && !UNITY_EDITOR
            PlayNativeHaptic((int)hapticType);
#endif
        }
    }
}
