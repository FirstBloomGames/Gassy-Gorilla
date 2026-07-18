using FirstBloom.ArcadeFramework.Save;

namespace FirstBloom.Games.GassyGorilla
{
    public static class GassyExpeditionProgressStore
    {
        private const string UnlockKey = "GassyGorilla_Expedition_UnlockedIndex";
        private const string StarsPrefix = "GassyGorilla_Expedition_Stars_";

        public static int GetHighestUnlockedIndex()
        {
            return ArcadeProgressStore.GetInt(UnlockKey, 0);
        }

        public static bool IsUnlocked(int index)
        {
            return index >= 0 && index <= GetHighestUnlockedIndex();
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
                }
            }
        }
    }
}
