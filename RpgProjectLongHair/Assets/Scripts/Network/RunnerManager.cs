using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

public class RunnerManager : MonoBehaviour, INetworkRunnerCallbacks
{
    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    public event Action<NetworkObject> OnPlayerSpawned;

    [Header("Spawners")]
    [SerializeField] private ItemSpawner _itemSpawner;
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private PlayerSpawner _playerSpawner;

    private NetworkRunner _runner;

    private bool _isTryingToJoin;
    private bool _wasRejectedByDuplicateLogin;

    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();
    public IReadOnlyDictionary<PlayerRef, NetworkObject> SpawnedPlayers => _spawnedPlayers;

    private HashSet<string> _connectedPlayerIds = new();

    private const int MAX_PLAYERS = 4;

    /// <summary>true cuando este RunnerManager solo está en modo browser (lista de salas).</summary>
    private bool _isBrowserOnly = false;

    /// <summary>true durante el shutdown controlado — evita que OnDisconnectedFromServer resetee al login.</summary>
    private bool _isShuttingDownControlled = false;

    private bool _lockOnQueued;
    private bool _jumpQueued;
    private bool _isSprinting;
    private bool _lastAttackHeld;

    private int _scrollDelta;
    private bool _scrollConsumed;
    public static bool IsInputBlocked { get; private set; } = false;
    public static bool IsInventoryOpen { get; private set; } = false;

    public async void StartRunner(GameMode mode, string sessionName, Action onFail)
    {
        _isBrowserOnly = false;
        _isShuttingDownControlled = false;

        _isTryingToJoin = true;
        _wasRejectedByDuplicateLogin = false;

        if (_runner != null)
        {
            Debug.LogWarning("[RunnerManager] StartRunner llamado pero ya hay un runner activo. Haciendo shutdown primero.");
            await ShutdownAsync();
        }

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        string playerId = AuthenticationManager.Instance.PlayerId;

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("[RunnerManager] PlayerId inválido.");
            onFail?.Invoke();
            return;
        }

        int charIndex = CharacterSelection.SelectedCharacter;

        var cloudSave = gameObject.GetComponent<PlayerCloudSave>()
                       ?? gameObject.AddComponent<PlayerCloudSave>();

        PlayerSaveData saveData = await cloudSave.LoadPlayerData();

        string tokenPayload;

