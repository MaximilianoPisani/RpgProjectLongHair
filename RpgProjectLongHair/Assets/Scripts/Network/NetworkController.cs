using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class NetworkController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _joinRoomButton;
    [SerializeField] private Button _logoutButton;

    [Header("Prefabs")]
    [SerializeField] private RunnerManager _runnerManagerPrefab;

    private RunnerManager _runnerManagerInstance;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _createRoomButton.onClick.AddListener(() => TryStartRunner(GameMode.Host));
        _joinRoomButton.onClick.AddListener(() => TryStartRunner(GameMode.Client));
        _logoutButton.onClick.AddListener(OnSignOutClicked);
    }

    private void TryStartRunner(GameMode mode)
    {
        if (!GameFlowManager.Instance.CanConnect())
        {
            Debug.LogError("[Network] No permitido conectar");
            return;
        }

        if (!AuthenticationManager.Instance.IsSessionValid)
        {
            Debug.LogError("[Network] No autenticado");
            return;
        }

        if (_runnerManagerInstance == null)
        {
            _runnerManagerInstance = Instantiate(_runnerManagerPrefab);
            _runnerManagerInstance.name = "RunnerManager";

            _runnerManagerInstance.OnPlayerSpawned += HandlePlayerSpawned;

            _runnerManagerInstance.StartRunner(mode, OnRunnerFailed);
        }
    }

    private void HandlePlayerSpawned(NetworkObject playerObj)
    {
        Debug.Log("[Network] Player spawned");

        _lobbyPanel.SetActive(false);

        GameFlowManager.Instance.EnterGameplay();
    }

    private void OnRunnerFailed()
    {
        if (_runnerManagerInstance != null)
        {
            Destroy(_runnerManagerInstance.gameObject);
            _runnerManagerInstance = null;
        }

        _lobbyPanel.SetActive(true);
    }

    private async void OnSignOutClicked()
    {
        Debug.Log("[Network] Logout");

        if (_runnerManagerInstance != null)
        {
            var runner = _runnerManagerInstance.GetComponent<NetworkRunner>();

            if (runner != null)
            {
                await runner.Shutdown();
            }

            Destroy(_runnerManagerInstance.gameObject);
            _runnerManagerInstance = null;
        }

        AuthenticationManager.Instance?.SignOut();

        GameFlowManager.Instance?.ResetToLogin();
    }
}
