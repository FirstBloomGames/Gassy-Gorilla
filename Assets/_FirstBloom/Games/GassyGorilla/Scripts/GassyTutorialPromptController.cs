using System.Collections;
using FirstBloom.ArcadeFramework.UI;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public class GassyTutorialPromptController : MonoBehaviour
    {
        [SerializeField] private GorillaController gorilla;
        [SerializeField] private TextOverlay overlay;
        [SerializeField] private float openingDelay = 0.45f;
        [SerializeField] private float openingHold = 2.4f;
        [SerializeField] private float pickupHintDelay = 0.25f;
        [SerializeField] private float pickupHintHold = 1.9f;
        [SerializeField] private float vineHold = 2f;

        private bool sawBoost;
        private bool sawVine;
        private bool openingAllowed = true;
        private Coroutine openingRoutine;

        private void OnEnable()
        {
            if (gorilla != null)
            {
                gorilla.Boosted += HandleBoosted;
                gorilla.BoostFailed += HandleBoostFailed;
                gorilla.VineGrabbed += HandleVineGrabbed;
                gorilla.VineReleased += HandleVineReleased;
            }
        }

        private void OnDisable()
        {
            if (gorilla != null)
            {
                gorilla.Boosted -= HandleBoosted;
                gorilla.BoostFailed -= HandleBoostFailed;
                gorilla.VineGrabbed -= HandleVineGrabbed;
                gorilla.VineReleased -= HandleVineReleased;
            }
        }

        private void Start()
        {
            if (overlay != null)
            {
                overlay.HideInstant();
            }

            if (openingAllowed)
            {
                openingRoutine = StartCoroutine(OpeningPromptRoutine());
            }
        }

        private IEnumerator OpeningPromptRoutine()
        {
            yield return new WaitForSeconds(openingDelay);

            if (!sawBoost && overlay != null)
            {
                overlay.Show("TAP TO BOOST", openingHold);
            }

            openingRoutine = null;
        }

        private void HandleBoosted()
        {
            if (sawBoost)
            {
                return;
            }

            sawBoost = true;
            if (openingRoutine != null)
            {
                StopCoroutine(openingRoutine);
                openingRoutine = null;
            }

            StartCoroutine(ShowPickupHintRoutine());
        }

        private IEnumerator ShowPickupHintRoutine()
        {
            yield return new WaitForSeconds(pickupHintDelay);

            if (overlay != null && !sawVine)
            {
                overlay.Show("GRAB FOOD FOR FART FUEL", pickupHintHold);
            }
        }

        private void HandleBoostFailed()
        {
            if (overlay != null)
            {
                overlay.Show("NEED FOOD FOR FUEL", pickupHintHold);
            }
        }

        private void HandleVineGrabbed()
        {
            if (overlay != null && !sawVine)
            {
                overlay.Show("TAP TO LAUNCH", vineHold);
            }
        }

        private void HandleVineReleased()
        {
            if (overlay != null && !sawVine)
            {
                overlay.HideInstant();
            }

            sawVine = true;
        }

        public void HideForGameOver()
        {
            StopAllCoroutines();
            openingRoutine = null;
            if (overlay != null)
            {
                overlay.HideInstant();
            }
        }

        public void PauseForStory()
        {
            openingAllowed = false;
            if (openingRoutine != null)
            {
                StopCoroutine(openingRoutine);
                openingRoutine = null;
            }

            if (overlay != null)
            {
                overlay.HideInstant();
            }
        }

        public void BeginForRun()
        {
            openingAllowed = true;
            if (!sawBoost && openingRoutine == null)
            {
                openingRoutine = StartCoroutine(OpeningPromptRoutine());
            }
        }

        public void PauseForSystemMenu()
        {
            StopAllCoroutines();
            openingRoutine = null;
            if (overlay != null)
            {
                overlay.HideInstant();
            }
        }

        public void ResumeAfterSystemMenu()
        {
            if (openingAllowed && !sawBoost && openingRoutine == null)
            {
                openingRoutine = StartCoroutine(OpeningPromptRoutine());
            }
        }
    }
}
