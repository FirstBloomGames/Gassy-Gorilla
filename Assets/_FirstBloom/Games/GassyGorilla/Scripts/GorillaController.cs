using System;
using System.Collections;
using FirstBloom.ArcadeFramework.Accessibility;
using FirstBloom.ArcadeFramework.Audio;
using FirstBloom.ArcadeFramework.Camera;
using FirstBloom.ArcadeFramework.Core;
using FirstBloom.ArcadeFramework.Input;
using FirstBloom.ArcadeFramework.VFX;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class GorillaController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float forwardSpeed = 4.65f;
        [SerializeField] private float fartBoostVelocity = 5.95f;
        [SerializeField] private float boostForwardKick = 0.58f;
        [SerializeField] private float boostForwardKickDuration = 0.18f;
        [SerializeField] private float horizontalCruiseReturn = 2.4f;
        [SerializeField] private float maxVerticalSpeed = 7.35f;
        [SerializeField] private float gravityScale = 1.5f;
        [SerializeField] private float boostCooldown = 0.08f;
        [SerializeField] private float boostInputBuffer = 0.1f;
        [SerializeField] private float boostFallRecovery = 0.34f;
        [SerializeField] private float boostUpwardCarry = 0.16f;
        [SerializeField] private float maxBoostVerticalBonus = 1.1f;
        [Min(0.01f)] [SerializeField] private float difficultySpeedResponse = 0.35f;

        [Header("Fuel")]
        [SerializeField] private float maxFuel = 100f;
        [SerializeField] private float fuelDrainPerBoost = 18f;
        [SerializeField] private float passiveRefillPerSecond;
        [SerializeField] private float failedBoostFeedbackCooldown = 0.35f;

        [Header("Swinging")]
        [SerializeField] private float swingAngleDegrees = 26f;
        [SerializeField] private float swingSpeed = 2.35f;
        [SerializeField] private Vector2 vineReleaseVelocity = new Vector2(4.7f, 2.4f);
        [SerializeField] private float swingEntryArcHeight = 0.14f;
        [SerializeField] private float swingEntryBlendDuration = 0.13f;
        [SerializeField] private float swingEntryOvershoot = 0.04f;
        [SerializeField] private float swingMomentumInheritance = 0.55f;
        [SerializeField] private float verticalMomentumInheritance = 0.38f;
        [SerializeField] private float releaseReachForwardBonus = 5.25f;
        [SerializeField] private float releaseReachLiftBonus = 2.7f;
        [SerializeField] private float releasePowerExponent = 1.25f;
        [SerializeField] private float returningReleasePowerScale = 0.58f;
        [SerializeField] private float maxInheritedSwingSpeed = 8.5f;
        [SerializeField] private float maxReleaseForwardSpeed = 11.8f;
        [Min(0f)] [SerializeField] private float minimumVineReleaseLift = 4f;
        [Min(0.1f)] [SerializeField] private float vineReleaseSafetyDuration = 1f;
        [SerializeField] private float vineReleaseDangerY = -1.72f;
        [Min(0f)] [SerializeField] private float vineReleaseSafetyClearance = 0.35f;
        [SerializeField] private float swingCameraSmoothingMultiplier = 1.85f;
        [SerializeField] private float vineSlowMoScale = 0.88f;
        [SerializeField] private float vineSlowMoDuration = 0.07f;

        [Header("Sticky Sap")]
        [Range(0.1f, 0.3f)]
        [SerializeField] private float stickySapMinimumHoldDuration = 0.16f;

        [Header("Polish")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private ParticleSystem fartPuff;
        [SerializeField] private ParticleSystem fartShockwaveBurst;
        [SerializeField] private ParticleSystem fartSparkBurst;
        [SerializeField] private ParticleSystem speedLineBurst;
        [SerializeField] private Renderer fartCloudRenderer;
        [SerializeField] private Renderer fartCoreRenderer;
        [SerializeField] private Renderer fartRingRenderer;
        [SerializeField] private Renderer[] fartAccentRenderers;
        [SerializeField] private SmoothCameraFollow2D cameraFollow;

        private Rigidbody2D body;
        private GassyGorillaGameManager gameManager;
        private bool inputEnabled = true;
        private float nextBoostTime;
        private float bufferedBoostUntil;
        private float originalGravityScale;
        private Coroutine squashRoutine;
        private Coroutine fartCloudRoutine;
        private MaterialPropertyBlock fartCloudPropertyBlock;
        private float forwardKickTimer;
        private float nextFailedBoostFeedbackTime;
        private VineSwingTrigger currentVine;
        private Transform swingPivot;
        private Vector3 swingRestOffset;
        private Vector3 swingEntryStartPosition;
        private float swingAttachTime;
        private float swingRadius;
        private float swingTimer;
        private float currentSwingAngleDegrees;
        private float currentReleasePower;
        private Vector2 currentSwingVelocity;
        private bool crocodileQaMode;
        private bool crocodileQaAutoDodge;
        private bool crocodileQaAutoHit;
        private float targetDifficultySpeedMultiplier = 1f;
        private float currentDifficultySpeedMultiplier = 1f;
        private bool isStickySap;
        private float stickyForwardSpeedScale = 1f;
        private GassyStickySapTrap currentStickySap;
        private Transform stickySapAnchor;
        private Vector3 stickySapEntryPosition;
        private float stickySapCapturedAt;
        private bool isBounceBloomCompressing;
        private GassyBounceBloom currentBounceBloom;
        private Transform bounceBloomAnchor;
        private Vector3 bounceBloomEntryPosition;
        private float bounceBloomStartedAt;
        private float bounceBloomCompressionDuration;
        private float bounceBloomLiftVelocity;
        private float bounceBloomForwardKick;

        public event Action<float, float> FuelChanged;
        public event Action Boosted;
        public event Action BoostFailed;
        public event Action VineGrabbed;
        public event Action VineReleased;
        public event Action StickySapCaught;
        public event Action StickySapEscaped;
        public event Action BounceBloomCompressed;
        public event Action BounceBloomLaunched;

        public float CurrentFuel { get; private set; }
        public float MaxFuel { get { return maxFuel; } }
        public float EffectiveForwardSpeed { get { return forwardSpeed * currentDifficultySpeedMultiplier; } }
        public float DifficultySpeedMultiplier { get { return currentDifficultySpeedMultiplier; } }
        public bool IsSwinging { get; private set; }
        public float CurrentSwingAngleDegrees { get { return currentSwingAngleDegrees; } }
        public float CurrentReleasePower { get { return currentReleasePower; } }
        public Vector2 CurrentSwingVelocity { get { return currentSwingVelocity; } }
        public float MaximumVerticalSpeed { get { return maxVerticalSpeed; } }
        public float GravityScale { get { return gravityScale; } }
        public float MinimumVineReleaseLift { get { return minimumVineReleaseLift; } }
        public float VineReleaseSafetyDuration { get { return vineReleaseSafetyDuration; } }
        public float VineReleaseDangerY { get { return vineReleaseDangerY; } }
        public float VineReleaseSafetyClearance { get { return vineReleaseSafetyClearance; } }
        public bool IsStickySap { get { return isStickySap; } }
        public bool IsBounceBloomCompressing
        {
            get { return isBounceBloomCompressing; }
        }
        public bool IsAnchoredLessonInteraction
        {
            get { return isStickySap || isBounceBloomCompressing; }
        }
        public bool ShouldAutoDodgeCrocodileForQa { get { return crocodileQaMode && crocodileQaAutoDodge; } }
        public bool ShouldAutoHitCrocodileForQa { get { return crocodileQaMode && crocodileQaAutoHit; } }
        public Vector3 CurrentVineGripPosition
        {
            get
            {
                if (IsSwinging && currentVine != null)
                {
                    return currentVine.GrabPoint.position;
                }

                return transform.position;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            gameManager = FindAnyObjectByType<GassyGorillaGameManager>();

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            originalGravityScale = gravityScale;
            body.gravityScale = gravityScale;
            CurrentFuel = maxFuel;
        }

        private void Start()
        {
            NotifyFuelChanged();
            HideFartCloud();
        }

        private void Update()
        {
            if (!inputEnabled || gameManager == null || !gameManager.IsRunActive)
            {
                return;
            }

            if (IsSwinging)
            {
                UpdateSwingPose();
            }
            else if (isStickySap)
            {
                UpdateStickySapPose();
            }
            else if (isBounceBloomCompressing)
            {
                UpdateBounceBloomPose();
            }
            else if (passiveRefillPerSecond > 0f)
            {
                RefillFuel(passiveRefillPerSecond * Time.deltaTime, false);
            }

            if (!IsSwinging && !isStickySap && !isBounceBloomCompressing)
            {
                UpdateAirTilt();
            }

            if (OneTouchInput.WasPressedThisFrame(true, true))
            {
                if (ArcadeAudioManager.Instance != null)
                {
                    ArcadeAudioManager.Instance.NotifyUserGesture();
                }

                if (IsSwinging)
                {
                    bufferedBoostUntil = 0f;
                    ReleaseFromVine();
                }
                else if (isStickySap)
                {
                    bufferedBoostUntil = 0f;
                    TryEscapeStickySap();
                }
                else if (isBounceBloomCompressing)
                {
                    bufferedBoostUntil = 0f;
                }
                else
                {
                    bool boosted = TryFartBoost();
                    if (!boosted && Time.time < nextBoostTime && CurrentFuel >= fuelDrainPerBoost)
                    {
                        bufferedBoostUntil = Time.time + boostInputBuffer;
                    }
                }
            }

            if (!IsSwinging &&
                !isStickySap &&
                !isBounceBloomCompressing &&
                bufferedBoostUntil > 0f)
            {
                if (Time.time > bufferedBoostUntil)
                {
                    bufferedBoostUntil = 0f;
                }
                else if (Time.time >= nextBoostTime)
                {
                    bufferedBoostUntil = 0f;
                    TryFartBoost();
                }
            }
        }

        private void FixedUpdate()
        {
            if (gameManager == null ||
                !gameManager.IsRunActive ||
                IsSwinging ||
                isStickySap ||
                isBounceBloomCompressing)
            {
                return;
            }

            currentDifficultySpeedMultiplier = Mathf.MoveTowards(
                currentDifficultySpeedMultiplier,
                targetDifficultySpeedMultiplier,
                difficultySpeedResponse * Time.fixedDeltaTime);

            Vector2 velocity = GetVelocity();
            float targetForwardSpeed = EffectiveForwardSpeed *
                (isStickySap ? stickyForwardSpeedScale : 1f);
            if (forwardKickTimer > 0f)
            {
                forwardKickTimer -= Time.fixedDeltaTime;
                targetForwardSpeed += boostForwardKick;
            }

            velocity.x = velocity.x < targetForwardSpeed
                ? targetForwardSpeed
                : Mathf.MoveTowards(velocity.x, targetForwardSpeed, horizontalCruiseReturn * Time.fixedDeltaTime);
            velocity.y = Mathf.Clamp(velocity.y, -maxVerticalSpeed, maxVerticalSpeed);
            SetVelocity(velocity);
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }

        public void SetDifficultySpeedMultiplier(float multiplier)
        {
            targetDifficultySpeedMultiplier = Mathf.Clamp(multiplier, 0.8f, 1.2f);
        }

        public bool TryFartBoost()
        {
            if (isStickySap)
            {
                return TryEscapeStickySap();
            }

            if (isBounceBloomCompressing)
            {
                return false;
            }

            if (Time.time < nextBoostTime)
            {
                return false;
            }

            if (CurrentFuel < fuelDrainPerBoost)
            {
                HandleBoostFailed();
                return false;
            }

            nextBoostTime = Time.time + boostCooldown;
            forwardKickTimer = boostForwardKickDuration;
            bufferedBoostUntil = 0f;
            CurrentFuel = crocodileQaMode
                ? maxFuel
                : Mathf.Max(0f, CurrentFuel - fuelDrainPerBoost);
            NotifyFuelChanged();

            Vector2 velocity = GetVelocity();
            velocity.x = Mathf.Max(velocity.x, EffectiveForwardSpeed + boostForwardKick);
            float verticalBonus = velocity.y < 0f
                ? Mathf.Min(maxBoostVerticalBonus, -velocity.y * boostFallRecovery)
                : Mathf.Min(maxBoostVerticalBonus * 0.55f, velocity.y * boostUpwardCarry);
            velocity.y = Mathf.Min(maxVerticalSpeed, fartBoostVelocity + verticalBonus);
            SetVelocity(velocity);

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.Boost);
            }

            if (fartPuff != null)
            {
                fartPuff.Play();
            }

            if (fartShockwaveBurst != null)
            {
                fartShockwaveBurst.Play();
            }

            if (fartSparkBurst != null)
            {
                fartSparkBurst.Play();
            }

            ShowFartCloudBurst();
            PlaySpeedLines();

            if (cameraFollow != null)
            {
                cameraFollow.Shake(0.08f, 0.16f);
                cameraFollow.AddActionLookahead(new Vector2(0.22f, 0.08f), 0.08f);
            }

            PlaySquash(new Vector3(1.12f, 0.88f, 1f), 0.11f);
            if (Boosted != null)
            {
                Boosted.Invoke();
            }

            return true;
        }

        public bool TryEnterStickySap(float forwardSpeedScale)
        {
            return TryEnterStickySap(null, null, forwardSpeedScale);
        }

        public bool TryEnterStickySap(
            GassyStickySapTrap trap,
            Transform anchor,
            float forwardSpeedScale)
        {
            if (isStickySap ||
                isBounceBloomCompressing ||
                IsSwinging ||
                !inputEnabled ||
                gameManager == null ||
                !gameManager.IsRunActive)
            {
                return false;
            }

            isStickySap = true;
            currentStickySap = trap;
            stickySapAnchor = anchor;
            stickySapEntryPosition = transform.position;
            stickySapCapturedAt = Time.time;
            stickyForwardSpeedScale =
                Mathf.Clamp(forwardSpeedScale, 0.4f, 0.65f);
            bufferedBoostUntil = 0f;

            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            SetVelocity(Vector2.zero);
            transform.rotation = Quaternion.identity;

            PlaySquash(new Vector3(1.08f, 0.84f, 1.04f), 0.18f);
            if (cameraFollow != null)
            {
                cameraFollow.Shake(0.08f, 0.14f);
                cameraFollow.AddActionLookahead(
                    new Vector2(0.08f, -0.08f),
                    0.1f);
            }

            GassyRunEvents.RaiseInteractionStarted(
                GassyInteractionType.SapEscape);
            if (StickySapCaught != null)
            {
                StickySapCaught.Invoke();
            }

            return true;
        }

        public bool TryEscapeStickySap()
        {
            if (!isStickySap ||
                Time.time - stickySapCapturedAt < stickySapMinimumHoldDuration ||
                gameManager == null ||
                !gameManager.IsRunActive)
            {
                return false;
            }

            GassyStickySapTrap releasedTrap = currentStickySap;
            Vector2 releasePosition = transform.position;
            isStickySap = false;
            currentStickySap = null;
            stickySapAnchor = null;
            stickyForwardSpeedScale = 1f;
            nextBoostTime = Time.time + boostCooldown;
            forwardKickTimer = boostForwardKickDuration;

            body.position = releasePosition;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = originalGravityScale;
            body.position = releasePosition;
            Vector2 velocity = new Vector2(
                EffectiveForwardSpeed + boostForwardKick,
                Mathf.Clamp(
                    fartBoostVelocity * 0.94f,
                    -maxVerticalSpeed,
                    maxVerticalSpeed));
            SetVelocity(velocity);

            releasedTrap?.NotifyEscaped(this);
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.SapPop);
            }

            if (fartPuff != null)
            {
                fartPuff.Play();
            }

            if (fartShockwaveBurst != null)
            {
                fartShockwaveBurst.Play();
            }

            ShowFartCloudBurst();
            PlaySpeedLines();
            PlaySquash(new Vector3(1.2f, 0.84f, 1.05f), 0.16f);

            if (cameraFollow != null)
            {
                cameraFollow.Shake(0.13f, 0.18f);
                cameraFollow.AddActionLookahead(
                    new Vector2(0.56f, 0.26f),
                    0.18f);
            }

            ArcadeHaptics.Play(ArcadeHapticType.Medium);
            GassyRunEvents.RaiseInteractionCompleted(
                GassyInteractionType.SapEscape);
            if (StickySapEscaped != null)
            {
                StickySapEscaped.Invoke();
            }

            return true;
        }

        public void CancelStickySap(GassyStickySapTrap trap)
        {
            if (!isStickySap ||
                (currentStickySap != null && currentStickySap != trap))
            {
                return;
            }

            isStickySap = false;
            currentStickySap = null;
            stickySapAnchor = null;
            stickyForwardSpeedScale = 1f;
            if (body != null && gameManager != null && gameManager.IsRunActive)
            {
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = originalGravityScale;
                SetVelocity(new Vector2(EffectiveForwardSpeed, 0f));
            }
        }

        public bool ApplyUpdraft(float liftVelocity)
        {
            if (IsSwinging ||
                isStickySap ||
                isBounceBloomCompressing ||
                gameManager == null ||
                !gameManager.IsRunActive)
            {
                return false;
            }

            Vector2 velocity = GetVelocity();
            velocity.x = Mathf.Max(velocity.x, EffectiveForwardSpeed);
            velocity.y = Mathf.Clamp(
                Mathf.Max(velocity.y, liftVelocity),
                -maxVerticalSpeed,
                maxVerticalSpeed);
            SetVelocity(velocity);

            GassyRunEvents.RaiseInteractionStarted(GassyInteractionType.UpdraftRide);
            PlaySpeedLines();
            PlaySquash(new Vector3(1.04f, 1.1f, 1f), 0.16f);
            if (cameraFollow != null)
            {
                cameraFollow.AddActionLookahead(new Vector2(0.16f, 0.3f), 0.18f);
            }

            return true;
        }

        public bool TryBeginBounceBloom(
            GassyBounceBloom bloom,
            Transform anchor,
            float compressionDuration,
            float liftVelocity,
            float forwardVelocity)
        {
            if (bloom == null ||
                anchor == null ||
                IsSwinging ||
                isStickySap ||
                isBounceBloomCompressing ||
                !inputEnabled ||
                gameManager == null ||
                !gameManager.IsRunActive)
            {
                return false;
            }

            isBounceBloomCompressing = true;
            currentBounceBloom = bloom;
            bounceBloomAnchor = anchor;
            bounceBloomEntryPosition = transform.position;
            bounceBloomStartedAt = Time.time;
            bounceBloomCompressionDuration =
                Mathf.Clamp(compressionDuration, 0.1f, 0.3f);
            bounceBloomLiftVelocity = liftVelocity;
            bounceBloomForwardKick = forwardVelocity;
            bufferedBoostUntil = 0f;

            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            SetVelocity(Vector2.zero);
            transform.rotation = Quaternion.identity;

            GassyRunEvents.RaiseInteractionStarted(
                GassyInteractionType.BounceBloom);
            PlaySquash(new Vector3(1.14f, 0.8f, 1.06f), 0.16f);
            if (cameraFollow != null)
            {
                cameraFollow.Shake(0.06f, 0.12f);
                cameraFollow.AddActionLookahead(
                    new Vector2(0.12f, -0.06f),
                    0.1f);
            }

            if (BounceBloomCompressed != null)
            {
                BounceBloomCompressed.Invoke();
            }

            return true;
        }

        public bool ApplyBounceBloom(
            float liftVelocity,
            float forwardVelocity)
        {
            if (IsSwinging ||
                isStickySap ||
                isBounceBloomCompressing ||
                !inputEnabled ||
                gameManager == null ||
                !gameManager.IsRunActive)
            {
                return false;
            }

            GassyRunEvents.RaiseInteractionStarted(
                GassyInteractionType.BounceBloom);
            ApplyBounceBloomLaunch(liftVelocity, forwardVelocity);
            return true;
        }

