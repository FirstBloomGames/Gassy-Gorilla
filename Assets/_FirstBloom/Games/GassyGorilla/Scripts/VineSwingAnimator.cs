using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public class VineSwingAnimator : MonoBehaviour
    {
        [SerializeField] private Transform vineVisual;
        [SerializeField] private Renderer[] glowRenderers;
        [SerializeField] private float swayDegrees = 5.5f;
        [SerializeField] private float swaySpeed = 0.78f;
        [SerializeField] private float glowPulseSpeed = 5f;
        [SerializeField] private float occupiedFollowTime = 0.055f;
        [SerializeField] private float releaseReturnTime = 0.32f;
        [SerializeField] private float maxOccupiedDegrees = 35f;
        [SerializeField] private Color launchReadyColor = new Color(1f, 0.88f, 0.25f, 0.82f);

        private Color[] baseGlowColors;
        private MaterialPropertyBlock[] glowBlocks;
        private bool occupied;
        private float occupiedTargetDegrees;
        private float currentDegrees;
        private float angularVelocity;
        private float releasePower;

        public bool HasAmbientSway
        {
            get { return vineVisual != null && swayDegrees > 0.1f && swaySpeed > 0.01f; }
        }

        private void Awake()
        {
            if (vineVisual == null)
            {
                vineVisual = transform;
            }

            currentDegrees = NormalizeAngle(vineVisual.localEulerAngles.z);

            if (glowRenderers != null)
            {
                baseGlowColors = new Color[glowRenderers.Length];
                glowBlocks = new MaterialPropertyBlock[glowRenderers.Length];
                for (int i = 0; i < glowRenderers.Length; i++)
                {
                    baseGlowColors[i] = ReadRendererColor(glowRenderers[i]);
                    glowBlocks[i] = new MaterialPropertyBlock();
                }
            }
        }

        private void Update()
        {
            if (vineVisual != null)
            {
                if (occupied)
                {
                    currentDegrees = occupiedTargetDegrees;
                    angularVelocity = 0f;
                }
                else
                {
                    float ambientSway = Mathf.Sin(Time.time * swaySpeed + transform.position.x) * swayDegrees;
                    currentDegrees = Mathf.SmoothDampAngle(currentDegrees, ambientSway, ref angularVelocity, releaseReturnTime, 360f, Time.deltaTime);
                }

                vineVisual.localRotation = Quaternion.Euler(0f, 0f, currentDegrees);
            }

            if (glowRenderers == null || baseGlowColors == null)
            {
                return;
            }

            float pulse = 0.75f + Mathf.Sin(Time.time * glowPulseSpeed) * 0.25f;
            for (int i = 0; i < glowRenderers.Length; i++)
            {
                if (glowRenderers[i] != null)
                {
                    Color baseColor = baseGlowColors[i];
                    Color color = occupied
                        ? Color.Lerp(baseColor, launchReadyColor, releasePower)
                        : baseColor;
                    color.a *= pulse;
                    SetRendererColor(i, color);
                }
            }
        }

        public void SetOccupied(bool value)
        {
            occupied = value;
            if (occupied)
            {
                occupiedTargetDegrees = currentDegrees;
                angularVelocity = 0f;
            }
            else
            {
                releasePower = 0f;
            }
        }

        public void DriveOccupiedSwing(float angleDegrees)
        {
            occupied = true;
            occupiedTargetDegrees = Mathf.Clamp(angleDegrees, -maxOccupiedDegrees, maxOccupiedDegrees);
            currentDegrees = occupiedTargetDegrees;
            angularVelocity = 0f;

            if (vineVisual != null)
            {
                vineVisual.localRotation = Quaternion.Euler(0f, 0f, currentDegrees);
            }
        }

        public void SetReleasePower(float power)
        {
            releasePower = Mathf.Clamp01(power);
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

        private void SetRendererColor(int index, Color color)
        {
            if (glowRenderers == null || glowBlocks == null || index < 0 || index >= glowRenderers.Length || glowRenderers[index] == null)
            {
                return;
            }

            glowRenderers[index].GetPropertyBlock(glowBlocks[index]);
            glowBlocks[index].SetColor("_BaseColor", color);
            glowBlocks[index].SetColor("_Color", color);
            glowRenderers[index].SetPropertyBlock(glowBlocks[index]);
        }

        private static float NormalizeAngle(float degrees)
        {
            return degrees > 180f ? degrees - 360f : degrees;
        }
    }
}
