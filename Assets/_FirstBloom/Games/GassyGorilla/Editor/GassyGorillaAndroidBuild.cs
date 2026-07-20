using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla.EditorTools
{
    public static class GassyGorillaAndroidBuild
    {
        public const string ApplicationIdentifier =
            "com.firstbloomgames.gassygorilla";
        public const int MinimumApiLevel = 26;
        public const int TargetApiLevel = 36;

        private const string QaOutputPath =
            "Builds/Android/GassyGorilla-QA.apk";
        private const string PlayOutputPath =
            "Builds/Android/GassyGorilla-Play.aab";
        private const string KeystorePathVariable =
            "GG_ANDROID_KEYSTORE_PATH";
        private const string KeystorePasswordVariable =
            "GG_ANDROID_KEYSTORE_PASS";
        private const string KeyAliasVariable =
            "GG_ANDROID_KEY_ALIAS";
        private const string KeyAliasPasswordVariable =
            "GG_ANDROID_KEY_ALIAS_PASS";

        [MenuItem(
            "First Bloom/Gassy Gorilla/Android/Configure Android Player Settings")]
        public static void ConfigureAndroidPlayerSettings()
        {
            PlayerSettings.companyName = "First Bloom Games";
            PlayerSettings.productName = "Gassy Gorilla";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android,
                ApplicationIdentifier);

            PlayerSettings.Android.minSdkVersion =
                AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion =
                AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARM64;

            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Android,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.Android,
                ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(
                NamedBuildTarget.Android,
                Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.stripEngineCode = true;

            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.defaultInterfaceOrientation =
                UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.Android.optimizedFramePacing = true;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName = string.Empty;
            PlayerSettings.Android.keystorePass = string.Empty;
            PlayerSettings.Android.keyaliasName = string.Empty;
            PlayerSettings.Android.keyaliasPass = string.Empty;

            AssetDatabase.SaveAssets();
            ValidateAndroidPlayerSettings();
            Debug.Log(
                "Gassy Gorilla Android settings configured for API 36, " +
                "IL2CPP, ARM64, and landscape play.");
        }

        [MenuItem(
            "First Bloom/Gassy Gorilla/Android/Validate Android Player Settings")]
        public static void ValidateAndroidPlayerSettings()
        {
            List<string> errors = CollectValidationErrors();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Gassy Gorilla Android validation failed:\n - " +
                    string.Join("\n - ", errors));
            }

            Debug.Log(
                "Gassy Gorilla Android validation passed: API 26-36, " +
                "IL2CPP, ARM64, landscape-only, size-optimized.");
        }

        [MenuItem("First Bloom/Gassy Gorilla/Android/Build QA APK")]
        public static void BuildQaApk()
        {
            BuildAndroid(false);
        }

        [MenuItem("First Bloom/Gassy Gorilla/Android/Build Play AAB")]
        public static void BuildPlayAab()
        {
            BuildAndroid(true);
        }

        public static void BuildQaApkBatch()
        {
            BuildQaApk();
        }

        public static void BuildPlayAabBatch()
        {
            BuildPlayAab();
        }

        private static void BuildAndroid(bool appBundle)
        {
            ConfigureAndroidPlayerSettings();
            GassyGorillaSceneValidator.ValidateBuiltScenes();

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException(
                    "Gassy Gorilla has no enabled scenes in Build Settings.");
            }

            string projectRoot =
                Directory.GetParent(Application.dataPath).FullName;
            string relativeOutput =
                appBundle ? PlayOutputPath : QaOutputPath;
            string outputPath =
                Path.Combine(
                    projectRoot,
                    relativeOutput.Replace(
                        '/',
                        Path.DirectorySeparatorChar));
            string outputFolder = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new InvalidOperationException(
                    "Android output folder could not be resolved.");
            }

            Directory.CreateDirectory(outputFolder);
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            AndroidSigningSnapshot signingSnapshot =
                AndroidSigningSnapshot.Capture();
            bool previousBuildAppBundle =
                EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle = appBundle;
                if (appBundle)
                {
                    ApplyReleaseSigningFromEnvironment();
                }
                else
                {
                    PlayerSettings.Android.useCustomKeystore = false;
                }

                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Gassy Gorilla Android build failed with result " +
                        report.summary.result + ".");
                }

                Debug.Log(
                    "Gassy Gorilla " +
                    (appBundle ? "Play AAB" : "QA APK") +
                    " built at " + outputPath +
                    " (" + report.summary.totalSize + " bytes).");
            }
            finally
            {
                signingSnapshot.Restore();
                EditorUserBuildSettings.buildAppBundle =
                    previousBuildAppBundle;
            }
        }

        private static List<string> CollectValidationErrors()
        {
            List<string> errors = new List<string>();
            string identifier =
                PlayerSettings.GetApplicationIdentifier(
                    NamedBuildTarget.Android);
            if (!string.Equals(
                identifier,
                ApplicationIdentifier,
                StringComparison.Ordinal))
            {
                errors.Add(
                    "Application identifier must be " +
                    ApplicationIdentifier + ".");
            }

            if (PlayerSettings.Android.minSdkVersion !=
                AndroidSdkVersions.AndroidApiLevel26)
            {
                errors.Add("Minimum Android API must be 26.");
            }

            if (PlayerSettings.Android.targetSdkVersion !=
                AndroidSdkVersions.AndroidApiLevel36)
            {
                errors.Add("Target Android API must be 36.");
            }

            if ((PlayerSettings.Android.targetArchitectures &
                AndroidArchitecture.ARM64) == 0)
            {
                errors.Add("Android ARM64 architecture is required.");
            }

            if (PlayerSettings.GetScriptingBackend(
                NamedBuildTarget.Android) !=
                ScriptingImplementation.IL2CPP)
            {
                errors.Add("Android scripting backend must be IL2CPP.");
            }

            if (PlayerSettings.GetManagedStrippingLevel(
                NamedBuildTarget.Android) !=
                ManagedStrippingLevel.High)
            {
                errors.Add("Android managed stripping must be High.");
            }

            if (PlayerSettings.GetIl2CppCodeGeneration(
                NamedBuildTarget.Android) !=
                Il2CppCodeGeneration.OptimizeSize)
            {
                errors.Add(
                    "Android IL2CPP code generation must optimize size.");
            }

            if (PlayerSettings.allowedAutorotateToPortrait ||
                PlayerSettings.allowedAutorotateToPortraitUpsideDown ||
                !PlayerSettings.allowedAutorotateToLandscapeLeft ||
                !PlayerSettings.allowedAutorotateToLandscapeRight)
            {
                errors.Add(
                    "Android must allow only landscape-left and " +
                    "landscape-right autorotation.");
            }

            if (PlayerSettings.runInBackground)
            {
                errors.Add(
                    "Android must stop background play so focus loss " +
                    "can pause safely.");
            }

            if (PlayerSettings.Android.useCustomKeystore)
            {
                errors.Add(
                    "Android release signing must only be enabled " +
                    "temporarily by the signed AAB build command.");
            }

            return errors;
        }

        private static void ApplyReleaseSigningFromEnvironment()
        {
            string keystorePath =
                RequireEnvironmentVariable(KeystorePathVariable);
            string keystorePassword =
                RequireEnvironmentVariable(KeystorePasswordVariable);
            string keyAlias =
                RequireEnvironmentVariable(KeyAliasVariable);
            string keyAliasPassword =
                RequireEnvironmentVariable(KeyAliasPasswordVariable);

            keystorePath = Path.GetFullPath(keystorePath);
            if (!File.Exists(keystorePath))
            {
                throw new FileNotFoundException(
                    "Android release keystore does not exist.",
                    keystorePath);
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasName = keyAlias;
            PlayerSettings.Android.keyaliasPass = keyAliasPassword;
        }

        private static string RequireEnvironmentVariable(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    "Play AAB signing requires environment variable " +
                    name + ".");
            }

            return value;
        }

        private readonly struct AndroidSigningSnapshot
        {
            private readonly bool useCustomKeystore;
            private readonly string keystoreName;
            private readonly string keystorePass;
            private readonly string keyaliasName;
            private readonly string keyaliasPass;

            private AndroidSigningSnapshot(
                bool customKeystore,
                string storeName,
                string storePass,
                string aliasName,
                string aliasPass)
            {
                useCustomKeystore = customKeystore;
                keystoreName = storeName;
                keystorePass = storePass;
                keyaliasName = aliasName;
                keyaliasPass = aliasPass;
            }

            public static AndroidSigningSnapshot Capture()
            {
                return new AndroidSigningSnapshot(
                    PlayerSettings.Android.useCustomKeystore,
                    PlayerSettings.Android.keystoreName,
                    PlayerSettings.Android.keystorePass,
                    PlayerSettings.Android.keyaliasName,
                    PlayerSettings.Android.keyaliasPass);
            }

            public void Restore()
            {
                PlayerSettings.Android.useCustomKeystore =
                    useCustomKeystore;
                PlayerSettings.Android.keystoreName = keystoreName;
                PlayerSettings.Android.keystorePass = keystorePass;
                PlayerSettings.Android.keyaliasName = keyaliasName;
                PlayerSettings.Android.keyaliasPass = keyaliasPass;
            }
        }
    }
}
