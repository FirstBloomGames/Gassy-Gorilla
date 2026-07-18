using System;

namespace FirstBloom.ArcadeFramework.Core
{
    public static class ArcadeRunSession
    {
        public static ArcadeRunMode Mode { get; private set; } = ArcadeRunMode.Endless;
        public static string ContentId { get; private set; } = string.Empty;

        public static void SelectEndless()
        {
            Mode = ArcadeRunMode.Endless;
            ContentId = string.Empty;
        }

        public static bool SelectFinite(string contentId)
        {
            if (string.IsNullOrWhiteSpace(contentId))
            {
                return false;
            }

            Mode = ArcadeRunMode.Finite;
            ContentId = contentId.Trim();
            return true;
        }

        public static bool IsFiniteContent(string contentId)
        {
            return Mode == ArcadeRunMode.Finite &&
                string.Equals(ContentId, contentId, StringComparison.Ordinal);
        }
    }
}
