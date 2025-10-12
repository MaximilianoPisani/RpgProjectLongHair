using UnityEngine;
using Fusion;

public class PlayerCameraHandler : NetworkBehaviour
{
    [SerializeField] private GameObject _playerCameraRoot; // Object with Camera and CinemachineVirtualCamera

    public override void Spawned()
    {
        if (_playerCameraRoot == null)
        {
            Debug.LogWarning($"[PlayerCameraHandler] Player camera was not assigned in {name}");
            return;
        }

        if (HasInputAuthority)
        {
            // Enable camera only for this local player
            _playerCameraRoot.SetActive(true);
            Debug.Log($"[PlayerCameraHandler] Camera activated for local player {Object.InputAuthority}");
        }
        else
        {
            // Disable other players cameras
            _playerCameraRoot.SetActive(false);
        }
    }
}