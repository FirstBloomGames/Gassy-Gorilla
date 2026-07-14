using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class CrocodileAmbushHitbox : MonoBehaviour
    {
        [SerializeField] private CrocodileAmbushController controller;

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;

            if (controller == null)
            {
                controller = GetComponentInParent<CrocodileAmbushController>();
            }
        }

        public void Bind(CrocodileAmbushController owner)
        {
            controller = owner;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryBite(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryBite(other);
        }

        private void TryBite(Collider2D other)
        {
            if (controller == null || other == null)
            {
                return;
            }

            GorillaController gorilla = other.GetComponentInParent<GorillaController>();
            if (gorilla != null)
            {
                controller.TryBite(gorilla);
            }
        }
    }
}
