using System.Collections;
using System.Collections.Generic;
using FirstBloom.ArcadeFramework.Accessibility;
using FirstBloom.ArcadeFramework.Audio;
using FirstBloom.ArcadeFramework.UI;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public sealed class GassyBadgeTracker : MonoBehaviour
    {
        [SerializeField] private GorillaController gorilla;
        [SerializeField] private GassyScoreManager scoreManager;
        [SerializeField] private GassyGorillaGameManager gameManager;
        [SerializeField] private GassyExpeditionCatalog expeditionCatalog;
        [SerializeField] private TextOverlay badgeToast;
        [SerializeField] private float toastDuration = 2.1f;

        private readonly Queue<GassyBadgeDefinition> pendingToasts =
            new Queue<GassyBadgeDefinition>();
        private Coroutine toastRoutine;

        public bool IsConfigured
        {
            get
            {
                return gorilla != null &&
                    scoreManager != null &&
                    gameManager != null &&
                    expeditionCatalog != null &&
                    badgeToast != null;
            }
        }

        private void Awake()
        {
            GassyBadgeService.Reconcile(expeditionCatalog, false);
        }

        private void OnEnable()
        {
            GassyBadgeService.BadgeUnlocked += HandleBadgeUnlocked;
            GassyRunEvents.FoodCollected += HandleFoodCollected;
            GassyRunEvents.CrocodileDodged += HandleCrocodileDodged;

            if (gorilla != null)
            {
                gorilla.Boosted += HandleBoosted;
                gorilla.VineReleased += HandleVineReleased;
            }

            if (scoreManager != null)
            {
                scoreManager.DistanceChanged += HandleDistanceChanged;
            }
        }

        private void OnDisable()
        {
            GassyBadgeService.BadgeUnlocked -= HandleBadgeUnlocked;
            GassyRunEvents.FoodCollected -= HandleFoodCollected;
            GassyRunEvents.CrocodileDodged -= HandleCrocodileDodged;

            if (gorilla != null)
            {
                gorilla.Boosted -= HandleBoosted;
                gorilla.VineReleased -= HandleVineReleased;
            }

            if (scoreManager != null)
            {
                scoreManager.DistanceChanged -= HandleDistanceChanged;
            }

            pendingToasts.Clear();
            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
                toastRoutine = null;
            }
        }

        private static void HandleBoosted()
        {
            GassyBadgeService.AddProgress(GassyBadgeMetric.SuccessfulBoosts, 1);
        }

        private static void HandleVineReleased()
        {
            GassyBadgeService.AddProgress(GassyBadgeMetric.VineReleases, 1);
        }

        private static void HandleFoodCollected(FoodPickupType pickupType)
        {
            GassyBadgeService.AddProgress(GassyBadgeMetric.FoodPickups, 1);
        }

        private static void HandleCrocodileDodged()
        {
            GassyBadgeService.AddProgress(GassyBadgeMetric.CrocodileDodges, 1);
        }

        private void HandleDistanceChanged(float distance)
        {
            if (gameManager == null || gameManager.IsExpedition)
            {
                return;
            }

            GassyBadgeService.SetProgressIfGreater(
                GassyBadgeMetric.EndlessDistance,
                Mathf.FloorToInt(distance));
        }

        private void HandleBadgeUnlocked(GassyBadgeDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            pendingToasts.Enqueue(definition);
            if (toastRoutine == null)
            {
                toastRoutine = StartCoroutine(ShowBadgeToasts());
            }
        }

        private IEnumerator ShowBadgeToasts()
        {
            while (pendingToasts.Count > 0)
            {
                GassyBadgeDefinition definition = pendingToasts.Dequeue();
                if (badgeToast != null)
                {
                    badgeToast.Show(
                        "BADGE EARNED\n" + definition.DisplayTitle.ToUpperInvariant(),
                        toastDuration);
                }

                if (ArcadeAudioManager.Instance != null)
                {
                    ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.Milestone, 0.52f);
                }

                ArcadeHaptics.Play(ArcadeHapticType.Success);
                yield return new WaitForSecondsRealtime(toastDuration + 0.18f);
            }

            toastRoutine = null;
        }
    }
}
