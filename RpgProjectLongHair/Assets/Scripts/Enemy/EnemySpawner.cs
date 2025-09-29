using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [SerializeField] private NetworkObject _enemyPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private int _enemyCount = 3;

    private List<NetworkObject> _spawnedEnemies = new List<NetworkObject>();

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
            Transform spawnPoint = _spawnPoints[i % _spawnPoints.Length];
            Vector3 pos = spawnPoint.position;

            NetworkObject enemy = runner.Spawn(_enemyPrefab, pos, Quaternion.identity);

            if (enemy != null)
            {
                _spawnedEnemies.Add(enemy);
            }
        }
    }

    public List<NetworkObject> GetEnemies() => _spawnedEnemies;
}