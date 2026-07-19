using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public enum GassyExpeditionObjectiveType
    {
        ReachFinish,
        CollectFood,
        VineReleases,
        CrocodileDodges,
        FinishWithFuel,
        CompleteInteraction,
        CompleteInteractionSet
    }

    [CreateAssetMenu(fileName = "GG_Expedition_", menuName = "First Bloom/Gassy Gorilla/Expedition")]
    public sealed class GassyExpeditionDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string expeditionId = "expedition";
        [Min(0)] [SerializeField] private int orderIndex;
        [Min(0)] [SerializeField] private int chapterIndex;
        [SerializeField] private string chapterTitle = "Chapter";
        [SerializeField] private string displayTitle = "Expedition";

        [Header("Story")]
        [TextArea(2, 4)] [SerializeField] private string openingStory;
        [TextArea(2, 4)] [SerializeField] private string successStory;
        [TextArea(1, 3)] [SerializeField] private string lessonText;

        [Header("Objective")]
        [SerializeField] private GassyExpeditionObjectiveType objectiveType;
        [TextArea(1, 3)] [SerializeField] private string objectiveText;
        [Min(0)] [SerializeField] private int targetCount;
        [Min(0f)] [SerializeField] private float targetFuel;
        [SerializeField] private GassyInteractionType targetInteraction;
        [SerializeField] private GassyInteractionType requiredInteractions;

        [Header("Route")]
        [SerializeField] private RunChunkDefinition[] route = Array.Empty<RunChunkDefinition>();
        [Min(0.5f)] [SerializeField] private float finishInset = 1.2f;

        [Header("Stars")]
        [Range(0f, 100f)] [SerializeField] private float twoStarFuel = 35f;
        [Range(0f, 100f)] [SerializeField] private float threeStarFuel = 65f;

        public string ExpeditionId { get { return expeditionId; } }
        public int OrderIndex { get { return orderIndex; } }
        public int ChapterIndex { get { return chapterIndex; } }
        public string ChapterTitle { get { return chapterTitle; } }
        public string DisplayTitle { get { return displayTitle; } }
        public string OpeningStory { get { return openingStory; } }
        public string SuccessStory { get { return successStory; } }
        public string LessonText { get { return lessonText; } }
        public GassyExpeditionObjectiveType ObjectiveType { get { return objectiveType; } }
        public string ObjectiveText { get { return objectiveText; } }
        public int TargetCount { get { return targetCount; } }
        public float TargetFuel { get { return targetFuel; } }
        public GassyInteractionType TargetInteraction { get { return targetInteraction; } }
        public GassyInteractionType RequiredInteractions { get { return requiredInteractions; } }
        public RunChunkDefinition[] Route { get { return route; } }
        public float FinishInset { get { return finishInset; } }

        public float RouteLength
        {
            get
            {
                float length = 0f;
                if (route == null)
                {
                    return length;
                }

                for (int i = 0; i < route.Length; i++)
                {
                    if (route[i] != null)
                    {
                        length += route[i].Length;
                    }
                }

                return length;
            }
        }

        public void Configure(
            string id,
            int index,
            int chapter,
            string chapterName,
            string title,
            string opening,
            string success,
            string lesson,
            GassyExpeditionObjectiveType type,
            string objective,
            int count,
            float fuel,
            GassyInteractionType interaction,
            GassyInteractionType interactionSet,
            RunChunkDefinition[] authoredRoute,
            float inset,
            float silverFuel,
            float goldFuel)
        {
            expeditionId = id;
            orderIndex = Mathf.Max(0, index);
            chapterIndex = Mathf.Max(0, chapter);
            chapterTitle = chapterName;
            displayTitle = title;
            openingStory = opening;
            successStory = success;
            lessonText = lesson;
            objectiveType = type;
            objectiveText = objective;
            targetCount = Mathf.Max(0, count);
            targetFuel = Mathf.Max(0f, fuel);
            targetInteraction = interaction;
            requiredInteractions = interactionSet;
            route = authoredRoute ?? Array.Empty<RunChunkDefinition>();
            finishInset = Mathf.Max(0.5f, inset);
            twoStarFuel = Mathf.Clamp(silverFuel, 0f, 100f);
            threeStarFuel = Mathf.Clamp(goldFuel, twoStarFuel, 100f);
        }

        public int CalculateStars(float finishFuel)
        {
            if (finishFuel + 0.01f >= threeStarFuel)
            {
                return 3;
            }

            if (finishFuel + 0.01f >= twoStarFuel)
            {
                return 2;
            }

            return 1;
        }

        public int CountObjectiveOpportunities()
        {
            if (route == null)
            {
                return 0;
            }

            int count = 0;
            for (int routeIndex = 0; routeIndex < route.Length; routeIndex++)
            {
                RunChunkDefinition chunk = route[routeIndex];
                if (chunk == null || chunk.Spawns == null)
                {
                    continue;
                }

                RunChunkSpawn[] spawns = chunk.Spawns;
                for (int spawnIndex = 0; spawnIndex < spawns.Length; spawnIndex++)
                {
                    RunChunkSpawn spawn = spawns[spawnIndex];
                    if (spawn == null || spawn.Prefab == null)
                    {
                        continue;
                    }

                    if (objectiveType == GassyExpeditionObjectiveType.CollectFood &&
                        spawn.Kind == RunChunkSpawnKind.Pickup)
                    {
                        count++;
                    }
                    else if (objectiveType == GassyExpeditionObjectiveType.VineReleases &&
                        spawn.Kind == RunChunkSpawnKind.SwingVine)
                    {
                        count++;
                    }
                    else if (objectiveType == GassyExpeditionObjectiveType.CrocodileDodges &&
                        spawn.Prefab.GetComponentInChildren<CrocodileAmbushController>(true) != null)
                    {
                        count++;
                    }
                    else if (objectiveType == GassyExpeditionObjectiveType.CompleteInteraction)
                    {
                        GassyInteractionMarker marker =
                            spawn.Prefab.GetComponentInChildren<GassyInteractionMarker>(true);
                        if (marker != null && marker.InteractionType == targetInteraction)
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }

        public GassyInteractionType CollectAvailableInteractions()
        {
            if (route == null)
            {
                return GassyInteractionType.None;
            }

            GassyInteractionType available = GassyInteractionType.None;
            for (int routeIndex = 0; routeIndex < route.Length; routeIndex++)
            {
                RunChunkDefinition chunk = route[routeIndex];
                if (chunk == null || chunk.Spawns == null)
                {
                    continue;
                }

                RunChunkSpawn[] spawns = chunk.Spawns;
                for (int spawnIndex = 0; spawnIndex < spawns.Length; spawnIndex++)
                {
                    RunChunkSpawn spawn = spawns[spawnIndex];
                    if (spawn == null || spawn.Prefab == null)
                    {
                        continue;
                    }

                    GassyInteractionMarker marker =
                        spawn.Prefab.GetComponentInChildren<GassyInteractionMarker>(true);
                    if (marker != null)
                    {
                        available |= marker.InteractionType;
                    }
                }
            }

            return available;
        }

        public void AppendValidationErrors(List<string> errors)
        {
            string label = string.IsNullOrWhiteSpace(displayTitle) ? name : displayTitle;
            if (string.IsNullOrWhiteSpace(expeditionId))
            {
                errors.Add(label + " has no expedition id.");
            }

            if (string.IsNullOrWhiteSpace(displayTitle) ||
                string.IsNullOrWhiteSpace(chapterTitle) ||
                string.IsNullOrWhiteSpace(openingStory) ||
                string.IsNullOrWhiteSpace(successStory) ||
                string.IsNullOrWhiteSpace(lessonText) ||
                string.IsNullOrWhiteSpace(objectiveText))
            {
                errors.Add(label + " is missing player-facing chapter, title, story, lesson, success, or objective copy.");
            }

            if (route == null || route.Length < 4)
            {
                errors.Add(label + " needs at least four authored chunks.");
                return;
            }

            for (int i = 0; i < route.Length; i++)
            {
                if (route[i] == null)
                {
                    errors.Add(label + " has a missing route chunk at index " + i + ".");
                    continue;
                }

                if (i > 0 && route[i - 1] != null && !route[i].CanFollow(route[i - 1]))
                {
                    errors.Add(
                        label + " has an invalid route transition from '" +
                        route[i - 1].name + "' to '" + route[i].name + "'.");
                }
            }

            if (RouteLength < 30f)
            {
                errors.Add(label + " is too short to support a readable finite run.");
            }

            if (finishInset >= RouteLength)
            {
                errors.Add(label + " places its finish inset beyond the route.");
            }

            if ((objectiveType == GassyExpeditionObjectiveType.CollectFood ||
                objectiveType == GassyExpeditionObjectiveType.VineReleases ||
                objectiveType == GassyExpeditionObjectiveType.CrocodileDodges ||
                objectiveType == GassyExpeditionObjectiveType.CompleteInteraction) &&
                targetCount <= 0)
            {
                errors.Add(label + " has a count objective without a positive target.");
            }

            int opportunities = CountObjectiveOpportunities();
            if (targetCount > 0 && opportunities < targetCount)
            {
                errors.Add(label + " only provides " + opportunities + " opportunities for a target of " + targetCount + ".");
            }

            if (objectiveType == GassyExpeditionObjectiveType.FinishWithFuel &&
                (targetFuel < 1f || targetFuel > 100f))
            {
                errors.Add(label + " has an invalid finish-fuel requirement.");
            }

            if (objectiveType == GassyExpeditionObjectiveType.CompleteInteraction)
            {
                int interactionValue = (int)targetInteraction;
                if (targetInteraction == GassyInteractionType.None ||
                    (interactionValue & (interactionValue - 1)) != 0)
                {
                    errors.Add(label + " needs exactly one target interaction.");
                }
            }

            if (objectiveType == GassyExpeditionObjectiveType.CompleteInteractionSet)
            {
                if (requiredInteractions == GassyInteractionType.None)
                {
                    errors.Add(label + " has an empty interaction mastery set.");
                }
                else
                {
                    GassyInteractionType available = CollectAvailableInteractions();
                    GassyInteractionType missing = requiredInteractions & ~available;
                    if (missing != GassyInteractionType.None)
                    {
                        errors.Add(label + " is missing mastery interactions: " + missing + ".");
                    }
                }
            }

            if (twoStarFuel > threeStarFuel)
            {
                errors.Add(label + " has reversed star thresholds.");
            }
        }
    }
}
