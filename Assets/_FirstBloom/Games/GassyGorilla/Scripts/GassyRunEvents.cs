using System;

namespace FirstBloom.Games.GassyGorilla
{
    public static class GassyRunEvents
    {
        public static event Action<FoodPickupType> FoodCollected;
        public static event Action CrocodileDodged;
        public static event Action<GassyInteractionType> InteractionStarted;
        public static event Action<GassyInteractionType> InteractionCompleted;

        public static void RaiseFoodCollected(FoodPickupType pickupType)
        {
            if (FoodCollected != null)
            {
                FoodCollected.Invoke(pickupType);
            }
        }

        public static void RaiseCrocodileDodged()
        {
            if (CrocodileDodged != null)
            {
                CrocodileDodged.Invoke();
            }
        }

        public static void RaiseInteractionStarted(GassyInteractionType interactionType)
        {
            if (interactionType != GassyInteractionType.None && InteractionStarted != null)
            {
                InteractionStarted.Invoke(interactionType);
            }
        }

        public static void RaiseInteractionCompleted(GassyInteractionType interactionType)
        {
            if (interactionType != GassyInteractionType.None && InteractionCompleted != null)
            {
                InteractionCompleted.Invoke(interactionType);
            }
        }
    }
}
