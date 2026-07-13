using UnityEngine;

namespace FirstBloom.ArcadeFramework.Spawning
{
    public class ArcadeSpawner2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform distanceSource;
        [SerializeField] private GameObject[] prefabs;

        [Header("Spawn Placement")]
        [SerializeField] private float spawnAheadDistance = 14f;
        [SerializeField] private float minY = -1f;
        [SerializeField] private float maxY = 3.25f;
        [SerializeField] private Vector2 randomScaleRange = Vector2.one;

        [Header("Rhythm")]
        [SerializeField] private bool spawning = true;
        [SerializeField] private float startAfterDistance = 4f;
        [SerializeField] private float minIntervalDistance = 4f;
        [SerializeField] private float maxIntervalDistance = 7f;
        [Range(0f, 1f)] [SerializeField] private float spawnChance = 1f;

        [Header("Distance Ramp")]
        [SerializeField] private float rampCompleteDistance;
        [SerializeField] private float minIntervalDistanceAtRamp = -1f;
        [SerializeField] private float maxIntervalDistanceAtRamp = -1f;
        [Range(-1f, 1f)] [SerializeField] private float spawnChanceAtRamp = -1f;

        private float nextSpawnDistance;

        public Transform DistanceSource
        {
            get { return distanceSource; }
            set { distanceSource = value; }
        }

        public GameObject[] Prefabs
        {
            get { return prefabs; }
            set { prefabs = value; }
        }

        protected virtual void Start()
        {
            ResetSpawner();
        }

        protected virtual void Update()
        {
            if (!spawning || distanceSource == null)
            {
                return;
            }

            float currentDistance = distanceSource.position.x;
            if (currentDistance < nextSpawnDistance)
            {
                return;
            }

            if (Random.value <= GetCurrentSpawnChance(currentDistance))
            {
                SpawnOne(currentDistance);
            }

            ScheduleNext(currentDistance);
        }

        public void SetSpawning(bool value)
        {
            spawning = value;
        }

        public void ResetSpawner()
        {
            float currentDistance = distanceSource != null ? distanceSource.position.x : 0f;
            nextSpawnDistance = currentDistance + Mathf.Max(0f, startAfterDistance);
        }

        protected virtual GameObject SpawnOne(float currentDistance)
        {
            GameObject prefab = ChoosePrefab();
            if (prefab == null)
            {
                return null;
            }

            Vector3 position = GetSpawnPosition(currentDistance);
            GameObject spawned = Instantiate(prefab, position, Quaternion.identity);
            float scale = Random.Range(randomScaleRange.x, randomScaleRange.y);
            if (scale > 0f && !Mathf.Approximately(scale, 1f))
            {
                spawned.transform.localScale = spawned.transform.localScale * scale;
            }

            PrepareSpawnedObject(spawned);
            return spawned;
        }

        protected virtual GameObject ChoosePrefab()
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return null;
            }

            return prefabs[Random.Range(0, prefabs.Length)];
        }

        protected virtual Vector3 GetSpawnPosition(float currentDistance)
        {
            return new Vector3(currentDistance + spawnAheadDistance, Random.Range(minY, maxY), 0f);
        }

        protected virtual void PrepareSpawnedObject(GameObject spawned)
        {
        }

        private void ScheduleNext(float currentDistance)
        {
            float ramp = GetRamp(currentDistance);
            float rampedMinInterval = minIntervalDistanceAtRamp >= 0f ? Mathf.Lerp(minIntervalDistance, minIntervalDistanceAtRamp, ramp) : minIntervalDistance;
            float rampedMaxInterval = maxIntervalDistanceAtRamp >= 0f ? Mathf.Lerp(maxIntervalDistance, maxIntervalDistanceAtRamp, ramp) : maxIntervalDistance;
            float interval = Random.Range(rampedMinInterval, Mathf.Max(rampedMinInterval, rampedMaxInterval));
            nextSpawnDistance = currentDistance + Mathf.Max(0.5f, interval);
        }

        private float GetCurrentSpawnChance(float currentDistance)
        {
            if (spawnChanceAtRamp < 0f)
            {
                return spawnChance;
            }

            return Mathf.Clamp01(Mathf.Lerp(spawnChance, spawnChanceAtRamp, GetRamp(currentDistance)));
        }

        private float GetRamp(float currentDistance)
        {
            if (rampCompleteDistance <= 0f)
            {
                return 0f;
            }

            float startDistance = distanceSource != null ? Mathf.Max(0f, currentDistance - Mathf.Max(0f, startAfterDistance)) : currentDistance;
            return Mathf.Clamp01(startDistance / rampCompleteDistance);
        }
    }
}
