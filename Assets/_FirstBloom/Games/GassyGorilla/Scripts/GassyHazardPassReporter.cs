using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [RequireComponent(typeof(GassyInteractionMarker))]
    public sealed class GassyHazardPassReporter : MonoBehaviour
    {
        [Min(0f)] [SerializeField] private float passOffset = 0.8f;

        private GassyInteractionMarker marker;
        private GorillaController player;
        private GassyGorillaGameManager gameManager;
        private bool reported;

        public float PassOffset { get { return passOffset; } }
        public bool IsConfigured
        {
            get
            {
                GassyInteractionMarker activeMarker =
                    marker != null ? marker : GetComponent<GassyInteractionMarker>();
                return activeMarker != null && activeMarker.IsConfigured &&
                    passOffset >= 0.25f;
            }
        }

        private void Awake()
        {
            marker = GetComponent<GassyInteractionMarker>();
        }

        private void OnEnable()
        {
            reported = false;
            ResolveDependencies();
        }

        private void Update()
        {
            if (reported)
            {
                return;
            }

            ResolveDependencies();
            if (player == null || gameManager == null || !gameManager.IsRunActive)
            {
                return;
            }

            if (player.transform.position.x <= transform.position.x + passOffset)
            {
                return;
            }

            reported = true;
            GassyRunEvents.RaiseInteractionCompleted(marker.InteractionType);
        }

        private void ResolveDependencies()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<GorillaController>();
            }

            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GassyGorillaGameManager>();
            }
        }
    }
}
