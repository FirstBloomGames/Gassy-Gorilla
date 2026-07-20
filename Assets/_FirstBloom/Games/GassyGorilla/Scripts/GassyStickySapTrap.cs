using FirstBloom.ArcadeFramework.Accessibility;
using FirstBloom.ArcadeFramework.Audio;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [RequireComponent(typeof(GassyInteractionMarker))]
    public sealed class GassyStickySapTrap : MonoBehaviour
    {
        [Header("Contact")]
        [SerializeField] private Collider2D trigger;
        [SerializeField] private Transform catchAnchor;
        [Range(0.35f, 0.8f)]
        [SerializeField] private float forwardSpeedScale = 0.52f;

        [Header("Presentation")]
        [SerializeField] private Transform supportRoot;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform[] stickyStrands;
        [SerializeField] private ParticleSystem catchBurst;
        [SerializeField] private ParticleSystem escapeBurst;
        [Range(0f, 0.12f)]
        [SerializeField] private float idlePulseAmount = 0.035f;
        [Range(0.2f, 4f)]
        [SerializeField] private float idlePulseSpeed = 1.35f;

        private GassyInteractionMarker marker;
        private GorillaController occupiedPlayer;
        private bool consumed;
        private Vector3 visualBaseScale;
        private Vector3[] strandOriginLocalPositions;
        private Vector3[] strandBaseScales;
        private float caughtAt = -10f;
        private float escapedAt = -10f;

        public float ForwardSpeedScale { get { return forwardSpeedScale; } }
        public Collider2D Trigger { get { return trigger; } }
        public Transform CatchAnchor { get { return catchAnchor; } }
        public Transform SupportRoot { get { return supportRoot; } }
        public int StrandCount
        {
            get { return stickyStrands != null ? stickyStrands.Length : 0; }
        }
        public bool IsOccupied { get { return occupiedPlayer != null; } }
        public bool IsConfigured
        {
            get
            {
                GassyInteractionMarker activeMarker =
                    marker != null ? marker : GetComponent<GassyInteractionMarker>();
                BoxCollider2D box = trigger as BoxCollider2D;
                return activeMarker != null &&
                    activeMarker.InteractionType == GassyInteractionType.SapEscape &&
                    box != null &&
                    box.isTrigger &&
                    box.size.x >= 2.5f &&
                    box.size.y >= 1.15f &&
                    catchAnchor != null &&
                    supportRoot != null &&
                    visualRoot != null &&
                    stickyStrands != null &&
                    stickyStrands.Length >= 5 &&
                    catchBurst != null &&
                    escapeBurst != null &&
                    forwardSpeedScale >= 0.4f &&
                    forwardSpeedScale <= 0.65f;
            }
        }

        private void Awake()
        {
            marker = GetComponent<GassyInteractionMarker>();
            visualBaseScale =
                visualRoot != null ? visualRoot.localScale : Vector3.one;
            CacheStrands();
        }

        private void OnEnable()
        {
            consumed = false;
            occupiedPlayer = null;
            caughtAt = -10f;
            escapedAt = -10f;
            if (trigger != null)
            {
                trigger.enabled = true;
            }

            if (visualRoot != null)
            {
                visualRoot.localScale = visualBaseScale;
            }

            SetStrandsVisible(false);
        }

        private void OnDisable()
        {
            GorillaController player = occupiedPlayer;
            occupiedPlayer = null;
            if (player != null)
            {
                player.CancelStickySap(this);
            }

            SetStrandsVisible(false);
        }

        private void Update()
        {
            UpdateSapSurface();
            UpdateStickyStrands();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            TryCapture(other.GetComponentInParent<GorillaController>());
        }

        public bool TryCapture(GorillaController player)
        {
            if (consumed ||
                occupiedPlayer != null ||
                player == null ||
                !player.TryEnterStickySap(
                    this,
                    catchAnchor,
                    forwardSpeedScale))
            {
                return false;
            }

            consumed = true;
            occupiedPlayer = player;
            caughtAt = Time.time;
            escapedAt = -10f;
            if (trigger != null)
            {
                trigger.enabled = false;
            }

            SetStrandsVisible(true);
            if (catchBurst != null)
            {
                catchBurst.Play();
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.SapCatch);
            }

            ArcadeHaptics.Play(ArcadeHapticType.Medium);
            return true;
        }

        public void NotifyEscaped(GorillaController player)
        {
            if (player == null || occupiedPlayer != player)
            {
                return;
            }

            occupiedPlayer = null;
            escapedAt = Time.time;
            SetStrandsVisible(false);
            if (escapeBurst != null)
            {
                escapeBurst.Play();
            }
        }

        public void NotifyCancelled(GorillaController player)
        {
            if (player != null && occupiedPlayer != player)
            {
                return;
            }

            occupiedPlayer = null;
            SetStrandsVisible(false);
        }

        private void UpdateSapSurface()
        {
            if (visualRoot == null)
            {
                return;
            }

            float motionScale =
                ArcadeAccessibilitySettings.ReducedMotion ? 0.25f : 1f;
            if (occupiedPlayer != null)
            {
                float elapsed = Mathf.Max(0f, Time.time - caughtAt);
                float settle = Mathf.Clamp01(elapsed / 0.18f);
                float wobble = Mathf.Sin(elapsed * 12f) *
                    Mathf.Exp(-elapsed * 3.4f) *
                    motionScale;
                Vector3 multiplier = new Vector3(
                    1f + settle * 0.1f + wobble * 0.05f,
                    1f - settle * 0.24f - wobble * 0.08f,
                    1f + settle * 0.06f);
                visualRoot.localScale =
                    Vector3.Scale(visualBaseScale, multiplier);
                return;
            }

            float escapeElapsed = Time.time - escapedAt;
            if (escapeElapsed >= 0f && escapeElapsed <= 0.5f)
            {
                float normalized = Mathf.Clamp01(escapeElapsed / 0.5f);
                float rebound = Mathf.Sin(normalized * Mathf.PI * 2.4f) *
                    (1f - normalized) *
                    motionScale;
                Vector3 multiplier = new Vector3(
                    1f - rebound * 0.12f,
                    1f + rebound * 0.26f,
                    1f - rebound * 0.06f);
                visualRoot.localScale =
                    Vector3.Scale(visualBaseScale, multiplier);
                return;
            }

            float pulse = Mathf.Sin(Time.time * idlePulseSpeed) *
                idlePulseAmount *
                motionScale;
            visualRoot.localScale = Vector3.Scale(
                visualBaseScale,
                new Vector3(1f + pulse, 1f - pulse * 0.45f, 1f + pulse));
        }

        private void UpdateStickyStrands()
        {
            if (occupiedPlayer == null ||
                stickyStrands == null ||
                strandOriginLocalPositions == null ||
                strandBaseScales == null)
            {
                return;
            }

            float motionScale =
                ArcadeAccessibilitySettings.ReducedMotion ? 0.25f : 1f;
            Vector3 playerCenter =
                occupiedPlayer.transform.position + new Vector3(0f, -0.12f, 0.04f);
            for (int i = 0;
                i < stickyStrands.Length &&
                i < strandOriginLocalPositions.Length &&
                i < strandBaseScales.Length;
                i++)
            {
                Transform strand = stickyStrands[i];
                if (strand == null)
                {
                    continue;
                }

                Vector3 origin =
                    transform.TransformPoint(strandOriginLocalPositions[i]);
                float phase = Time.time * 7.5f + i * 1.9f;
                Vector3 target = playerCenter + new Vector3(
                    (i - (stickyStrands.Length - 1) * 0.5f) * 0.14f,
                    Mathf.Sin(phase) * 0.04f * motionScale,
                    -0.03f * i);
                Vector3 direction = target - origin;
                float length = Mathf.Max(0.08f, direction.magnitude);
                strand.position = origin + direction * 0.5f;
                strand.up = direction / length;
                float thicknessPulse =
                    1f + Mathf.Sin(phase * 1.3f) * 0.08f * motionScale;
                Vector3 baseScale = strandBaseScales[i];
                strand.localScale = new Vector3(
                    baseScale.x * thicknessPulse,
                    length * 0.5f,
                    baseScale.z * thicknessPulse);
            }
        }

        private void CacheStrands()
        {
            if (stickyStrands == null)
            {
                strandOriginLocalPositions = null;
                strandBaseScales = null;
                return;
            }

            strandOriginLocalPositions =
                new Vector3[stickyStrands.Length];
            strandBaseScales = new Vector3[stickyStrands.Length];
            for (int i = 0; i < stickyStrands.Length; i++)
            {
                Transform strand = stickyStrands[i];
                strandOriginLocalPositions[i] =
                    strand != null ? strand.localPosition : Vector3.zero;
                strandBaseScales[i] =
                    strand != null ? strand.localScale : Vector3.one;
            }
        }

        private void SetStrandsVisible(bool visible)
        {
            if (stickyStrands == null)
            {
                return;
            }

            for (int i = 0; i < stickyStrands.Length; i++)
            {
                Transform strand = stickyStrands[i];
                if (strand == null)
                {
                    continue;
                }

                strand.gameObject.SetActive(visible);
                if (!visible &&
                    strandOriginLocalPositions != null &&
                    i < strandOriginLocalPositions.Length &&
                    strandBaseScales != null &&
                    i < strandBaseScales.Length)
                {
                    strand.localPosition = strandOriginLocalPositions[i];
                    strand.localRotation = Quaternion.identity;
                    strand.localScale = strandBaseScales[i];
                }
            }
        }
    }
}
