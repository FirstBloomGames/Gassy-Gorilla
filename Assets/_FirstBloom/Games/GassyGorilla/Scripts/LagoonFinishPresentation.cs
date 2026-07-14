using System.Collections;
using FirstBloom.ArcadeFramework.Audio;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public sealed class LagoonFinishPresentation : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Reflection")]
        [SerializeField] private Transform reflectionRoot;
        [SerializeField] private Renderer[] reflectionRenderers;
        [SerializeField] private Rigidbody2D velocitySource;
        [SerializeField] private float waterSurfaceY = -1.61f;
        [SerializeField] private float reflectionWorldZ = -0.32f;
        [SerializeField] private float reflectionDepth = 0.09f;
        [SerializeField] private float reflectionVerticalCompression = 0.14f;
        [SerializeField] private float reflectionFullAlphaHeight = 0.75f;
        [SerializeField] private float reflectionFadeHeight = 4.7f;
        [SerializeField] private float reflectionImpactFadeDuration = 0.22f;
        [SerializeField] private Color reflectionColor = new Color(0.32f, 0.62f, 0.52f, 0.58f);

        [Header("Water Impact")]
        [SerializeField] private Transform impactRoot;
        [SerializeField] private ParticleSystem[] impactParticles;
        [SerializeField] private Transform[] rippleTransforms;
        [SerializeField] private Renderer[] rippleRenderers;
        [SerializeField] private float impactWorldZ = -0.2f;
        [SerializeField] private float rippleDuration = 0.72f;
        [SerializeField] private float rippleStagger = 0.1f;
        [SerializeField] private float rippleExpansion = 5.2f;
        [SerializeField] private Color rippleColor = new Color(0.68f, 1f, 0.9f, 0.58f);

        [Header("Crocodile Finish")]
        [SerializeField] private GameObject crocodileRoot;
        [SerializeField] private Animator crocodileAnimator;
        [SerializeField] private Renderer[] playerVisualRenderers;
        [SerializeField] private float crocodileChompDelay = 0.46f;
        [SerializeField] private float crocodileSettleDelay = 0.72f;
        [SerializeField] private float chompVolume = 0.92f;

        private MaterialPropertyBlock propertyBlock;
        private Vector3 reflectionBaseScale = Vector3.one;
        private Vector3[] rippleBaseScales;
        private Coroutine rippleRoutine;
        private Coroutine crocodileRoutine;
        private float reflectionImpactAlpha = 1f;
        private bool impactPlayed;

        public bool HasPlayedImpact
        {
            get { return impactPlayed; }
        }

        public float WaterSurfaceY
        {
            get { return waterSurfaceY; }
        }

        public bool HasCrocodileFinish
        {
            get { return crocodileRoot != null && crocodileAnimator != null; }
        }

        public GameObject CrocodileRoot
        {
            get { return crocodileRoot; }
        }

        public Animator CrocodileAnimator
        {
            get { return crocodileAnimator; }
        }

        private void Awake()
        {
            if (velocitySource == null)
            {
                velocitySource = GetComponent<Rigidbody2D>();
            }

            propertyBlock = new MaterialPropertyBlock();
            if (reflectionRoot != null)
            {
                reflectionBaseScale = reflectionRoot.localScale;
            }

            int rippleCount = rippleTransforms != null ? rippleTransforms.Length : 0;
            rippleBaseScales = new Vector3[rippleCount];
            for (int i = 0; i < rippleCount; i++)
            {
                if (rippleTransforms[i] != null)
                {
                    rippleBaseScales[i] = rippleTransforms[i].localScale;
                }
            }

            HideRipples();
        }

        private void OnEnable()
        {
            impactPlayed = false;
            reflectionImpactAlpha = 1f;
            SetPlayerVisualsVisible(true);
            if (crocodileRoot != null)
            {
                crocodileRoot.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (rippleRoutine != null)
            {
                StopCoroutine(rippleRoutine);
                rippleRoutine = null;
            }

            if (crocodileRoutine != null)
            {
                StopCoroutine(crocodileRoutine);
                crocodileRoutine = null;
            }

            SetPlayerVisualsVisible(true);
            if (crocodileRoot != null)
            {
                crocodileRoot.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (reflectionRoot == null)
            {
                return;
            }

            Vector2 velocity = velocitySource != null ? GetVelocity(velocitySource) : Vector2.zero;
            float height = Mathf.Max(0f, transform.position.y - waterSurfaceY);
            float heightFade = 1f - Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(reflectionFullAlphaHeight, reflectionFadeHeight, height));

            if (impactPlayed)
            {
                float fadeSpeed = reflectionImpactFadeDuration <= 0f ? 1000f : 1f / reflectionImpactFadeDuration;
                reflectionImpactAlpha = Mathf.MoveTowards(reflectionImpactAlpha, 0f, fadeSpeed * Time.unscaledDeltaTime);
            }

            float wave = Mathf.Sin(Time.unscaledTime * 2.35f + transform.position.x * 0.22f);
            float horizontalStretch = 1f + Mathf.Clamp(Mathf.Abs(velocity.x) * 0.012f, 0f, 0.13f) + wave * 0.035f;
            float verticalRipple = 1f - Mathf.Clamp(Mathf.Abs(velocity.y) * 0.008f, 0f, 0.08f) - wave * 0.025f;
            float reflectedY = waterSurfaceY - reflectionDepth - height * reflectionVerticalCompression;
            float reflectedX = transform.position.x + velocity.x * 0.012f + wave * 0.045f;

            reflectionRoot.position = new Vector3(reflectedX, reflectedY, reflectionWorldZ);
            reflectionRoot.rotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(-velocity.y * 0.65f, -5f, 5f) + wave * 1.2f);
            reflectionRoot.localScale = new Vector3(
                reflectionBaseScale.x * horizontalStretch,
                reflectionBaseScale.y * verticalRipple,
                reflectionBaseScale.z);

            SetRendererGroupColor(reflectionRenderers, reflectionColor, heightFade * reflectionImpactAlpha);
        }

        public bool PlayWaterImpact(Vector3 playerPosition)
        {
            if (impactPlayed)
            {
                return false;
            }

            impactPlayed = true;
            if (impactRoot != null)
            {
                impactRoot.SetParent(null, true);
                impactRoot.position = new Vector3(playerPosition.x, waterSurfaceY, impactWorldZ);
                impactRoot.rotation = Quaternion.identity;
                impactRoot.localScale = Vector3.one;
            }

            if (impactParticles != null)
            {
                for (int i = 0; i < impactParticles.Length; i++)
                {
                    ParticleSystem particles = impactParticles[i];
                    if (particles == null)
                    {
                        continue;
                    }

                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particles.Play(true);
                }
            }

            if (rippleRoutine != null)
            {
                StopCoroutine(rippleRoutine);
            }

            rippleRoutine = StartCoroutine(AnimateRipples());
            if (crocodileRoutine != null)
            {
                StopCoroutine(crocodileRoutine);
            }

            if (HasCrocodileFinish)
            {
                crocodileRoutine = StartCoroutine(AnimateCrocodileFinish());
            }

            return true;
        }

        private IEnumerator AnimateCrocodileFinish()
        {
            if (crocodileRoot != null)
            {
                crocodileRoot.SetActive(true);
            }

            if (crocodileAnimator != null)
            {
                crocodileAnimator.Rebind();
                crocodileAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                crocodileAnimator.Play("Lunge_Snap", 0, 0f);
                crocodileAnimator.Update(0f);
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, crocodileChompDelay));
            SetPlayerVisualsVisible(false);
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.Chomp, chompVolume);
            }

            float settleWait = Mathf.Max(0f, crocodileSettleDelay - crocodileChompDelay);
            if (settleWait > 0f)
            {
                yield return new WaitForSecondsRealtime(settleWait);
            }

            if (crocodileAnimator != null)
            {
                crocodileAnimator.Play("Settle_Submerge", 0, 0f);
            }

            crocodileRoutine = null;
        }

        private IEnumerator AnimateRipples()
        {
            int count = Mathf.Min(
                rippleTransforms != null ? rippleTransforms.Length : 0,
                rippleRenderers != null ? rippleRenderers.Length : 0);
            float duration = Mathf.Max(0.05f, rippleDuration);
            float elapsed = 0f;
            float totalDuration = duration + Mathf.Max(0f, rippleStagger) * Mathf.Max(0, count - 1);

            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < count; i++)
                {
                    Transform ripple = rippleTransforms[i];
                    Renderer rippleRenderer = rippleRenderers[i];
                    if (ripple == null || rippleRenderer == null)
                    {
                        continue;
                    }

                    float localTime = elapsed - i * rippleStagger;
                    if (localTime < 0f || localTime > duration)
                    {
                        rippleRenderer.enabled = false;
                        continue;
                    }

                    float t = Mathf.Clamp01(localTime / duration);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    Vector3 baseScale = i < rippleBaseScales.Length ? rippleBaseScales[i] : Vector3.one;
                    float width = Mathf.Lerp(0.45f, rippleExpansion * (1f + i * 0.12f), eased);
                    float thickness = Mathf.Lerp(0.72f, 1.3f, eased);
                    ripple.localScale = new Vector3(baseScale.x * width, baseScale.y * thickness, baseScale.z);
                    rippleRenderer.enabled = true;

                    float alpha = Mathf.Sin(t * Mathf.PI) * Mathf.Pow(1f - t, 0.45f);
                    SetRendererColor(rippleRenderer, rippleColor, alpha);
                }

                yield return null;
            }

            HideRipples();
            rippleRoutine = null;
        }

        private void HideRipples()
        {
            if (rippleRenderers == null)
            {
                return;
            }

            for (int i = 0; i < rippleRenderers.Length; i++)
            {
                if (rippleRenderers[i] != null)
                {
                    rippleRenderers[i].enabled = false;
                }
            }
        }

        public void SetPlayerVisualsVisible(bool visible)
        {
            if (playerVisualRenderers == null)
            {
                return;
            }

            for (int i = 0; i < playerVisualRenderers.Length; i++)
            {
                if (playerVisualRenderers[i] != null)
                {
                    playerVisualRenderers[i].enabled = visible;
                }
            }
        }

        private void SetRendererGroupColor(Renderer[] renderers, Color color, float alphaScale)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                SetRendererColor(renderers[i], color, alphaScale);
            }
        }

        private void SetRendererColor(Renderer targetRenderer, Color color, float alphaScale)
        {
            if (targetRenderer == null)
            {
                return;
            }

            float alpha = color.a * Mathf.Clamp01(alphaScale);
            targetRenderer.enabled = alpha > 0.002f;
            color.a = alpha;
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(ColorId, color);
            propertyBlock.SetColor(BaseColorId, color);
            targetRenderer.SetPropertyBlock(propertyBlock);
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
