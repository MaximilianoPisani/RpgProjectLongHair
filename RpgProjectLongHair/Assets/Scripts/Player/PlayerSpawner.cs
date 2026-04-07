using UnityEngine;
using Fusion;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private Transform _spawnPoint;

    public NetworkObject SpawnPlayer(NetworkRunner runner, PlayerRef playerRef)
    {
        if (_spawnPoint == null || _playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] Falta prefab o spawn");
            return null;
        }

        return runner.Spawn(
            _playerPrefab,
            _spawnPoint.position,
            _spawnPoint.rotation,
            playerRef
        );
    }
}
