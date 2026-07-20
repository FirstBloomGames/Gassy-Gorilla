using System.Runtime.InteropServices;
using UnityEngine;

namespace FirstBloom.ArcadeFramework.Accessibility
{
    public static class ArcadeHaptics
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal", EntryPoint = "FirstBloom_PlayHaptic")]
        private static extern void PlayNativeHaptic(int hapticType);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private const int LongPressFeedback = 0;
        private const int VirtualKeyFeedback = 1;
        private const int KeyboardTapFeedback = 3;
        private const int ConfirmFeedback = 16;
        private const int RejectFeedback = 17;

        private static AndroidJavaObject androidActivity;
        private static AndroidJavaObject androidDecorView;
        private static int androidSdkVersion;
        private static bool androidUnavailable;
#endif

        public static void Play(ArcadeHapticType hapticType)
        {
            if (!ArcadeAccessibilitySettings.HapticsEnabled)
            {
                return;
            }

#if UNITY_IOS && !UNITY_EDITOR
            PlayNativeHaptic((int)hapticType);
#elif UNITY_ANDROID && !UNITY_EDITOR
            PlayAndroidHaptic(hapticType);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void PlayAndroidHaptic(ArcadeHapticType hapticType)
        {
            if (!EnsureAndroidView())
            {
                return;
            }

            int feedbackConstant = GetAndroidFeedbackConstant(hapticType);
            androidActivity.Call(
                "runOnUiThread",
                new AndroidJavaRunnable(
                    () =>
                    {
                        if (androidDecorView != null)
                        {
                            androidDecorView.Call<bool>(
                                "performHapticFeedback",
                                feedbackConstant);
                        }
                    }));
        }

        private static bool EnsureAndroidView()
        {
            if (androidUnavailable)
            {
                return false;
            }

            if (androidActivity != null && androidDecorView != null)
            {
                return true;
            }

            try
            {
                using (AndroidJavaClass unityPlayer =
                    new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaClass version =
                    new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    androidActivity =
                        unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    androidSdkVersion = version.GetStatic<int>("SDK_INT");
                }

                if (androidActivity == null)
                {
                    androidUnavailable = true;
                    return false;
                }

                AndroidJavaObject window =
                    androidActivity.Call<AndroidJavaObject>("getWindow");
                androidDecorView =
                    window != null
                        ? window.Call<AndroidJavaObject>("getDecorView")
                        : null;
                if (window != null)
                {
                    window.Dispose();
                }

                androidUnavailable = androidDecorView == null;
                return !androidUnavailable;
            }
            catch (AndroidJavaException)
            {
                androidUnavailable = true;
                return false;
            }
        }

        private static int GetAndroidFeedbackConstant(
            ArcadeHapticType hapticType)
        {
            switch (hapticType)
            {
                case ArcadeHapticType.Light:
                    return KeyboardTapFeedback;
                case ArcadeHapticType.Medium:
                    return VirtualKeyFeedback;
                case ArcadeHapticType.Heavy:
                    return LongPressFeedback;
                case ArcadeHapticType.Success:
                    return androidSdkVersion >= 30
                        ? ConfirmFeedback
                        : VirtualKeyFeedback;
                case ArcadeHapticType.Failure:
                    return androidSdkVersion >= 30
                        ? RejectFeedback
                        : LongPressFeedback;
                default:
                    return VirtualKeyFeedback;
            }
        }
#endif
    }
}
