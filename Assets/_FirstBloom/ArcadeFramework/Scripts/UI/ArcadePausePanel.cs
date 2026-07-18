using UnityEngine;

namespace FirstBloom.ArcadeFramework.UI
{
    public sealed class ArcadePausePanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroupPanel panel;

        public bool IsConfigured { get { return panel != null; } }
        public bool IsVisible { get { return panel != null && panel.IsVisible; } }

        private void Awake()
        {
            if (panel == null)
            {
                panel = GetComponent<CanvasGroupPanel>();
            }
        }

        public void Show()
        {
            if (panel != null)
            {
                panel.Show();
            }
        }

        public void Hide()
        {
            if (panel != null)
            {
                panel.Hide();
            }
        }
    }
}
