using UnityEngine;
using System.Collections;

namespace FirstBloom.Games.GassyGorilla
{
    [RequireComponent(typeof(Collider2D))]
    public class VineSwingTrigger : MonoBehaviour
    {
        [SerializeField] private Transform grabPoint;
        [SerializeField] private Transform pivotPoint;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Renderer[] glowRenderers;
        [SerializeField] private VineSwingAnimator swingAnimator;
        [SerializeField] private float regrabCooldown = 1.1f;
        [SerializeField] private float catchRadius = 1.45f;
        [SerializeField] private float grabPunchDuration = 0.18f;
        [SerializeField] private float grabPunchScale = 1.12f;
        [SerializeField] private Color readyColor = new Color(0.45f, 1f, 0.25f, 0.58f);
        [SerializeField] private Color usedColor = new Color(0.22f, 0.5f, 0.2f, 0.28f);

        private float nextAvailableTime;
        private Vector3 baseVisualScale;
        private Coroutine punchRoutine;
        private MaterialPropertyBlock[] glowBlocks;

        public Transform GrabPoint { get { return grabPoint != null ? grabPoint : transform; } }
        public Transform PivotPoint { get { return pivotPoint != null ? pivotPoint : transform; } }

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (swingAnimator == null)
            {
                swingAnimator = GetComponent<VineSwingAnimator>();
            }

            baseVisualScale = visualRoot.localScale;
            if (glowRenderers != null)
            {
                glowBlocks = new MaterialPropertyBlock[glowRenderers.Length];
                for (int i = 0; i < glowBlocks.Length; i++)
                {
                    glowBlocks[i] = new MaterialPropertyBlock();
                }
            }

            SetGlowColor(readyColor);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCatch(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryCatch(other);
        }

        private void TryCatch(Collider2D other)
        {
            if (Time.time < nextAvailableTime)
            {
                return;
            }

            GorillaController gorilla = other.GetComponent<GorillaController>();
            if (gorilla == null)
            {
                return;
            }

            if (catchRadius > 0f)
            {
                float distance = Vector2.Distance(gorilla.transform.position, GrabPoint.position);
                if (distance > catchRadius)
                {
                    return;
                }
            }

            gorilla.TryAttachToVine(this);
        }

        public void NotifyGrabbed()
        {
            if (swingAnimator != null)
            {
                swingAnimator.SetOccupied(true);
            }

            SetGlowColor(Color.white);
            PlayPunch(1f, grabPunchScale);
        }

        public void NotifyReleased()
        {
            nextAvailableTime = Time.time + regrabCooldown;
            if (swingAnimator != null)
            {
                swingAnimator.SetOccupied(false);
            }

            SetGlowColor(usedColor);
            PlayPunch(grabPunchScale, 0.96f);
        }

        public void DriveOccupiedSwing(float angleDegrees)
        {
            if (swingAnimator != null)
            {
                swingAnimator.DriveOccupiedSwing(angleDegrees);
            }
        }

        private void SetGlowColor(Color color)
        {
            if (glowRenderers == null)
            {
                return;
            }

            for (int i = 0; i < glowRenderers.Length; i++)
            {
                if (glowRenderers[i] != null)
                {
                    glowRenderers[i].GetPropertyBlock(glowBlocks[i]);
                    glowBlocks[i].SetColor("_BaseColor", color);
                    glowBlocks[i].SetColor("_Color", color);
                    glowRenderers[i].SetPropertyBlock(glowBlocks[i]);
                }
            }
        }

        private void PlayPunch(float fromScale, float toScale)
        {
            if (visualRoot == null)
            {
                return;
            }

            if (punchRoutine != null)
            {
                StopCoroutine(punchRoutine);
            }

            punchRoutine = StartCoroutine(PunchRoutine(fromScale, toScale));
        }

        private IEnumerator PunchRoutine(float fromScale, float toScale)
        {
            float duration = Mathf.Max(0.03f, grabPunchDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI);
                float scale = Mathf.Lerp(fromScale, 1f, t) + wave * (toScale - 1f);
                visualRoot.localScale = baseVisualScale * scale;
                yield return null;
            }

            visualRoot.localScale = baseVisualScale;
            punchRoutine = null;
        }
    }
}
