using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [Serializable]
    public sealed class RunTagWeightMultiplier
    {
        [SerializeField] private RunChunkTag tags;
        [Min(0f)] [SerializeField] private float multiplier = 1f;

        public RunChunkTag Tags { get { return tags; } }
        public float Multiplier { get { return Mathf.Max(0f, multiplier); } }

        public RunTagWeightMultiplier(RunChunkTag tags, float multiplier)
        {
            this.tags = tags;
            this.multiplier = Mathf.Max(0f, multiplier);
        }
    }

    [Serializable]
    public sealed class RunDifficultyStage
    {
        [SerializeField] private string displayName = "Stage";
        [Min(0f)] [SerializeField] private float startDistance;
        [Min(0.1f)] [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private RunTagWeightMultiplier[] tagWeights = Array.Empty<RunTagWeightMultiplier>();

        public string DisplayName { get { return displayName; } }
        public float StartDistance { get { return Mathf.Max(0f, startDistance); } }
        public float SpeedMultiplier { get { return Mathf.Max(0.1f, speedMultiplier); } }
        public RunTagWeightMultiplier[] TagWeights { get { return tagWeights; } }

        public RunDifficultyStage(
            string displayName,
            float startDistance,
            float speedMultiplier,
            RunTagWeightMultiplier[] tagWeights)
        {
            this.displayName = displayName;
            this.startDistance = Mathf.Max(0f, startDistance);
            this.speedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            this.tagWeights = tagWeights ?? Array.Empty<RunTagWeightMultiplier>();
        }

        public float EvaluateTagWeight(RunChunkTag candidateTags)
        {
            float weight = 1f;
            if (tagWeights == null)
            {
                return weight;
            }

            for (int i = 0; i < tagWeights.Length; i++)
            {
                RunTagWeightMultiplier modifier = tagWeights[i];
                if (modifier != null && (candidateTags & modifier.Tags) != 0)
                {
                    weight *= modifier.Multiplier;
                }
            }

            return Mathf.Max(0f, weight);
        }
    }

    [CreateAssetMenu(fileName = "GG_RunDifficulty", menuName = "First Bloom/Gassy Gorilla/Run Difficulty Profile")]
    public sealed class RunDifficultyProfile : ScriptableObject
    {
        [Header("Crescendo")]
        [SerializeField] private RunDifficultyStage[] stages = Array.Empty<RunDifficultyStage>();
        [Min(1f)] [SerializeField] private float maximumIntensityDistance = 550f;
        [Min(0.1f)] [SerializeField] private float maximumSpeedMultiplier = 1.14f;

        [Header("Fairness")]
        [Min(0f)] [SerializeField] private float predatorUnlockDistance = 90f;
        [Min(1)] [SerializeField] private int maximumPressure = 3;
        [Min(1)] [SerializeField] private int predatorCooldownChunks = 4;
        [Range(0f, 1f)] [SerializeField] private float lowFuelThreshold = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float fuelRecoveryThreshold = 0.45f;
        [Min(1)] [SerializeField] private int lowFuelRecoveryDeadline = 2;

        [Header("Low Fuel Weighting")]
        [Min(1f)] [SerializeField] private float lowFuelFuelMultiplier = 2.3f;
        [Min(1f)] [SerializeField] private float lowFuelRecoveryMultiplier = 2f;
        [Range(0f, 1f)] [SerializeField] private float lowFuelPressureMultiplier = 0.35f;

        public int StageCount { get { return stages != null ? stages.Length : 0; } }
        public float MaximumIntensityDistance { get { return Mathf.Max(1f, maximumIntensityDistance); } }
        public float MaximumSpeedMultiplier { get { return Mathf.Max(0.1f, maximumSpeedMultiplier); } }
        public float PredatorUnlockDistance { get { return Mathf.Max(0f, predatorUnlockDistance); } }
        public int MaximumPressure { get { return Mathf.Max(1, maximumPressure); } }
        public int PredatorCooldownChunks { get { return Mathf.Max(1, predatorCooldownChunks); } }
        public float LowFuelThreshold { get { return Mathf.Clamp01(lowFuelThreshold); } }
        public float FuelRecoveryThreshold { get { return Mathf.Clamp01(fuelRecoveryThreshold); } }
        public int LowFuelRecoveryDeadline { get { return Mathf.Max(1, lowFuelRecoveryDeadline); } }
        public float LowFuelFuelMultiplier { get { return Mathf.Max(1f, lowFuelFuelMultiplier); } }
        public float LowFuelRecoveryMultiplier { get { return Mathf.Max(1f, lowFuelRecoveryMultiplier); } }
        public float LowFuelPressureMultiplier { get { return Mathf.Clamp01(lowFuelPressureMultiplier); } }

        public int GetStageIndex(float distance)
        {
            if (stages == null || stages.Length == 0)
            {
                return 0;
            }

            float safeDistance = Mathf.Max(0f, distance);
            int stage = 0;
            for (int i = 1; i < stages.Length; i++)
            {
                RunDifficultyStage candidate = stages[i];
                if (candidate == null || safeDistance < candidate.StartDistance)
                {
                    break;
                }

                stage = i;
            }

            return stage;
        }

        public string GetStageName(int stageIndex)
        {
            RunDifficultyStage stage = GetStage(stageIndex);
            return stage != null && !string.IsNullOrWhiteSpace(stage.DisplayName)
                ? stage.DisplayName
                : "Stage " + Mathf.Max(0, stageIndex);
        }

        public float GetStageStartDistance(int stageIndex)
        {
            RunDifficultyStage stage = GetStage(stageIndex);
            return stage != null ? stage.StartDistance : 0f;
        }

        public float GetStageRepresentativeDistance(int stageIndex)
        {
            RunDifficultyStage stage = GetStage(stageIndex);
            if (stage == null)
            {
                return 0f;
            }

            RunDifficultyStage next = GetStage(stageIndex + 1);
            float endDistance = next != null ? next.StartDistance : MaximumIntensityDistance;
            return Mathf.Lerp(stage.StartDistance, endDistance, 0.5f);
        }

        public float EvaluateIntensity(float distance)
        {
            return Mathf.Clamp01(Mathf.Max(0f, distance) / MaximumIntensityDistance);
        }

        public float EvaluateSpeedMultiplier(float distance)
        {
            RunDifficultyStage stage = GetStage(GetStageIndex(distance));
            if (stage == null)
            {
                return 1f;
            }

            float safeDistance = Mathf.Max(0f, distance);
            int stageIndex = GetStageIndex(safeDistance);
            RunDifficultyStage nextStage = GetStage(stageIndex + 1);
            float targetDistance = nextStage != null ? nextStage.StartDistance : MaximumIntensityDistance;
            float targetSpeed = nextStage != null ? nextStage.SpeedMultiplier : MaximumSpeedMultiplier;

            if (targetDistance <= stage.StartDistance + 0.01f)
            {
                return targetSpeed;
            }

            float t = Mathf.InverseLerp(stage.StartDistance, targetDistance, safeDistance);
            return Mathf.Lerp(stage.SpeedMultiplier, targetSpeed, Mathf.SmoothStep(0f, 1f, t));
        }

        public float EvaluateTagWeight(int stageIndex, RunChunkTag tags)
        {
            RunDifficultyStage stage = GetStage(stageIndex);
            return stage != null ? stage.EvaluateTagWeight(tags) : 1f;
        }

        public void Configure(
            RunDifficultyStage[] configuredStages,
            float intensityDistance,
            float maxSpeedMultiplier,
            float predatorUnlock,
            int pressureLimit,
            int predatorCooldown,
            float lowFuel,
            float recoveredFuel,
            int recoveryDeadline,
            float fuelWeight,
            float recoveryWeight,
            float pressureWeight)
        {
            stages = configuredStages ?? Array.Empty<RunDifficultyStage>();
            maximumIntensityDistance = Mathf.Max(1f, intensityDistance);
            maximumSpeedMultiplier = Mathf.Max(0.1f, maxSpeedMultiplier);
            predatorUnlockDistance = Mathf.Max(0f, predatorUnlock);
            maximumPressure = Mathf.Max(1, pressureLimit);
            predatorCooldownChunks = Mathf.Max(1, predatorCooldown);
            lowFuelThreshold = Mathf.Clamp01(lowFuel);
            fuelRecoveryThreshold = Mathf.Clamp01(recoveredFuel);
            lowFuelRecoveryDeadline = Mathf.Max(1, recoveryDeadline);
            lowFuelFuelMultiplier = Mathf.Max(1f, fuelWeight);
            lowFuelRecoveryMultiplier = Mathf.Max(1f, recoveryWeight);
            lowFuelPressureMultiplier = Mathf.Clamp01(pressureWeight);
        }

        public void AppendValidationErrors(List<string> errors)
        {
            if (stages == null || stages.Length != 5)
            {
                errors.Add("Run difficulty profile must define exactly five crescendo stages.");
                return;
            }

            float previousDistance = -1f;
            float previousSpeed = 0f;
            for (int i = 0; i < stages.Length; i++)
            {
                RunDifficultyStage stage = stages[i];
                if (stage == null)
                {
                    errors.Add("Run difficulty profile has a missing stage at index " + i + ".");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(stage.DisplayName))
                {
                    errors.Add("Run difficulty stage " + i + " has no display name.");
                }

                if (stage.StartDistance <= previousDistance)
                {
                    errors.Add("Run difficulty stage distances must be strictly increasing.");
                }

                if (stage.SpeedMultiplier < previousSpeed)
                {
                    errors.Add("Run difficulty speed multipliers must not decrease.");
                }

                previousDistance = stage.StartDistance;
                previousSpeed = stage.SpeedMultiplier;
            }

            if (stages[0] != null && stages[0].StartDistance > 0.01f)
            {
                errors.Add("Run difficulty profile must begin at zero metres.");
            }

            if (MaximumIntensityDistance <= previousDistance)
            {
                errors.Add("Maximum intensity distance must occur after the final stage begins.");
            }

            if (MaximumSpeedMultiplier < previousSpeed || MaximumSpeedMultiplier > 1.2f)
            {
                errors.Add("Maximum speed multiplier must remain between the final stage speed and 1.20.");
            }

            if (LowFuelThreshold >= FuelRecoveryThreshold)
            {
                errors.Add("Fuel recovery threshold must be above the low-fuel threshold.");
            }
        }

        private RunDifficultyStage GetStage(int stageIndex)
        {
            if (stages == null || stages.Length == 0 || stageIndex < 0 || stageIndex >= stages.Length)
            {
                return null;
            }

            return stages[stageIndex];
        }
    }
}
