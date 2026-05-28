using UnityEngine;

public class MinimapBillboard : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera cam = PlayerCamera.Local != null
            ? PlayerCamera.Local.GetComponent<Camera>()
            : Camera.main;

        if (cam == null) return;

        Vector3 euler = transform.eulerAngles;

        // Solo seguir la rotación Y de la cámara
        euler.y = cam.transform.eulerAngles.y;

        transform.eulerAngles = euler;
    }
}