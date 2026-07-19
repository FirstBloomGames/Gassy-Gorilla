using FirstBloom.ArcadeFramework.Accessibility;
using FirstBloom.ArcadeFramework.Audio;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [RequireComponent(typeof(GassyInteractionMarker))]
    public sealed class GassyStickySapTrap : MonoBehaviour
    {
        [SerializeField] private Collider2D trigger;
        [SerializeField] private Transform visualRoot;
        [Range(0.35f, 0.8f)] [SerializeField] private float forwardSpeedScale = 0.52f;

        private GassyInteractionMarker marker;
        private bool consumed;
        private Vector3 visualBaseScale;

        public float ForwardSpeedScale { get { return forwardSpeedScale; } }
        public Collider2D Trigger { get { return trigger; } }
        public bool IsConfigured
        {
            get
            {
                GassyInteractionMarker activeMarker =
                    marker != null ? marker : GetComponent<GassyInteractionMarker>();
                return activeMarker != null &&
                    activeMarker.InteractionType == GassyInteractionType.SapEscape &&
                    trigger != null && trigger.isTrigger && visualRoot != null &&
                    forwardSpeedScale >= 0.4f && forwardSpeedScale <= 0.65f;
            }
        }

        private void Awake()
        {
            marker = GetComponent<GassyInteractionMarker>();
            visualBaseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
        }

        private void OnEnable()
        {
            consumed = false;
            if (trigger != null)
            {
                trigger.enabled = true;
            }

            if (visualRoot != null)
            {
                visualRoot.localScale = visualBaseScale;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || other == null)
            {
                return;
            }

            GorillaController player = other.GetComponentInParent<GorillaController>();
            if (player == null || !player.TryEnterStickySap(forwardSpeedScale))
            {
                return;
            }

            consumed = true;
            if (trigger != null)
            {
                trigger.enabled = false;
            }

            if (visualRoot != null)
            {
                visualRoot.localScale = visualBaseScale * 0.82f;
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.SapCatch);
            }

            ArcadeHaptics.Play(ArcadeHapticType.Medium);
        }
    }
}
