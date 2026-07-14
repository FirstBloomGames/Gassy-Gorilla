using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public sealed class RunChunkDirector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform distanceSource;
        [SerializeField] private RunChunkDefinition[] chunkDefinitions = Array.Empty<RunChunkDefinition>();
        [SerializeField] private RunChunkDefinition[] openingSequence = Array.Empty<RunChunkDefinition>();

        [Header("Generation")]
        [SerializeField] private bool spawning;
        [SerializeField] private bool prewarmOnStart = true;
        [SerializeField] private float firstChunkStartX;
        [Min(8f)] [SerializeField] private float spawnAheadDistance = 36f;
        [Min(4f)] [SerializeField] private float cleanupBehindDistance = 18f;
        [Min(0)] [SerializeField] private int recentHistoryLength = 2;

        [Header("Difficulty")]
        [Min(1f)] [SerializeField] private float distancePerDifficultyStep = 75f;
        [Min(0)] [SerializeField] private int maximumDifficulty = 4;

        [Header("Deterministic Runs")]
        [SerializeField] private bool randomizeSeed = true;
        [SerializeField] private int fixedSeed = 142857;
        [SerializeField] private bool logSeed = true;

        private readonly List<ActiveChunk> activeChunks = new List<ActiveChunk>();
        private readonly List<RunChunkDefinition> recentDefinitions = new List<RunChunkDefinition>();
        private System.Random random;
        private RunChunkDefinition previousDefinition;
        private int openingIndex;
        private int chunkIndex;
        private float nextChunkStartX;

        public int CurrentSeed { get; private set; }
        public bool IsSpawning { get { return spawning; } }
        public float FirstChunkStartX { get { return firstChunkStartX; } }
        public RunChunkDefinition[] OpeningSequence { get { return openingSequence; } }

        private void Start()
        {
            ResetDirector();
            if (prewarmOnStart && distanceSource != null)
            {
                FillAhead();
            }
        }

        private void Update()
        {
            if (!spawning || distanceSource == null)
            {
                return;
            }

            FillAhead();
            CleanupBehind();
        }

        public void SetSpawning(bool value)
        {
            spawning = value;
            if (spawning)
            {
                EnsureRandom();
                FillAhead();
            }
        }

        public void ResetDirector()
        {
            for (int i = activeChunks.Count - 1; i >= 0; i--)
            {
                if (activeChunks[i].Root != null)
                {
                    Destroy(activeChunks[i].Root);
                }
            }

            activeChunks.Clear();
            recentDefinitions.Clear();
            previousDefinition = null;
            openingIndex = 0;
            chunkIndex = 0;
            nextChunkStartX = firstChunkStartX;
            random = null;
            InitializeRandom();

            if (spawning)
            {
                FillAhead();
            }
        }

        public void ConfigureSeed(int seed, bool useRandomSeed)
        {
            fixedSeed = seed;
            randomizeSeed = useRandomSeed;
            ResetDirector();
        }

        public void ConfigureSeedForQa(string seedValue)
        {
            int seed;
            if (!int.TryParse(seedValue, out seed))
            {
                Debug.LogWarning("Ignoring invalid Gassy Gorilla QA seed: " + seedValue, this);
                return;
            }

            ConfigureSeed(seed, false);
        }

        public void ConfigureContent(
            Transform source,
            RunChunkDefinition[] definitions,
            RunChunkDefinition[] authoredOpening)
        {
            distanceSource = source;
            chunkDefinitions = definitions ?? Array.Empty<RunChunkDefinition>();
            openingSequence = authoredOpening ?? Array.Empty<RunChunkDefinition>();
        }

        public void AppendValidationErrors(List<string> errors, int simulatedTransitions)
        {
            if (distanceSource == null)
            {
                errors.Add("Run chunk director has no distance source.");
            }

            if (chunkDefinitions == null || chunkDefinitions.Length < 7)
            {
                errors.Add("Run chunk director needs at least seven authored chunk definitions.");
                return;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            int mainPoolCount = 0;
            bool hasVine = false;
            bool hasNoVine = false;
            bool hasRecovery = false;
            bool hasHazard = false;
            bool hasPredator = false;

            for (int i = 0; i < chunkDefinitions.Length; i++)
            {
                RunChunkDefinition definition = chunkDefinitions[i];
                if (definition == null)
                {
                    errors.Add("Run chunk director has a missing definition at index " + i + ".");
                    continue;
                }

                definition.AppendValidationErrors(errors);
                if (!ids.Add(definition.ChunkId))
                {
                    errors.Add("Run chunk id is duplicated: " + definition.ChunkId + ".");
                }

                if (definition.AllowInMainPool)
                {
                    mainPoolCount++;
                }

                hasVine |= (definition.Tags & RunChunkTag.Vine) != 0;
                hasNoVine |= (definition.Tags & RunChunkTag.NoVine) != 0;
                hasRecovery |= (definition.Tags & RunChunkTag.Recovery) != 0;
                hasHazard |= (definition.Tags & RunChunkTag.Hazard) != 0;
                hasPredator |= (definition.Tags & RunChunkTag.Predator) != 0;
            }

            if (mainPoolCount < 4)
            {
                errors.Add("Run chunk director needs at least four main-pool chunks.");
            }

            if (!hasVine || !hasNoVine || !hasRecovery || !hasHazard)
            {
                errors.Add("Run chunk library must include vine, vine-free, recovery, and hazard beats.");
            }

            if (!hasPredator)
            {
                errors.Add("Run chunk library must include a spaced predator beat.");
            }

            ValidateOpeningSequence(errors);
            SimulateTransitions(errors, Mathf.Max(1, simulatedTransitions));
        }

        private void FillAhead()
        {
            float targetX = distanceSource.position.x + spawnAheadDistance;
            int safety = 0;
            while (nextChunkStartX < targetX && safety < 32)
            {
                RunChunkDefinition definition = GetNextDefinition();
                if (definition == null)
                {
                    Debug.LogError("Run chunk generation stopped because no compatible definition was available.", this);
                    spawning = false;
                    return;
                }

                SpawnChunk(definition);
                safety++;
            }
        }

        private RunChunkDefinition GetNextDefinition()
        {
            if (openingSequence != null && openingIndex < openingSequence.Length)
            {
                RunChunkDefinition openingDefinition = openingSequence[openingIndex++];
                if (openingDefinition != null)
                {
                    Remember(openingDefinition);
                    return openingDefinition;
                }
            }

            int difficulty = GetDifficulty();
            RunChunkDefinition selected = SelectWeightedDefinition(
                chunkDefinitions,
                previousDefinition,
                recentDefinitions,
                difficulty,
                random,
                true);

            if (selected == null)
            {
                selected = SelectWeightedDefinition(
                    chunkDefinitions,
                    previousDefinition,
                    recentDefinitions,
                    difficulty,
                    random,
                    false);
            }

            if (selected != null)
            {
                Remember(selected);
            }

            return selected;
        }

        private void SpawnChunk(RunChunkDefinition definition)
        {
            GameObject root = new GameObject("RunChunk_" + chunkIndex.ToString("D3") + "_" + definition.ChunkId);
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(nextChunkStartX, 0f, 0f);

            RunChunkSpawn[] spawns = definition.Spawns;
            for (int i = 0; i < spawns.Length; i++)
            {
                RunChunkSpawn spawn = spawns[i];
                if (spawn == null || spawn.Prefab == null)
                {
                    continue;
                }

                GameObject instance = Instantiate(spawn.Prefab, root.transform);
                instance.name = spawn.Prefab.name + "_" + i.ToString("D2");
                instance.transform.localPosition = spawn.LocalPosition;
                instance.transform.localRotation = spawn.LocalRotation;
                instance.transform.localScale = Vector3.Scale(instance.transform.localScale, spawn.LocalScale);
            }

            float endX = nextChunkStartX + definition.Length;
            activeChunks.Add(new ActiveChunk(root, endX));
            nextChunkStartX = endX;
            chunkIndex++;
        }

        private void CleanupBehind()
        {
            float cleanupX = distanceSource.position.x - cleanupBehindDistance;
            while (activeChunks.Count > 0 && activeChunks[0].EndX < cleanupX)
            {
                ActiveChunk oldest = activeChunks[0];
                activeChunks.RemoveAt(0);
                if (oldest.Root != null)
                {
                    Destroy(oldest.Root);
                }
            }
        }

        private void Remember(RunChunkDefinition definition)
        {
            previousDefinition = definition;
            recentDefinitions.Add(definition);
            int historyLimit = Mathf.Max(0, recentHistoryLength);
            while (recentDefinitions.Count > historyLimit)
            {
                recentDefinitions.RemoveAt(0);
            }
        }

        private int GetDifficulty()
        {
            if (distanceSource == null || distancePerDifficultyStep <= 0f)
            {
                return 0;
            }

            float runDistance = Mathf.Max(0f, distanceSource.position.x - firstChunkStartX);
            return Mathf.Clamp(Mathf.FloorToInt(runDistance / distancePerDifficultyStep), 0, maximumDifficulty);
        }

        private void EnsureRandom()
        {
            if (random == null)
            {
                InitializeRandom();
            }
        }

        private void InitializeRandom()
        {
            CurrentSeed = randomizeSeed
                ? unchecked(Environment.TickCount * 397 ^ fixedSeed)
                : fixedSeed;
            random = new System.Random(CurrentSeed);

            if (logSeed)
            {
                Debug.Log("Gassy Gorilla run seed: " + CurrentSeed, this);
            }
        }

        private void ValidateOpeningSequence(List<string> errors)
        {
            if (openingSequence == null || openingSequence.Length < 3)
            {
                errors.Add("Run chunk director needs a controlled opening sequence with at least three chunks.");
                return;
            }

            RunChunkDefinition previous = null;
            for (int i = 0; i < openingSequence.Length; i++)
            {
                RunChunkDefinition current = openingSequence[i];
                if (current == null)
                {
                    errors.Add("Opening sequence has a missing chunk at index " + i + ".");
                    continue;
                }

                if (!current.CanFollow(previous))
                {
                    errors.Add("Opening sequence transition is incompatible: " +
                        (previous != null ? previous.ChunkId : "Start") + " to " + current.ChunkId + ".");
                }

                previous = current;
            }
        }

        private void SimulateTransitions(List<string> errors, int count)
        {
            System.Random simulationRandom = new System.Random(8675309);
            List<RunChunkDefinition> history = new List<RunChunkDefinition>();
            RunChunkDefinition previous = openingSequence != null && openingSequence.Length > 0
                ? openingSequence[openingSequence.Length - 1]
                : null;

            for (int i = 0; i < count; i++)
            {
                int difficulty = maximumDifficulty <= 0 ? 0 : i % (maximumDifficulty + 1);
                RunChunkDefinition selected = SelectWeightedDefinition(
                    chunkDefinitions,
                    previous,
                    history,
                    difficulty,
                    simulationRandom,
                    true);

                if (selected == null)
                {
                    selected = SelectWeightedDefinition(
                        chunkDefinitions,
                        previous,
                        history,
                        difficulty,
                        simulationRandom,
                        false);
                }

                if (selected == null)
                {
                    errors.Add("Run chunk simulation found no compatible selection at transition " + i + ".");
                    return;
                }

                if (!selected.CanFollow(previous))
                {
                    errors.Add("Run chunk simulation produced an incompatible transition from " +
                        (previous != null ? previous.ChunkId : "Start") + " to " + selected.ChunkId + ".");
                    return;
                }

                previous = selected;
                history.Add(selected);
                while (history.Count > Mathf.Max(0, recentHistoryLength))
                {
                    history.RemoveAt(0);
                }
            }
        }

        private static RunChunkDefinition SelectWeightedDefinition(
            RunChunkDefinition[] definitions,
            RunChunkDefinition previous,
            List<RunChunkDefinition> history,
            int difficulty,
            System.Random source,
            bool respectHistory)
        {
            if (definitions == null || source == null)
            {
                return null;
            }

            float totalWeight = 0f;
            for (int i = 0; i < definitions.Length; i++)
            {
                RunChunkDefinition candidate = definitions[i];
                if (!IsEligible(candidate, previous, history, difficulty, respectHistory))
                {
                    continue;
                }

                totalWeight += candidate.SelectionWeight;
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            double roll = source.NextDouble() * totalWeight;
            for (int i = 0; i < definitions.Length; i++)
            {
                RunChunkDefinition candidate = definitions[i];
                if (!IsEligible(candidate, previous, history, difficulty, respectHistory))
                {
                    continue;
                }

                roll -= candidate.SelectionWeight;
                if (roll <= 0d)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool IsEligible(
            RunChunkDefinition candidate,
            RunChunkDefinition previous,
            List<RunChunkDefinition> history,
            int difficulty,
            bool respectHistory)
        {
            if (candidate == null || !candidate.AllowInMainPool || candidate.SelectionWeight <= 0f)
            {
                return false;
            }

            if (!candidate.SupportsDifficulty(difficulty) || !candidate.CanFollow(previous))
            {
                return false;
            }

            return !respectHistory || history == null || !history.Contains(candidate);
        }

        private readonly struct ActiveChunk
        {
            public readonly GameObject Root;
            public readonly float EndX;

            public ActiveChunk(GameObject root, float endX)
            {
                Root = root;
                EndX = endX;
            }
        }
    }
}
