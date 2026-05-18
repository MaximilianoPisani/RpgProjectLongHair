using UnityEngine;
using Fusion;

public class PlayerCameraHandler : NetworkBehaviour
{
    [SerializeField] private GameObject _playerCameraRoot;

    public override void Spawned()
    {
        if (_playerCameraRoot == null)
        {
            Debug.LogWarning($"[PlayerCameraHandler] Player camera was not assigned in {name}");
            return;
        }

        if (HasInputAuthority)
        {
            _playerCameraRoot.SetActive(true);
            // Inicializar la cámara
            var playerCamera = _playerCameraRoot.GetComponentInChildren<PlayerCamera>();
            if (playerCamera != null)
                playerCamera.Init(transform);

            Debug.Log($"[PlayerCameraHandler] Cámara activada para jugador local {Object.InputAuthority}");
        }
        else
        {
            _playerCameraRoot.SetActive(false);
        }
    }
}