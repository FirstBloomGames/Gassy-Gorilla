using FirstBloom.ArcadeFramework.Save;

namespace FirstBloom.Games.GassyGorilla
{
    public static class GassyExpeditionProgressStore
    {
        private const string UnlockKey = "GassyGorilla_Expedition_UnlockedIndex";
        private const string StarsPrefix = "GassyGorilla_Expedition_Stars_";
        private const string FailurePrefix = "GassyGorilla_Expedition_Failures_";
        private const string VoicePrefix = "GassyGorilla_Expedition_Voice_";

        public static int GetHighestUnlockedIndex()
        {
            return ArcadeProgressStore.GetInt(UnlockKey, 0);
        }

        public static bool IsUnlocked(int index)
        {
            return index >= 0 && index <= GetHighestUnlockedIndex();
        }

        public static int ReconcileUnlocks(GassyExpeditionCatalog catalog)
        {
            if (catalog == null || catalog.Count <= 0)
            {
                return 0;
            }

            int storedIndex = GetHighestUnlockedIndex();
            int unlockedIndex = UnityEngine.Mathf.Clamp(storedIndex, 0, catalog.Count - 1);
            while (unlockedIndex < catalog.Count - 1)
            {
                GassyExpeditionDefinition current = catalog.GetByIndex(unlockedIndex);
                if (current == null || GetBestStars(current.ExpeditionId) <= 0)
                {
                    break;
                }

                unlockedIndex++;
            }

            if (unlockedIndex > storedIndex)
            {
                ArcadeProgressStore.SetInt(UnlockKey, unlockedIndex);
            }

            return unlockedIndex;
        }

        public static int GetBestStars(string expeditionId)
        {
            return string.IsNullOrWhiteSpace(expeditionId)
                ? 0
                : ArcadeProgressStore.GetInt(StarsPrefix + expeditionId, 0);
        }

        public static void Complete(GassyExpeditionDefinition definition, int stars, int catalogCount)
        {
            if (definition == null)
            {
                return;
            }

            ArcadeProgressStore.SetIntIfGreater(
                StarsPrefix + definition.ExpeditionId,
                stars);

            int nextIndex = definition.OrderIndex + 1;
            if (nextIndex < catalogCount)
            {
                ArcadeProgressStore.SetIntIfGreater(UnlockKey, nextIndex);
            }

            ClearFailures(definition);
        }

        public static int RecordFailure(GassyExpeditionDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            string key = FailurePrefix + definition.ExpeditionId;
            int attempts = ArcadeProgressStore.GetInt(key, 0) + 1;
            ArcadeProgressStore.SetInt(key, attempts);
            return attempts;
        }

        public static int GetFailureCount(
            GassyExpeditionDefinition definition)
        {
            return definition == null
                ? 0
                : ArcadeProgressStore.GetInt(
                    FailurePrefix + definition.ExpeditionId,
                    0);
        }

        public static void ClearFailures(
            GassyExpeditionDefinition definition)
        {
            if (definition != null)
            {
                ArcadeProgressStore.Delete(
                    FailurePrefix + definition.ExpeditionId);
            }
        }

        public static bool HasHeardVoice(
            GassyExpeditionDefinition definition,
            string momentId)
        {
            return definition != null &&
                !string.IsNullOrWhiteSpace(momentId) &&
                ArcadeProgressStore.GetBool(
                    VoiceKey(definition, momentId));
        }

        public static void MarkVoiceHeard(
            GassyExpeditionDefinition definition,
            string momentId)
        {
            if (definition != null &&
                !string.IsNullOrWhiteSpace(momentId))
            {
                ArcadeProgressStore.SetBool(
                    VoiceKey(definition, momentId),
                    true);
            }
        }

        public static void ResetAll(GassyExpeditionCatalog catalog)
        {
            ArcadeProgressStore.Delete(UnlockKey);
            if (catalog == null || catalog.Expeditions == null)
            {
                return;
            }

            for (int i = 0; i < catalog.Expeditions.Length; i++)
            {
                GassyExpeditionDefinition definition = catalog.Expeditions[i];
                if (definition != null)
                {
                    ArcadeProgressStore.Delete(StarsPrefix + definition.ExpeditionId);
                    ArcadeProgressStore.Delete(FailurePrefix + definition.ExpeditionId);
                    ArcadeProgressStore.Delete(VoiceKey(definition, "opening"));
                    ArcadeProgressStore.Delete(VoiceKey(definition, "lesson"));
                    ArcadeProgressStore.Delete(VoiceKey(definition, "success"));
                    ArcadeProgressStore.Delete(VoiceKey(definition, "hint"));
                }
            }
        }

        private static string VoiceKey(
            GassyExpeditionDefinition definition,
            string momentId)
        {
            return VoicePrefix + definition.ExpeditionId + "_" + momentId;
        }
    }
}
