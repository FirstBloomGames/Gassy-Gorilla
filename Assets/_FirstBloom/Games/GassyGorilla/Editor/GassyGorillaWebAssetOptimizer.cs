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
        private const string MeshyRoot = "Assets/_FirstBloom/Games/GassyGorilla/Models/Meshy/";
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
                if (!path.StartsWith(MeshyRoot, StringComparison.OrdinalIgnoreCase))
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

        private static bool OptimizeTexture(string path, TextureImporter importer)
        {
            int targetSize = GetTargetTextureSize(path);
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(WebGlPlatform);

            bool alreadyOptimized = settings.overridden
                && settings.maxTextureSize == targetSize
                && settings.format == TextureImporterFormat.Automatic
                && settings.textureCompression == TextureImporterCompression.Compressed
                && settings.compressionQuality == 70
                && settings.crunchedCompression;

            if (alreadyOptimized)
            {
                return false;
            }

            settings.name = WebGlPlatform;
            settings.overridden = true;
            settings.maxTextureSize = targetSize;
            settings.format = TextureImporterFormat.Automatic;
            settings.textureCompression = TextureImporterCompression.Compressed;
            settings.compressionQuality = 70;
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