        if (saveData.hasCheckpoint)
        {
            Vector3 cp = saveData.CheckpointPosition;

            tokenPayload = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0}|{1}|{2}|{3}|{4}|1",
                playerId,
                charIndex,
                cp.x,
                cp.y,
                cp.z
            );
        }
        else
        {
            tokenPayload = $"{playerId}|{charIndex}|0|0|0|0";
        }

        var startGameArgs = new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            PlayerCount = MAX_PLAYERS,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            IsVisible = true,
            IsOpen = true,
            ConnectionToken = Encoding.UTF8.GetBytes(tokenPayload)
        };

        Debug.Log($"[RunnerManager] StartGame — mode:{mode} session:'{sessionName}'");

        var result = await _runner.StartGame(startGameArgs);

        if (!result.Ok)
        {
            Debug.LogError($"[RunnerManager] StartGame falló: {result.ShutdownReason}");

            _isTryingToJoin = false;

            onFail?.Invoke();
        }
        else
        {
            Debug.Log($"[RunnerManager] Sala '{sessionName}' creada/unida OK");
        }
    }

    public async void StartRunnerWithToken(GameMode mode, string sessionName, string tokenPayload, Action onFail)
    {
        _isBrowserOnly = false;
        _isShuttingDownControlled = false;

        if (_runner != null)
        {
            await ShutdownAsync();
        }

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var startGameArgs = new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            PlayerCount = MAX_PLAYERS,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            IsVisible = true,
            IsOpen = true,
            ConnectionToken = Encoding.UTF8.GetBytes(tokenPayload)
        };

        var result = await _runner.StartGame(startGameArgs);

        if (!result.Ok)
        {
            Debug.LogError($"[RunnerManager] StartGame falló: {result.ShutdownReason}");
            onFail?.Invoke();
        }
    }
    // ARRANCAR en modo browser (solo escuchar la lista de salas)
    public async void StartLobbyBrowser(Action onFail)
    {
        _isBrowserOnly = true;
        _isShuttingDownControlled = false;

        if (_runner != null)
        {
            Debug.LogWarning("[RunnerManager] StartLobbyBrowser llamado pero ya hay un runner activo.");
            await ShutdownAsync();
        }

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = false;

        Debug.Log("[RunnerManager] Entrando al ClientServer lobby...");

        var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);

        if (!result.Ok)
        {
            Debug.LogError($"[RunnerManager] No se pudo entrar al lobby: {result.ShutdownReason}");
            onFail?.Invoke();
        }
        else
        {
            Debug.Log("[RunnerManager] Escuchando salas en ClientServer lobby");
        }
    }

    // SHUTDOWN controlado — aguardable (Task) para encadenar con await
    public async Task ShutdownAsync()
    {
        _isShuttingDownControlled = true;

        if (_runner != null)
        {
            Debug.Log($"[RunnerManager] ShutdownAsync (browserOnly:{_isBrowserOnly})");
            await _runner.Shutdown();
            _runner = null;
        }

        _isShuttingDownControlled = false;
    }

    // Helpers de gameplay
    public static void SetInventoryOpen(bool open)
    {
        IsInventoryOpen = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    public static void SetInputBlocked(bool blocked)
    {
        IsInputBlocked = blocked;
    }

    public void RemoveItem(NetworkObject item)
    {
        if (item != null && item.Runner != null)
            item.Runner.Despawn(item);
    }

    // Update — cola de inputs
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && !IsInventoryOpen) _lockOnQueued = true;
        if (Input.GetKeyDown(KeyCode.Space) && !IsInventoryOpen) _jumpQueued = true;
        _isSprinting = Input.GetKey(KeyCode.LeftShift) && !IsInventoryOpen;

        if (!IsInventoryOpen)
        {
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");

            if (Mathf.Abs(scroll) > 0.01f && !_scrollConsumed)
            {
                _scrollDelta = scroll > 0f ? -1 : 1;
                _scrollConsumed = true;
            }
            else if (Mathf.Abs(scroll) <= 0.01f)
            {
                _scrollConsumed = false;
            }
        }
    }

    // INetworkRunnerCallbacks
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        if (runner.ActivePlayers.Count() >= MAX_PLAYERS)
        {
            request.Refuse();
            return;
        }
        request.Accept();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        if (_spawnedPlayers.Count >= MAX_PLAYERS)
        {
            runner.Disconnect(player);
            return;
        }

        byte[] token = runner.GetPlayerConnectionToken(player);

        if (token == null || token.Length == 0)
        {
            runner.Disconnect(player);
            return;
        }

        string tokenStr = Encoding.UTF8.GetString(token);
        string[] parts = tokenStr.Split('|');

        if (parts.Length < 6 || !int.TryParse(parts[1], out int characterIndex))
        {
            Debug.LogError($"[RunnerManager] Token mal formado: {tokenStr}");
            runner.Disconnect(player);
            return;
        }

        string playerId = parts[0];

        if (string.IsNullOrEmpty(playerId))
        {
            runner.Disconnect(player);
            return;
        }

        if (_connectedPlayerIds.Contains(playerId))
        {
            Debug.LogWarning($"[RunnerManager] Login duplicado detectado: {playerId}");

            runner.Disconnect(player);

            return;
        }

        bool hasCheckpoint = parts[5] == "1";

        Vector3 spawnPosition = Vector3.zero;
        bool useCheckpoint = false;

        if (hasCheckpoint &&
            float.TryParse(parts[2], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float cx) &&
            float.TryParse(parts[3], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float cy) &&
            float.TryParse(parts[4], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float cz))
        {
            spawnPosition = new Vector3(cx, cy, cz);
            useCheckpoint = true;
        }

        _connectedPlayerIds.Add(playerId);

        NetworkObject playerObj;

        if (useCheckpoint)
            playerObj = _playerSpawner.SpawnPlayerAtPosition(
                runner,
                player,
                characterIndex,
                spawnPosition
            );
        else
            playerObj = _playerSpawner.SpawnPlayer(
                runner,
                player,
                characterIndex
            );

        if (playerObj == null)
            return;

        runner.SetPlayerObject(player, playerObj);

        _spawnedPlayers[player] = playerObj;

        Debug.Log($"[RunnerManager] Player conectado: {playerId}");

        if (player == runner.LocalPlayer)
        {
            _isTryingToJoin = false;
            OnPlayerSpawned?.Invoke(playerObj);
        }

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
            // NavMeshPlayerTracker.Despawned() se dispara solo con runner.Despawn(),
            // pero si usás NetworkBehaviour.Despawned() en el tracker, esto es automático.
            // Si no usás NetworkBehaviour, desregistrá manualmente acá:
            // NavMeshTileManager.Instance?.UnregisterPlayer(obj.transform);
            runner.Despawn(obj);
            _spawnedPlayers.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (IsInventoryOpen || IsInputBlocked)
        {
            _lastAttackHeld = false;
            _scrollDelta = 0;
            _scrollConsumed = false;
            input.Set(new NetworkInputData());
            return;
        }

        Vector3 inputMove = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        if (inputMove.magnitude < 0.1f)
            inputMove = Vector3.zero;

        Transform cameraTransform = PlayerCamera.Local != null
            ? PlayerCamera.Local.transform
            : (Camera.main != null ? Camera.main.transform : null);

        Vector3 movementDir = Vector3.zero;
        Quaternion aimRot = Quaternion.identity;
        Vector3 shootDir = Vector3.forward;
        Vector3 aimPoint = Vector3.zero;

        if (cameraTransform != null)
        {
            movementDir = cameraTransform.forward * inputMove.z + cameraTransform.right * inputMove.x;
            movementDir.y = 0f;
            movementDir.Normalize();

            aimRot = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);

            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                aimPoint = hit.point;
            }
            else
            {
                aimPoint = ray.origin + ray.direction * 200f;
            }

            // Dirección aproximada (se corrige después con el muzzle)
            shootDir = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z).normalized;
        }


        bool attackNow = Input.GetMouseButton(0);

        var data = new NetworkInputData
        {
            moveDirection = movementDir,
            interact = Input.GetKey(KeyCode.E),
            jump = _jumpQueued,
            attack = attackNow,
            attackJustPressed = attackNow && !_lastAttackHeld,
            attackRange = attackNow,
            sprint = _isSprinting,
            equipSlot = -1,
            aimRotation = aimRot,
            LockOnPressed = _lockOnQueued,
            shootDirection = shootDir,
            aimPoint = aimPoint,
            scrollDelta = _scrollDelta
        };

        _lastAttackHeld = attackNow;
        _lockOnQueued = false;
        _jumpQueued = false;
        _scrollDelta = 0;

        input.Set(data);
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"[RunnerManager] OnSessionListUpdated ? {sessionList.Count} sala(s)");
        foreach (var s in sessionList)
            Debug.Log($"  · '{s.Name}' | {s.PlayerCount}/{s.MaxPlayers} | open:{s.IsOpen} visible:{s.IsVisible}");

        RoomBrowser.Instance?.RefreshList(sessionList);
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log(
            $"[RunnerManager] Desconectado: {reason} | " +
            $"browserOnly:{_isBrowserOnly} | " +
            $"controlled:{_isShuttingDownControlled}"
        );

        if (_isShuttingDownControlled)
        {
            Debug.Log("[RunnerManager] Desconexión controlada — ignorando");

            _runner = null;
            return;
        }

        _runner = null;

        string message;

        if (_isTryingToJoin)
        {
            message = "Esta cuenta ya está en uso. Iniciá sesión de nuevo.";
            _isTryingToJoin = false;
            _wasRejectedByDuplicateLogin = true;
        }
        else
        {
            message = reason switch
            {
                NetDisconnectReason.Timeout =>
                    "Se perdió la conexión con el servidor.",

                _ =>
                    "Desconectado del servidor."
            };
        }

        Debug.LogWarning($"[RunnerManager] {message}");

        AuthenticationManager.Instance?.SignOut();

        GameFlowManager.Instance?.ForceReturnToLogin(message);
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remote, NetConnectFailedReason reason)
    {
        Debug.LogWarning($"[RunnerManager] Conexión fallida: {reason}");
    }

    // ?? Callbacks vacíos requeridos por la interfaz 
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnShutdown(NetworkRunner r, ShutdownReason s) { }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr msg) { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, ReliableKey k, ArraySegment<byte> d) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef p, ReliableKey k, float pr) { }
    public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i) { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> d) { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken h) { }
    public void OnSceneLoadDone(NetworkRunner r) { }
    public void OnSceneLoadStart(NetworkRunner r) { }
}