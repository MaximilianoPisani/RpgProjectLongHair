using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.AI;

[System.Serializable]
public class SpawnData
{
    public NetworkObject Prefab;
    public Transform[] SpawnPoints;
    public int Count = 1;
}

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [SerializeField] private List<SpawnData> _spawnDatas = new List<SpawnData>();
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

        _spawnedEnemies.Clear();

        foreach (var data in _spawnDatas)
        {
            if (data.Prefab == null || data.SpawnPoints.Length == 0) continue;

            for (int i = 0; i < data.Count; i++)
            {
                Transform spawnPoint = data.SpawnPoints[i % data.SpawnPoints.Length];
                Vector3 spawnPos = spawnPoint.position;

                if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    spawnPos = hit.position;
                }
                else
                {
                    Debug.LogWarning($"Spawn point {spawnPoint.name} It is not close to the NavMesh");
                    continue;
                }

                NetworkObject enemy = runner.Spawn(data.Prefab, spawnPos, Quaternion.identity);
                if (enemy != null)
                {
                    _spawnedEnemies.Add(enemy);
                }
            }
        }
    }

    public List<NetworkObject> GetEnemies() => _spawnedEnemies;
}