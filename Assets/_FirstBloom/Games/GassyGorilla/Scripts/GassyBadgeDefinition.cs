namespace FirstBloom.Games.GassyGorilla
{
    public enum GassyBadgeMetric
    {
        SuccessfulBoosts,
        VineReleases,
        FoodPickups,
        CrocodileDodges,
        EndlessDistance,
        ExpeditionStars,
        CompletedExpeditions
    }

    public sealed class GassyBadgeDefinition
    {
        public string Id { get; private set; }
        public string DisplayTitle { get; private set; }
        public string Description { get; private set; }
        public GassyBadgeMetric Metric { get; private set; }
        public int Target { get; private set; }

        public GassyBadgeDefinition(
            string id,
            string displayTitle,
            string description,
            GassyBadgeMetric metric,
            int target)
        {
            Id = id;
            DisplayTitle = displayTitle;
            Description = description;
            Metric = metric;
            Target = target;
        }

        public string FormatProgress(int progress)
        {
            int clamped = progress < 0 ? 0 : (progress > Target ? Target : progress);
            if (Metric == GassyBadgeMetric.EndlessDistance)
            {
                return clamped + " / " + Target + " m";
            }

            return clamped + " / " + Target;
        }
    }
}
