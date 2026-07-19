using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public sealed class GassyInteractionMarker : MonoBehaviour
    {
        [SerializeField] private GassyInteractionType interactionType;

        public GassyInteractionType InteractionType { get { return interactionType; } }
        public bool IsConfigured { get { return interactionType != GassyInteractionType.None; } }

        public void Configure(GassyInteractionType type)
        {
            interactionType = type;
        }
    }
}
