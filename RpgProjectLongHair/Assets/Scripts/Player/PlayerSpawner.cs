using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }

    [Header("Player Prefabs")]
    [SerializeField] private NetworkObject _player1Prefab;
    [SerializeField] private NetworkObject _player2Prefab;
    [SerializeField] private NetworkObject _player3Prefab;

    [SerializeField] private Transform _spawnPoint;

    private Dictionary<PlayerRef, int> playerSelections = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetPlayerSelection(PlayerRef player, int characterIndex)
    {
        playerSelections[player] = characterIndex;
    }

    public NetworkObject SpawnPlayer(NetworkRunner runner, PlayerRef playerRef)
    {
        if (_spawnPoint == null)
        {
            Debug.LogError("[PlayerSpawner] SpawnPoint missing.");
            return null;
        }

        int selectedCharacter = playerSelections.TryGetValue(playerRef, out int selection)
            ? selection
            : 1;

        NetworkObject prefabToSpawn = GetPrefab(selectedCharacter);

        NetworkObject player = runner.Spawn(
            prefabToSpawn,
            _spawnPoint.position,
            _spawnPoint.rotation,
            playerRef
        );

        return player;
    }

    private NetworkObject GetPrefab(int index)
    {
        return index switch
        {
            1 => _player1Prefab,
            2 => _player2Prefab,
            3 => _player3Prefab,
            _ => _player1Prefab
        };
    }
}