using UnityEngine;

namespace FirstBloom.ArcadeFramework.Spawning
{
    public class DestroyBehindTarget : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float destroyDistanceBehind = 20f;

        private void Start()
        {
            if (target == null && UnityEngine.Camera.main != null)
            {
                target = UnityEngine.Camera.main.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (transform.position.x < target.position.x - destroyDistanceBehind)
            {
                Destroy(gameObject);
            }
        }
    }
}
