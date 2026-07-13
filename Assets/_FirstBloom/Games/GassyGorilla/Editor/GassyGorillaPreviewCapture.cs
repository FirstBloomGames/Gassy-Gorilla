using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla.EditorTools
{
    public static class GassyGorillaPreviewCapture
    {
        private const string GameRoot = "Assets/_FirstBloom/Games/GassyGorilla";
        private const string MainMenuScenePath = GameRoot + "/Scenes/MainMenu.unity";
        private const string GameScenePath = GameRoot + "/Scenes/Game.unity";

        [MenuItem("First Bloom/Gassy Gorilla/Capture Scene Previews")]
        public static void CaptureScenePreviews()
        {
            string outputRoot = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "Previews");
            Directory.CreateDirectory(outputRoot);

            CaptureScene(MainMenuScenePath, Path.Combine(outputRoot, "GassyGorilla_MainMenu.png"));
            CaptureScene(GameScenePath, Path.Combine(outputRoot, "GassyGorilla_Game.png"));
            Debug.Log("Gassy Gorilla scene previews captured to " + outputRoot);
        }

        private static void CaptureScene(string scenePath, string outputPath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (scenePath == GameScenePath)
            {
                StageOpeningChunksForPreview();
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("No main camera found for preview capture: " + scenePath);
                return;
            }

            Canvas[] canvases = Object.FindObjectsByType<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[i].worldCamera = camera;
                    canvases[i].planeDistance = 5f;
                }
            }

            RenderTexture renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D screenshot = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;

            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            screenshot.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            screenshot.Apply();

            File.WriteAllBytes(outputPath, screenshot.EncodeToPNG());

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(screenshot);
        }

        private static void StageOpeningChunksForPreview()
        {
            RunChunkDirector[] directors = Object.FindObjectsByType<RunChunkDirector>(FindObjectsInactive.Include);
            if (directors.Length == 0)
            {
                return;
            }

            RunChunkDirector director = directors[0];
            GameObject gorillaModel = GameObject.Find("Visual_Gorilla_3D");
            if (gorillaModel != null)
            {
                gorillaModel.transform.localRotation *= Quaternion.Euler(0f, -42f, 0f);
            }

            GameObject previewRoot = new GameObject("Preview_RunChunks");
            previewRoot.transform.SetParent(director.transform, false);

            float chunkStartX = director.FirstChunkStartX;
            RunChunkDefinition[] opening = director.OpeningSequence;
            for (int chunkIndex = 0; chunkIndex < opening.Length; chunkIndex++)
            {
                RunChunkDefinition definition = opening[chunkIndex];
                if (definition == null)
                {
                    continue;
                }

                GameObject chunkRoot = new GameObject("Preview_" + definition.ChunkId);
                chunkRoot.transform.SetParent(previewRoot.transform, false);
                chunkRoot.transform.position = new Vector3(chunkStartX, 0f, 0f);

                RunChunkSpawn[] spawns = definition.Spawns;
                for (int spawnIndex = 0; spawnIndex < spawns.Length; spawnIndex++)
                {
                    RunChunkSpawn spawn = spawns[spawnIndex];
                    if (spawn == null || spawn.Prefab == null)
                    {
                        continue;
                    }

                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(spawn.Prefab, chunkRoot.transform);
                    instance.name = "Preview_" + spawn.Prefab.name + "_" + spawnIndex;
                    instance.transform.localPosition = spawn.LocalPosition;
                    instance.transform.localRotation = spawn.LocalRotation;
                    instance.transform.localScale = Vector3.Scale(instance.transform.localScale, spawn.LocalScale);

                    Transform vineSwingRoot = instance.transform.Find("PivotPoint/VineSwingRoot");
                    if (vineSwingRoot != null)
                    {
                        float previewSway = ((chunkIndex + spawnIndex) & 1) == 0 ? 4.5f : -4.5f;
                        vineSwingRoot.localRotation = Quaternion.Euler(0f, 0f, previewSway);
                    }
                }

                chunkStartX += definition.Length;
            }
        }
    }
}
