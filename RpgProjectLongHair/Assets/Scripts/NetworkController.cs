using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class NetworkController : MonoBehaviour, INetworkRunnerCallbacks
{

    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _joinRoomButton;
    [SerializeField] private NetworkRunner _networkRunner;
    [SerializeField] private NetworkSceneManagerDefault _networkSceneManagerDefault;
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private PlayerInput _playerInput; 

    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    private void Start()
    {
        _createRoomButton.onClick.AddListener(CreateRoom);
        _joinRoomButton.onClick.AddListener(JoinRoom);
    }

    private async void CreateRoom()
    {
        var gameArg = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = "Room_01",
            SceneManager = _networkSceneManagerDefault
        };

        var result = await _networkRunner.StartGame(gameArg);

        if (!result.Ok)
        {
            Debug.LogError($"Failed to create room: {result.ShutdownReason}");
            Debug.LogError($"Error: {result.ErrorMessage}");
        }
    }

    private async void JoinRoom()
    {
        var gameArg = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = "Room_01",
            SceneManager = _networkSceneManagerDefault
        };

        var result = await _networkRunner.StartGame(gameArg);

        if (!result.Ok)
        {
            Debug.LogError(result.ShutdownReason);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        NetworkObject playerObject = runner.Spawn(
            _playerPrefab,
            new Vector3(UnityEngine.Random.Range(-3, 3), 0, 0),
            Quaternion.identity,
            player
        );
        _spawnedPlayers[player] = playerObject;

        if (player == runner.LocalPlayer)
        {
            _lobbyPanel.SetActive(false);
            _playerInput = playerObject.GetComponent<PlayerInput>();
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.ContainsKey(player))
        {
            runner.Despawn(_spawnedPlayers[player]);
            _spawnedPlayers.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = new NetworkInputData();

        data.move = _playerInput.actions["Move"].ReadValue<Vector2>();
        data.jump = _playerInput.actions["Jump"].triggered;

        input.Set(data);
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }
}
