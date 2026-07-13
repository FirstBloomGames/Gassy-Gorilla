using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstBloom.Games.GassyGorilla.EditorTools
{
    public static class GassyGorillaSceneSimplifier
    {
        private const string GameScenePath = "Assets/_FirstBloom/Games/GassyGorilla/Scenes/Game.unity";
        private const string MainMenuScenePath = "Assets/_FirstBloom/Games/GassyGorilla/Scenes/MainMenu.unity";

        private static readonly string[] GameObjectsToRemove =
        {
            "Distant_MeshyForestDepth_3D",
            "Foreground_3DDecor",
            "Ground_3D"
        };

        private static readonly string[] MenuObjectsToRemove =
        {
            "Menu_MeshyForestDepth_3D"
        };

        [MenuItem("First Bloom/Gassy Gorilla/Simplify Jungle Staging")]
        public static void ApplySceneCleanup()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            int removed = 0;

            try
            {
                removed += RemoveObjectsFromScene(GameScenePath, GameObjectsToRemove);
                removed += RemoveObjectsFromScene(MainMenuScenePath, MenuObjectsToRemove);
            }
            finally
            {
                if (previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Gassy Gorilla jungle simplification removed " + removed + " clutter roots.");
        }

        private static int RemoveObjectsFromScene(string scenePath, string[] objectNames)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                throw new InvalidOperationException("Required scene is missing: " + scenePath);
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int removed = 0;

            foreach (string objectName in objectNames)
            {
                GameObject target = FindRoot(scene, objectName);
                if (target == null)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(target);
                removed++;
            }

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            return removed;
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, objectName, StringComparison.Ordinal))
                {
                    return root;
                }
            }

            return null;
        }
    }
}
