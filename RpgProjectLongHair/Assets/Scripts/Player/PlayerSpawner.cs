using UnityEngine;
using Fusion;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Prefabs de personaje (índice 0 = personaje 1, índice 1 = personaje 2)")]
    [SerializeField] private NetworkObject[] _characterPrefabs;

    [Header("Puntos de spawn (uno por jugador, o solo uno si quieren mismo punto)")]
    [SerializeField] private Transform[] _spawnPoints;

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

        // Registrar con el baker inmediatamente al spawnear.
        // Esto reemplaza el uso de GetPlayerObject() en el baker,
        // que no funciona en clientes ni antes de SetPlayerObject().
        if (spawned != null)
            AreaFloorBaker.RegisterPlayer(spawned.transform);

        Debug.Log($"[PlayerSpawner] Spawneado personaje {characterIndex} " +
                  $"(prefab índice {prefabIndex}) para {playerRef}");

        return spawned;
    }

    public NetworkObject SpawnPlayer(NetworkRunner runner, PlayerRef playerRef)
    {
        Debug.LogWarning("[PlayerSpawner] Llamado sin characterIndex — usando selección local del host.");
        return SpawnPlayer(runner, playerRef, CharacterSelection.SelectedCharacter);
    }

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
}