using System;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class NetworkController : MonoBehaviour
{
    public static NetworkController Instance { get; private set; }

    [Header("UI — Lobby principal")]
    [SerializeField] private GameObject _lobbyPanel;

    [Header("Sub-paneles")]
    [SerializeField] private RoomBrowser _roomBrowser;
    [SerializeField] private RoomCreator _roomCreator;

    [Header("Runner de juego — asignar el RunnerManager de la escena")]
    [SerializeField] private RunnerManager _runnerManager;

    [Header("Prefab para el browser runner (solo lobby/lista de salas)")]
    [SerializeField] private RunnerManager _browserRunnerManagerPrefab;

    private RunnerManager _browserRunnerInstance;

    private bool _isConnecting = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _roomCreator.OnRoomNameConfirmed += HandleCreateRoom;
        _roomBrowser.OnRoomSelected += HandleJoinRoom;
    }

    public void OnLobbyOpened()
    {
        Debug.Log("[NetworkController] Lobby abierto");

        if (!GameFlowManager.Instance.CanConnect()) return;

        if (_browserRunnerInstance == null)
        {
            _browserRunnerInstance = Instantiate(_browserRunnerManagerPrefab);
            _browserRunnerInstance.name = "RunnerManager_Browser";
            _browserRunnerInstance.StartLobbyBrowser(OnBrowserFailed);
        }

        _isConnecting = false;
    }

    private void HandleCreateRoom(string roomName)
    {
        if (_isConnecting) return;
        Debug.Log($"[NetworkController] Crear sala: '{roomName}'");

        if (!ValidateCanConnect()) return;
        _isConnecting = true;

        ShutdownBrowserRunnerAsync(() =>
        {
            _runnerManager.OnPlayerSpawned += HandlePlayerSpawned;
            _runnerManager.StartRunner(GameMode.Host, roomName, OnRunnerFailed);
        });
    }

    private void HandleJoinRoom(string sessionName)
    {
        if (_isConnecting) return;
        Debug.Log($"[NetworkController] Unirse a sala: '{sessionName}'");

        if (!ValidateCanConnect()) return;
        _isConnecting = true;

        ShutdownBrowserRunnerAsync(() =>
        {
            _runnerManager.OnPlayerSpawned += HandlePlayerSpawned;
            _runnerManager.StartRunner(GameMode.Client, sessionName, OnRunnerFailed);
        });
    }

    private void HandlePlayerSpawned(NetworkObject playerObj)
    {
        Debug.Log("[NetworkController] Player spawned ? gameplay");
        _runnerManager.OnPlayerSpawned -= HandlePlayerSpawned;
        _isConnecting = false;
        _lobbyPanel.SetActive(false);
        GameFlowManager.Instance.EnterGameplay();
    }

    private void OnRunnerFailed()
    {
        Debug.LogError("[NetworkController] Runner falló — volviendo al lobby browser");
        _runnerManager.OnPlayerSpawned -= HandlePlayerSpawned;
        _isConnecting = false;
        _lobbyPanel.SetActive(true);

        if (_browserRunnerInstance == null)
        {
            _browserRunnerInstance = Instantiate(_browserRunnerManagerPrefab);
            _browserRunnerInstance.name = "RunnerManager_Browser";
            _browserRunnerInstance.StartLobbyBrowser(OnBrowserFailed);
        }
    }

    private void OnBrowserFailed()
    {
        Debug.LogWarning("[NetworkController] Browser runner falló — lista de salas no disponible");
        _browserRunnerInstance = null;
    }

    private async void ShutdownBrowserRunnerAsync(Action onComplete)
    {
        if (_browserRunnerInstance != null)
        {
            var instance = _browserRunnerInstance;
            _browserRunnerInstance = null;            

            await instance.ShutdownAsync();
            Destroy(instance.gameObject);

            Debug.Log("[NetworkController] Browser runner cerrado OK");
        }
        else
        {
            Debug.Log("[NetworkController] No había browser runner activo");
        }

        onComplete?.Invoke();
    }

    private bool ValidateCanConnect()
    {
        if (!GameFlowManager.Instance.CanConnect())
        {
            Debug.LogError("[NetworkController] CanConnect falló");
            _isConnecting = false;
            return false;
        }
        if (!AuthenticationManager.Instance.IsSessionValid)
        {
            Debug.LogError("[NetworkController] Sesión inválida");
            _isConnecting = false;
            return false;
        }
        return true;
    }

    public async System.Threading.Tasks.Task ShutdownAllRunners()
    {
        _isConnecting = false;

        if (_browserRunnerInstance != null)
        {
            await _browserRunnerInstance.ShutdownAsync();
            Destroy(_browserRunnerInstance.gameObject);
            _browserRunnerInstance = null;
        }

        if (_runnerManager != null)
        {
            await _runnerManager.ShutdownAsync();
        }

        Debug.Log("[NetworkController] Todos los runners cerrados");
    }
}