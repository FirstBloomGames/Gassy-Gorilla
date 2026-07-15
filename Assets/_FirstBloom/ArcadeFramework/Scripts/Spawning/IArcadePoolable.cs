namespace FirstBloom.ArcadeFramework.Spawning
{
    public interface IArcadePoolable
    {
        void OnSpawnedFromPool();
        void OnDespawnedToPool();
    }
}
