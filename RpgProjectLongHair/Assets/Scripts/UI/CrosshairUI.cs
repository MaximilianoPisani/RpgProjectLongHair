using UnityEngine;
using Fusion;
using System.Linq;

public class CrosshairUI : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform crosshair;
    public Camera cam;

    [Header("Settings")]
    public float smoothSpeed = 20f;

    private Vector3 _currentScreenPos;

    private PlayerStateMachine _player;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        // Buscar el player LOCAL (con InputAuthority)
        _player = FindObjectsByType<PlayerStateMachine>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.Object.HasInputAuthority);

        _currentScreenPos = crosshair.position;
    }

    void Update()
    {
        if (cam == null || _player == null) return;

        Vector3 worldPoint = _player.InputData.aimPoint;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPoint);

        // Evita que el crosshair aparezca si está detrás de cámara
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