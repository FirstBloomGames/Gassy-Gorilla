using System;
using System.Collections.Generic;
using FirstBloom.ArcadeFramework.Spawning;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public sealed class RunChunkDirector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform distanceSource;
        [SerializeField] private GorillaController fuelSource;
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
        [SerializeField] private RunDifficultyProfile difficultyProfile;
        [Min(1f)] [SerializeField] private float distancePerDifficultyStep = 75f;
        [Min(0)] [SerializeField] private int maximumDifficulty = 4;

        [Header("Deterministic Runs")]
        [SerializeField] private bool randomizeSeed = true;
        [SerializeField] private int fixedSeed = 142857;
        [SerializeField] private bool logSeed = true;

        private readonly List<ActiveChunk> activeChunks = new List<ActiveChunk>();
        private readonly List<RunChunkDefinition> recentDefinitions = new List<RunChunkDefinition>();
        private readonly Dictionary<RunChunkDefinition, Stack<GameObject>> chunkPool = new Dictionary<RunChunkDefinition, Stack<GameObject>>();
        private readonly Dictionary<GameObject, IArcadePoolable[]> chunkPoolables = new Dictionary<GameObject, IArcadePoolable[]>();
        private System.Random random;
        private RunChunkDefinition previousDefinition;
        private int openingIndex;
        private int chunkIndex;
        private float nextChunkStartX;
        private bool poolPrewarmed;
        private int createdChunkCount;
        private readonly FairnessState fairnessState = new FairnessState();
        private float lastReportedIntensity = -1f;
        private int lastReportedStage = -1;
        private bool finiteRoute;
        private bool finiteRouteExhausted;
        private float configuredFiniteRouteEndX;

        public event Action<float, int> DifficultyChanged;

        public int CurrentSeed { get; private set; }
        public bool IsSpawning { get { return spawning; } }
        public float FirstChunkStartX { get { return firstChunkStartX; } }
        public RunChunkDefinition[] OpeningSequence { get { return openingSequence; } }
        public int CreatedChunkCount { get { return createdChunkCount; } }
        public RunDifficultyProfile DifficultyProfile { get { return difficultyProfile; } }
        public float CurrentRunDistance { get; private set; }
        public float CurrentIntensity { get; private set; }
        public int CurrentDifficultyStage { get; private set; }
        public int CurrentPressure { get { return fairnessState.Pressure; } }
        public int PredatorCooldownRemaining { get { return fairnessState.PredatorCooldownRemaining; } }
        public bool IsFiniteRoute { get { return finiteRoute; } }
        public bool IsFiniteRouteExhausted { get { return finiteRouteExhausted; } }
        public float ConfiguredFiniteRouteEndX { get { return configuredFiniteRouteEndX; } }

        private void Start()
        {
            if (prewarmOnStart)
            {
                PrewarmPool();
            }

            ResetDirector();
            UpdateDifficulty(true);
            if (prewarmOnStart && distanceSource != null)
            {
                FillAhead();
            }
        }

        private void Update()
        {
            UpdateDifficulty(false);

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
                    RecycleChunk(activeChunks[i]);
                }
            }

            activeChunks.Clear();
            recentDefinitions.Clear();
            previousDefinition = null;
            fairnessState.Reset();
            openingIndex = 0;
            chunkIndex = 0;
            nextChunkStartX = firstChunkStartX;
            finiteRouteExhausted = false;
            lastReportedIntensity = -1f;
            lastReportedStage = -1;
            random = null;
            InitializeRandom();
            UpdateDifficulty(true);

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

        public bool ConfigureOpeningForQa(string primaryChunkId)
        {
            RunChunkDefinition primary = null;
            RunChunkDefinition recovery = null;
            for (int i = 0; i < chunkDefinitions.Length; i++)
            {
                RunChunkDefinition definition = chunkDefinitions[i];
                if (definition == null)
                {
                    continue;
                }

                if (string.Equals(definition.ChunkId, primaryChunkId, StringComparison.OrdinalIgnoreCase))
                {
                    primary = definition;
                }
                else if (string.Equals(definition.ChunkId, "Recovery", StringComparison.OrdinalIgnoreCase))
                {
                    recovery = definition;
                }
            }

            if (primary == null)
            {
                Debug.LogWarning("Could not configure Gassy Gorilla QA opening for missing chunk " + primaryChunkId + ".", this);
                return false;
            }

            openingSequence = recovery != null
                ? new[] { primary, recovery, primary, recovery }
                : new[] { primary, primary };
            ConfigureSeed(71626, false);
            Debug.Log("[GG_QA] Opening sequence forced to repeated " + primary.ChunkId + " checks.", this);
            return true;
        }

        public void ConfigureContent(
            Transform source,
            RunChunkDefinition[] definitions,
            RunChunkDefinition[] authoredOpening)
        {
            distanceSource = source;
            chunkDefinitions = definitions ?? Array.Empty<RunChunkDefinition>();
            openingSequence = authoredOpening ?? Array.Empty<RunChunkDefinition>();
            finiteRoute = false;
            finiteRouteExhausted = false;
            configuredFiniteRouteEndX = 0f;
        }

        public void ConfigureFiniteRoute(RunChunkDefinition[] authoredRoute)
        {
            finiteRoute = true;
            finiteRouteExhausted = false;
            openingSequence = authoredRoute ?? Array.Empty<RunChunkDefinition>();
            configuredFiniteRouteEndX = firstChunkStartX;
            for (int i = 0; i < openingSequence.Length; i++)
            {
                if (openingSequence[i] != null)
                {
                    configuredFiniteRouteEndX += openingSequence[i].Length;
                }
            }

            ResetDirector();
        }

        public void ConfigureDifficulty(GorillaController controller, RunDifficultyProfile profile)
        {
            fuelSource = controller;
            difficultyProfile = profile;
            fairnessState.Reset();
            UpdateDifficulty(true);
        }

        public void AppendValidationErrors(List<string> errors, int simulatedTransitions)
        {
            if (distanceSource == null)
            {
                errors.Add("Run chunk director has no distance source.");
            }

            if (chunkDefinitions == null || chunkDefinitions.Length < 13)
            {
                errors.Add("Run chunk director needs at least thirteen authored definitions including its opening beat.");
                return;
            }

            if (difficultyProfile == null)
            {
                errors.Add("Run chunk director has no difficulty profile.");
            }
            else
            {
                difficultyProfile.AppendValidationErrors(errors);
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

            if (mainPoolCount < 12)
            {
                errors.Add("Run chunk director needs at least twelve main-pool chunks for premium run variety.");
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
                    if (finiteRoute && finiteRouteExhausted)
                    {
                        spawning = false;
                        return;
                    }

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
                    Remember(openingDefinition, fairnessState, GetFuelNormalized());
                    return openingDefinition;
                }
            }

            if (finiteRoute)
            {
                finiteRouteExhausted = true;
                return null;
            }

            float runDistance = GetRunDistance();
            int difficulty = GetDifficulty(runDistance);
            float fuelNormalized = GetFuelNormalized();
            UpdateFuelRescueState(difficultyProfile, fairnessState, fuelNormalized);
            RunChunkDefinition selected = SelectWeightedDefinition(
                chunkDefinitions,
                previousDefinition,
                recentDefinitions,
                difficulty,
                runDistance,
                fuelNormalized,
                difficultyProfile,
                fairnessState,
                random,
                true,
                false);

            if (selected == null)
            {
                selected = SelectWeightedDefinition(
                    chunkDefinitions,
                    previousDefinition,
                    recentDefinitions,
                    difficulty,
                    runDistance,
                    fuelNormalized,
                    difficultyProfile,
                    fairnessState,
                    random,
                    false,
                    false);
            }

            if (selected == null)
            {
                selected = SelectWeightedDefinition(
                    chunkDefinitions,
                    previousDefinition,
                    recentDefinitions,
                    difficulty,
                    runDistance,
                    fuelNormalized,
                    difficultyProfile,
                    fairnessState,
                    random,
                    false,
                    true);

                if (selected != null)
                {
                    Debug.LogWarning(
                        "Run chunk selection used its recovery fallback at " + runDistance.ToString("F1") + " m.",
                        this);
                }
            }

            if (selected != null)
            {
                Remember(selected, fairnessState, fuelNormalized);
            }

            return selected;
        }

        private void SpawnChunk(RunChunkDefinition definition)
        {
            GameObject root = AcquireChunk(definition);
            root.name = "RunChunk_" + chunkIndex.ToString("D3") + "_" + definition.ChunkId;
            root.transform.position = new Vector3(nextChunkStartX, 0f, 0f);
            ActivatePoolables(root);
            root.SetActive(true);
            NotifyPoolables(root, true);

            float endX = nextChunkStartX + definition.Length;
            activeChunks.Add(new ActiveChunk(root, definition, endX));
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
                    RecycleChunk(oldest);
                }
            }
        }

        private void PrewarmPool()
        {
            if (poolPrewarmed)
            {
                return;
            }

            poolPrewarmed = true;
            HashSet<RunChunkDefinition> definitions = new HashSet<RunChunkDefinition>();
            AddDefinitions(definitions, openingSequence);
            AddDefinitions(definitions, chunkDefinitions);
            foreach (RunChunkDefinition definition in definitions)
            {
                GameObject root = CreateChunkInstance(definition);
                StoreChunk(definition, root);
            }

            Debug.Log("[GG_PERF] Prewarmed " + definitions.Count + " authored run chunks for hitch-free reuse.", this);
        }

        private static void AddDefinitions(HashSet<RunChunkDefinition> target, RunChunkDefinition[] source)
        {
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != null)
                {
                    target.Add(source[i]);
                }
            }
        }

        private GameObject AcquireChunk(RunChunkDefinition definition)
        {
            if (chunkPool.TryGetValue(definition, out Stack<GameObject> available))
            {
                while (available.Count > 0)
                {
                    GameObject pooled = available.Pop();
                    if (pooled != null)
                    {
                        return pooled;
                    }
                }
            }

            return CreateChunkInstance(definition);
        }

        private GameObject CreateChunkInstance(RunChunkDefinition definition)
        {
            GameObject root = new GameObject("PooledRunChunk_" + definition.ChunkId);
            root.transform.SetParent(transform, false);
            root.SetActive(false);

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

            DestroyBehindTarget[] standaloneCleanup = root.GetComponentsInChildren<DestroyBehindTarget>(true);
            for (int i = 0; i < standaloneCleanup.Length; i++)
            {
                standaloneCleanup[i].enabled = false;
            }

            CachePoolables(root);

            createdChunkCount++;
            return root;
        }

        private void RecycleChunk(ActiveChunk chunk)
        {
            if (chunk.Root == null || chunk.Definition == null)
            {
                return;
            }

            NotifyPoolables(chunk.Root, false);
            chunk.Root.SetActive(false);
            StoreChunk(chunk.Definition, chunk.Root);
        }

        private void StoreChunk(RunChunkDefinition definition, GameObject root)
        {
            if (!chunkPool.TryGetValue(definition, out Stack<GameObject> available))
            {
                available = new Stack<GameObject>();
                chunkPool.Add(definition, available);
            }

            available.Push(root);
        }

        private void ActivatePoolables(GameObject root)
        {
            IArcadePoolable[] poolables = GetPoolables(root);
            for (int i = 0; i < poolables.Length; i++)
            {
                if (poolables[i] is MonoBehaviour behaviour && behaviour != null)
                {
                    behaviour.gameObject.SetActive(true);
                }
            }
        }

        private void NotifyPoolables(GameObject root, bool spawned)
        {
            IArcadePoolable[] poolables = GetPoolables(root);
            for (int i = 0; i < poolables.Length; i++)
            {
                IArcadePoolable poolable = poolables[i];
                if (poolable == null)
                {
                    continue;
                }

                if (spawned)
                {
                    poolable.OnSpawnedFromPool();
                }
                else
                {
                    poolable.OnDespawnedToPool();
                }
            }
        }

        private IArcadePoolable[] GetPoolables(GameObject root)
        {
            if (!chunkPoolables.TryGetValue(root, out IArcadePoolable[] poolables))
            {
                poolables = CachePoolables(root);
            }

            return poolables;
        }

        private IArcadePoolable[] CachePoolables(GameObject root)
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            List<IArcadePoolable> poolables = new List<IArcadePoolable>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IArcadePoolable poolable)
                {
                    poolables.Add(poolable);
                }
            }

            IArcadePoolable[] cached = poolables.ToArray();
            chunkPoolables[root] = cached;
            return cached;
        }

        private void Remember(RunChunkDefinition definition, FairnessState state, float fuelNormalized)
        {
            previousDefinition = definition;
            recentDefinitions.Add(definition);
            int historyLimit = Mathf.Max(0, recentHistoryLength);
            while (recentDefinitions.Count > historyLimit)
            {
                recentDefinitions.RemoveAt(0);
            }

            AdvanceFairnessState(difficultyProfile, state, definition, fuelNormalized);
        }

        private float GetRunDistance()
        {
            if (distanceSource == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, distanceSource.position.x - firstChunkStartX);
        }

        private int GetDifficulty(float runDistance)
        {
            if (difficultyProfile != null)
            {
                return difficultyProfile.GetStageIndex(runDistance);
            }

            if (distancePerDifficultyStep <= 0f)
            {
                return 0;
            }

            return Mathf.Clamp(Mathf.FloorToInt(runDistance / distancePerDifficultyStep), 0, maximumDifficulty);
        }

        private float GetFuelNormalized()
        {
            if (fuelSource == null || fuelSource.MaxFuel <= 0.01f)
            {
                return 1f;
            }

            return Mathf.Clamp01(fuelSource.CurrentFuel / fuelSource.MaxFuel);
        }

        private void UpdateDifficulty(bool forceNotification)
        {
            CurrentRunDistance = GetRunDistance();
            CurrentDifficultyStage = GetDifficulty(CurrentRunDistance);
            CurrentIntensity = difficultyProfile != null
                ? difficultyProfile.EvaluateIntensity(CurrentRunDistance)
                : Mathf.Clamp01(CurrentDifficultyStage / (float)Mathf.Max(1, maximumDifficulty));

            float speedMultiplier = difficultyProfile != null
                ? difficultyProfile.EvaluateSpeedMultiplier(CurrentRunDistance)
                : 1f;
            if (fuelSource != null)
            {
                fuelSource.SetDifficultySpeedMultiplier(speedMultiplier);
            }

            bool stageChanged = CurrentDifficultyStage != lastReportedStage;
            bool intensityChanged = Mathf.Abs(CurrentIntensity - lastReportedIntensity) >= 0.01f;
            if (forceNotification || stageChanged || intensityChanged)
            {
                if (stageChanged && difficultyProfile != null)
                {
                    Debug.Log(
                        "[GG_DIFFICULTY] Entered " + difficultyProfile.GetStageName(CurrentDifficultyStage) +
                        " at " + CurrentRunDistance.ToString("F0") + "m, intensity=" + CurrentIntensity.ToString("F2") +
                        ", speed=" + speedMultiplier.ToString("F2") + "x.",
                        this);
                }

                lastReportedStage = CurrentDifficultyStage;
                lastReportedIntensity = CurrentIntensity;
                DifficultyChanged?.Invoke(CurrentIntensity, CurrentDifficultyStage);
            }
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
            if (difficultyProfile == null)
            {
                return;
            }

            int transitionsPerStage = Mathf.Max(100, count);
            RunSimulationMetrics firstMetrics = default;
            RunSimulationMetrics finalMetrics = default;

            for (int stage = 0; stage < difficultyProfile.StageCount; stage++)
            {
                RunSimulationMetrics metrics = SimulateStage(errors, stage, transitionsPerStage);
                if (stage == 0)
                {
                    firstMetrics = metrics;
                }

                if (stage == difficultyProfile.StageCount - 1)
                {
                    finalMetrics = metrics;
                }

                if (metrics.RecoveryRate < 0.1f)
                {
                    errors.Add("Difficulty stage " + difficultyProfile.GetStageName(stage) +
                        " produces too little recovery space: " + metrics.RecoveryRate.ToString("P1") + ".");
                }

                if (stage == 0 && metrics.PredatorCount > 0)
                {
                    errors.Add("Welcome-stage simulation selected a predator before its unlock distance.");
                }

                Debug.Log(
                    "[GG_DIFFICULTY] " + difficultyProfile.GetStageName(stage) +
                    " hazard=" + metrics.HazardRate.ToString("P1") +
                    " predator=" + metrics.PredatorRate.ToString("P1") +
                    " recovery=" + metrics.RecoveryRate.ToString("P1") +
                    " maxPressure=" + metrics.MaximumPressure + ".",
                    this);
            }

            if (finalMetrics.HazardRate <= firstMetrics.HazardRate + 0.025f)
            {
                errors.Add("Final difficulty simulation does not increase hazard pressure enough over the opening stage.");
            }

            if (finalMetrics.PredatorRate <= firstMetrics.PredatorRate + 0.01f)
            {
                errors.Add("Final difficulty simulation does not introduce a measurable predator cadence.");
            }

            ValidateLowFuelRecovery(errors);
        }

        private RunSimulationMetrics SimulateStage(List<string> errors, int stage, int count)
        {
            System.Random simulationRandom = new System.Random(8675309 + stage * 7919);
            List<RunChunkDefinition> history = new List<RunChunkDefinition>();
            FairnessState state = new FairnessState();
            RunChunkDefinition previous = openingSequence != null && openingSequence.Length > 0
                ? openingSequence[openingSequence.Length - 1]
                : null;
            float distance = difficultyProfile.GetStageRepresentativeDistance(stage);
            const float fuelNormalized = 0.72f;
            int hazards = 0;
            int predators = 0;
            int recoveries = 0;
            int maxPressureSeen = 0;

            for (int i = 0; i < count; i++)
            {
                UpdateFuelRescueState(difficultyProfile, state, fuelNormalized);
                RunChunkDefinition selected = SelectWeightedDefinition(
                    chunkDefinitions,
                    previous,
                    history,
                    stage,
                    distance,
                    fuelNormalized,
                    difficultyProfile,
                    state,
                    simulationRandom,
                    true,
                    false);

                if (selected == null)
                {
                    selected = SelectWeightedDefinition(
                        chunkDefinitions,
                        previous,
                        history,
                        stage,
                        distance,
                        fuelNormalized,
                        difficultyProfile,
                        state,
                        simulationRandom,
                        false,
                        false);
                }

                if (selected == null)
                {
                    selected = SelectWeightedDefinition(
                        chunkDefinitions,
                        previous,
                        history,
                        stage,
                        distance,
                        fuelNormalized,
                        difficultyProfile,
                        state,
                        simulationRandom,
                        false,
                        true);
                }

                if (selected == null)
                {
                    errors.Add("Difficulty simulation found no fair selection in " +
                        difficultyProfile.GetStageName(stage) + " at transition " + i + ".");
                    break;
                }

                if (!selected.CanFollow(previous))
                {
                    errors.Add("Difficulty simulation produced an incompatible transition from " +
                        (previous != null ? previous.ChunkId : "Start") + " to " + selected.ChunkId + ".");
                    break;
                }

                bool isHazard = HasTag(selected, RunChunkTag.Hazard);
                bool isPredator = HasTag(selected, RunChunkTag.Predator);
                bool isRecovery = HasTag(selected, RunChunkTag.Recovery);
                if (isHazard && previous != null && HasTag(previous, RunChunkTag.Hazard))
                {
                    errors.Add("Difficulty simulation selected consecutive hazards in " +
                        difficultyProfile.GetStageName(stage) + ".");
                    break;
                }

                if (isPredator && state.PredatorCooldownRemaining > 0)
                {
                    errors.Add("Difficulty simulation violated the predator cooldown in " +
                        difficultyProfile.GetStageName(stage) + ".");
                    break;
                }

                if ((state.RecoveryRequired || state.Pressure >= difficultyProfile.MaximumPressure) && !isRecovery)
                {
                    errors.Add("Difficulty simulation failed to force recovery after pressure in " +
                        difficultyProfile.GetStageName(stage) + ".");
                    break;
                }

                AdvanceFairnessState(difficultyProfile, state, selected, fuelNormalized);
                maxPressureSeen = Mathf.Max(maxPressureSeen, state.Pressure);
                if (state.Pressure > difficultyProfile.MaximumPressure)
                {
                    errors.Add("Difficulty simulation exceeded its pressure limit in " +
                        difficultyProfile.GetStageName(stage) + ".");
                    break;
                }

                hazards += isHazard ? 1 : 0;
                predators += isPredator ? 1 : 0;
                recoveries += isRecovery ? 1 : 0;
                previous = selected;
                RememberForSimulation(history, selected);
            }

            return new RunSimulationMetrics(count, hazards, predators, recoveries, maxPressureSeen);
        }

        private void ValidateLowFuelRecovery(List<string> errors)
        {
            int stage = Mathf.Max(0, difficultyProfile.StageCount - 1);
            float distance = difficultyProfile.GetStageRepresentativeDistance(stage);
            float fuelNormalized = Mathf.Max(0.01f, difficultyProfile.LowFuelThreshold * 0.65f);
            FairnessState state = new FairnessState();
            List<RunChunkDefinition> history = new List<RunChunkDefinition>();
            RunChunkDefinition previous = openingSequence != null && openingSequence.Length > 0
                ? openingSequence[openingSequence.Length - 1]
                : null;
            System.Random simulationRandom = new System.Random(424242);
            bool offeredFuel = false;

            for (int i = 0; i < difficultyProfile.LowFuelRecoveryDeadline; i++)
            {
                UpdateFuelRescueState(difficultyProfile, state, fuelNormalized);
                RunChunkDefinition selected = SelectWeightedDefinition(
                    chunkDefinitions,
                    previous,
                    history,
                    stage,
                    distance,
                    fuelNormalized,
                    difficultyProfile,
                    state,
                    simulationRandom,
                    false,
                    false);

                if (selected == null)
                {
                    selected = SelectWeightedDefinition(
                        chunkDefinitions,
                        previous,
                        history,
                        stage,
                        distance,
                        fuelNormalized,
                        difficultyProfile,
                        state,
                        simulationRandom,
                        false,
                        true);
                }

                if (selected == null)
                {
                    break;
                }

                offeredFuel = HasTag(selected, RunChunkTag.Fuel) || HasTag(selected, RunChunkTag.Recovery);
                AdvanceFairnessState(difficultyProfile, state, selected, fuelNormalized);
                previous = selected;
                RememberForSimulation(history, selected);
                if (offeredFuel)
                {
                    break;
                }
            }

            if (!offeredFuel)
            {
                errors.Add("Low-fuel simulation did not offer fuel or recovery within " +
                    difficultyProfile.LowFuelRecoveryDeadline + " generated chunks.");
            }
        }

        private void RememberForSimulation(List<RunChunkDefinition> history, RunChunkDefinition selected)
        {
            history.Add(selected);
            while (history.Count > Mathf.Max(0, recentHistoryLength))
            {
                history.RemoveAt(0);
            }
        }

        private static RunChunkDefinition SelectWeightedDefinition(
            RunChunkDefinition[] definitions,
            RunChunkDefinition previous,
            List<RunChunkDefinition> history,
            int difficulty,
            float distance,
            float fuelNormalized,
            RunDifficultyProfile profile,
            FairnessState state,
            System.Random source,
            bool respectHistory,
            bool recoveryOnly)
        {
            if (definitions == null || source == null)
            {
                return null;
            }

            float totalWeight = 0f;
            for (int i = 0; i < definitions.Length; i++)
            {
                RunChunkDefinition candidate = definitions[i];
                if (!IsEligible(
                    candidate,
                    previous,
                    history,
                    difficulty,
                    distance,
                    fuelNormalized,
                    profile,
                    state,
                    respectHistory,
                    recoveryOnly))
                {
                    continue;
                }

                totalWeight += GetCandidateWeight(candidate, difficulty, profile, state);
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            double roll = source.NextDouble() * totalWeight;
            for (int i = 0; i < definitions.Length; i++)
            {
                RunChunkDefinition candidate = definitions[i];
                if (!IsEligible(
                    candidate,
                    previous,
                    history,
                    difficulty,
                    distance,
                    fuelNormalized,
                    profile,
                    state,
                    respectHistory,
                    recoveryOnly))
                {
                    continue;
                }

                roll -= GetCandidateWeight(candidate, difficulty, profile, state);
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
            float distance,
            float fuelNormalized,
            RunDifficultyProfile profile,
            FairnessState state,
            bool respectHistory,
            bool recoveryOnly)
        {
            if (candidate == null || !candidate.AllowInMainPool || candidate.SelectionWeight <= 0f)
            {
                return false;
            }

            if (!candidate.SupportsDifficulty(difficulty) || !candidate.CanFollow(previous))
            {
                return false;
            }

            if (respectHistory && history != null && history.Contains(candidate))
            {
                return false;
            }

            bool isRecovery = HasTag(candidate, RunChunkTag.Recovery);
            bool isFuel = HasTag(candidate, RunChunkTag.Fuel);
            bool isHazard = HasTag(candidate, RunChunkTag.Hazard);
            bool isPredator = HasTag(candidate, RunChunkTag.Predator);
            if (recoveryOnly && !isRecovery)
            {
                return false;
            }

            if (profile == null || state == null)
            {
                return true;
            }

            if ((state.RecoveryRequired || state.Pressure >= profile.MaximumPressure) && !isRecovery)
            {
                return false;
            }

            if (state.LowFuelRescueActive && state.FuelOpportunityDeadline <= 1 && !isFuel && !isRecovery)
            {
                return false;
            }

            if (isHazard && previous != null && HasTag(previous, RunChunkTag.Hazard))
            {
                return false;
            }

            if (GetPressureCost(candidate) + state.Pressure > profile.MaximumPressure && !isRecovery)
            {
                return false;
            }

            if (isPredator)
            {
                if (distance < profile.PredatorUnlockDistance || state.PredatorCooldownRemaining > 0)
                {
                    return false;
                }

                if (state.LowFuelRescueActive || fuelNormalized < profile.FuelRecoveryThreshold)
                {
                    return false;
                }
            }

            return true;
        }

        private static float GetCandidateWeight(
            RunChunkDefinition candidate,
            int difficulty,
            RunDifficultyProfile profile,
            FairnessState state)
        {
            float weight = candidate.SelectionWeight;
            if (profile == null)
            {
                return weight;
            }

            weight *= profile.EvaluateTagWeight(difficulty, candidate.Tags);
            if (state != null && state.LowFuelRescueActive)
            {
                if (HasTag(candidate, RunChunkTag.Fuel))
                {
                    weight *= profile.LowFuelFuelMultiplier;
                }

                if (HasTag(candidate, RunChunkTag.Recovery))
                {
                    weight *= profile.LowFuelRecoveryMultiplier;
                }

                if (GetPressureCost(candidate) > 0)
                {
                    weight *= profile.LowFuelPressureMultiplier;
                }
            }

            return Mathf.Max(0f, weight);
        }

        private static void UpdateFuelRescueState(
            RunDifficultyProfile profile,
            FairnessState state,
            float fuelNormalized)
        {
            if (profile == null || state == null)
            {
                return;
            }

            if (!state.LowFuelRescueActive && fuelNormalized <= profile.LowFuelThreshold)
            {
                state.LowFuelRescueActive = true;
                state.FuelOpportunityDeadline = profile.LowFuelRecoveryDeadline;
            }
            else if (state.LowFuelRescueActive && fuelNormalized >= profile.FuelRecoveryThreshold)
            {
                state.LowFuelRescueActive = false;
                state.FuelOpportunityDeadline = profile.LowFuelRecoveryDeadline;
            }
        }

        private static void AdvanceFairnessState(
            RunDifficultyProfile profile,
            FairnessState state,
            RunChunkDefinition selected,
            float fuelNormalized)
        {
            if (profile == null || state == null || selected == null)
            {
                return;
            }

            bool isPredator = HasTag(selected, RunChunkTag.Predator);
            bool isRecovery = HasTag(selected, RunChunkTag.Recovery);
            bool isFuel = HasTag(selected, RunChunkTag.Fuel);

            if (isPredator)
            {
                state.PredatorCooldownRemaining = profile.PredatorCooldownChunks;
                state.RecoveryRequired = true;
            }
            else if (state.PredatorCooldownRemaining > 0)
            {
                state.PredatorCooldownRemaining--;
            }

            if (isRecovery)
            {
                state.Pressure = 0;
                state.RecoveryRequired = false;
            }
            else
            {
                if (HasTag(selected, RunChunkTag.Beginner))
                {
                    state.Pressure = Mathf.Max(0, state.Pressure - 1);
                }

                state.Pressure += GetPressureCost(selected);
            }

            UpdateFuelRescueState(profile, state, fuelNormalized);
            if (state.LowFuelRescueActive)
            {
                if (isFuel || isRecovery)
                {
                    state.FuelOpportunityDeadline = profile.LowFuelRecoveryDeadline;
                }
                else
                {
                    state.FuelOpportunityDeadline = Mathf.Max(0, state.FuelOpportunityDeadline - 1);
                }
            }
        }

        private static int GetPressureCost(RunChunkDefinition definition)
        {
            if (definition == null || HasTag(definition, RunChunkTag.Recovery))
            {
                return 0;
            }

            if (HasTag(definition, RunChunkTag.Predator))
            {
                return 2;
            }

            return HasTag(definition, RunChunkTag.Hazard) || HasTag(definition, RunChunkTag.Boost) ? 1 : 0;
        }

        private static bool HasTag(RunChunkDefinition definition, RunChunkTag tag)
        {
            return definition != null && (definition.Tags & tag) != 0;
        }

        private sealed class FairnessState
        {
            public int Pressure;
            public int PredatorCooldownRemaining;
            public bool RecoveryRequired;
            public bool LowFuelRescueActive;
            public int FuelOpportunityDeadline;

            public void Reset()
            {
                Pressure = 0;
                PredatorCooldownRemaining = 0;
                RecoveryRequired = false;
                LowFuelRescueActive = false;
                FuelOpportunityDeadline = int.MaxValue;
            }
        }

        private readonly struct RunSimulationMetrics
        {
            public readonly int PredatorCount;
            public readonly int MaximumPressure;
            public readonly float HazardRate;
            public readonly float PredatorRate;
            public readonly float RecoveryRate;

            public RunSimulationMetrics(
                int count,
                int hazards,
                int predators,
                int recoveries,
                int maximumPressure)
            {
                int safeCount = Mathf.Max(1, count);
                PredatorCount = predators;
                MaximumPressure = maximumPressure;
                HazardRate = hazards / (float)safeCount;
                PredatorRate = predators / (float)safeCount;
                RecoveryRate = recoveries / (float)safeCount;
            }
        }

        private readonly struct ActiveChunk
        {
            public readonly GameObject Root;
            public readonly RunChunkDefinition Definition;
            public readonly float EndX;

            public ActiveChunk(GameObject root, RunChunkDefinition definition, float endX)
            {
                Root = root;
                Definition = definition;
                EndX = endX;
            }
        }
    }
}
