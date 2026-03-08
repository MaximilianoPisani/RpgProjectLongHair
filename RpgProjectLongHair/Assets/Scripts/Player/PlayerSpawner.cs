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

    private Dictionary<PlayerRef, int> playerSelections = new Dictionary<PlayerRef, int>();

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

        Debug.Log($"[PlayerSpawner] Player {player} selected character {characterIndex}");
    }

    public NetworkObject SpawnPlayer(NetworkRunner runner, PlayerRef playerRef)
    {
        if (_spawnPoint == null)
        {
            Debug.LogError("[PlayerSpawner] SpawnPoint missing.");
            return null;
        }

        int selectedCharacter = 1;

        if (playerSelections.TryGetValue(playerRef, out int selection))
        {
            selectedCharacter = selection;
        }

        NetworkObject prefabToSpawn = GetPrefab(selectedCharacter);

        NetworkObject player = runner.Spawn(
            prefabToSpawn,
            _spawnPoint.position,
            _spawnPoint.rotation,
            playerRef
        );

        Debug.Log($"[PlayerSpawner] Spawned Player {playerRef} with character {selectedCharacter}");

        return player;
    }

    private NetworkObject GetPrefab(int index)
    {
        switch (index)
        {
            case 1: return _player1Prefab;
            case 2: return _player2Prefab;
            case 3: return _player3Prefab;
        }

        return _player1Prefab;
    }
}