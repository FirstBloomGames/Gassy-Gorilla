using UnityEngine;

namespace FirstBloom.ArcadeFramework.Core
{
    public class ArcadeHazard : MonoBehaviour
    {
        [SerializeField] private string targetTag = "Player";

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryHit(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryHit(collision.gameObject);
        }

        private void TryHit(GameObject target)
        {
            if (!string.IsNullOrEmpty(targetTag) && !target.CompareTag(targetTag))
            {
                return;
            }

            target.SendMessage("OnArcadeHazardHit", this, SendMessageOptions.DontRequireReceiver);
        }
    }
}
