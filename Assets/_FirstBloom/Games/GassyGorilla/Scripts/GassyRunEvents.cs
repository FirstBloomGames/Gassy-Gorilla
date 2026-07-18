using System;

namespace FirstBloom.Games.GassyGorilla
{
    public static class GassyRunEvents
    {
        public static event Action<FoodPickupType> FoodCollected;
        public static event Action CrocodileDodged;

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
    }
}
