using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public sealed class ExpeditionFinishLine : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D finishTrigger;
        [SerializeField] private Transform pulseRoot;
        [SerializeField] private Renderer[] glowRenderers;
        [SerializeField] private float pulseSpeed = 2.2f;
        [SerializeField] private float pulseAmount = 0.055f;

        private Vector3 pulseBaseScale = Vector3.one;
        private bool reached;

        public bool IsConfigured
        {
            get
            {
                return finishTrigger != null && finishTrigger.isTrigger &&
                    pulseRoot != null && glowRenderers != null && glowRenderers.Length > 0;
            }
        }

        public float WorldX { get { return transform.position.x; } }

        private void Awake()
        {
            if (pulseRoot != null)
            {
                pulseBaseScale = pulseRoot.localScale;
            }
        }

        private void OnEnable()
        {
            reached = false;
            if (finishTrigger != null)
            {
                finishTrigger.enabled = true;
            }
        }

        private void Update()
        {
            if (pulseRoot == null)
            {
                return;
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
            pulseRoot.localScale = pulseBaseScale * pulse;
        }

        public void Configure(float worldX)
        {
            Vector3 position = transform.position;
            position.x = worldX;
            transform.position = position;
            reached = false;
            gameObject.SetActive(true);
            if (finishTrigger != null)
            {
                finishTrigger.enabled = true;
            }
        }

        public void MarkReached()
        {
            reached = true;
            if (finishTrigger != null)
            {
                finishTrigger.enabled = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (reached || other == null || other.GetComponentInParent<GorillaController>() == null)
            {
                return;
            }

            GassyGorillaGameManager manager = GassyGorillaGameManager.Instance;
            if (manager != null && manager.IsRunActive)
            {
                manager.ReachExpeditionFinish(this);
            }
        }
    }
}
