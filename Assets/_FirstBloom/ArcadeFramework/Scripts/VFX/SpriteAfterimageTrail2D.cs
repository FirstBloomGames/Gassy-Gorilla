using System.Collections;
using UnityEngine;

namespace FirstBloom.ArcadeFramework.VFX
{
    public class SpriteAfterimageTrail2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer source;
        [SerializeField] private Color startColor = new Color(0.65f, 1f, 0.62f, 0.34f);
        [SerializeField] private float fadeDuration = 0.24f;
        [SerializeField] private float spawnInterval = 0.035f;
        [SerializeField] private int sortingOrderOffset = -1;

        private Coroutine burstRoutine;

        public void Burst(float duration)
        {
            if (source == null)
            {
                source = GetComponentInChildren<SpriteRenderer>();
            }

            if (source == null)
            {
                return;
            }

            if (burstRoutine != null)
            {
                StopCoroutine(burstRoutine);
            }

            burstRoutine = StartCoroutine(BurstRoutine(Mathf.Max(0.02f, duration)));
        }

        private IEnumerator BurstRoutine(float duration)
        {
            float elapsed = 0f;
            float nextSpawn = 0f;

            while (elapsed < duration)
            {
                if (elapsed >= nextSpawn)
                {
                    SpawnAfterimage();
                    nextSpawn += Mathf.Max(0.01f, spawnInterval);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            burstRoutine = null;
        }

        private void SpawnAfterimage()
        {
            GameObject ghost = new GameObject("Sprite_Afterimage");
            ghost.transform.position = source.transform.position;
            ghost.transform.rotation = source.transform.rotation;
            ghost.transform.localScale = source.transform.lossyScale;

            SpriteRenderer renderer = ghost.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.flipX = source.flipX;
            renderer.flipY = source.flipY;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder + sortingOrderOffset;
            renderer.color = startColor;

            StartCoroutine(FadeAndDestroy(renderer));
        }

        private IEnumerator FadeAndDestroy(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                yield break;
            }

            float duration = Mathf.Max(0.02f, fadeDuration);
            float elapsed = 0f;
            Color color = startColor;

            while (elapsed < duration && renderer != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                color.a = Mathf.Lerp(startColor.a, 0f, t);
                renderer.color = color;
                yield return null;
            }

            if (renderer != null)
            {
                Destroy(renderer.gameObject);
            }
        }
    }
}
