using UnityEngine;

namespace FirstBloom.ArcadeFramework.VFX
{
    public class ParallaxBand2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float parallaxX = 0.35f;
        [SerializeField] private float tileWidth = 16f;
        [SerializeField] private Transform[] tiles;

        private float startTargetX;
        private float startX;

        private void Start()
        {
            if (target == null && UnityEngine.Camera.main != null)
            {
                target = UnityEngine.Camera.main.transform;
            }

            startTargetX = target != null ? target.position.x : 0f;
            startX = transform.position.x;

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
            transform.position = position;

            if (tileWidth <= 0f || tiles == null)
            {
                return;
            }

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null)
                {
                    continue;
                }

                float delta = target.position.x - tiles[i].position.x;
                if (delta > tileWidth)
                {
                    tiles[i].position += Vector3.right * tileWidth * tiles.Length;
                }
            }
        }
    }
}
