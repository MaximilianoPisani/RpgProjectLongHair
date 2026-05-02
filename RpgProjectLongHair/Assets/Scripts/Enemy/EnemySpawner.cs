using System.Collections;
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
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SpawnEnemies(NetworkRunner runner)
    {
        if (!runner.IsServer) return;
        StartCoroutine(SpawnRoutine(runner));
    }

    private IEnumerator SpawnRoutine(NetworkRunner runner)
    {
        // Esperar a que el baker haya terminado su primer bake sobre el jugador.
        // Sin esto los enemigos spawnean antes de que exista NavMesh y el agente
        // no encuentra superficie, quedando inutilizable.
        if (AreaFloorBaker.Instance != null)
        {
            Debug.Log("[EnemySpawner] Esperando NavMesh...");
            yield return new WaitUntil(() => AreaFloorBaker.Instance.IsReady);
            Debug.Log("[EnemySpawner] NavMesh lista. Spawneando enemigos.");
        }

        _spawnedEnemies.Clear();

        foreach (var data in _spawnDatas)
        {
            if (data.Prefab == null || data.SpawnPoints == null || data.SpawnPoints.Length == 0)
                continue;

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
                    Debug.LogWarning($"[EnemySpawner] '{spawnPoint.name}' no está cerca de NavMesh. Saltando.");
                    continue;
                }

                NetworkObject enemy = runner.Spawn(data.Prefab, spawnPos, Quaternion.identity);
                if (enemy != null)
                    _spawnedEnemies.Add(enemy);
            }
        }

        Debug.Log($"[EnemySpawner] Spawneados: {_spawnedEnemies.Count} enemigos.");
    }

    public List<NetworkObject> GetEnemies() => _spawnedEnemies;
}