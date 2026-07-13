using UnityEngine;

namespace FirstBloom.ArcadeFramework.VFX
{
    public class AmbientSway2D : MonoBehaviour
    {
        [SerializeField] private float positionAmplitudeX = 0.05f;
        [SerializeField] private float positionAmplitudeY = 0.02f;
        [SerializeField] private float rotationAmplitudeDegrees = 2f;
        [SerializeField] private float scaleAmplitude = 0.015f;
        [SerializeField] private float speed = 1f;
        [SerializeField] private float phase;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private Vector3 baseLocalScale;

        private void Awake()
        {
            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;
            baseLocalScale = transform.localScale;
        }

        private void Update()
        {
            float wave = Mathf.Sin(Time.time * speed + phase);
            float waveOffset = Mathf.Sin(Time.time * speed * 0.73f + phase + 1.9f);

            transform.localPosition = baseLocalPosition + new Vector3(wave * positionAmplitudeX, waveOffset * positionAmplitudeY, 0f);
            transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, wave * rotationAmplitudeDegrees);
            float scale = 1f + waveOffset * scaleAmplitude;
            transform.localScale = baseLocalScale * scale;
        }
    }
}
