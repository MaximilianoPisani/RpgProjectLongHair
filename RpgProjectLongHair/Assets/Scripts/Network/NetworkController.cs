using System;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class NetworkController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _joinRoomButton;

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
    }

    private void TryStartRunner(GameMode mode)
    {
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
        Debug.Log("[UIController] Player spawned, hiding lobby UI.");
        _lobbyPanel.SetActive(false);
    }

    private void OnRunnerFailed()
    {
        if (_runnerManagerInstance != null)
        {
            Destroy(_runnerManagerInstance.gameObject);
            _runnerManagerInstance = null;
        }

        Debug.Log("[UIController] Runner failed. Ready to retry.");
        _lobbyPanel.SetActive(true);
    }
}