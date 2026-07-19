using FirstBloom.ArcadeFramework.Accessibility;
using FirstBloom.ArcadeFramework.Audio;
using FirstBloom.ArcadeFramework.Camera;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [RequireComponent(typeof(GassyInteractionMarker))]
    public sealed class GassyMudGeyserController : MonoBehaviour
    {
        private enum GeyserState
        {
            Dormant,
            Warning,
            Erupting,
            Resolved
        }

        [Header("References")]
        [SerializeField] private GameObject warningRoot;
        [SerializeField] private Transform warningPulse;
        [SerializeField] private GameObject eruptionRoot;
        [SerializeField] private Transform eruptionColumn;
        [SerializeField] private Collider2D eruptionHitbox;

        [Header("Timing")]
        [Min(2f)] [SerializeField] private float activationDistance = 5.8f;
        [Range(0.65f, 1.2f)] [SerializeField] private float warningDuration = 0.88f;
        [Range(0.35f, 1f)] [SerializeField] private float eruptionDuration = 0.62f;
        [Min(0.25f)] [SerializeField] private float passOffset = 0.9f;

        private GassyInteractionMarker marker;
        private GorillaController player;
        private GassyGorillaGameManager gameManager;
        private SmoothCameraFollow2D cameraFollow;
        private GeyserState state;
        private float stateTimer;
        private bool reported;
        private Vector3 warningBaseScale;
        private Vector3 eruptionBaseScale;

        public float ActivationDistance { get { return activationDistance; } }
        public float WarningDuration { get { return warningDuration; } }
        public float EruptionDuration { get { return eruptionDuration; } }
        public Collider2D EruptionHitbox { get { return eruptionHitbox; } }
        public bool IsConfigured
        {
            get
            {
                GassyInteractionMarker activeMarker =
                    marker != null ? marker : GetComponent<GassyInteractionMarker>();
                return activeMarker != null &&
                    activeMarker.InteractionType == GassyInteractionType.GeyserDodge &&
                    warningRoot != null && warningPulse != null &&
                    eruptionRoot != null && eruptionColumn != null &&
                    eruptionHitbox != null && activationDistance >= 4.5f &&
                    warningDuration >= 0.8f && eruptionDuration >= 0.45f;
            }
        }

        private void Awake()
        {
            marker = GetComponent<GassyInteractionMarker>();
            warningBaseScale = warningPulse != null ? warningPulse.localScale : Vector3.one;
            eruptionBaseScale = eruptionColumn != null ? eruptionColumn.localScale : Vector3.one;
        }

        private void OnEnable()
        {
            state = GeyserState.Dormant;
            stateTimer = 0f;
            reported = false;
            SetWarningVisible(false);
            SetEruptionVisible(false);
            ResolveDependencies();
        }

        private void OnDisable()
        {
            if (eruptionHitbox != null)
            {
                eruptionHitbox.enabled = false;
            }
        }

        private void Update()
        {
            ResolveDependencies();
            if (player == null || gameManager == null || !gameManager.IsRunActive)
            {
                return;
            }

            float approach = transform.position.x - player.transform.position.x;
            switch (state)
            {
                case GeyserState.Dormant:
                    if (approach <= activationDistance && approach >= -passOffset)
                    {
                        BeginWarning();
                    }
                    break;

                case GeyserState.Warning:
                    stateTimer += Time.deltaTime;
                    UpdateWarningVisual();
                    if (stateTimer >= warningDuration)
                    {
                        BeginEruption();
                    }
                    break;

                case GeyserState.Erupting:
                    stateTimer += Time.deltaTime;
                    UpdateEruptionVisual();
                    if (stateTimer >= eruptionDuration)
                    {
                        ResolveEruption();
                    }
                    break;

                case GeyserState.Resolved:
                    ReportDodgeWhenPassed();
                    break;
            }
        }

        private void BeginWarning()
        {
            state = GeyserState.Warning;
            stateTimer = 0f;
            SetWarningVisible(true);
            GassyRunEvents.RaiseInteractionStarted(GassyInteractionType.GeyserDodge);

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.GeyserWarning);
            }

            ArcadeHaptics.Play(ArcadeHapticType.Light);
        }

        private void BeginEruption()
        {
            state = GeyserState.Erupting;
            stateTimer = 0f;
            SetWarningVisible(false);
            SetEruptionVisible(true);

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.GeyserBurst);
            }

            if (cameraFollow != null)
            {
                cameraFollow.Shake(0.08f, 0.18f);
            }
        }

        private void ResolveEruption()
        {
            state = GeyserState.Resolved;
            stateTimer = 0f;
            SetEruptionVisible(false);
            ReportDodgeWhenPassed();
        }

        private void ReportDodgeWhenPassed()
        {
            if (reported || player == null ||
                player.transform.position.x <= transform.position.x + passOffset)
            {
                return;
            }

            reported = true;
            GassyRunEvents.RaiseInteractionCompleted(GassyInteractionType.GeyserDodge);
        }

        private void UpdateWarningVisual()
        {
            if (warningPulse == null)
            {
                return;
            }

            float normalized = warningDuration <= 0f ? 1f : Mathf.Clamp01(stateTimer / warningDuration);
            float pulse = ArcadeAccessibilitySettings.ReducedMotion
                ? 1f
                : 0.9f + Mathf.Sin(normalized * Mathf.PI * 8f) * 0.1f;
            warningPulse.localScale = warningBaseScale * pulse;
        }

        private void UpdateEruptionVisual()
        {
            if (eruptionColumn == null || ArcadeAccessibilitySettings.ReducedMotion)
            {
                return;
            }

            float normalized = eruptionDuration <= 0f ? 1f : Mathf.Clamp01(stateTimer / eruptionDuration);
            float height = normalized < 0.2f
                ? Mathf.SmoothStep(0.2f, 1f, normalized / 0.2f)
                : (normalized > 0.78f
                    ? Mathf.SmoothStep(1f, 0.12f, (normalized - 0.78f) / 0.22f)
                    : 1f);
            eruptionColumn.localScale = new Vector3(
                eruptionBaseScale.x,
                eruptionBaseScale.y * height,
                eruptionBaseScale.z);
        }

        private void SetWarningVisible(bool visible)
        {
            if (warningRoot != null)
            {
                warningRoot.SetActive(visible);
            }

            if (!visible && warningPulse != null)
            {
                warningPulse.localScale = warningBaseScale;
            }
        }

        private void SetEruptionVisible(bool visible)
        {
            if (eruptionRoot != null)
            {
                eruptionRoot.SetActive(visible);
            }

            if (eruptionHitbox != null)
            {
                eruptionHitbox.enabled = visible;
            }

            if (eruptionColumn != null)
            {
                eruptionColumn.localScale = eruptionBaseScale;
            }
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

            if (cameraFollow == null)
            {
                cameraFollow = FindAnyObjectByType<SmoothCameraFollow2D>();
            }
        }
    }
}
