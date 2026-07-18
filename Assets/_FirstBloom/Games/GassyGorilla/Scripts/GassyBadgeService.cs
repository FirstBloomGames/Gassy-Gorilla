using System;
using FirstBloom.ArcadeFramework.Save;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public static class GassyBadgeService
    {
        private const string Scope = "GassyGorilla";

        private static readonly GassyBadgeDefinition[] BadgeDefinitions =
        {
            new GassyBadgeDefinition(
                "first-blast",
                "First Blast",
                "Perform one successful fart boost.",
                GassyBadgeMetric.SuccessfulBoosts,
                1),
            new GassyBadgeDefinition(
                "vine-time",
                "Vine Time",
                "Release from 10 vines across all runs.",
                GassyBadgeMetric.VineReleases,
                10),
            new GassyBadgeDefinition(
                "bean-counter",
                "Bean Counter",
                "Collect 50 food pickups across all runs.",
                GassyBadgeMetric.FoodPickups,
                50),
            new GassyBadgeDefinition(
                "swamp-smarts",
                "Swamp Smarts",
                "Dodge 5 crocodile ambushes.",
                GassyBadgeMetric.CrocodileDodges,
                5),
            new GassyBadgeDefinition(
                "hundred-meter-hero",
                "Hundred Meter Hero",
                "Reach 100 m in Endless Run.",
                GassyBadgeMetric.EndlessDistance,
                100),
            new GassyBadgeDefinition(
                "jungle-legend",
                "Jungle Legend",
                "Reach 500 m in Endless Run.",
                GassyBadgeMetric.EndlessDistance,
                500),
            new GassyBadgeDefinition(
                "star-collector",
                "Star Collector",
                "Earn 10 Expedition stars.",
                GassyBadgeMetric.ExpeditionStars,
                10),
            new GassyBadgeDefinition(
                "home-for-dinner",
                "Home for Dinner",
                "Complete all five Expeditions.",
                GassyBadgeMetric.CompletedExpeditions,
                5)
        };

        public static event Action<GassyBadgeDefinition> BadgeUnlocked;

        public static GassyBadgeDefinition[] Definitions { get { return BadgeDefinitions; } }
        public static int Count { get { return BadgeDefinitions.Length; } }

        public static int GetProgress(GassyBadgeDefinition definition)
        {
            return definition == null
                ? 0
                : ArcadeAchievementStore.GetProgress(Scope, definition.Id);
        }

        public static bool IsUnlocked(GassyBadgeDefinition definition)
        {
            return definition != null &&
                ArcadeAchievementStore.IsUnlocked(Scope, definition.Id);
        }

        public static int GetUnlockedCount()
        {
            int count = 0;
            for (int i = 0; i < BadgeDefinitions.Length; i++)
            {
                if (IsUnlocked(BadgeDefinitions[i]))
                {
                    count++;
                }
            }

            return count;
        }

        public static void AddProgress(GassyBadgeMetric metric, int amount, bool notify = true)
        {
            if (amount <= 0)
            {
                return;
            }

            for (int i = 0; i < BadgeDefinitions.Length; i++)
            {
                GassyBadgeDefinition definition = BadgeDefinitions[i];
                if (definition.Metric != metric)
                {
                    continue;
                }

                int next = GetProgress(definition) + amount;
                ApplyProgress(definition, next, notify);
            }
        }

        public static void SetProgressIfGreater(GassyBadgeMetric metric, int value, bool notify = true)
        {
            for (int i = 0; i < BadgeDefinitions.Length; i++)
            {
                GassyBadgeDefinition definition = BadgeDefinitions[i];
                if (definition.Metric == metric)
                {
                    ApplyProgress(definition, value, notify);
                }
            }
        }

        public static void Reconcile(GassyExpeditionCatalog expeditionCatalog, bool notify)
        {
            int bestDistance = Mathf.FloorToInt(
                HighScoreStore.GetBestDistance(GassyGorillaGameManager.BestDistanceKey));
            SetProgressIfGreater(GassyBadgeMetric.EndlessDistance, bestDistance, notify);

            if (expeditionCatalog == null || expeditionCatalog.Expeditions == null)
            {
                return;
            }

            int totalStars = 0;
            int completed = 0;
            for (int i = 0; i < expeditionCatalog.Expeditions.Length; i++)
            {
                GassyExpeditionDefinition definition = expeditionCatalog.Expeditions[i];
                if (definition == null)
                {
                    continue;
                }

                int stars = GassyExpeditionProgressStore.GetBestStars(definition.ExpeditionId);
                totalStars += stars;
                if (stars > 0)
                {
                    completed++;
                }
            }

            SetProgressIfGreater(GassyBadgeMetric.ExpeditionStars, totalStars, notify);
            SetProgressIfGreater(GassyBadgeMetric.CompletedExpeditions, completed, notify);
        }

        private static void ApplyProgress(
            GassyBadgeDefinition definition,
            int value,
            bool notify)
        {
            int clamped = Mathf.Clamp(value, 0, definition.Target);
            ArcadeAchievementStore.SetProgressIfGreater(Scope, definition.Id, clamped);
            if (clamped < definition.Target ||
                !ArcadeAchievementStore.TryUnlock(Scope, definition.Id))
            {
                return;
            }

            if (notify && BadgeUnlocked != null)
            {
                BadgeUnlocked.Invoke(definition);
            }
        }
    }
}