#if UNITY_EDITOR
        public bool CompleteBounceBloomForQa()
        {
            if (!isBounceBloomCompressing)
            {
                return false;
            }

            CompleteBounceBloomLaunch();
            return true;
        }
#endif

        public void CancelBounceBloom(GassyBounceBloom bloom)
        {
            if (!isBounceBloomCompressing ||
                (currentBounceBloom != null && currentBounceBloom != bloom))
            {
                return;
            }

            isBounceBloomCompressing = false;
            currentBounceBloom = null;
            bounceBloomAnchor = null;
            if (body != null && gameManager != null && gameManager.IsRunActive)
            {
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = originalGravityScale;
                SetVelocity(new Vector2(EffectiveForwardSpeed, 0f));
            }
        }

        private void CompleteBounceBloomLaunch()
        {
            if (!isBounceBloomCompressing)
            {
                return;
            }

            GassyBounceBloom launchedBloom = currentBounceBloom;
            Vector2 releasePosition = transform.position;
            float liftVelocity = bounceBloomLiftVelocity;
            float forwardVelocity = bounceBloomForwardKick;
            isBounceBloomCompressing = false;
            currentBounceBloom = null;
            bounceBloomAnchor = null;
            nextBoostTime = Time.time + Mathf.Min(boostCooldown, 0.12f);

            body.position = releasePosition;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = originalGravityScale;
            body.position = releasePosition;
            ApplyBounceBloomLaunch(liftVelocity, forwardVelocity);
            launchedBloom?.NotifyLaunched(this);
            if (BounceBloomLaunched != null)
            {
                BounceBloomLaunched.Invoke();
            }
        }

        private void ApplyBounceBloomLaunch(
            float liftVelocity,
            float forwardVelocity)
        {
            Vector2 velocity = GetVelocity();
            velocity.x = Mathf.Max(
                velocity.x,
                EffectiveForwardSpeed + Mathf.Max(0f, forwardVelocity));
            velocity.y = Mathf.Clamp(
                Mathf.Max(velocity.y, liftVelocity),
                -maxVerticalSpeed,
                maxVerticalSpeed);
            SetVelocity(velocity);
            forwardKickTimer = Mathf.Max(forwardKickTimer, 0.2f);
            bufferedBoostUntil = 0f;

            PlaySpeedLines();
            PlaySquash(new Vector3(1.2f, 0.82f, 1.06f), 0.16f);
            if (cameraFollow != null)
            {
                cameraFollow.Shake(0.12f, 0.18f);
                cameraFollow.AddActionLookahead(
                    new Vector2(0.48f, 0.48f),
                    0.22f);
            }
        }

        private void HandleBoostFailed()
        {
            if (Time.time < nextFailedBoostFeedbackTime)
            {
                return;
            }

            nextFailedBoostFeedbackTime = Time.time + failedBoostFeedbackCooldown;

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.BoostFailed);
            }

            if (cameraFollow != null)
            {
                cameraFollow.Shake(0.05f, 0.09f);
            }

            PlaySquash(new Vector3(0.94f, 1.04f, 1f), 0.08f);

            if (BoostFailed != null)
            {
                BoostFailed.Invoke();
            }
        }

        public void RefillFuel(float amount, bool playPickupPolish = true)
        {
            if (amount <= 0f)
            {
                return;
            }

            CurrentFuel = Mathf.Clamp(CurrentFuel + amount, 0f, maxFuel);
            NotifyFuelChanged();

            if (playPickupPolish)
            {
                PlaySquash(new Vector3(1.08f, 1.08f, 1f), 0.12f);
            }
        }

        public bool TryAttachToVine(VineSwingTrigger vine)
        {
            if (vine == null ||
                IsSwinging ||
                isStickySap ||
                isBounceBloomCompressing ||
                gameManager == null || !gameManager.IsRunActive)
            {
                return false;
            }

            currentVine = vine;
            swingPivot = vine.PivotPoint;
            Transform grabPoint = vine.GrabPoint;

            Vector3 snapPosition = grabPoint != null ? grabPoint.position : vine.transform.position;

            swingEntryStartPosition = transform.position;
            Vector3 pivotPosition = swingPivot != null ? swingPivot.position : vine.transform.position;
            Vector3 caughtOffset = snapPosition - pivotPosition;
            swingRadius = caughtOffset.magnitude;
            if (swingRadius < 0.5f)
            {
                swingRadius = 1.5f;
            }

            swingRestOffset = Vector3.down * swingRadius;

            float entryAngle = Mathf.Atan2(caughtOffset.x, -caughtOffset.y) * Mathf.Rad2Deg;
            currentSwingAngleDegrees = Mathf.Clamp(entryAngle, -swingAngleDegrees * 0.9f, swingAngleDegrees * 0.9f);
            float normalizedEntryAngle = swingAngleDegrees <= 0.01f
                ? 0f
                : Mathf.Clamp(currentSwingAngleDegrees / swingAngleDegrees, -0.98f, 0.98f);

            swingAttachTime = Time.time;
            swingTimer = Mathf.Asin(normalizedEntryAngle);
            currentReleasePower = CalculateReleasePower();
            currentSwingVelocity = CalculateSwingVelocity();
            IsSwinging = true;

            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            SetVelocity(Vector2.zero);
            transform.rotation = Quaternion.identity;
            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, currentSwingAngleDegrees);
            }

            currentVine.NotifyGrabbed();

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.VineGrab);
            }

            if (ArcadeTimeController.Instance != null)
            {
                ArcadeTimeController.Instance.PlaySlowMotion(vineSlowMoScale, vineSlowMoDuration);
            }

            if (cameraFollow != null)
            {
                cameraFollow.SetFollowSmoothingMultiplier(swingCameraSmoothingMultiplier);
                cameraFollow.Shake(0.12f, 0.18f);
                cameraFollow.AddActionLookahead(new Vector2(0.28f, 0.18f), 0.1f);
            }

            PlaySquash(new Vector3(0.94f, 1.12f, 1f), 0.15f);
            if (VineGrabbed != null)
            {
                VineGrabbed.Invoke();
            }

            return true;
        }

        public void ReleaseFromVine()
        {
            if (!IsSwinging)
            {
                return;
            }

            float releasePower = currentReleasePower;
            Vector2 releaseVelocity = CalculateVineReleaseVelocity(releasePower);
            Vector2 releasePosition = transform.position;

            if (gameManager != null && gameManager.IsVineQaMode)
            {
                Debug.Log("[GG_VINE_QA] Release position=" + transform.position + " angle=" +
                    currentSwingAngleDegrees.ToString("0.00") + " power=" + releasePower.ToString("0.00") +
                    " velocity=" + releaseVelocity + " staleBodyPosition=" + body.position + ".", this);
            }

            IsSwinging = false;
            transform.rotation = Quaternion.identity;
            body.position = releasePosition;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = originalGravityScale;
            body.position = releasePosition;
            SetVelocity(releaseVelocity);

            if (currentVine != null)
            {
                currentVine.NotifyReleased();
                currentVine = null;
            }

            swingPivot = null;

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.VineRelease);
            }

            if (cameraFollow != null)
            {
                cameraFollow.SetFollowSmoothingMultiplier(1f);
                cameraFollow.Shake(Mathf.Lerp(0.1f, 0.2f, releasePower), 0.2f);
                cameraFollow.AddActionLookahead(
                    new Vector2(Mathf.Lerp(0.42f, 0.92f, releasePower), Mathf.Lerp(0.12f, 0.3f, releasePower)),
                    0.2f);
            }

            PlaySquash(Vector3.Lerp(new Vector3(1.08f, 0.94f, 1f), new Vector3(1.2f, 0.86f, 1f), releasePower), 0.14f);
            PlaySpeedLines();
            currentReleasePower = 0f;
            currentSwingVelocity = Vector2.zero;
            if (VineReleased != null)
            {
                VineReleased.Invoke();
            }
        }

        public void PrepareForIntro()
        {
            ClearTransientInteractions();
            ResetSwingState();
            IsSwinging = false;
            inputEnabled = false;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            SetVelocity(Vector2.zero);
            transform.rotation = Quaternion.identity;
            HideFartCloud();
            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.identity;
                visualRoot.localScale = Vector3.one;
            }
        }

        public void BeginRun()
        {
            ClearTransientInteractions();
            ResetSwingState();
            IsSwinging = false;
            inputEnabled = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = originalGravityScale;
            SetVelocity(Vector2.zero);
        }

        public void ConfigureCrocodileQa(string mode)
        {
            crocodileQaMode = true;
            crocodileQaAutoDodge = string.Equals(mode, "dodge", StringComparison.OrdinalIgnoreCase);
            crocodileQaAutoHit = string.Equals(mode, "hit", StringComparison.OrdinalIgnoreCase);
            CurrentFuel = maxFuel;
            NotifyFuelChanged();
        }

        public void PrepareForCrocodileQaHit(float targetY)
        {
            if (!ShouldAutoHitCrocodileForQa || body == null)
            {
                return;
            }

            ClearTransientInteractions();
            ResetSwingState();
            IsSwinging = false;
            inputEnabled = false;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = originalGravityScale;
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
            SetVelocity(new Vector2(Mathf.Max(GetVelocity().x, EffectiveForwardSpeed), 0f));
        }

        public void RecoverForCrocodileQa(float recoveryY)
        {
            if (!crocodileQaMode || body == null)
            {
                return;
            }

            ClearTransientInteractions();
            ResetSwingState();
            IsSwinging = false;
            inputEnabled = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = originalGravityScale;
            transform.position = new Vector3(transform.position.x, recoveryY, transform.position.z);
            SetVelocity(new Vector2(EffectiveForwardSpeed, fartBoostVelocity * 0.72f));
            CurrentFuel = maxFuel;
            NotifyFuelChanged();
        }

        public void StopForGameOver()
        {
            StopForGameOver(float.NegativeInfinity);
        }

        public void StopForGameOver(float minimumRestY)
        {
            ClearTransientInteractions();
            ResetSwingState();
            IsSwinging = false;
            inputEnabled = false;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            SetVelocity(Vector2.zero);
            if (!float.IsNegativeInfinity(minimumRestY) && transform.position.y < minimumRestY)
            {
                transform.position = new Vector3(transform.position.x, minimumRestY, transform.position.z);
            }

            if (squashRoutine != null)
            {
                StopCoroutine(squashRoutine);
                squashRoutine = null;
            }

            HideFartCloud();
            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.identity;
                visualRoot.localScale = Vector3.one;
            }
        }

        private void OnArcadeHazardHit(ArcadeHazard hazard)
        {
            if (gameManager != null)
            {
                gameManager.GameOver("Hit " + hazard.name);
            }
        }

        private void UpdateSwingPose()
        {
            if (swingPivot == null)
            {
                ReleaseFromVine();
                return;
            }

            swingTimer += Time.deltaTime * swingSpeed;
            currentSwingAngleDegrees = Mathf.Sin(swingTimer) * swingAngleDegrees;
            currentSwingVelocity = CalculateSwingVelocity();
            currentReleasePower = CalculateReleasePower();
            Quaternion swingRotation = Quaternion.Euler(0f, 0f, currentSwingAngleDegrees);

            Vector3 targetPosition;
            if (currentVine != null)
            {
                currentVine.DriveOccupiedSwing(currentSwingAngleDegrees);
                currentVine.SetReleasePower(currentReleasePower);
                targetPosition = currentVine.GrabPoint.position;
            }
            else
            {
                targetPosition = swingPivot.position + swingRotation * swingRestOffset;
            }

            float rawBlend = swingEntryBlendDuration <= 0f ? 1f : Mathf.Clamp01((Time.time - swingAttachTime) / swingEntryBlendDuration);
            float blend = EaseOutBack(rawBlend, swingEntryOvershoot);
            float entryArc = Mathf.Sin(rawBlend * Mathf.PI) * swingEntryArcHeight;
            transform.position = Vector3.LerpUnclamped(swingEntryStartPosition, targetPosition, blend) + Vector3.up * entryArc;
            transform.rotation = Quaternion.identity;

            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, currentSwingAngleDegrees);
            }
        }

        private Vector2 CalculateVineReleaseVelocity(float releasePower)
        {
            float phaseDirection = Mathf.Cos(swingTimer);
            float reachScale = phaseDirection < -0.08f ? returningReleasePowerScale : 1f;
            float inheritedForward = Mathf.Clamp(currentSwingVelocity.x, 0f, maxInheritedSwingSpeed) * swingMomentumInheritance;
            float inheritedLift = Mathf.Clamp(currentSwingVelocity.y, 0f, maxInheritedSwingSpeed) * verticalMomentumInheritance;
            Vector2 releaseVelocity = vineReleaseVelocity;
            releaseVelocity.x += inheritedForward + releasePower * releaseReachForwardBonus * reachScale;
            releaseVelocity.y += inheritedLift + releasePower * releaseReachLiftBonus * reachScale;
            releaseVelocity.x = Mathf.Clamp(releaseVelocity.x, EffectiveForwardSpeed + 0.35f, maxReleaseForwardSpeed);
            float safeLift = CalculateMinimumSafeVineReleaseLift(transform.position.y);
            float minimumLift = Mathf.Min(maxVerticalSpeed, Mathf.Max(minimumVineReleaseLift, safeLift));
            releaseVelocity.y = Mathf.Clamp(releaseVelocity.y, minimumLift, maxVerticalSpeed);
            return releaseVelocity;
        }

        public float CalculateMinimumSafeVineReleaseLift(float releaseY)
        {
            float duration = Mathf.Max(0.1f, vineReleaseSafetyDuration);
            float gravityAcceleration = Mathf.Abs(Physics2D.gravity.y * gravityScale);
            float targetY = vineReleaseDangerY + vineReleaseSafetyClearance;
            return (targetY - releaseY + 0.5f * gravityAcceleration * duration * duration) / duration;
        }

        private float CalculateReleasePower()
        {
            if (swingAngleDegrees <= 0.01f)
            {
                return 0f;
            }

            float forwardReach = Mathf.Clamp01(currentSwingAngleDegrees / swingAngleDegrees);
            return Mathf.Pow(Mathf.SmoothStep(0f, 1f, forwardReach), Mathf.Max(0.01f, releasePowerExponent));
        }

        private Vector2 CalculateSwingVelocity()
        {
            float angleRadians = currentSwingAngleDegrees * Mathf.Deg2Rad;
            float angularVelocityRadians = swingAngleDegrees * Mathf.Deg2Rad * Mathf.Cos(swingTimer) * swingSpeed;
            Vector2 velocity = new Vector2(
                swingRadius * Mathf.Cos(angleRadians) * angularVelocityRadians,
                swingRadius * Mathf.Sin(angleRadians) * angularVelocityRadians);
            return Vector2.ClampMagnitude(velocity, maxInheritedSwingSpeed);
        }

        private void ResetSwingState()
        {
            if (currentVine != null)
            {
                currentVine.NotifyReleased();
                currentVine = null;
            }

            currentReleasePower = 0f;
            currentSwingVelocity = Vector2.zero;
            swingPivot = null;
            swingRadius = 0f;
            if (cameraFollow != null)
            {
                cameraFollow.SetFollowSmoothingMultiplier(1f);
            }
        }

        private void ClearTransientInteractions()
        {
            if (currentStickySap != null)
            {
                currentStickySap.NotifyCancelled(this);
            }

            if (currentBounceBloom != null)
            {
                currentBounceBloom.NotifyCancelled(this);
            }

            isStickySap = false;
            stickyForwardSpeedScale = 1f;
            currentStickySap = null;
            stickySapAnchor = null;
            isBounceBloomCompressing = false;
            currentBounceBloom = null;
            bounceBloomAnchor = null;
        }

        private void UpdateStickySapPose()
        {
            Vector3 anchorPosition = stickySapAnchor != null
                ? stickySapAnchor.position
                : stickySapEntryPosition;
            float normalized = Mathf.Clamp01(
                (Time.time - stickySapCapturedAt) / 0.14f);
            float eased = Mathf.SmoothStep(0f, 1f, normalized);
            Vector3 targetPosition = Vector3.Lerp(
                stickySapEntryPosition,
                anchorPosition,
                eased);
            float motionScale =
                ArcadeAccessibilitySettings.ReducedMotion ? 0.25f : 1f;
            targetPosition.y +=
                Mathf.Sin(normalized * Mathf.PI) * 0.09f * motionScale;
            if (normalized >= 1f)
            {
                targetPosition.y += Mathf.Sin(
                    (Time.time - stickySapCapturedAt) * 5.4f) *
                    0.035f *
                    motionScale;
            }

            targetPosition.z = transform.position.z;
            body.position = targetPosition;
            transform.position = targetPosition;
        }

        private void UpdateBounceBloomPose()
        {
            Vector3 anchorPosition = bounceBloomAnchor != null
                ? bounceBloomAnchor.position
                : bounceBloomEntryPosition;
            float normalized = Mathf.Clamp01(
                (Time.time - bounceBloomStartedAt) /
                Mathf.Max(0.01f, bounceBloomCompressionDuration));
            float eased = Mathf.SmoothStep(0f, 1f, normalized);
            Vector3 targetPosition = Vector3.Lerp(
                bounceBloomEntryPosition,
                anchorPosition,
                eased);
            targetPosition.y += Mathf.Sin(normalized * Mathf.PI) * 0.08f;
            targetPosition.z = transform.position.z;
            body.position = targetPosition;
            transform.position = targetPosition;

            if (normalized >= 1f)
            {
                CompleteBounceBloomLaunch();
            }
        }

        private static float EaseOutBack(float t, float overshoot)
        {
            t = Mathf.Clamp01(t) - 1f;
            float strength = 1f + overshoot * 4f;
            return 1f + t * t * ((strength + 1f) * t + strength);
        }

        private void UpdateAirTilt()
        {
            if (visualRoot == null)
            {
                return;
            }

            float verticalVelocity = GetVelocity().y;
            float targetAngle = Mathf.Clamp(-verticalVelocity * 3.2f, -16f, 18f);
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetRotation, 10f * Time.deltaTime);
        }

        private void PlaySquash(Vector3 targetScale, float duration)
        {
            if (visualRoot == null)
            {
                return;
            }

            if (squashRoutine != null)
            {
                StopCoroutine(squashRoutine);
            }

            squashRoutine = StartCoroutine(SquashRoutine(targetScale, duration));
        }

        private IEnumerator SquashRoutine(Vector3 targetScale, float duration)
        {
            Vector3 original = Vector3.one;
            float halfDuration = Mathf.Max(0.02f, duration * 0.5f);
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                visualRoot.localScale = Vector3.Lerp(original, targetScale, elapsed / halfDuration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                visualRoot.localScale = Vector3.Lerp(targetScale, original, elapsed / halfDuration);
                yield return null;
            }

            visualRoot.localScale = original;
            squashRoutine = null;
        }

        private void ShowFartCloudBurst()
        {
            if (fartCloudRenderer == null)
            {
                return;
            }

            if (fartCloudRoutine != null)
            {
                StopCoroutine(fartCloudRoutine);
            }

            fartCloudRoutine = StartCoroutine(FartCloudBurstRoutine());
        }

        private void PlaySpeedLines()
        {
            if (speedLineBurst != null)
            {
                speedLineBurst.Play();
            }
        }

        private IEnumerator FartCloudBurstRoutine()
        {
            Transform cloud = fartCloudRenderer.transform;
            Vector3 basePosition = cloud.localPosition;
            Vector3 baseScale = cloud.localScale;
            Transform core = fartCoreRenderer != null ? fartCoreRenderer.transform : null;
            Vector3 corePosition = core != null ? core.localPosition : Vector3.zero;
            Vector3 coreScale = core != null ? core.localScale : Vector3.one;
            Transform ring = fartRingRenderer != null ? fartRingRenderer.transform : null;
            Vector3 ringPosition = ring != null ? ring.localPosition : Vector3.zero;
            Vector3 ringScale = ring != null ? ring.localScale : Vector3.one;
            Vector3[] accentPositions = CaptureRendererPositions(fartAccentRenderers);
            Vector3[] accentScales = CaptureRendererScales(fartAccentRenderers);

            SetRendererEnabled(fartCloudRenderer, true);
            SetRendererEnabled(fartCoreRenderer, true);
            SetRendererEnabled(fartRingRenderer, true);
            SetRendererEnabled(fartAccentRenderers, true);

            const float duration = 0.34f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float pop = Mathf.Sin(Mathf.Clamp01(t * 1.35f) * Mathf.PI);

                cloud.localPosition = basePosition + new Vector3(-0.26f * eased, 0.05f * pop, 0f);
                cloud.localScale = Vector3.Lerp(baseScale * 0.62f, baseScale * 1.42f, eased);
                SetFartCloudColor(fartCloudRenderer, new Color(0.66f, 1f, 0.42f, Mathf.Lerp(0.95f, 0f, eased)));

                if (core != null)
                {
                    core.localPosition = corePosition + new Vector3(-0.1f * eased, -0.01f * pop, 0f);
                    core.localScale = Vector3.Lerp(coreScale * 0.56f, coreScale * 1.18f, eased);
                    SetFartCloudColor(fartCoreRenderer, new Color(0.98f, 1f, 0.52f, Mathf.Lerp(0.95f, 0f, Mathf.Clamp01(t * 1.25f))));
                }

                if (ring != null)
                {
                    ring.localPosition = ringPosition + new Vector3(-0.34f * eased, 0.01f * pop, 0f);
                    ring.localScale = Vector3.Lerp(ringScale * 0.36f, ringScale * 2.05f, eased);
                    SetFartCloudColor(fartRingRenderer, new Color(0.78f, 1f, 0.26f, Mathf.Lerp(0.7f, 0f, eased)));
                }

                AnimateAccentRenderers(accentPositions, accentScales, eased, pop);
                yield return null;
            }

            cloud.localPosition = basePosition;
            cloud.localScale = baseScale;
            if (core != null)
            {
                core.localPosition = corePosition;
                core.localScale = coreScale;
            }

            if (ring != null)
            {
                ring.localPosition = ringPosition;
                ring.localScale = ringScale;
            }

            RestoreAccentRenderers(accentPositions, accentScales);
            SetRendererEnabled(fartCloudRenderer, false);
            SetRendererEnabled(fartCoreRenderer, false);
            SetRendererEnabled(fartRingRenderer, false);
            SetRendererEnabled(fartAccentRenderers, false);
            fartCloudRoutine = null;
        }

        private void HideFartCloud()
        {
            if (fartCloudRoutine != null)
            {
                StopCoroutine(fartCloudRoutine);
                fartCloudRoutine = null;
            }

            SetFartCloudColor(fartCloudRenderer, new Color(0.68f, 1f, 0.47f, 0f));
            SetFartCloudColor(fartCoreRenderer, new Color(0.98f, 1f, 0.52f, 0f));
            SetFartCloudColor(fartRingRenderer, new Color(0.78f, 1f, 0.26f, 0f));
            SetRendererEnabled(fartCloudRenderer, false);
            SetRendererEnabled(fartCoreRenderer, false);
            SetRendererEnabled(fartRingRenderer, false);
            SetRendererEnabled(fartAccentRenderers, false);
        }

        private void SetFartCloudColor(Renderer targetRenderer, Color color)
        {
            if (targetRenderer == null)
            {
                return;
            }

            if (fartCloudPropertyBlock == null)
            {
                fartCloudPropertyBlock = new MaterialPropertyBlock();
            }

            targetRenderer.GetPropertyBlock(fartCloudPropertyBlock);
            fartCloudPropertyBlock.SetColor("_BaseColor", color);
            fartCloudPropertyBlock.SetColor("_Color", color);
            targetRenderer.SetPropertyBlock(fartCloudPropertyBlock);
        }

        private Vector3[] CaptureRendererPositions(Renderer[] renderers)
        {
            if (renderers == null)
            {
                return null;
            }

            Vector3[] positions = new Vector3[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                positions[i] = renderers[i] != null ? renderers[i].transform.localPosition : Vector3.zero;
            }

            return positions;
        }

        private Vector3[] CaptureRendererScales(Renderer[] renderers)
        {
            if (renderers == null)
            {
                return null;
            }

            Vector3[] scales = new Vector3[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                scales[i] = renderers[i] != null ? renderers[i].transform.localScale : Vector3.one;
            }

            return scales;
        }

        private void AnimateAccentRenderers(Vector3[] basePositions, Vector3[] baseScales, float eased, float pop)
        {
            if (fartAccentRenderers == null || basePositions == null || baseScales == null)
            {
                return;
            }

            for (int i = 0; i < fartAccentRenderers.Length; i++)
            {
                Renderer accent = fartAccentRenderers[i];
                if (accent == null)
                {
                    continue;
                }

                float side = i % 2 == 0 ? 1f : -1f;
                float lift = 0.07f + i * 0.018f;
                accent.transform.localPosition = basePositions[i] + new Vector3(-0.18f * eased, side * lift * pop, 0f);
                accent.transform.localScale = Vector3.Lerp(baseScales[i] * 0.55f, baseScales[i] * (1.08f + i * 0.06f), eased);
                SetFartCloudColor(accent, new Color(0.72f, 1f, 0.44f, Mathf.Lerp(0.62f, 0f, eased)));
            }
        }

        private void RestoreAccentRenderers(Vector3[] basePositions, Vector3[] baseScales)
        {
            if (fartAccentRenderers == null || basePositions == null || baseScales == null)
            {
                return;
            }

            for (int i = 0; i < fartAccentRenderers.Length; i++)
            {
                if (fartAccentRenderers[i] == null)
                {
                    continue;
                }

                fartAccentRenderers[i].transform.localPosition = basePositions[i];
                fartAccentRenderers[i].transform.localScale = baseScales[i];
            }
        }

        private static void SetRendererEnabled(Renderer renderer, bool enabled)
        {
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }

        private static void SetRendererEnabled(Renderer[] renderers, bool enabled)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                SetRendererEnabled(renderers[i], enabled);
            }
        }

        private void NotifyFuelChanged()
        {
            if (FuelChanged != null)
            {
                FuelChanged.Invoke(CurrentFuel, maxFuel);
            }
        }

        private Vector2 GetVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }

        private void SetVelocity(Vector2 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = velocity;
#else
            body.velocity = velocity;
#endif
        }
    }
}
