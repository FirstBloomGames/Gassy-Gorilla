using System.Collections;
using FirstBloom.ArcadeFramework.Audio;
using FirstBloom.ArcadeFramework.Camera;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public sealed class CrocodileAmbushController : MonoBehaviour
    {
        private enum AmbushPhase
        {
            Waiting,
            Warning,
            Lunging,
            Settling,
            Complete
        }

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Encounter References")]
        [SerializeField] private Transform motionRoot;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private Collider2D biteCollider;
        [SerializeField] private Transform bitePoint;

        [Header("Warning")]
        [SerializeField] private GameObject warningRoot;
        [SerializeField] private Transform[] warningRipples;
        [SerializeField] private Renderer[] warningRenderers;
        [SerializeField] private ParticleSystem warningBubbles;
        [SerializeField] private ParticleSystem launchSplash;
        [SerializeField] private Color warningColor = new Color(1f, 0.82f, 0.28f, 0.9f);

        [Header("Fairness")]
        [Min(2f)] [SerializeField] private float activationDistance = 7.35f;
        [Min(0.5f)] [SerializeField] private float minimumLeadDistance = 1.6f;
        [Min(0f)] [SerializeField] private float minimumFuel = 17.5f;
        [SerializeField] private float skipBehindDistance = -0.8f;

        [Header("Timing")]
        [Min(0.1f)] [SerializeField] private float warningDuration = 0.8f;
        [Min(0.2f)] [SerializeField] private float lungeDuration = 0.84f;
        [Range(0f, 1f)] [SerializeField] private float biteWindowStart = 0.2f;
        [Range(0f, 1f)] [SerializeField] private float biteWindowEnd = 0.58f;
        [Range(0f, 1f)] [SerializeField] private float missSnapNormalizedTime = 0.48f;
        [Min(0.1f)] [SerializeField] private float settleDuration = 0.42f;
        [Min(0.05f)] [SerializeField] private float playerSnapDuration = 0.13f;

        [Header("Motion")]
        [SerializeField] private Vector3 submergedLocalPosition = new Vector3(0f, -0.18f, 0f);
        [SerializeField] private float lungeHeight = 2.55f;
        [SerializeField] private float lungeHorizontalTravel = -0.48f;
        [SerializeField] private float warningBobHeight = 0.045f;
        [SerializeField] private float settleDepth = 0.46f;
        [SerializeField] private Vector3 playerBiteOffset = new Vector3(0.08f, 0.08f, 0f);

        [Header("Polish")]
        [Range(0f, 1f)] [SerializeField] private float warningSplashVolume = 0.28f;
        [Range(0f, 1f)] [SerializeField] private float launchSplashVolume = 0.72f;
        [Range(0f, 1f)] [SerializeField] private float missChompVolume = 0.42f;
        [SerializeField] private float launchShakeIntensity = 0.14f;
        [SerializeField] private float launchShakeDuration = 0.24f;

        private AmbushPhase phase;
        private GassyGorillaGameManager gameManager;
        private GorillaController player;
        private SmoothCameraFollow2D cameraFollow;
        private LagoonFinishPresentation playerPresentation;
        private MaterialPropertyBlock propertyBlock;
        private Vector3[] warningBaseScales;
        private Coroutine encounterRoutine;
        private Coroutine playerSnapRoutine;
        private bool hitCommitted;
        private bool successfulBite;
        private bool qaDodgeBoosted;
        private bool qaHitPrepared;

        public bool IsConfigured
        {
            get
            {
                return motionRoot != null && visualRoot != null && animator != null &&
                    biteCollider != null && bitePoint != null && warningRoot != null &&
                    warningRipples != null && warningRenderers != null &&
                    warningRipples.Length == warningRenderers.Length && warningRipples.Length >= 2;
            }
        }

        public Transform MotionRoot { get { return motionRoot; } }
        public Transform BitePoint { get { return bitePoint; } }
        public Animator Animator { get { return animator; } }
        public Collider2D BiteCollider { get { return biteCollider; } }
        public float ActivationDistance { get { return activationDistance; } }
        public float MinimumLeadDistance { get { return minimumLeadDistance; } }
        public float MinimumFuel { get { return minimumFuel; } }
        public float WarningDuration { get { return warningDuration; } }
        public float LungeDuration { get { return lungeDuration; } }
        public float BiteWindowStart { get { return biteWindowStart; } }
        public float BiteWindowEnd { get { return biteWindowEnd; } }
        public int WarningRippleCount { get { return warningRipples != null ? warningRipples.Length : 0; } }
        public bool IsBiteWindowActive { get { return phase == AmbushPhase.Lunging && biteCollider != null && biteCollider.enabled; } }

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            CacheWarningScales();

            CrocodileAmbushHitbox hitbox = biteCollider != null
                ? biteCollider.GetComponent<CrocodileAmbushHitbox>()
                : GetComponentInChildren<CrocodileAmbushHitbox>(true);
            if (hitbox != null)
            {
                hitbox.Bind(this);
                if (biteCollider == null)
                {
                    biteCollider = hitbox.GetComponent<Collider2D>();
                }
            }

            ResetPresentation();
        }

        private void OnEnable()
        {
            ResetPresentation();
        }

        private void OnDisable()
        {
            if (encounterRoutine != null)
            {
                StopCoroutine(encounterRoutine);
                encounterRoutine = null;
            }

            if (playerSnapRoutine != null)
            {
                StopCoroutine(playerSnapRoutine);
                playerSnapRoutine = null;
            }

            if (biteCollider != null)
            {
                biteCollider.enabled = false;
            }

            if (playerPresentation != null)
            {
                playerPresentation.SetPlayerVisualsVisible(true);
            }
        }

        private void Update()
        {
            if (phase != AmbushPhase.Waiting)
            {
                return;
            }

            ResolveRuntimeReferences();
            if (gameManager == null || player == null || !gameManager.IsRunActive)
            {
                return;
            }

            float leadDistance = transform.position.x - player.transform.position.x;
            if (leadDistance < skipBehindDistance)
            {
                CompleteEncounter(false);
                return;
            }

            if (leadDistance > activationDistance)
            {
                return;
            }

            bool hasFairLead = leadDistance >= minimumLeadDistance;
            bool hasBoostFuel = player.CurrentFuel + 0.01f >= minimumFuel;
            if (!hasFairLead || !hasBoostFuel || player.IsSwinging)
            {
                CompleteEncounter(false);
                return;
            }

            encounterRoutine = StartCoroutine(PlayEncounter());
        }

        public void TryBite(GorillaController candidate)
        {
            if (candidate == null || candidate != player || hitCommitted ||
                phase != AmbushPhase.Lunging || biteCollider == null || !biteCollider.enabled)
            {
                return;
            }

            ResolveRuntimeReferences();
            if (gameManager == null || !gameManager.IsRunActive)
            {
                return;
            }

            hitCommitted = true;
            if (!gameManager.GameOverFromCrocodileAmbush(this))
            {
                hitCommitted = false;
            }
        }

        public void ConfirmSuccessfulBite(GorillaController bittenPlayer)
        {
            if (successfulBite || bittenPlayer == null)
            {
                return;
            }

            successfulBite = true;
            player = bittenPlayer;
            playerPresentation = player.GetComponent<LagoonFinishPresentation>();
            if (animator != null)
            {
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }

            if (playerSnapRoutine != null)
            {
                StopCoroutine(playerSnapRoutine);
            }

            playerSnapRoutine = StartCoroutine(SnapPlayerIntoBite());
        }

        private IEnumerator PlayEncounter()
        {
            phase = AmbushPhase.Warning;
            SetVisualActive(true);
            SetWarningActive(true);
            PlayAnimation("Idle_Submerged", AnimatorUpdateMode.Normal);
            PlayParticles(warningBubbles);
            PlaySfx(ArcadeSfxType.CrocodileWarning, warningSplashVolume);

            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, warningDuration);
            while (elapsed < duration)
            {
                if (!CanContinueEncounter())
                {
                    CompleteEncounter(false);
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                AnimateWarning(t);
                if (!qaDodgeBoosted && player != null && player.ShouldAutoDodgeCrocodileForQa && t >= 0.52f)
                {
                    qaDodgeBoosted = player.TryFartBoost();
                }
                if (!qaHitPrepared && player != null && player.ShouldAutoHitCrocodileForQa && t >= 0.7f)
                {
                    player.PrepareForCrocodileQaHit(0.25f);
                    qaHitPrepared = true;
                }
                yield return null;
            }

            SetWarningActive(false);
            PlayParticles(launchSplash);
            PlaySfx(ArcadeSfxType.Splash, launchSplashVolume);
            if (cameraFollow != null)
            {
                cameraFollow.Shake(launchShakeIntensity, launchShakeDuration);
            }

            PlayAnimation("Lunge_Snap", AnimatorUpdateMode.Normal);
            phase = AmbushPhase.Lunging;
            elapsed = 0f;
            duration = Mathf.Max(0.2f, lungeDuration);
            bool missSnapPlayed = false;

            while (elapsed < duration)
            {
                if (!successfulBite && !CanContinueEncounter())
                {
                    CompleteEncounter(false);
                    yield break;
                }

                elapsed += successfulBite ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                AnimateLunge(t);

                bool biteWindow = !hitCommitted && !successfulBite &&
                    t >= biteWindowStart && t <= biteWindowEnd &&
                    gameManager != null && gameManager.IsRunActive;
                if (biteCollider != null)
                {
                    biteCollider.enabled = biteWindow;
                }

                if (!missSnapPlayed && !successfulBite && t >= missSnapNormalizedTime)
                {
                    missSnapPlayed = true;
                    PlaySfx(ArcadeSfxType.Chomp, missChompVolume);
                }

                yield return null;
            }

            if (biteCollider != null)
            {
                biteCollider.enabled = false;
            }

            phase = AmbushPhase.Settling;
            PlayAnimation("Settle_Submerge", successfulBite ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal);
            Vector3 settleStart = motionRoot != null ? motionRoot.localPosition : submergedLocalPosition;
            Vector3 settleEnd = submergedLocalPosition + new Vector3(lungeHorizontalTravel, -settleDepth, 0f);
            elapsed = 0f;
            duration = Mathf.Max(0.1f, settleDuration);

            while (elapsed < duration)
            {
                elapsed += successfulBite ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (motionRoot != null)
                {
                    motionRoot.localPosition = Vector3.Lerp(settleStart, settleEnd, Mathf.SmoothStep(0f, 1f, t));
                }

                yield return null;
            }

            if (!successfulBite)
            {
                GassyRunEvents.RaiseCrocodileDodged();
            }

            CompleteEncounter(successfulBite);
        }

        private IEnumerator SnapPlayerIntoBite()
        {
            if (player == null || bitePoint == null)
            {
                yield break;
            }

            Transform playerTransform = player.transform;
            Vector3 startPosition = playerTransform.position;
            Quaternion startRotation = playerTransform.rotation;
            Quaternion biteRotation = Quaternion.Euler(0f, 0f, -16f);
            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, playerSnapDuration);

            while (elapsed < duration && playerTransform != null && bitePoint != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                playerTransform.position = Vector3.Lerp(startPosition, bitePoint.position + playerBiteOffset, eased);
                playerTransform.rotation = Quaternion.Slerp(startRotation, biteRotation, eased);
                yield return null;
            }

            if (playerPresentation != null)
            {
                playerPresentation.SetPlayerVisualsVisible(false);
            }

            playerSnapRoutine = null;
        }

        private void ResolveRuntimeReferences()
        {
            if (gameManager == null)
            {
                gameManager = GassyGorillaGameManager.Instance;
                if (gameManager == null)
                {
                    gameManager = FindAnyObjectByType<GassyGorillaGameManager>();
                }
            }

            if (player == null)
            {
                player = FindAnyObjectByType<GorillaController>();
                if (player != null)
                {
                    playerPresentation = player.GetComponent<LagoonFinishPresentation>();
                }
            }

            if (cameraFollow == null)
            {
                cameraFollow = FindAnyObjectByType<SmoothCameraFollow2D>();
            }
        }

        private bool CanContinueEncounter()
        {
            return gameManager != null && gameManager.IsRunActive && player != null;
        }

        private void AnimateWarning(float normalizedTime)
        {
            if (motionRoot != null)
            {
                float bob = Mathf.Sin(normalizedTime * Mathf.PI * 4f) * warningBobHeight;
                motionRoot.localPosition = submergedLocalPosition + Vector3.up * bob;
            }

            int count = Mathf.Min(
                warningRipples != null ? warningRipples.Length : 0,
                warningRenderers != null ? warningRenderers.Length : 0);
            for (int i = 0; i < count; i++)
            {
                Transform ripple = warningRipples[i];
                Renderer rippleRenderer = warningRenderers[i];
                if (ripple == null || rippleRenderer == null)
                {
                    continue;
                }

                float phaseOffset = i / (float)Mathf.Max(1, count);
                float pulse = Mathf.Repeat(normalizedTime * 1.7f + phaseOffset, 1f);
                Vector3 baseScale = i < warningBaseScales.Length ? warningBaseScales[i] : Vector3.one;
                float width = Mathf.Lerp(0.55f, 2.35f, pulse);
                ripple.localScale = new Vector3(baseScale.x * width, baseScale.y * Mathf.Lerp(0.85f, 1.25f, pulse), baseScale.z);
                rippleRenderer.enabled = true;
                SetRendererColor(rippleRenderer, warningColor, Mathf.Sin(pulse * Mathf.PI) * 0.95f);
            }
        }

        private void AnimateLunge(float normalizedTime)
        {
            if (motionRoot == null)
            {
                return;
            }

            float horizontal = Mathf.SmoothStep(0f, 1f, normalizedTime) * lungeHorizontalTravel;
            float vertical = Mathf.Sin(normalizedTime * Mathf.PI) * lungeHeight;
            motionRoot.localPosition = submergedLocalPosition + new Vector3(horizontal, vertical, 0f);
        }

        private void PlayAnimation(string stateName, AnimatorUpdateMode updateMode)
        {
            if (animator == null)
            {
                return;
            }

            animator.updateMode = updateMode;
            animator.Play(stateName, 0, 0f);
            animator.Update(0f);
        }

        private void CompleteEncounter(bool keepPlayerHidden)
        {
            phase = AmbushPhase.Complete;
            if (biteCollider != null)
            {
                biteCollider.enabled = false;
            }

            SetWarningActive(false);
            SetVisualActive(false);
            if (motionRoot != null)
            {
                motionRoot.localPosition = submergedLocalPosition;
            }

            if (!keepPlayerHidden && playerPresentation != null)
            {
                playerPresentation.SetPlayerVisualsVisible(true);
            }

            encounterRoutine = null;
        }

        private void ResetPresentation()
        {
            phase = AmbushPhase.Waiting;
            hitCommitted = false;
            successfulBite = false;
            qaDodgeBoosted = false;
            qaHitPrepared = false;
            if (motionRoot != null)
            {
                motionRoot.localPosition = submergedLocalPosition;
            }

            if (biteCollider != null)
            {
                biteCollider.enabled = false;
            }

            SetWarningActive(false);
            SetVisualActive(false);
        }

        private void CacheWarningScales()
        {
            int count = warningRipples != null ? warningRipples.Length : 0;
            warningBaseScales = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                warningBaseScales[i] = warningRipples[i] != null ? warningRipples[i].localScale : Vector3.one;
            }
        }

        private void SetWarningActive(bool active)
        {
            if (warningRoot != null)
            {
                warningRoot.SetActive(active);
            }

            if (warningRenderers == null)
            {
                return;
            }

            for (int i = 0; i < warningRenderers.Length; i++)
            {
                if (warningRenderers[i] != null)
                {
                    warningRenderers[i].enabled = active;
                }
            }
        }

        private void SetVisualActive(bool active)
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(active);
            }
        }

        private void SetRendererColor(Renderer targetRenderer, Color color, float alphaScale)
        {
            if (targetRenderer == null)
            {
                return;
            }

            color.a *= Mathf.Clamp01(alphaScale);
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(ColorId, color);
            propertyBlock.SetColor(BaseColorId, color);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private static void PlayParticles(ParticleSystem particles)
        {
            if (particles == null)
            {
                return;
            }

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play(true);
        }

        private static void PlaySfx(ArcadeSfxType type, float volume)
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(type, volume);
            }
        }
    }
}
