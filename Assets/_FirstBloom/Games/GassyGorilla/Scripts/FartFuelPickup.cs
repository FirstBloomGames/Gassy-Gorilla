using FirstBloom.ArcadeFramework.Audio;
using FirstBloom.ArcadeFramework.Spawning;
using System.Collections;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [RequireComponent(typeof(Collider2D))]
    public class FartFuelPickup : MonoBehaviour, IArcadePoolable
    {
        private const float PickupCadenceResetDelay = 0.45f;
        private static readonly float[] PickupCadencePitchScales =
        {
            1f,
            1.02930f,
            1.05946f,
            1.09051f,
            1.12246f
        };

        private static float lastPickupAudioTime = float.NegativeInfinity;
        private static int pickupCadenceStep;

        [SerializeField] private FoodPickupType pickupType = FoodPickupType.Bean;
        [SerializeField] private float refillAmount = 20f;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private float bobHeight = 0.12f;
        [SerializeField] private float bobSpeed = 4f;
        [SerializeField] private float spinDegreesPerSecond = 35f;
        [SerializeField] private float collectDuration = 0.18f;
        [SerializeField] private float collectArcHeight = 0.45f;
        [SerializeField] private float attractionRadius = 1.18f;
        [SerializeField] private float attractionSpeed = 5.8f;
        [SerializeField] private float attractionRampSpeed = 7.5f;
        [SerializeField] private ParticleSystem collectSparkle;

        private Vector3 startPosition;
        private Vector3 startScale;
        private bool collected;
        private Collider2D pickupCollider;
        private Transform attractionTarget;
        private float attractionWeight;
        private Renderer[] renderers;
        private Color[] baseRendererColors;
        private MaterialPropertyBlock[] rendererBlocks;
        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;
        private Vector3 initialLocalScale;
        private bool hasSpawnPose;

        private void Awake()
        {
            pickupCollider = GetComponent<Collider2D>();
            pickupCollider.isTrigger = true;
            renderers = GetComponentsInChildren<Renderer>();
            baseRendererColors = new Color[renderers.Length];
            rendererBlocks = new MaterialPropertyBlock[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                baseRendererColors[i] = ReadRendererColor(renderers[i]);
                rendererBlocks[i] = new MaterialPropertyBlock();
            }
        }

        private void Start()
        {
            CaptureSpawnPose();
            ResetForSpawn();
        }

        private void Update()
        {
            if (collected)
            {
                return;
            }

            Vector3 bobbedPosition = startPosition;
            bobbedPosition.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;

            if (attractionTarget != null && attractionRadius > 0f)
            {
                float distance = Vector2.Distance(transform.position, attractionTarget.position);
                float targetWeight = distance <= attractionRadius ? 1f : 0f;
                attractionWeight = Mathf.MoveTowards(attractionWeight, targetWeight, attractionRampSpeed * Time.deltaTime);
            }

            if (attractionWeight > 0f && attractionTarget != null)
            {
                Vector3 attractPosition = attractionTarget.position + Vector3.up * 0.18f;
                Vector3 pulledPosition = Vector3.MoveTowards(transform.position, attractPosition, attractionSpeed * attractionWeight * Time.deltaTime);
                transform.position = Vector3.Lerp(bobbedPosition, pulledPosition, attractionWeight);
            }
            else
            {
                transform.position = bobbedPosition;
            }

            transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected)
            {
                return;
            }

            GorillaController gorilla = other.GetComponent<GorillaController>();
            if (gorilla == null)
            {
                return;
            }

            collected = true;
            if (pickupCollider != null)
            {
                pickupCollider.enabled = false;
            }

            if (collectSparkle != null)
            {
                collectSparkle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                collectSparkle.Play();
            }

            gorilla.RefillFuel(refillAmount, true);
            GassyRunEvents.RaiseFoodCollected(pickupType);

            if (ArcadeAudioManager.Instance != null)
            {
                if (pickupSound != null)
                {
                    ArcadeAudioManager.Instance.PlaySfx(pickupSound);
                }
                else
                {
                    ArcadeAudioManager.Instance.PlaySfx(
                        ArcadeSfxType.Pickup,
                        1f,
                        GetPickupCadencePitchScale());
                }
            }

            StartCoroutine(CollectRoutine(gorilla.transform));
        }

        public void Configure(FoodPickupType type, float refill)
        {
            pickupType = type;
            refillAmount = refill;
        }

        private static float GetPickupCadencePitchScale()
        {
            float now = Time.unscaledTime;
            pickupCadenceStep = now - lastPickupAudioTime > PickupCadenceResetDelay
                ? 0
                : Mathf.Min(pickupCadenceStep + 1, PickupCadencePitchScales.Length - 1);
            lastPickupAudioTime = now;
            return PickupCadencePitchScales[pickupCadenceStep];
        }

        private IEnumerator CollectRoutine(Transform target)
        {
            Vector3 from = transform.position;
            Vector3 to = target != null ? target.position + Vector3.up * 0.25f : from + Vector3.up * 0.2f;
            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, collectDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector3 position = Vector3.Lerp(from, to, eased);
                position.y += Mathf.Sin(t * Mathf.PI) * collectArcHeight;
                transform.position = position;
                transform.localScale = Vector3.Lerp(startScale * 1.12f, Vector3.zero, eased);
                SetRendererAlpha(1f - eased);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        public void OnSpawnedFromPool()
        {
            CaptureSpawnPose();
            ResetForSpawn();
        }

        public void OnDespawnedToPool()
        {
            StopAllCoroutines();
            if (collectSparkle != null)
            {
                collectSparkle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void CaptureSpawnPose()
        {
            if (hasSpawnPose)
            {
                return;
            }

            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
            initialLocalScale = transform.localScale;
            hasSpawnPose = true;
        }

        private void ResetForSpawn()
        {
            StopAllCoroutines();
            collected = false;
            attractionWeight = 0f;
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
            transform.localScale = initialLocalScale;
            startPosition = transform.position;
            startScale = initialLocalScale;

            if (pickupCollider != null)
            {
                pickupCollider.enabled = true;
            }

            if (collectSparkle != null)
            {
                collectSparkle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            SetRendererAlpha(1f);
            if (attractionTarget == null)
            {
                GorillaController gorilla = FindAnyObjectByType<GorillaController>();
                if (gorilla != null)
                {
                    attractionTarget = gorilla.transform;
                }
            }
        }

        private void SetRendererAlpha(float alpha)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                Color color = i < baseRendererColors.Length ? baseRendererColors[i] : Color.white;
                color.a = alpha;
                renderers[i].GetPropertyBlock(rendererBlocks[i]);
                rendererBlocks[i].SetColor("_BaseColor", color);
                rendererBlocks[i].SetColor("_Color", color);
                renderers[i].SetPropertyBlock(rendererBlocks[i]);
            }
        }

        private Color ReadRendererColor(Renderer renderer)
        {
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return Color.white;
            }

            if (renderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                return renderer.sharedMaterial.GetColor("_BaseColor");
            }

            if (renderer.sharedMaterial.HasProperty("_Color"))
            {
                return renderer.sharedMaterial.GetColor("_Color");
            }

            return Color.white;
        }
    }
}
