using FirstBloom.ArcadeFramework.Accessibility;
using FirstBloom.ArcadeFramework.Audio;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [RequireComponent(typeof(GassyInteractionMarker))]
    public sealed class GassyBounceBloom : MonoBehaviour
    {
        [SerializeField] private Collider2D trigger;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform[] launchLeaves;
        [SerializeField] private ParticleSystem leafBurst;
        [Range(4f, 7f)] [SerializeField] private float liftVelocity = 6.1f;
        [Range(0f, 2f)] [SerializeField] private float forwardKick = 0.9f;
        [Range(0f, 0.15f)] [SerializeField] private float idleSway = 0.045f;
        [Range(0f, 4f)] [SerializeField] private float idleSwaySpeed = 1.8f;

        private GassyInteractionMarker marker;
        private Vector3 visualBaseScale;
        private Quaternion[] leafBaseRotations;
        private bool consumed;
        private float launchedAt = -10f;

        public Collider2D Trigger { get { return trigger; } }
        public float LiftVelocity { get { return liftVelocity; } }
        public float ForwardKick { get { return forwardKick; } }
        public bool IsConfigured
        {
            get
            {
                GassyInteractionMarker activeMarker =
                    marker != null ? marker : GetComponent<GassyInteractionMarker>();
                return activeMarker != null &&
                    activeMarker.InteractionType ==
                        GassyInteractionType.BounceBloom &&
                    trigger != null &&
                    trigger.isTrigger &&
                    visualRoot != null &&
                    launchLeaves != null &&
                    launchLeaves.Length >= 3 &&
                    liftVelocity >= 5f &&
                    liftVelocity <= 6.8f;
            }
        }

        private void Awake()
        {
            marker = GetComponent<GassyInteractionMarker>();
            visualBaseScale =
                visualRoot != null ? visualRoot.localScale : Vector3.one;
            CacheLeafRotations();
        }

        private void OnEnable()
        {
            consumed = false;
            launchedAt = -10f;
            if (trigger != null)
            {
                trigger.enabled = true;
            }

            if (visualRoot != null)
            {
                visualRoot.localScale = visualBaseScale;
            }
        }

        private void Update()
        {
            UpdateLaunchCompression();
            UpdateLeafSway();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || other == null)
            {
                return;
            }

            GorillaController player =
                other.GetComponentInParent<GorillaController>();
            if (player == null ||
                !player.ApplyBounceBloom(liftVelocity, forwardKick))
            {
                return;
            }

            consumed = true;
            launchedAt = Time.time;
            if (trigger != null)
            {
                trigger.enabled = false;
            }

            if (leafBurst != null)
            {
                leafBurst.Play();
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(
                    ArcadeSfxType.BounceBloom);
            }

            ArcadeHaptics.Play(ArcadeHapticType.Light);
            GassyRunEvents.RaiseInteractionCompleted(
                GassyInteractionType.BounceBloom);
        }

        private void UpdateLaunchCompression()
        {
            if (visualRoot == null)
            {
                return;
            }

            float elapsed = Time.time - launchedAt;
            if (elapsed < 0f || elapsed > 0.36f)
            {
                visualRoot.localScale = visualBaseScale;
                return;
            }

            float motionScale =
                ArcadeAccessibilitySettings.ReducedMotion ? 0.35f : 1f;
            float normalized = Mathf.Clamp01(elapsed / 0.36f);
            float rebound = Mathf.Sin(normalized * Mathf.PI * 2f) *
                (1f - normalized);
            Vector3 multiplier = new Vector3(
                1f + rebound * 0.16f * motionScale,
                1f - rebound * 0.24f * motionScale,
                1f + rebound * 0.08f * motionScale);
            visualRoot.localScale = Vector3.Scale(
                visualBaseScale,
                multiplier);
        }

        private void UpdateLeafSway()
        {
            if (launchLeaves == null || leafBaseRotations == null)
            {
                return;
            }

            float motionScale =
                ArcadeAccessibilitySettings.ReducedMotion ? 0.2f : 1f;
            for (int i = 0;
                i < launchLeaves.Length && i < leafBaseRotations.Length;
                i++)
            {
                Transform leaf = launchLeaves[i];
                if (leaf == null)
                {
                    continue;
                }

                float phase = Time.time * idleSwaySpeed + i * 1.4f;
                float angle = Mathf.Sin(phase) *
                    idleSway *
                    90f *
                    motionScale;
                leaf.localRotation =
                    leafBaseRotations[i] *
                    Quaternion.Euler(0f, 0f, angle);
            }
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
    }
}
