using UnityEngine;

namespace FirstBloom.ArcadeFramework.VFX
{
    public class ParallaxBand2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float parallaxX = 0.35f;
        [SerializeField] private float parallaxY;
        [SerializeField] private float tileWidth = 16f;
        [SerializeField] private Transform[] tiles;

        private float startTargetX;
        private float startTargetY;
        private float startX;
        private float startY;
        private UnityEngine.Camera targetCamera;

        private void Start()
        {
            if (target == null && UnityEngine.Camera.main != null)
            {
                target = UnityEngine.Camera.main.transform;
            }

            startTargetX = target != null ? target.position.x : 0f;
            startTargetY = target != null ? target.position.y : 0f;
            startX = transform.position.x;
            startY = transform.position.y;
            targetCamera = target != null ? target.GetComponent<UnityEngine.Camera>() : null;

            if (tiles == null || tiles.Length == 0)
            {
                tiles = new Transform[transform.childCount];
                for (int i = 0; i < transform.childCount; i++)
                {
                    tiles[i] = transform.GetChild(i);
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 position = transform.position;
            position.x = startX + (target.position.x - startTargetX) * parallaxX;
            position.y = startY + (target.position.y - startTargetY) * parallaxY;
            transform.position = position;

            if (tileWidth <= 0f || tiles == null)
            {
                return;
            }

            float halfViewWidth = targetCamera != null && targetCamera.orthographic
                ? targetCamera.orthographicSize * targetCamera.aspect
                : tileWidth * 0.75f;
            float viewportLeft = target.position.x - halfViewWidth;
            float cycleWidth = tileWidth * tiles.Length;

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null)
                {
                    continue;
                }

                int safety = 0;
                while (tiles[i].position.x + tileWidth * 0.5f < viewportLeft - 0.05f && safety < 2)
                {
                    tiles[i].position += Vector3.right * cycleWidth;
                    safety++;
                }
            }
        }
    }
}
