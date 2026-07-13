using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstBloom.Games.GassyGorilla.EditorTools
{
    public static class GassyGorillaPerformanceAudit
    {
        private const string ModelRoot = "Assets/_FirstBloom/Games/GassyGorilla/Models";
        private const string PrefabRoot = "Assets/_FirstBloom/Games/GassyGorilla/Prefabs";
        private const string RunChunkRoot = "Assets/_FirstBloom/Games/GassyGorilla/ScriptableObjects/RunChunks";
        private const string GameScenePath = "Assets/_FirstBloom/Games/GassyGorilla/Scenes/Game.unity";

        [MenuItem("First Bloom/Gassy Gorilla/Audit Runtime Geometry")]
        public static void AuditRuntimeGeometry()
        {
            List<GeometryEntry> models = AuditModels();
            List<GeometryEntry> prefabs = AuditPrefabs();
            List<GeometryEntry> chunks = AuditRunChunks();
            GeometryEntry scene = AuditScene();

            LogEntries("MODEL", models, 20);
            LogEntries("PREFAB", prefabs, 20);
            LogEntries("CHUNK", chunks, chunks.Count);
            Debug.Log(FormatEntry("SCENE", scene));

            long largestPickupTriangles = 0;
            for (int i = 0; i < prefabs.Count; i++)
            {
                if (prefabs[i].Name.IndexOf("Pickup_", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    largestPickupTriangles = Math.Max(largestPickupTriangles, prefabs[i].Triangles);
                }
            }

            if (largestPickupTriangles > 50000)
            {
                Debug.LogWarning("[GG_PERF] Pickup geometry exceeds the 50,000 triangle arcade-prop budget. Largest pickup: " + largestPickupTriangles.ToString("N0") + " triangles.");
            }

            Debug.Log("[GG_PERF] Runtime geometry audit complete. Source artwork was inspected without modification.");
        }

        [MenuItem("First Bloom/Gassy Gorilla/Validate Runtime Geometry Budgets")]
        public static void ValidateRuntimeGeometryBudgets()
        {
            const long pickupBudget = 50000;
            const long vineBudget = 75000;
            const long prefabBudget = 100000;
            const long chunkBudget = 150000;

            List<string> violations = new List<string>();
            List<GeometryEntry> prefabs = AuditPrefabs();
            for (int i = 0; i < prefabs.Count; i++)
            {
                GeometryEntry prefab = prefabs[i];
                long budget = prefab.Name.StartsWith("Pickup_", StringComparison.OrdinalIgnoreCase)
                    ? pickupBudget
                    : prefab.Name.Equals("Vine_Swingable", StringComparison.OrdinalIgnoreCase)
                        ? vineBudget
                        : prefabBudget;
                if (prefab.Triangles > budget)
                {
                    violations.Add(prefab.Name + " has " + prefab.Triangles.ToString("N0")
                        + " triangles (budget " + budget.ToString("N0") + ").");
                }
            }

            List<GeometryEntry> chunks = AuditRunChunks();
            for (int i = 0; i < chunks.Count; i++)
            {
                if (chunks[i].Triangles > chunkBudget)
                {
                    violations.Add("Run chunk " + chunks[i].Name + " has "
                        + chunks[i].Triangles.ToString("N0") + " triangles (budget "
                        + chunkBudget.ToString("N0") + ").");
                }
            }

            if (violations.Count > 0)
            {
                throw new InvalidOperationException(
                    "Gassy Gorilla runtime geometry budgets failed:\n- "
                    + string.Join("\n- ", violations));
            }

            Debug.Log("[GG_PERF] Runtime geometry budgets passed for " + prefabs.Count
                + " prefabs and " + chunks.Count + " run chunks.");
        }

        private static List<GeometryEntry> AuditModels()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ModelRoot });
            List<GeometryEntry> entries = new List<GeometryEntry>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                HashSet<Mesh> meshes = new HashSet<Mesh>();
                GeometryEntry entry = new GeometryEntry(Path.GetFileNameWithoutExtension(path), path);

                for (int j = 0; j < assets.Length; j++)
                {
                    Mesh mesh = assets[j] as Mesh;
                    if (mesh == null || !meshes.Add(mesh))
                    {
                        continue;
                    }

                    entry.AddMesh(mesh);
                }

                string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
                if (File.Exists(fullPath))
                {
                    entry.FileBytes = new FileInfo(fullPath).Length;
                }

                if (entry.Meshes > 0)
                {
                    entries.Add(entry);
                }
            }

            SortByTriangles(entries);
            return entries;
        }

        private static List<GeometryEntry> AuditPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
            List<GeometryEntry> entries = new List<GeometryEntry>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                GeometryEntry entry = AuditHierarchy(prefab, prefab.name, path);
                entries.Add(entry);
            }

            SortByTriangles(entries);
            return entries;
        }

        private static List<GeometryEntry> AuditRunChunks()
        {
            string[] guids = AssetDatabase.FindAssets("t:RunChunkDefinition", new[] { RunChunkRoot });
            List<GeometryEntry> entries = new List<GeometryEntry>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                RunChunkDefinition definition = AssetDatabase.LoadAssetAtPath<RunChunkDefinition>(path);
                if (definition == null)
                {
                    continue;
                }

                GeometryEntry entry = new GeometryEntry(definition.ChunkId, path);
                RunChunkSpawn[] spawns = definition.Spawns;
                for (int j = 0; j < spawns.Length; j++)
                {
                    if (spawns[j] == null || spawns[j].Prefab == null)
                    {
                        continue;
                    }

                    entry.Add(AuditHierarchy(spawns[j].Prefab, spawns[j].Prefab.name, AssetDatabase.GetAssetPath(spawns[j].Prefab)));
                }

                entries.Add(entry);
            }

            SortByTriangles(entries);
            return entries;
        }

        private static GeometryEntry AuditScene()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            GeometryEntry entry = new GeometryEntry(scene.name, GameScenePath);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                entry.Add(AuditHierarchy(roots[i], roots[i].name, GameScenePath));
            }

            return entry;
        }

        private static GeometryEntry AuditHierarchy(GameObject root, string name, string path)
        {
            GeometryEntry entry = new GeometryEntry(name, path);
            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                if (meshFilters[i].sharedMesh != null)
                {
                    entry.AddMesh(meshFilters[i].sharedMesh);
                }
            }

            SkinnedMeshRenderer[] skinnedMeshes = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinnedMeshes.Length; i++)
            {
                if (skinnedMeshes[i].sharedMesh != null)
                {
                    entry.AddMesh(skinnedMeshes[i].sharedMesh);
                }
            }

            entry.Renderers = root.GetComponentsInChildren<Renderer>(true).Length;
            entry.Particles = root.GetComponentsInChildren<ParticleSystem>(true).Length;
            entry.Animators = root.GetComponentsInChildren<Animator>(true).Length;
            entry.Behaviours = root.GetComponentsInChildren<MonoBehaviour>(true).Length;
            return entry;
        }

        private static void LogEntries(string kind, List<GeometryEntry> entries, int maximum)
        {
            int count = Math.Min(maximum, entries.Count);
            for (int i = 0; i < count; i++)
            {
                Debug.Log(FormatEntry(kind, entries[i]));
            }
        }

        private static string FormatEntry(string kind, GeometryEntry entry)
        {
            return "[GG_PERF][" + kind + "] " + entry.Name
                + " | tris=" + entry.Triangles.ToString("N0")
                + " vertices=" + entry.Vertices.ToString("N0")
                + " meshes=" + entry.Meshes
                + " renderers=" + entry.Renderers
                + " particles=" + entry.Particles
                + " animators=" + entry.Animators
                + " behaviours=" + entry.Behaviours
                + (entry.FileBytes > 0 ? " fileMB=" + (entry.FileBytes / (1024f * 1024f)).ToString("F2") : "")
                + " | " + entry.Path;
        }

        private static void SortByTriangles(List<GeometryEntry> entries)
        {
            entries.Sort((left, right) => right.Triangles.CompareTo(left.Triangles));
        }

        private sealed class GeometryEntry
        {
            public readonly string Name;
            public readonly string Path;
            public long Triangles;
            public long Vertices;
            public int Meshes;
            public int Renderers;
            public int Particles;
            public int Animators;
            public int Behaviours;
            public long FileBytes;

            public GeometryEntry(string name, string path)
            {
                Name = name;
                Path = path;
            }

            public void AddMesh(Mesh mesh)
            {
                Meshes++;
                Vertices += mesh.vertexCount;
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    Triangles += (long)mesh.GetIndexCount(i) / 3L;
                }
            }

            public void Add(GeometryEntry other)
            {
                Triangles += other.Triangles;
                Vertices += other.Vertices;
                Meshes += other.Meshes;
                Renderers += other.Renderers;
                Particles += other.Particles;
                Animators += other.Animators;
                Behaviours += other.Behaviours;
            }
        }
    }
}
