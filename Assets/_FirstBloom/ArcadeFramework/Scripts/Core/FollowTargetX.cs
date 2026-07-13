using UnityEngine;

namespace FirstBloom.ArcadeFramework.Core
{
    public class FollowTargetX : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset;
        [SerializeField] private bool followY;

        private float fixedZ;

        public Transform Target
        {
            get { return target; }
            set { target = value; }
        }

        private void Awake()
        {
            fixedZ = transform.position.z;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 position = transform.position;
            position.x = target.position.x + offset.x;

            if (followY)
            {
                position.y = target.position.y + offset.y;
            }

            position.z = fixedZ + offset.z;
            transform.position = position;
        }
    }
}
