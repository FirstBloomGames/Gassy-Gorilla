using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [Flags]
    public enum RunChunkTag
    {
        None = 0,
        Beginner = 1 << 0,
        Boost = 1 << 1,
        Fuel = 1 << 2,
        Vine = 1 << 3,
        Hazard = 1 << 4,
        Recovery = 1 << 5,
        NoVine = 1 << 6
    }

    public enum RunChunkSpawnKind
    {
        Pickup,
        Hazard,
        SwingVine,
        Decoration
    }

    [Serializable]
    public sealed class RunChunkSpawn
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private RunChunkSpawnKind kind;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Vector3 localEulerAngles;
        [SerializeField] private Vector3 localScale = Vector3.one;

        public GameObject Prefab { get { return prefab; } }
        public RunChunkSpawnKind Kind { get { return kind; } }
        public Vector3 LocalPosition { get { return localPosition; } }
        public Quaternion LocalRotation { get { return Quaternion.Euler(localEulerAngles); } }
        public Vector3 LocalScale { get { return localScale; } }

        public RunChunkSpawn()
        {
            localScale = Vector3.one;
        }

        public RunChunkSpawn(
            GameObject prefab,
            RunChunkSpawnKind kind,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            this.prefab = prefab;
            this.kind = kind;
            this.localPosition = localPosition;
            this.localEulerAngles = localEulerAngles;
            this.localScale = localScale;
        }
    }

    [CreateAssetMenu(fileName = "GG_RunChunk_", menuName = "First Bloom/Gassy Gorilla/Run Chunk")]
    public sealed class RunChunkDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string chunkId = "Chunk";
        [SerializeField] private RunChunkTag tags;
        [SerializeField] private bool allowInMainPool = true;

        [Header("Selection")]
        [Min(0.1f)] [SerializeField] private float length = 8f;
        [Min(0f)] [SerializeField] private float selectionWeight = 1f;
        [Min(0)] [SerializeField] private int minimumDifficulty;
        [Min(0)] [SerializeField] private int maximumDifficulty = 10;
        [SerializeField] private RunChunkTag blockedPreviousTags;
        [SerializeField] private RunChunkTag blockedNextTags;

        [Header("Route Envelope")]
        [SerializeField] private Vector2 entryHeightRange = new Vector2(-0.5f, 3.5f);
        [SerializeField] private Vector2 exitHeightRange = new Vector2(-0.5f, 3.5f);
        [SerializeField] private Vector2 entryFuelRange = new Vector2(0f, 100f);
        [SerializeField] private Vector2 exitFuelRange = new Vector2(0f, 100f);
        [Min(0f)] [SerializeField] private float minimumReactionDistance = 3.5f;

        [Header("Authored Content")]
        [SerializeField] private RunChunkSpawn[] spawns = Array.Empty<RunChunkSpawn>();

        public string ChunkId { get { return chunkId; } }
        public RunChunkTag Tags { get { return tags; } }
        public bool AllowInMainPool { get { return allowInMainPool; } }
        public float Length { get { return Mathf.Max(0.1f, length); } }
        public float SelectionWeight { get { return Mathf.Max(0f, selectionWeight); } }
        public int MinimumDifficulty { get { return minimumDifficulty; } }
        public int MaximumDifficulty { get { return maximumDifficulty; } }
        public Vector2 EntryHeightRange { get { return Ordered(entryHeightRange); } }
        public Vector2 ExitHeightRange { get { return Ordered(exitHeightRange); } }
        public Vector2 EntryFuelRange { get { return Ordered(entryFuelRange); } }
        public Vector2 ExitFuelRange { get { return Ordered(exitFuelRange); } }
        public float MinimumReactionDistance { get { return minimumReactionDistance; } }
        public RunChunkSpawn[] Spawns { get { return spawns; } }

        public bool SupportsDifficulty(int difficulty)
        {
            return difficulty >= minimumDifficulty && difficulty <= maximumDifficulty;
        }

        public bool CanFollow(RunChunkDefinition previous)
        {
            if (previous == null)
            {
                return true;
            }

            if ((blockedPreviousTags & previous.tags) != 0 || (previous.blockedNextTags & tags) != 0)
            {
                return false;
            }

            if (!RangesOverlap(previous.ExitHeightRange, EntryHeightRange))
            {
                return false;
            }

            return previous.ExitFuelRange.y + 0.01f >= EntryFuelRange.x;
        }

        public void Configure(
            string id,
            RunChunkTag chunkTags,
            bool includeInMainPool,
            float chunkLength,
            float weight,
            int minDifficulty,
            int maxDifficulty,
            RunChunkTag previousTagBlock,
            RunChunkTag nextTagBlock,
            Vector2 entryHeights,
            Vector2 exitHeights,
            Vector2 entryFuel,
            Vector2 exitFuel,
            float reactionDistance,
            RunChunkSpawn[] authoredSpawns)
        {
            chunkId = id;
            tags = chunkTags;
            allowInMainPool = includeInMainPool;
            length = chunkLength;
            selectionWeight = weight;
            minimumDifficulty = minDifficulty;
            maximumDifficulty = Mathf.Max(minDifficulty, maxDifficulty);
            blockedPreviousTags = previousTagBlock;
            blockedNextTags = nextTagBlock;
            entryHeightRange = entryHeights;
            exitHeightRange = exitHeights;
            entryFuelRange = entryFuel;
            exitFuelRange = exitFuel;
            minimumReactionDistance = reactionDistance;
            spawns = authoredSpawns ?? Array.Empty<RunChunkSpawn>();
        }

        public void AppendValidationErrors(List<string> errors)
        {
            string label = string.IsNullOrWhiteSpace(chunkId) ? name : chunkId;
            if (string.IsNullOrWhiteSpace(chunkId))
            {
                errors.Add(name + " has no chunk id.");
            }

            if (length < 3f)
            {
                errors.Add(label + " is shorter than the minimum readable chunk length.");
            }

            if (allowInMainPool && selectionWeight <= 0f)
            {
                errors.Add(label + " is in the main pool but has no selection weight.");
            }

            if (minimumDifficulty > maximumDifficulty)
            {
                errors.Add(label + " has an invalid difficulty range.");
            }

            if (minimumReactionDistance > length)
            {
                errors.Add(label + " has a reaction distance longer than the chunk.");
            }

            ValidateRange(label, "entry height", entryHeightRange, errors);
            ValidateRange(label, "exit height", exitHeightRange, errors);
            ValidateRange(label, "entry fuel", entryFuelRange, errors);
            ValidateRange(label, "exit fuel", exitFuelRange, errors);

            if (spawns == null)
            {
                errors.Add(label + " has a null spawn list.");
                return;
            }

            for (int i = 0; i < spawns.Length; i++)
            {
                RunChunkSpawn spawn = spawns[i];
                if (spawn == null || spawn.Prefab == null)
                {
                    errors.Add(label + " has a missing prefab at spawn index " + i + ".");
                    continue;
                }

                if (spawn.LocalPosition.x < 0f || spawn.LocalPosition.x > length)
                {
                    errors.Add(label + " places " + spawn.Prefab.name + " outside the chunk length.");
                }

                if (spawn.Kind == RunChunkSpawnKind.Hazard && spawn.LocalPosition.x < minimumReactionDistance)
                {
                    errors.Add(label + " places hazard " + spawn.Prefab.name + " inside the minimum reaction distance.");
                }

                if (spawn.LocalScale.x <= 0f || spawn.LocalScale.y <= 0f || spawn.LocalScale.z <= 0f)
                {
                    errors.Add(label + " gives " + spawn.Prefab.name + " a non-positive scale.");
                }
            }

            ValidateCriticalSpacing(label, errors);
        }

        private void ValidateCriticalSpacing(string label, List<string> errors)
        {
            for (int i = 0; i < spawns.Length; i++)
            {
                RunChunkSpawn first = spawns[i];
                if (first == null || first.Prefab == null || first.Kind == RunChunkSpawnKind.Pickup || first.Kind == RunChunkSpawnKind.Decoration)
                {
                    continue;
                }

                for (int j = i + 1; j < spawns.Length; j++)
                {
                    RunChunkSpawn second = spawns[j];
                    if (second == null || second.Prefab == null || second.Kind == RunChunkSpawnKind.Pickup || second.Kind == RunChunkSpawnKind.Decoration)
                    {
                        continue;
                    }

                    float horizontalDistance = Mathf.Abs(first.LocalPosition.x - second.LocalPosition.x);
                    if (horizontalDistance < Mathf.Max(1.25f, minimumReactionDistance * 0.35f))
                    {
                        errors.Add(label + " places critical objects " + first.Prefab.name + " and " + second.Prefab.name + " too close together.");
                    }
                }
            }
        }

        private static void ValidateRange(string label, string rangeName, Vector2 range, List<string> errors)
        {
            if (range.x > range.y)
            {
                errors.Add(label + " has a reversed " + rangeName + " range.");
            }
        }

        private static Vector2 Ordered(Vector2 range)
        {
            return range.x <= range.y ? range : new Vector2(range.y, range.x);
        }

        private static bool RangesOverlap(Vector2 first, Vector2 second)
        {
            return first.x <= second.y && second.x <= first.y;
        }
    }
}
