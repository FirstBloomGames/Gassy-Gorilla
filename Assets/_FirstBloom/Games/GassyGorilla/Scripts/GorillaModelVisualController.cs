using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public class GorillaModelVisualController : MonoBehaviour
    {
        private enum TemporaryPose
        {
            None,
            Boost,
            Release,
            Failed
        }

        [Header("References")]
        [SerializeField] private GorillaController gorilla;
        [SerializeField] private GameObject modelRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody2D velocitySource;
        [SerializeField] private Collider2D bodyCollider;

        [Header("Animator States")]
        [SerializeField] private string idleState = "Idle";
        [SerializeField] private string cruiseState = "Run";
        [SerializeField] private string boostState = "Boost";
        [SerializeField] private string swingState = "Swing";
        [SerializeField] private string releaseState = "VineRelease";
        [SerializeField] private float crossFadeDuration = 0.07f;

        [Header("Animator Speeds")]
        [SerializeField] private float idleAnimationSpeed = 0.95f;
        [SerializeField] private float cruiseAnimationSpeed = 1.08f;
        [SerializeField] private float boostAnimationSpeed = 1.55f;
        [SerializeField] private float swingAnimationSpeed = 0.82f;
        [SerializeField] private float releaseAnimationSpeed = 1.85f;
        [SerializeField] private float boostStartTime = 0.1f;
        [SerializeField] private float swingStartTime = 0.38f;
        [SerializeField] private float releaseStartTime = 0.2f;

        [Header("Vine Grip Lock")]
        [SerializeField] private bool lockSwingPose = true;
        [SerializeField, Range(0f, 1f)] private float swingGripPoseNormalizedTime = 0.38f;
        [SerializeField] private string leftHandBoneName = "LeftHand";
        [SerializeField] private string rightHandBoneName = "RightHand";
        [SerializeField] private Vector2 gripTargetOffset;

        [Header("Animation Polish")]
        [SerializeField] private float boostPoseHold = 0.2f;
        [SerializeField] private float releasePoseHold = 0.3f;
        [SerializeField] private float failedPoseHold = 0.1f;
        [SerializeField] private float scaleBlendSpeed = 14f;
        [SerializeField] private float leanBlendSpeed = 11f;
        [SerializeField] private float idleBreathSpeed = 2.2f;
        [SerializeField] private float idleBreathAmount = 0.022f;
        [SerializeField] private float velocityBankDegrees = 2.25f;
        [SerializeField] private float maxBankDegrees = 13f;
        [SerializeField] private float boostLeanDegrees = -10f;
        [SerializeField] private float swingLeanDegrees = 6f;
        [SerializeField] private float swingPosePulseDegrees = 1.25f;
        [SerializeField] private float releaseLeanDegrees = -13f;
        [SerializeField] private float travelYawDegrees = -42f;
        [SerializeField] private float boostYawDegrees = -50f;
        [SerializeField] private float swingYawDegrees = -58f;
        [SerializeField] private float releaseYawDegrees = -56f;
        [SerializeField] private float yawBlendSpeed = 9f;
        [SerializeField] private Vector3 boostScale = new Vector3(1.12f, 0.9f, 1.04f);
        [SerializeField] private Vector3 releaseScale = new Vector3(1.16f, 0.9f, 1.04f);
        [SerializeField] private Vector3 swingScale = new Vector3(0.98f, 1.08f, 1.02f);
        [SerializeField] private Vector3 failedScale = new Vector3(0.94f, 1.06f, 1f);

        private Renderer[] modelRenderers;
        private Vector3 modelBaseScale = Vector3.one;
        private Quaternion modelBaseRotation = Quaternion.identity;
        private Vector3 modelBaseLocalPosition;
        private Vector2 bodyColliderBaseOffset;
        private Vector3 currentScaleMultiplier = Vector3.one;
        private Vector3 targetScaleMultiplier = Vector3.one;
        private float currentLeanDegrees;
        private float targetLeanDegrees;
        private float currentYawDegrees;
        private float targetYawDegrees;
        private float temporaryPoseUntil;
        private TemporaryPose temporaryPose;
        private string currentState;
        private Transform leftHand;
        private Transform rightHand;
        private bool swingPoseLocked;
        private bool gripLockActive;

        private void Awake()
        {
            if (gorilla == null)
            {
                gorilla = GetComponentInParent<GorillaController>();
            }

            if (velocitySource == null && gorilla != null)
            {
                velocitySource = gorilla.GetComponent<Rigidbody2D>();
            }

            if (bodyCollider == null && gorilla != null)
            {
                bodyCollider = gorilla.GetComponent<Collider2D>();
            }

            if (animator == null && modelRoot != null)
            {
                animator = modelRoot.GetComponentInChildren<Animator>(true);
            }

            if (modelRoot != null)
            {
                modelRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);
                modelBaseScale = modelRoot.transform.localScale;
                modelBaseRotation = modelRoot.transform.localRotation;
                modelBaseLocalPosition = modelRoot.transform.localPosition;
                currentYawDegrees = travelYawDegrees;
                targetYawDegrees = travelYawDegrees;
                ResolveGripBones();
            }

            if (bodyCollider != null)
            {
                bodyColliderBaseOffset = bodyCollider.offset;
            }
        }

        private void OnEnable()
        {
            if (gorilla != null)
            {
                gorilla.Boosted += HandleBoosted;
                gorilla.VineGrabbed += HandleVineGrabbed;
                gorilla.VineReleased += HandleVineReleased;
                gorilla.BoostFailed += HandleBoostFailed;
            }
        }

        private void OnDisable()
        {
            if (gorilla != null)
            {
                gorilla.Boosted -= HandleBoosted;
                gorilla.VineGrabbed -= HandleVineGrabbed;
                gorilla.VineReleased -= HandleVineReleased;
                gorilla.BoostFailed -= HandleBoostFailed;
            }

            ResetGripVisual();
        }

        private void Start()
        {
            PlayState(idleState, true, idleAnimationSpeed);
        }

        private void Update()
        {
            if (!HasRenderableModel())
            {
                return;
            }

            if (gorilla != null && gorilla.IsSwinging)
            {
                temporaryPose = TemporaryPose.None;
                temporaryPoseUntil = 0f;
                if (lockSwingPose)
                {
                    PlayLockedPoseWithFallback(swingState, boostState, swingGripPoseNormalizedTime);
                }
                else
                {
                    PlayStateWithFallback(swingState, boostState, false, swingAnimationSpeed);
                }

                float swingPulse = Mathf.Sin(Time.time * 5.8f) * swingPosePulseDegrees;
                SetPoseTarget(swingScale, swingLeanDegrees + swingPulse, swingYawDegrees);
                ApplySmoothedPose();
                return;
            }

            if (Time.time < temporaryPoseUntil)
            {
                ApplyTemporaryPose();
                ApplySmoothedPose();
                return;
            }

            temporaryPose = TemporaryPose.None;
            ApplyCruisePose();
            ApplySmoothedPose();
        }

        private void LateUpdate()
        {
            if (!HasRenderableModel())
            {
                return;
            }

            if (gorilla != null && gorilla.IsSwinging)
            {
                AlignHandsToVineGrip();
            }
            else if (gripLockActive)
            {
                ReleaseGripPreservingWorldPose();
            }
        }

        private void HandleBoosted()
        {
            temporaryPose = TemporaryPose.Boost;
            temporaryPoseUntil = Time.time + boostPoseHold;
            PlayState(boostState, true, boostAnimationSpeed, boostStartTime);
            SetPoseTarget(boostScale, boostLeanDegrees, boostYawDegrees);
        }

        private void HandleVineGrabbed()
        {
            temporaryPose = TemporaryPose.None;
            temporaryPoseUntil = 0f;
            if (lockSwingPose)
            {
                PlayLockedPoseWithFallback(swingState, boostState, swingGripPoseNormalizedTime);
            }
            else
            {
                PlayStateWithFallback(swingState, boostState, true, swingAnimationSpeed, swingStartTime);
            }

            SetPoseTarget(swingScale, swingLeanDegrees, swingYawDegrees);
        }

        private void HandleVineReleased()
        {
            ReleaseGripPreservingWorldPose();
            temporaryPose = TemporaryPose.Release;
            temporaryPoseUntil = Time.time + releasePoseHold;
            if (!PlayState(releaseState, true, releaseAnimationSpeed, releaseStartTime))
            {
                PlayState(boostState, true, boostAnimationSpeed, boostStartTime);
            }

            SetPoseTarget(releaseScale, releaseLeanDegrees, releaseYawDegrees);
        }

        private void HandleBoostFailed()
        {
            temporaryPose = TemporaryPose.Failed;
            temporaryPoseUntil = Time.time + failedPoseHold;
            SetPoseTarget(failedScale, 3f, travelYawDegrees * 0.5f);
        }

        private void ApplyTemporaryPose()
        {
            if (temporaryPose == TemporaryPose.Boost)
            {
                SetPoseTarget(boostScale, boostLeanDegrees, boostYawDegrees);
            }
            else if (temporaryPose == TemporaryPose.Release)
            {
                SetPoseTarget(releaseScale, releaseLeanDegrees, releaseYawDegrees);
            }
            else if (temporaryPose == TemporaryPose.Failed)
            {
                SetPoseTarget(failedScale, 3f, travelYawDegrees * 0.5f);
            }
        }

        private void ApplyCruisePose()
        {
            float verticalVelocity = GetVerticalVelocity();
            bool playedCruise = PlayState(cruiseState, false, cruiseAnimationSpeed);
            if (!playedCruise)
            {
                PlayState(idleState, false, idleAnimationSpeed);
            }

            float breath = Mathf.Sin(Time.time * idleBreathSpeed) * idleBreathAmount;
            Vector3 breathScale = new Vector3(1f + breath * 0.3f, 1f + breath, 1f);
            float bank = Mathf.Clamp(-verticalVelocity * velocityBankDegrees, -maxBankDegrees, maxBankDegrees);
            SetPoseTarget(breathScale, bank, travelYawDegrees);
        }

        private bool PlayStateWithFallback(string primaryState, string fallbackState, bool forceRestart, float speed, float startTime = 0f)
        {
            if (PlayState(primaryState, forceRestart, speed, startTime))
            {
                return true;
            }

            return PlayState(fallbackState, forceRestart, speed, startTime);
        }

        private bool PlayLockedPoseWithFallback(string primaryState, string fallbackState, float normalizedTime)
        {
            if (PlayLockedPose(primaryState, normalizedTime))
            {
                return true;
            }

            return PlayLockedPose(fallbackState, normalizedTime);
        }

        private bool PlayLockedPose(string stateName, float normalizedTime)
        {
            if (string.IsNullOrEmpty(stateName) || animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            int stateHash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, stateHash))
            {
                return false;
            }

            if (!swingPoseLocked || currentState != stateName)
            {
                currentState = stateName;
                animator.speed = 0f;
                animator.Play(stateHash, 0, Mathf.Clamp01(normalizedTime));
                animator.Update(0f);
                swingPoseLocked = true;
            }
            else
            {
                animator.speed = 0f;
            }

            return true;
        }

        private bool PlayState(string stateName, bool forceRestart, float speed, float startTime = 0f)
        {
            if (string.IsNullOrEmpty(stateName) || animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            int stateHash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, stateHash))
            {
                return false;
            }

            if (!forceRestart && currentState == stateName)
            {
                swingPoseLocked = false;
                animator.speed = speed;
                return true;
            }

            currentState = stateName;
            swingPoseLocked = false;
            animator.speed = speed;
            animator.CrossFadeInFixedTime(stateHash, crossFadeDuration, 0, Mathf.Max(0f, startTime));
            return true;
        }

        private void AlignHandsToVineGrip()
        {
            if (modelRoot == null || gorilla == null || !ResolveGripBones())
            {
                return;
            }

            Vector3 handCenter = (leftHand.position + rightHand.position) * 0.5f;
            Vector3 gripPosition = gorilla.CurrentVineGripPosition;
            gripPosition.x += gripTargetOffset.x;
            gripPosition.y += gripTargetOffset.y;

            Vector3 correction = gripPosition - handCenter;
            correction.z = 0f;
            modelRoot.transform.position += correction;
            gripLockActive = true;
            UpdateColliderForGrip();
        }

        private bool ResolveGripBones()
        {
            if (leftHand == null)
            {
                leftHand = FindDescendant(modelRoot != null ? modelRoot.transform : null, leftHandBoneName);
            }

            if (rightHand == null)
            {
                rightHand = FindDescendant(modelRoot != null ? modelRoot.transform : null, rightHandBoneName);
            }

            return leftHand != null && rightHand != null;
        }

        private static Transform FindDescendant(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrEmpty(targetName))
            {
                return null;
            }

            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                string candidateName = descendants[i].name;
                if (string.Equals(candidateName, targetName, System.StringComparison.OrdinalIgnoreCase)
                    || candidateName.EndsWith(":" + targetName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return descendants[i];
                }
            }

            return null;
        }

        private void UpdateColliderForGrip()
        {
            if (bodyCollider == null || gorilla == null || modelRoot == null || modelRoot.transform.parent == null)
            {
                return;
            }

            Vector3 baselineWorldPosition = modelRoot.transform.parent.TransformPoint(modelBaseLocalPosition);
            Vector3 visualDelta = gorilla.transform.InverseTransformVector(modelRoot.transform.position - baselineWorldPosition);
            bodyCollider.offset = bodyColliderBaseOffset + new Vector2(visualDelta.x, visualDelta.y);
        }

        private void ReleaseGripPreservingWorldPose()
        {
            if (!gripLockActive || modelRoot == null)
            {
                return;
            }

            Vector3 lockedModelPosition = modelRoot.transform.position;
            modelRoot.transform.localPosition = modelBaseLocalPosition;
            Vector3 bodyCorrection = lockedModelPosition - modelRoot.transform.position;
            bodyCorrection.z = 0f;

            if (gorilla != null)
            {
                Vector3 bodyPosition = gorilla.transform.position + bodyCorrection;
                if (velocitySource != null)
                {
                    velocitySource.position = new Vector2(bodyPosition.x, bodyPosition.y);
                }

                gorilla.transform.position = bodyPosition;
            }

            if (bodyCollider != null)
            {
                bodyCollider.offset = bodyColliderBaseOffset;
            }

            gripLockActive = false;
        }

        private void ResetGripVisual()
        {
            if (modelRoot != null)
            {
                modelRoot.transform.localPosition = modelBaseLocalPosition;
            }

            if (bodyCollider != null)
            {
                bodyCollider.offset = bodyColliderBaseOffset;
            }

            gripLockActive = false;
            swingPoseLocked = false;
        }

        private void SetPoseTarget(Vector3 scaleMultiplier, float leanDegrees, float yawDegrees)
        {
            targetScaleMultiplier = scaleMultiplier;
            targetLeanDegrees = leanDegrees;
            targetYawDegrees = yawDegrees;
        }

        private void ApplySmoothedPose()
        {
            if (modelRoot == null)
            {
                return;
            }

            float scaleBlend = 1f - Mathf.Exp(-scaleBlendSpeed * Time.deltaTime);
            float leanBlend = 1f - Mathf.Exp(-leanBlendSpeed * Time.deltaTime);
            float yawBlend = 1f - Mathf.Exp(-yawBlendSpeed * Time.deltaTime);
            currentScaleMultiplier = Vector3.Lerp(currentScaleMultiplier, targetScaleMultiplier, scaleBlend);
            currentLeanDegrees = Mathf.Lerp(currentLeanDegrees, targetLeanDegrees, leanBlend);
            currentYawDegrees = Mathf.LerpAngle(currentYawDegrees, targetYawDegrees, yawBlend);

            modelRoot.transform.localScale = new Vector3(
                modelBaseScale.x * currentScaleMultiplier.x,
                modelBaseScale.y * currentScaleMultiplier.y,
                modelBaseScale.z * currentScaleMultiplier.z);
            modelRoot.transform.localRotation = modelBaseRotation * Quaternion.Euler(0f, currentYawDegrees, currentLeanDegrees);
        }

        private float GetVerticalVelocity()
        {
            if (velocitySource == null)
            {
                return 0f;
            }

#if UNITY_6000_0_OR_NEWER
            return velocitySource.linearVelocity.y;
#else
            return velocitySource.velocity.y;
#endif
        }

        private bool HasRenderableModel()
        {
            if (modelRoot == null || !modelRoot.activeInHierarchy)
            {
                return false;
            }

            if (modelRenderers == null || modelRenderers.Length == 0)
            {
                modelRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            }

            return modelRenderers != null && modelRenderers.Length > 0;
        }
    }
}
