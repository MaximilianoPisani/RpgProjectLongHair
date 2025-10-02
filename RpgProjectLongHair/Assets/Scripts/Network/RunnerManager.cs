using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
public class RunnerManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public event Action<NetworkObject> OnPlayerSpawned;

    [Header("Spawners")]
    [SerializeField] private ItemSpawner _itemSpawner;
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private PlayerSpawner _playerSpawner; 

    private NetworkRunner _runner;
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();
    private List<NetworkObject> _spawnedItems = new List<NetworkObject>();

    public async void StartRunner(GameMode mode, Action onFail)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var startGameArgs = new StartGameArgs
        {
            GameMode = mode,
            SessionName = "Room_01",
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        var result = await _runner.StartGame(startGameArgs);

        if (!result.Ok)
        {
            Debug.LogError($"[RunnerManager] Failed to start: {result.ShutdownReason}");
            onFail?.Invoke();
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        var playerObj = _playerSpawner.SpawnPlayer(runner, player); 
        if (playerObj == null) return;

        _spawnedPlayers[player] = playerObj;
        Debug.Log($"[RunnerManager] Player {player} spawned.");

        if (player == runner.LocalPlayer)
        {
            OnPlayerSpawned?.Invoke(playerObj);
        }

        if (runner.IsServer && _spawnedPlayers.Count == 1)
        {
            _itemSpawner.SpawnItems(runner);
            _enemySpawner?.SpawnEnemies(runner);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer && _spawnedPlayers.TryGetValue(player, out var obj))
        {
            runner.Despawn(obj);
            _spawnedPlayers.Remove(player);
        }
    }

    private void SpawnItem(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        ItemSpawner.Instance.SpawnItems(runner);

        Debug.Log("[RunnerManager] SpawnItem called through ItemSpawner.");
    }

    public void RemoveItem(NetworkObject item)
    {
        if (_spawnedItems.Contains(item))
        {
            _spawnedItems.Remove(item);
            _runner.Despawn(item);
        }
    }
    private Vector3 GetRandomSpawnPosition()
    {
        return new Vector3(UnityEngine.Random.Range(-3, 3), 0, UnityEngine.Random.Range(-3, 3));
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        Vector3 move = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        bool interact = Input.GetKey(KeyCode.E);

        var data = new NetworkInputData
        {
            moveDirection = move,
            interact = interact
        };

        input.Set(data);
    }
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[RunnerManager] Connected to server, hiding lobby UI.");

        if (runner.LocalPlayer == PlayerRef.None) return;

        OnPlayerSpawned?.Invoke(null);
    }

    public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnShutdown(NetworkRunner r, ShutdownReason s) { }
    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) { }
    public void OnConnectFailed(NetworkRunner r, NetAddress remote, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr msg) { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, ReliableKey k, ArraySegment<byte> d) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef p, ReliableKey k, float pr) { }
    public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i) { }
    public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> s) { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> d) { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken h) { }
    public void OnSceneLoadDone(NetworkRunner r) { }
    public void OnSceneLoadStart(NetworkRunner r) { }

}