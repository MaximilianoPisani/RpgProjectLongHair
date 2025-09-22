using UnityEngine;
using Fusion;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [SerializeField] private NetworkObject _enemyPrefab;
    [SerializeField] private int _enemyCount = 3;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SpawnEnemies(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        for (int i = 0; i < _enemyCount; i++)
        {
            Vector3 pos = new Vector3
            (
                Random.Range(-8f, 8f),
                0.5f,
                Random.Range(-8f, 8f)
            );
            runner.Spawn(_enemyPrefab, pos, Quaternion.identity);
        }
    }
}