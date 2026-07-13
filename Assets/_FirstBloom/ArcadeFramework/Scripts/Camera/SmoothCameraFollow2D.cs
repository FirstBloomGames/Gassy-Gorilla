using UnityEngine;

namespace FirstBloom.ArcadeFramework.Camera
{
    public class SmoothCameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(4f, 0f, -10f);
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private bool clampY = true;
        [SerializeField] private float minY = -0.75f;
        [SerializeField] private float maxY = 2.75f;
        [SerializeField] private bool followY = true;
        [SerializeField] private Rigidbody2D velocitySource;
        [SerializeField] private float speedLookaheadX = 0.18f;
        [SerializeField] private float speedLookaheadY = 0.08f;
        [SerializeField] private float maxExtraLookahead = 1.1f;
        [SerializeField] private bool dynamicZoom = true;
        [SerializeField] private float baseOrthographicSize = 5.2f;
        [SerializeField] private float zoomOutAtSpeed = 0.42f;
        [SerializeField] private float maxZoomSpeed = 9f;
        [SerializeField] private float zoomSmoothTime = 0.24f;
        [SerializeField] private float actionLookaheadReturnTime = 0.22f;

        private Vector3 velocity;
        private Vector3 actionLookahead;
        private Vector3 actionLookaheadVelocity;
        private float actionLookaheadHoldUntil;
        private UnityEngine.Camera cameraComponent;
        private float zoomVelocity;
        private float shakeTimeRemaining;
        private float shakeDuration;
        private float shakeIntensity;
        private float followSmoothingMultiplier = 1f;

        public Transform Target
        {
            get { return target; }
            set { target = value; }
        }

        private void Awake()
        {
            cameraComponent = GetComponent<UnityEngine.Camera>();
            if (cameraComponent != null && baseOrthographicSize <= 0f)
            {
                baseOrthographicSize = cameraComponent.orthographicSize;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector2 targetVelocity = velocitySource != null ? GetVelocity(velocitySource) : Vector2.zero;
            Vector3 lookahead = new Vector3(
                Mathf.Clamp(targetVelocity.x * speedLookaheadX, 0f, maxExtraLookahead),
                Mathf.Clamp(targetVelocity.y * speedLookaheadY, -maxExtraLookahead * 0.4f, maxExtraLookahead * 0.4f),
                0f);

            if (Time.unscaledTime > actionLookaheadHoldUntil)
            {
                actionLookahead = Vector3.SmoothDamp(actionLookahead, Vector3.zero, ref actionLookaheadVelocity, actionLookaheadReturnTime, Mathf.Infinity, Time.unscaledDeltaTime);
            }

            Vector3 desired = target.position + offset + lookahead + actionLookahead;
            if (!followY)
            {
                desired.y = transform.position.y;
            }
            else if (clampY)
            {
                desired.y = Mathf.Clamp(desired.y, minY, maxY);
            }

            float effectiveSmoothTime = Mathf.Max(0.01f, smoothTime * followSmoothingMultiplier);
            Vector3 position = Vector3.SmoothDamp(transform.position, desired, ref velocity, effectiveSmoothTime);

            if (shakeTimeRemaining > 0f)
            {
                shakeTimeRemaining -= Time.unscaledDeltaTime;
                float fade = shakeDuration <= 0f ? 0f : Mathf.Clamp01(shakeTimeRemaining / shakeDuration);
                fade *= fade;
                Vector2 shake = Random.insideUnitCircle * shakeIntensity * fade;
                position.x += shake.x;
                position.y += shake.y;
                if (shakeTimeRemaining <= 0f)
                {
                    shakeIntensity = 0f;
                }
            }

            transform.position = position;

            if (dynamicZoom && cameraComponent != null && cameraComponent.orthographic)
            {
                float speed = Mathf.Clamp(targetVelocity.magnitude, 0f, maxZoomSpeed);
                float targetSize = baseOrthographicSize + Mathf.InverseLerp(0f, maxZoomSpeed, speed) * zoomOutAtSpeed;
                cameraComponent.orthographicSize = Mathf.SmoothDamp(cameraComponent.orthographicSize, targetSize, ref zoomVelocity, zoomSmoothTime);
            }
        }

        public void Shake(float intensity, float duration)
        {
            if (duration >= shakeTimeRemaining)
            {
                shakeDuration = Mathf.Max(0.01f, duration);
            }

            shakeIntensity = shakeTimeRemaining <= 0f ? intensity : Mathf.Max(shakeIntensity, intensity);
            shakeTimeRemaining = Mathf.Max(shakeTimeRemaining, duration);
        }

        public void AddActionLookahead(Vector2 amount, float holdDuration)
        {
            actionLookahead = new Vector3(amount.x, amount.y, 0f);
            actionLookaheadVelocity = Vector3.zero;
            actionLookaheadHoldUntil = Time.unscaledTime + Mathf.Max(0f, holdDuration);
        }

        public void SetVelocitySource(Rigidbody2D source)
        {
            velocitySource = source;
        }

        public void SetFollowSmoothingMultiplier(float multiplier)
        {
            followSmoothingMultiplier = Mathf.Max(0.25f, multiplier);
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = target.position + offset;
            if (!followY)
            {
                desired.y = transform.position.y;
            }
            else if (clampY)
            {
                desired.y = Mathf.Clamp(desired.y, minY, maxY);
            }

            transform.position = desired;
            velocity = Vector3.zero;
            actionLookahead = Vector3.zero;
            actionLookaheadVelocity = Vector3.zero;
            shakeTimeRemaining = 0f;
            shakeIntensity = 0f;

            if (dynamicZoom && cameraComponent != null && cameraComponent.orthographic)
            {
                cameraComponent.orthographicSize = baseOrthographicSize;
                zoomVelocity = 0f;
            }
        }

        private static Vector2 GetVelocity(Rigidbody2D body)
        {
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }
    }
}
