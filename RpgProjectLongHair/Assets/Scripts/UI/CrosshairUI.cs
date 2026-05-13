using Fusion;
using UnityEngine;

public class CrosshairUI : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform crosshair;
    public Camera cam;

    [Header("Settings")]
    public float smoothSpeed = 20f;

    private Vector3 _currentScreenPos;
    private PlayerStateMachine _player;
    private RunnerManager _runnerManager;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        // Reemplazo correcto del deprecated
        _runnerManager = FindFirstObjectByType<RunnerManager>();

        if (_runnerManager != null)
        {
            _runnerManager.OnPlayerSpawned += OnPlayerSpawned;
        }

        _currentScreenPos = crosshair.position;
    }

    private void OnDestroy()
    {
        // MUY importante: evitar leaks/eventos colgados
        if (_runnerManager != null)
        {
            _runnerManager.OnPlayerSpawned -= OnPlayerSpawned;
        }
    }

    private void OnPlayerSpawned(NetworkObject obj)
    {
        var player = obj.GetComponent<PlayerStateMachine>();

        if (player != null && player.Object.HasInputAuthority)
        {
            _player = player;
        }
    }

    void Update()
    {
        if (cam == null || _player == null) return;

        Vector3 worldPoint = _player.InputData.aimPoint;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPoint);

        // Ocultar si está detrás
        if (screenPos.z < 0)
        {
            if (crosshair.gameObject.activeSelf)
                crosshair.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (!crosshair.gameObject.activeSelf)
                crosshair.gameObject.SetActive(true);
        }

        _currentScreenPos = Vector3.Lerp(
            _currentScreenPos,
            screenPos,
            Time.deltaTime * smoothSpeed
        );

        crosshair.position = _currentScreenPos;
    }
}