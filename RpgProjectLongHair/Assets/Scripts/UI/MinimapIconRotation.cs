using UnityEngine;

public class MinimapIconRotation : MonoBehaviour
{
    [SerializeField]
    private Vector3 rotationOffset =
        new Vector3(90f, 0f, 0f);

    private Transform _localPlayer;

    private void Start()
    {
        if (PlayerCamera.Local != null)
            _localPlayer = PlayerCamera.Local.transform.root;
    }

    private void LateUpdate()
    {
        if (_localPlayer == null) return;

        transform.rotation = Quaternion.Euler(
            rotationOffset.x,
            _localPlayer.eulerAngles.y + rotationOffset.y,
            rotationOffset.z
        );
    }
}