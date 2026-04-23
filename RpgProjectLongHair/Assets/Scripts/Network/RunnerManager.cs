using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
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
    public IReadOnlyDictionary<PlayerRef, NetworkObject> SpawnedPlayers => _spawnedPlayers;

    private HashSet<string> _connectedPlayerIds = new();

    private const int MAX_PLAYERS = 4;

    private bool _lockOnQueued;
    private bool _jumpQueued;
    private bool _isSprinting;
    public static bool IsInputBlocked { get; private set; } = false;
    public static bool IsInventoryOpen { get; private set; } = false;

    public async void StartRunner(GameMode mode, Action onFail)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        string playerId = AuthenticationManager.Instance.PlayerId;

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("[RunnerManager] PlayerId inválido. ¿No estás logueado?");
            onFail?.Invoke();
            return;
        }

        int characterIndex = CharacterSelection.SelectedCharacter;
        string tokenPayload = $"{playerId}|{characterIndex}";

        var startGameArgs = new StartGameArgs
        {
            GameMode = mode,
            SessionName = "Room_01",
            PlayerCount = MAX_PLAYERS,
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            ConnectionToken = Encoding.UTF8.GetBytes(tokenPayload)
        };

        var result = await _runner.StartGame(startGameArgs);

        if (!result.Ok)
        {
            Debug.LogError($"[RunnerManager] Failed to start: {result.ShutdownReason}");
            onFail?.Invoke();
        }
    }

    public static void SetInventoryOpen(bool open)
    {
        IsInventoryOpen = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && !IsInventoryOpen) _lockOnQueued = true;
        if (Input.GetKeyDown(KeyCode.Space) && !IsInventoryOpen) _jumpQueued = true;
        _isSprinting = Input.GetKey(KeyCode.LeftShift) && !IsInventoryOpen;
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        if (runner.ActivePlayers.Count() >= MAX_PLAYERS)
        {
            Debug.Log("[RunnerManager] Sala llena, rechazando conexión");
            request.Refuse();
            return;
        }

        request.Accept();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        if (_spawnedPlayers.Count >= MAX_PLAYERS)
        {
            Debug.Log("[RunnerManager] Sala llena (fallback), desconectando jugador");
            runner.Disconnect(player);
            return;
        }

        byte[] token = runner.GetPlayerConnectionToken(player);

        if (token == null || token.Length == 0)
        {
            Debug.LogError("[RunnerManager] Token inválido");
            runner.Disconnect(player);
            return;
        }

        string tokenStr = Encoding.UTF8.GetString(token);
        string[] parts = tokenStr.Split('|');

        if (parts.Length < 2 || !int.TryParse(parts[1], out int characterIndex))
        {
            Debug.LogError($"[RunnerManager] Token mal formado: {tokenStr}");
            runner.Disconnect(player);
            return;
        }

        string playerId = parts[0];

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("[RunnerManager] PlayerId vacío en token");
            runner.Disconnect(player);
            return;
        }

        if (_connectedPlayerIds.Contains(playerId))
        {
            Debug.LogWarning($"[RunnerManager] Cuenta ya en uso: {playerId}");
            runner.Disconnect(player);
            return;
        }

        _connectedPlayerIds.Add(playerId);

        var playerObj = _playerSpawner.SpawnPlayer(runner, player, characterIndex);

        if (playerObj == null) return;

        runner.SetPlayerObject(player, playerObj);
        _spawnedPlayers[player] = playerObj;

        Debug.Log($"[RunnerManager] Player conectado: {playerId}, personaje: {characterIndex}");

        if (player == runner.LocalPlayer)
            OnPlayerSpawned?.Invoke(playerObj);

        if (_spawnedPlayers.Count == 1)
        {
            _itemSpawner.SpawnItems(_runner);
            _enemySpawner?.SpawnEnemies(runner);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        byte[] token = runner.GetPlayerConnectionToken(player);

        if (token != null && token.Length > 0)
        {
            string tokenStr = Encoding.UTF8.GetString(token);
            string[] parts = tokenStr.Split('|');
            string playerId = parts[0];

            if (_connectedPlayerIds.Contains(playerId))
                _connectedPlayerIds.Remove(playerId);
        }

        if (_spawnedPlayers.TryGetValue(player, out var obj))
        {
            runner.Despawn(obj);
            _spawnedPlayers.Remove(player);
        }
    }

    public void RemoveItem(NetworkObject item)
    {
        if (item != null && item.Runner != null)
            item.Runner.Despawn(item);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (IsInventoryOpen || IsInputBlocked)
        {
            input.Set(new NetworkInputData());
            return;
        }

        Vector3 inputMove = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        Transform cameraTransform = PlayerCamera.Local != null
            ? PlayerCamera.Local.transform
            : (Camera.main != null ? Camera.main.transform : null);

        Vector3 movementDir = Vector3.zero;
        Quaternion aimRot = Quaternion.identity;
        Vector3 shootDir = Vector3.forward;

        if (cameraTransform != null)
        {
            movementDir = cameraTransform.forward * inputMove.z + cameraTransform.right * inputMove.x;
            movementDir.y = 0f;
            movementDir.Normalize();

            aimRot = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);

            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            shootDir = Physics.Raycast(ray, out RaycastHit hit, 200f)
                ? (hit.point - Camera.main.transform.position).normalized
                : ray.direction.normalized;
        }

        var data = new NetworkInputData
        {
            moveDirection = movementDir,
            interact = Input.GetKey(KeyCode.E),
            jump = _jumpQueued,
            attack = Input.GetMouseButton(0),
            attackRange = Input.GetMouseButton(1),
            sprint = _isSprinting,
            equipSlot = -1,
            aimRotation = aimRot,
            LockOnPressed = _lockOnQueued,
            shootDirection = shootDir
        };

        _lockOnQueued = false;
        _jumpQueued = false;

        input.Set(data);
    }

    public static void SetInputBlocked(bool blocked)
    {
        IsInputBlocked = blocked;
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        if (runner.LocalPlayer == PlayerRef.None) return;
        OnPlayerSpawned?.Invoke(null);
    }

    public async void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[RunnerManager] Desconectado: {reason}");

        if (AuthenticationManager.Instance != null)
            AuthenticationManager.Instance.SignOut();

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.ResetToLogin();

        if (runner != null)
            await runner.Shutdown();

        _runner = null;
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remote, NetConnectFailedReason reason)
    {
        Debug.Log($"[RunnerManager] Conexión fallida: {reason}");
    }

    public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnShutdown(NetworkRunner r, ShutdownReason s) { }
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