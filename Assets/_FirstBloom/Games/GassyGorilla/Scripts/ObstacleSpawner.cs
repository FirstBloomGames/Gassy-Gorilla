using FirstBloom.ArcadeFramework.Spawning;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public class ObstacleSpawner : ArcadeSpawner2D
    {
        [SerializeField] private float groundLaneY = -0.9f;
        [SerializeField] private Vector2 groundLaneJitter = new Vector2(-0.08f, 0.08f);
        [SerializeField] private Vector2 aerialLaneYRange = new Vector2(1.15f, 2.35f);

        protected override GameObject SpawnOne(float currentDistance)
        {
            GameObject prefab = ChoosePrefab();
            if (prefab == null)
            {
                return null;
            }

            Vector3 position = GetSpawnPosition(currentDistance);
            position.y = ChooseLaneY(prefab);
            GameObject spawned = Instantiate(prefab, position, Quaternion.identity);
            PrepareSpawnedObject(spawned);
            return spawned;
        }

        private float ChooseLaneY(GameObject prefab)
        {
            string prefabName = prefab != null ? prefab.name : "";
            if (prefabName.Contains("Vine"))
            {
                return Random.Range(aerialLaneYRange.x, aerialLaneYRange.y);
            }

            return groundLaneY + Random.Range(groundLaneJitter.x, groundLaneJitter.y);
        }
    }
}
