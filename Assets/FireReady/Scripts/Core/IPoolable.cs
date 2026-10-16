namespace FireReady.Core
{
    public interface IPoolable
    {
        // Called after the object is taken from the pool and activated.
        void OnSpawn();

        // Called before the object goes back to the pool. Must leave it looking new.
        void OnDespawn();
    }
}
