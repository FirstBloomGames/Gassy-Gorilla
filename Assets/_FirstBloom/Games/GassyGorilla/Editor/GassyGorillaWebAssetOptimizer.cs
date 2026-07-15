using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla.EditorTools
{
    public static class GassyGorillaWebAssetOptimizer
    {
        private const string GameRoot = "Assets/_FirstBloom/Games/GassyGorilla/";
        private const string MeshyRoot = "Assets/_FirstBloom/Games/GassyGorilla/Models/Meshy/";
        private const string ModelRoot = GameRoot + "Models/";
        private const string PaintedJungleTexturePath = GameRoot + "Textures/Generated3D/GG_JungleBackdrop_Painted3D_v1.png";
        private const string WebGlPlatform = "WebGL";

        [MenuItem("First Bloom/Gassy Gorilla/Optimize WebGL Asset Imports")]
        public static void ApplyWebGlImportSettings()
        {
            string[] scenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenePaths.Length == 0)
            {
                throw new InvalidOperationException("Gassy Gorilla has no enabled scenes to optimize.");
            }

            HashSet<string> dependencies = new HashSet<string>(
                AssetDatabase.GetDependencies(scenePaths, true),
                StringComparer.OrdinalIgnoreCase);

            int optimizedTextures = 0;
            int optimizedMeshes = 0;

            foreach (string path in dependencies.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                bool isMeshyAsset = path.StartsWith(MeshyRoot, StringComparison.OrdinalIgnoreCase);
                bool isPaintedBackdrop = path.Equals(PaintedJungleTexturePath, StringComparison.OrdinalIgnoreCase);
                if (!isMeshyAsset && !isPaintedBackdrop)
                {
                    continue;
                }

                AssetImporter importer = AssetImporter.GetAtPath(path);
                if (importer is TextureImporter textureImporter && OptimizeTexture(path, textureImporter))
                {
                    optimizedTextures++;
                }
                else if (importer is ModelImporter modelImporter && OptimizeStaticMesh(path, modelImporter))
                {
                    optimizedMeshes++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Gassy Gorilla WebGL optimization updated " + optimizedTextures
                + " textures and " + optimizedMeshes + " dense static meshes.");
        }

        public static void ValidateBuildDependencies()
        {
            string[] scenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            HashSet<string> dependencies = new HashSet<string>(
                AssetDatabase.GetDependencies(scenePaths, true),
                StringComparer.OrdinalIgnoreCase);
            List<string> violations = new List<string>();

            foreach (string path in dependencies.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (!path.StartsWith(ModelRoot, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null || importer.importAnimation)
                {
                    continue;
                }

                long triangles = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Mesh>()
                    .Sum(mesh => Enumerable.Range(0, mesh.subMeshCount)
                        .Sum(index => (long)mesh.GetIndexCount(index) / 3L));
                if (triangles > 100000)
                {
                    violations.Add(Path.GetFileName(path) + " has " + triangles.ToString("N0")
                        + " triangles and is referenced by a shipping scene.");
                }
            }

            if (violations.Count > 0)
            {
                throw new InvalidOperationException(
                    "Gassy Gorilla contains accidental heavyweight build dependencies:\n- "
                    + string.Join("\n- ", violations));
            }

            Debug.Log("[GG_PERF] Shipping scenes contain no accidental static model over 100,000 triangles.");
        }

        private static bool OptimizeTexture(string path, TextureImporter importer)
        {
            int targetSize = GetTargetTextureSize(path);
            bool isHero = path.IndexOf("HeroGorilla", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isPaintedBackdrop = path.Equals(PaintedJungleTexturePath, StringComparison.OrdinalIgnoreCase);
            int compressionQuality = isHero ? 92 : isPaintedBackdrop ? 78 : 70;
            bool importerChanged = false;

            if (isPaintedBackdrop)
            {
                if (importer.npotScale != TextureImporterNPOTScale.ToNearest)
                {
                    importer.npotScale = TextureImporterNPOTScale.ToNearest;
                    importerChanged = true;
                }

                if (importer.alphaSource != TextureImporterAlphaSource.None)
                {
                    importer.alphaSource = TextureImporterAlphaSource.None;
                    importerChanged = true;
                }

                if (!importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = true;
                    importerChanged = true;
                }

                if (importer.anisoLevel != 1)
                {
                    importer.anisoLevel = 1;
                    importerChanged = true;
                }
            }

            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(WebGlPlatform);

            bool alreadyOptimized = settings.overridden
                && settings.maxTextureSize == targetSize
                && settings.format == TextureImporterFormat.Automatic
                && settings.textureCompression == TextureImporterCompression.Compressed
                && settings.compressionQuality == compressionQuality
                && settings.crunchedCompression;

            if (alreadyOptimized && !importerChanged)
            {
                return false;
            }

            settings.name = WebGlPlatform;
            settings.overridden = true;
            settings.maxTextureSize = targetSize;
            settings.format = TextureImporterFormat.Automatic;
            settings.textureCompression = TextureImporterCompression.Compressed;
            settings.compressionQuality = compressionQuality;
            settings.crunchedCompression = true;
            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
            return true;
        }

        private static int GetTargetTextureSize(string path)
        {
            if (path.IndexOf("HeroGorilla", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 2048;
            }

            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.EndsWith("_emit", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith("_emission", StringComparison.OrdinalIgnoreCase))
            {
                return 512;
            }

            return 1024;
        }

        private static bool OptimizeStaticMesh(string path, ModelImporter importer)
        {
            if (importer.importAnimation
                || path.IndexOf("HeroGorilla", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            long vertexCount = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Mesh>()
                .Sum(mesh => (long)mesh.vertexCount);

            ModelImporterMeshCompression targetCompression = vertexCount >= 500000
                ? ModelImporterMeshCompression.High
                : vertexCount >= 100000
                    ? ModelImporterMeshCompression.Medium
                    : ModelImporterMeshCompression.Off;

            if (targetCompression == ModelImporterMeshCompression.Off
                || importer.meshCompression == targetCompression)
            {
                return false;
            }

            importer.meshCompression = targetCompression;
            importer.SaveAndReimport();
            Debug.Log(
                "Compressed dense static mesh " + path + " (" + vertexCount + " vertices) to "
                + targetCompression + ".");
            return true;
        }
    }
}
