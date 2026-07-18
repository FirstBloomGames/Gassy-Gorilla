using FirstBloom.ArcadeFramework.Accessibility;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public sealed class GassyFeedbackDirector : MonoBehaviour
    {
        [SerializeField] private GorillaController gorilla;

        public bool IsConfigured { get { return gorilla != null; } }

        private void OnEnable()
        {
            GassyRunEvents.CrocodileDodged += HandleCrocodileDodged;
            if (gorilla != null)
            {
                gorilla.Boosted += HandleBoosted;
                gorilla.VineGrabbed += HandleVineGrabbed;
                gorilla.VineReleased += HandleVineReleased;
            }
        }

        private void OnDisable()
        {
            GassyRunEvents.CrocodileDodged -= HandleCrocodileDodged;
            if (gorilla != null)
            {
                gorilla.Boosted -= HandleBoosted;
                gorilla.VineGrabbed -= HandleVineGrabbed;
                gorilla.VineReleased -= HandleVineReleased;
            }
        }

        private static void HandleBoosted()
        {
            ArcadeHaptics.Play(ArcadeHapticType.Light);
        }

        private static void HandleVineGrabbed()
        {
            ArcadeHaptics.Play(ArcadeHapticType.Medium);
        }

        private static void HandleVineReleased()
        {
            ArcadeHaptics.Play(ArcadeHapticType.Light);
        }

        private static void HandleCrocodileDodged()
        {
            ArcadeHaptics.Play(ArcadeHapticType.Medium);
        }
    }
}
