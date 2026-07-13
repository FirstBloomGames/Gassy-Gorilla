using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla.EditorTools
{
    public static class GassyGorillaWebBuild
    {
        private const string BuildFolderName = "Builds/WebGLPhone";

        [MenuItem("First Bloom/Gassy Gorilla/Build Phone Web Preview")]
        public static void BuildPhonePreview()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("Gassy Gorilla has no enabled scenes in Build Settings.");
            }

            GassyGorillaSceneSimplifier.ApplySceneCleanup();
            GassyGorillaWebAssetOptimizer.ApplyWebGlImportSettings();
            GassyGorillaParticleRepair.RepairProjectAssets();

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputPath = Path.Combine(projectRoot, BuildFolderName);
            Directory.CreateDirectory(outputPath);

            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.runInBackground = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.memorySize = 512;
            PlayerSettings.WebGL.initialMemorySize = 512;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.nameFilesAsHashes = true;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Gassy Gorilla phone preview build failed with result " + report.summary.result + ".");
            }

            OptimizeGeneratedPageForPhone(outputPath);
            Debug.Log(
                "Gassy Gorilla phone preview built at " + outputPath
                + " (" + report.summary.totalSize + " bytes)."
            );
        }

        private static void OptimizeGeneratedPageForPhone(string outputPath)
        {
            string indexPath = Path.Combine(outputPath, "index.html");
            if (!File.Exists(indexPath))
            {
                throw new FileNotFoundException("Unity did not generate the phone preview index page.", indexPath);
            }

            string html = File.ReadAllText(indexPath);
            html = html.Replace(
                "// config.devicePixelRatio = 1;",
                "config.devicePixelRatio = 1;"
            );
            html = html.Replace(
                "<body>",
                "<body style=\"overscroll-behavior:none; touch-action:none;\">"
            );
            File.WriteAllText(indexPath, html);
        }
    }
}
