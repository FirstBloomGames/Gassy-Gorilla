using FirstBloom.ArcadeFramework.Accessibility;
using FirstBloom.ArcadeFramework.Audio;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [RequireComponent(typeof(GassyInteractionMarker))]
    public sealed class GassyBounceBloom : MonoBehaviour
    {
        [Header("Contact")]
        [SerializeField] private Collider2D trigger;
        [SerializeField] private Transform contactAnchor;
        [Range(0.1f, 0.3f)]
        [SerializeField] private float compressionDuration = 0.16f;
        [Range(0f, 2f)]
        [SerializeField] private float maxEntryUpwardVelocity = 1.15f;
        [Range(5.5f, 8f)]
        [SerializeField] private float liftVelocity = 7.05f;
        [Range(0f, 2.5f)]
        [SerializeField] private float forwardKick = 1.35f;

        [Header("Presentation")]
        [SerializeField] private Transform supportRoot;
        [SerializeField] private Transform springRoot;
        [SerializeField] private Transform glowRoot;
        [SerializeField] private Transform[] launchLeaves;
        [SerializeField] private ParticleSystem leafBurst;
        [Range(0f, 0.15f)]
        [SerializeField] private float idleSway = 0.055f;
        [Range(0f, 4f)]
        [SerializeField] private float idleSwaySpeed = 1.45f;

        private GassyInteractionMarker marker;
        private GorillaController occupiedPlayer;
        private Vector3 springBaseScale;
        private Quaternion springBaseRotation;
        private Vector3 glowBaseScale;
        private Quaternion[] leafBaseRotations;
        private bool consumed;
        private float compressedAt = -10f;
        private float launchedAt = -10f;

        public Collider2D Trigger { get { return trigger; } }
        public Transform ContactAnchor { get { return contactAnchor; } }
        public Transform SupportRoot { get { return supportRoot; } }
        public Transform SpringRoot { get { return springRoot; } }
        public float CompressionDuration { get { return compressionDuration; } }
        public float MaxEntryUpwardVelocity
        {
            get { return maxEntryUpwardVelocity; }
        }
        public float LiftVelocity { get { return liftVelocity; } }
        public float ForwardKick { get { return forwardKick; } }
        public bool IsOccupied { get { return occupiedPlayer != null; } }
        public bool IsConfigured
        {
            get
            {
                GassyInteractionMarker activeMarker =
                    marker != null ? marker : GetComponent<GassyInteractionMarker>();
                BoxCollider2D box = trigger as BoxCollider2D;
                return activeMarker != null &&
                    activeMarker.InteractionType ==
                        GassyInteractionType.BounceBloom &&
                    box != null &&
                    box.isTrigger &&
                    box.size.x >= 2.8f &&
                    box.size.y >= 1f &&
                    contactAnchor != null &&
                    supportRoot != null &&
                    springRoot != null &&
                    glowRoot != null &&
                    launchLeaves != null &&
                    launchLeaves.Length >= 3 &&
                    leafBurst != null &&
                    compressionDuration >= 0.12f &&
                    compressionDuration <= 0.22f &&
                    liftVelocity >= 6.5f &&
                    liftVelocity <= 7.6f;
            }
        }

        private void Awake()
        {
            marker = GetComponent<GassyInteractionMarker>();
            springBaseScale =
                springRoot != null ? springRoot.localScale : Vector3.one;
            springBaseRotation =
                springRoot != null ? springRoot.localRotation : Quaternion.identity;
            glowBaseScale =
                glowRoot != null ? glowRoot.localScale : Vector3.one;
            CacheLeafRotations();
        }

        private void OnEnable()
        {
            consumed = false;
            occupiedPlayer = null;
            compressedAt = -10f;
            launchedAt = -10f;
            if (trigger != null)
            {
                trigger.enabled = true;
            }

            ResetVisualPose();
        }

        private void OnDisable()
        {
            GorillaController player = occupiedPlayer;
            occupiedPlayer = null;
            if (player != null)
            {
                player.CancelBounceBloom(this);
            }

            ResetVisualPose();
        }

        private void Update()
        {
            UpdateSpringAnimation();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            TryBounce(other.GetComponentInParent<GorillaController>());
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other == null || consumed || occupiedPlayer != null)
            {
                return;
            }

            TryBounce(other.GetComponentInParent<GorillaController>());
        }

        public bool TryBounce(GorillaController player)
        {
            if (consumed || occupiedPlayer != null || player == null)
            {
                return false;
            }

            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
            float verticalVelocity = playerBody != null
#if UNITY_6000_0_OR_NEWER
                ? playerBody.linearVelocity.y
#else
                ? playerBody.velocity.y
#endif
                : 0f;
            float surfaceY = contactAnchor != null
                ? contactAnchor.position.y
                : transform.position.y;
            if (verticalVelocity > maxEntryUpwardVelocity ||
                player.transform.position.y < surfaceY - 0.42f ||
                !player.TryBeginBounceBloom(
                    this,
                    contactAnchor,
                    compressionDuration,
                    liftVelocity,
                    forwardKick))
            {
                return false;
            }

            consumed = true;
            occupiedPlayer = player;
            compressedAt = Time.time;
            launchedAt = -10f;
            if (trigger != null)
            {
                trigger.enabled = false;
            }

            ArcadeHaptics.Play(ArcadeHapticType.Light);
            return true;
        }

        public void NotifyLaunched(GorillaController player)
        {
            if (player == null || occupiedPlayer != player)
            {
                return;
            }

            occupiedPlayer = null;
            launchedAt = Time.time;
            if (leafBurst != null)
            {
                leafBurst.Play();
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(
                    ArcadeSfxType.BounceBloom);
            }

            ArcadeHaptics.Play(ArcadeHapticType.Medium);
            GassyRunEvents.RaiseInteractionCompleted(
                GassyInteractionType.BounceBloom);
        }

        public void NotifyCancelled(GorillaController player)
        {
            if (player != null && occupiedPlayer != player)
            {
                return;
            }

            occupiedPlayer = null;
        }

        private void UpdateSpringAnimation()
        {
            if (springRoot == null)
            {
                return;
            }

            float motionScale =
                ArcadeAccessibilitySettings.ReducedMotion ? 0.3f : 1f;
            if (occupiedPlayer != null)
            {
                float normalized = Mathf.Clamp01(
                    (Time.time - compressedAt) /
                    Mathf.Max(0.01f, compressionDuration));
                float compression = Mathf.SmoothStep(0f, 1f, normalized);
                springRoot.localScale = Vector3.Scale(
                    springBaseScale,
                    new Vector3(
                        1f + compression * 0.18f * motionScale,
                        1f - compression * 0.44f * motionScale,
                        1f + compression * 0.08f * motionScale));
                springRoot.localRotation = springBaseRotation *
                    Quaternion.Euler(0f, 0f, -3f * compression * motionScale);
                ApplyLeafPose(compression, 0f, motionScale);
                UpdateGlow(1f + compression * 0.18f * motionScale);
                return;
            }

            float launchElapsed = Time.time - launchedAt;
            if (launchElapsed >= 0f && launchElapsed <= 0.56f)
            {
                float normalized = Mathf.Clamp01(launchElapsed / 0.56f);
                float rebound = Mathf.Sin(normalized * Mathf.PI * 3f) *
                    Mathf.Exp(-normalized * 2.2f) *
                    motionScale;
                springRoot.localScale = Vector3.Scale(
                    springBaseScale,
                    new Vector3(
                        1f - rebound * 0.13f,
                        1f + rebound * 0.52f,
                        1f - rebound * 0.06f));
                springRoot.localRotation = springBaseRotation *
                    Quaternion.Euler(0f, 0f, rebound * 3.5f);
                ApplyLeafPose(-rebound * 0.8f, 0f, motionScale);
                UpdateGlow(1f + Mathf.Abs(rebound) * 0.22f);
                return;
            }

            float idleWave = Mathf.Sin(Time.time * idleSwaySpeed);
            springRoot.localScale = Vector3.Scale(
                springBaseScale,
                new Vector3(
                    1f + idleWave * 0.018f * motionScale,
                    1f - idleWave * 0.028f * motionScale,
                    1f + idleWave * 0.012f * motionScale));
            springRoot.localRotation = springBaseRotation;
            ApplyLeafPose(0f, idleWave, motionScale);
            UpdateGlow(1f + idleWave * 0.05f * motionScale);
        }

        private void ApplyLeafPose(
            float bend,
            float idleWave,
            float motionScale)
        {
            if (launchLeaves == null || leafBaseRotations == null)
            {
                return;
            }

            for (int i = 0;
                i < launchLeaves.Length && i < leafBaseRotations.Length;
                i++)
            {
                Transform leaf = launchLeaves[i];
                if (leaf == null)
                {
                    continue;
                }

                float side = i - (launchLeaves.Length - 1) * 0.5f;
                float phase = Time.time * idleSwaySpeed + i * 1.35f;
                float idleAngle = Mathf.Sin(phase) *
                    idleSway *
                    90f *
                    motionScale;
                float bendAngle = bend * (14f + Mathf.Abs(side) * 3f);
                leaf.localRotation = leafBaseRotations[i] *
                    Quaternion.Euler(
                        bendAngle,
                        0f,
                        -side * bendAngle * 0.55f +
                        idleAngle +
                        idleWave * side * 1.4f);
            }
        }

        private void UpdateGlow(float scaleMultiplier)
        {
            if (glowRoot == null)
            {
                return;
            }

            glowRoot.localScale = Vector3.Scale(
                glowBaseScale,
                new Vector3(
                    scaleMultiplier,
                    Mathf.Lerp(1f, scaleMultiplier, 0.35f),
                    scaleMultiplier));
        }

        private void CacheLeafRotations()
        {
            if (launchLeaves == null)
            {
                leafBaseRotations = null;
                return;
            }

            leafBaseRotations =
                new Quaternion[launchLeaves.Length];
            for (int i = 0; i < launchLeaves.Length; i++)
            {
                leafBaseRotations[i] =
                    launchLeaves[i] != null
                        ? launchLeaves[i].localRotation
                        : Quaternion.identity;
            }
        }

        private void ResetVisualPose()
        {
            if (springRoot != null)
            {
                springRoot.localScale = springBaseScale;
                springRoot.localRotation = springBaseRotation;
            }

            if (glowRoot != null)
            {
                glowRoot.localScale = glowBaseScale;
            }

            if (launchLeaves == null || leafBaseRotations == null)
            {
                return;
            }

            for (int i = 0;
                i < launchLeaves.Length && i < leafBaseRotations.Length;
                i++)
            {
                if (launchLeaves[i] != null)
                {
                    launchLeaves[i].localRotation = leafBaseRotations[i];
                }
            }
        }
    }
}
