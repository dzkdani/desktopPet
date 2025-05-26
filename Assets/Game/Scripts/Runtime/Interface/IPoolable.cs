public interface IPoolable
{
    string PoolID { get; }
    void OnSpawn();
    void OnDespawn();
}