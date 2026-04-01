using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera cam = PlayerCamera.Local != null
            ? PlayerCamera.Local.GetComponent<Camera>()
            : Camera.main;

        if (cam == null) return;

        transform.rotation = cam.transform.rotation;
    }
}