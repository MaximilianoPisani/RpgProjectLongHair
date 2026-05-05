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

    [Header("Spawn Validation")]
    [Tooltip("Altura del raycast para encontrar el suelo")]
    public float RaycastHeight = 50f;
    [Tooltip("Layer del suelo")]
    public LayerMask GroundLayer = -1;
}

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [SerializeField] private List<SpawnData> _spawnDatas = new List<SpawnData>();
    [SerializeField] private float _respawnDelay = 5f;

    private List<NetworkObject> _spawnedEnemies = new List<NetworkObject>();
    private NetworkRunner _runner;

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

        _runner = runner;
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        // YA NO esperamos a que NavMesh esté lista
        // Los enemigos se spawnean inmediatamente y se activan cuando llegue el bake

        Debug.Log("[EnemySpawner] Iniciando spawn (sin esperar NavMesh)...");

        _spawnedEnemies.Clear();

        foreach (var data in _spawnDatas)
        {
            if (data.Prefab == null || data.SpawnPoints == null || data.SpawnPoints.Length == 0)
                continue;

            for (int i = 0; i < data.Count; i++)
            {
                Transform spawnPoint = data.SpawnPoints[i % data.SpawnPoints.Length];
                Vector3 spawnPos = FindGroundPosition(spawnPoint.position, data);

                if (spawnPos == Vector3.zero)
                {
                    Debug.LogWarning($"[EnemySpawner] No se encontró suelo debajo de '{spawnPoint.name}'. Saltando.");
                    continue;
                }

                // Spawn directo, SIN validar NavMesh
                NetworkObject enemy = _runner.Spawn(data.Prefab, spawnPos, Quaternion.identity);

                if (enemy != null)
                {
                    _spawnedEnemies.Add(enemy);
                    Debug.Log($"[EnemySpawner] Enemigo spawneado en {spawnPos} (NavMesh se activará cuando llegue el bake)");

                    // Suscribirse al respawn si tiene EnemyHealth
                    var health = enemy.GetComponent<EnemyHealth>();
                    if (health != null)
                    {
                        var capturedData = data;
                        var capturedPos = spawnPos;
                        var capturedEnemy = enemy;

                        health.OnDeath += () =>
                        {
                            _spawnedEnemies.Remove(capturedEnemy);
                            StartCoroutine(RespawnAfterDelay(capturedData, capturedPos));
                        };
                    }
                }
            }
        }

        Debug.Log($"[EnemySpawner] Spawneados: {_spawnedEnemies.Count} enemigos (estado: dormidos hasta que llegue NavMesh)");
        yield break;
    }

    /// <summary>
    /// Encuentra la posición del suelo usando raycast
    /// </summary>
    private Vector3 FindGroundPosition(Vector3 position, SpawnData data)
    {
        // Raycast desde arriba hacia abajo
        Vector3 rayStart = position + Vector3.up * data.RaycastHeight;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, data.RaycastHeight * 2f, data.GroundLayer))
        {
            return hit.point;
        }

        // Si no hay hit, usar la posición original
        Debug.LogWarning($"[EnemySpawner] No se encontró suelo en {position}, usando posición original");
        return position;
    }

    private IEnumerator RespawnAfterDelay(SpawnData data, Vector3 lastPosition)
    {
        yield return new WaitForSeconds(_respawnDelay);

        if (_runner == null || !_runner.IsRunning)
            yield break;

        // Respawn en la misma posición
        Vector3 spawnPos = FindGroundPosition(lastPosition, data);

        if (spawnPos != Vector3.zero)
        {
            NetworkObject enemy = _runner.Spawn(data.Prefab, spawnPos, Quaternion.identity);

            if (enemy != null)
            {
                _spawnedEnemies.Add(enemy);
                Debug.Log($"[EnemySpawner] Enemigo re-spawneado en {spawnPos}");
            }
        }
    }

    public List<NetworkObject> GetEnemies() => _spawnedEnemies;
    public int GetEnemyCount() => _spawnedEnemies.Count;

    private void OnDrawGizmos()
    {
        if (_spawnDatas == null) return;

        foreach (var data in _spawnDatas)
        {
            if (data.SpawnPoints == null) continue;

            Gizmos.color = Color.yellow;
            foreach (var sp in data.SpawnPoints)
            {
                if (sp != null)
                {
                    Gizmos.DrawWireSphere(sp.position, 1f);
                    Gizmos.DrawLine(sp.position, sp.position + Vector3.up * 3f);
                }
            }
        }
    }
}