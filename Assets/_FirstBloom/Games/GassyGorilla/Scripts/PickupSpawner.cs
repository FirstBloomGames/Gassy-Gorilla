using FirstBloom.ArcadeFramework.Spawning;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public class PickupSpawner : ArcadeSpawner2D
    {
        [SerializeField] private int minClusterCount = 1;
        [SerializeField] private int maxClusterCount = 3;
        [SerializeField] private float clusterSpacing = 0.75f;
        [SerializeField] private float clusterArcHeight = 0.35f;

        protected override GameObject SpawnOne(float currentDistance)
        {
            int count = Random.Range(minClusterCount, maxClusterCount + 1);
            GameObject first = null;
            Vector3 basePosition = GetSpawnPosition(currentDistance);

            for (int i = 0; i < count; i++)
            {
                GameObject prefab = ChoosePrefab();
                if (prefab == null)
                {
                    continue;
                }

                float centeredIndex = i - (count - 1) * 0.5f;
                Vector3 position = basePosition + new Vector3(centeredIndex * clusterSpacing, Mathf.Abs(centeredIndex) * -clusterArcHeight, 0f);
                GameObject spawned = Instantiate(prefab, position, Quaternion.identity);
                PrepareSpawnedObject(spawned);

                if (first == null)
                {
                    first = spawned;
                }
            }

            return first;
        }
    }
}
