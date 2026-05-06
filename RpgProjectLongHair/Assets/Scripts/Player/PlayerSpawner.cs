using UnityEngine;
using Fusion;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Prefabs de personaje (índice 0 = personaje 1, índice 1 = personaje 2)")]
    [SerializeField] private NetworkObject[] _characterPrefabs;

    [Header("Puntos de spawn (uno por jugador, o solo uno si quieren mismo punto)")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = true;

    /// <summary>
    /// Spawnea un jugador con el índice de personaje especificado.
    /// El registro en NavMeshTileManager lo hace NavMeshPlayerTracker.Spawned()
    /// automáticamente en todos los clientes — no hay nada que hacer acá.
    /// </summary>
    public NetworkObject SpawnPlayer(NetworkRunner runner, PlayerRef playerRef, int characterIndex)
    {
        if (_characterPrefabs == null || _characterPrefabs.Length == 0)
        {
            Debug.LogError("[PlayerSpawner] No hay prefabs asignados.");
            return null;
        }

        int prefabIndex = Mathf.Clamp(characterIndex - 1, 0, _characterPrefabs.Length - 1);
        NetworkObject prefab = _characterPrefabs[prefabIndex];

        if (prefab == null)
        {
            Debug.LogError($"[PlayerSpawner] Prefab en índice {prefabIndex} es null.");
            return null;
        }

        Transform spawnPoint = GetSpawnPoint(playerRef);

        NetworkObject spawned = runner.Spawn(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation,
            playerRef
        );

        if (spawned != null)
        {
            if (_showDebugLogs)
                Debug.Log($"[PlayerSpawner] Spawneado personaje {characterIndex} " +
                          $"(prefab índice {prefabIndex}) para {playerRef} en {spawnPoint.position}");
        }
        else
        {
            Debug.LogError($"[PlayerSpawner] Falló spawn para {playerRef}");
        }

        return spawned;
    }

    /// <summary>
    /// Compatibilidad con código antiguo — usa la selección local del host.
    /// </summary>
    public NetworkObject SpawnPlayer(NetworkRunner runner, PlayerRef playerRef)
    {
        if (_showDebugLogs)
            Debug.LogWarning("[PlayerSpawner] Llamado sin characterIndex — usando selección local del host.");

        return SpawnPlayer(runner, playerRef, CharacterSelection.SelectedCharacter);
    }

    /// <summary>
    /// Punto de spawn distribuido por PlayerRef.
    /// </summary>
    private Transform GetSpawnPoint(PlayerRef playerRef)
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogWarning("[PlayerSpawner] Sin spawn points, usando posición del transform.");
            return transform;
        }

        int idx = (playerRef.RawEncoded - 1) % _spawnPoints.Length;
        return _spawnPoints[idx];
    }

    /// <summary>
    /// Mantener por si algún sistema externo lo llama — ya no hace nada
    /// porque NavMeshPlayerTracker.Despawned() se encarga automáticamente.
    /// Podés borrarlo cuando confirmes que nadie más lo llama.
    /// </summary>
    public void DespawnPlayer(Transform playerTransform) { }
}