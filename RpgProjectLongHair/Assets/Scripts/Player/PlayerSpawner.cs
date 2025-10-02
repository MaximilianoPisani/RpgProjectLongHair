using UnityEngine;
using Fusion;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }

    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private Transform _spawnPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public NetworkObject SpawnPlayer(NetworkRunner runner, PlayerRef playerRef)
    {
        if (_playerPrefab == null || _spawnPoint == null)
        {
            Debug.LogError("[PlayerSpawner] Prefab or SpawnPoint unassigned.");
            return null;
        }

        Vector3 pos = _spawnPoint.position;
        Quaternion rot = _spawnPoint.rotation;

        NetworkObject player = runner.Spawn(_playerPrefab, pos, rot, playerRef);
        return player;
    }
}