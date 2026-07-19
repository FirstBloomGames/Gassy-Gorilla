using FirstBloom.ArcadeFramework.Accessibility;
using FirstBloom.ArcadeFramework.Audio;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [RequireComponent(typeof(GassyInteractionMarker))]
    public sealed class GassyCanopyUpdraft : MonoBehaviour
    {
        [SerializeField] private Collider2D trigger;
        [SerializeField] private Transform glowColumn;
        [SerializeField] private Transform[] leafVisuals;
        [Range(3f, 7f)] [SerializeField] private float liftVelocity = 5.2f;
        [Range(0.2f, 1.5f)] [SerializeField] private float riseSpeed = 0.78f;
        [Range(0.2f, 3f)] [SerializeField] private float swirlSpeed = 1.35f;
        [Min(1f)] [SerializeField] private float visualHeight = 4.4f;

        private GassyInteractionMarker marker;
        private Vector3[] leafBasePositions;
        private Vector3 glowBaseScale;
        private bool consumed;
        private float elapsed;

        public float LiftVelocity { get { return liftVelocity; } }
        public int LeafCount { get { return leafVisuals != null ? leafVisuals.Length : 0; } }
        public Collider2D Trigger { get { return trigger; } }
        public bool IsConfigured
        {
            get
            {
                GassyInteractionMarker activeMarker =
                    marker != null ? marker : GetComponent<GassyInteractionMarker>();
                return activeMarker != null &&
                    activeMarker.InteractionType == GassyInteractionType.UpdraftRide &&
                    trigger != null && trigger.isTrigger && glowColumn != null &&
                    leafVisuals != null && leafVisuals.Length >= 5 && leafVisuals.Length <= 8 &&
                    liftVelocity >= 4f && liftVelocity <= 6.2f;
            }
        }

        private void Awake()
        {
            marker = GetComponent<GassyInteractionMarker>();
            glowBaseScale = glowColumn != null ? glowColumn.localScale : Vector3.one;
            CacheLeafPositions();
        }

        private void OnEnable()
        {
            consumed = false;
            elapsed = 0f;
            if (trigger != null)
            {
                trigger.enabled = true;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (glowColumn != null)
            {
                float pulse = ArcadeAccessibilitySettings.ReducedMotion
                    ? 1f
                    : 0.94f + Mathf.Sin(elapsed * 2.4f) * 0.06f;
                glowColumn.localScale = glowBaseScale * pulse;
            }

            if (leafVisuals == null || leafBasePositions == null)
            {
                return;
            }

            float motionScale = ArcadeAccessibilitySettings.ReducedMotion ? 0.18f : 1f;
            for (int i = 0; i < leafVisuals.Length && i < leafBasePositions.Length; i++)
            {
                Transform leaf = leafVisuals[i];
                if (leaf == null)
                {
                    continue;
                }

                Vector3 origin = leafBasePositions[i];
                float normalizedY = Mathf.Repeat(
                    origin.y + visualHeight * 0.5f + elapsed * riseSpeed * motionScale,
                    visualHeight) - visualHeight * 0.5f;
                float phase = elapsed * swirlSpeed * motionScale + i * 1.37f;
                leaf.localPosition = new Vector3(
                    origin.x + Mathf.Sin(phase) * 0.24f * motionScale,
                    normalizedY,
                    origin.z + Mathf.Cos(phase) * 0.12f * motionScale);
                leaf.localRotation = Quaternion.Euler(
                    origin.y * 18f,
                    phase * Mathf.Rad2Deg,
                    Mathf.Sin(phase) * 18f * motionScale);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || other == null)
            {
                return;
            }

            GorillaController player = other.GetComponentInParent<GorillaController>();
            if (player == null || !player.ApplyUpdraft(liftVelocity))
            {
                return;
            }

            consumed = true;
            if (trigger != null)
            {
                trigger.enabled = false;
            }

            GassyRunEvents.RaiseInteractionCompleted(GassyInteractionType.UpdraftRide);
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.Updraft);
            }

            ArcadeHaptics.Play(ArcadeHapticType.Light);
        }

        private void CacheLeafPositions()
        {
            if (leafVisuals == null)
            {
                leafBasePositions = null;
                return;
            }

            leafBasePositions = new Vector3[leafVisuals.Length];
            for (int i = 0; i < leafVisuals.Length; i++)
            {
                leafBasePositions[i] = leafVisuals[i] != null
                    ? leafVisuals[i].localPosition
                    : Vector3.zero;
            }
        }
    }
}
