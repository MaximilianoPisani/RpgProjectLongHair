using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class MinimapInstance : NetworkBehaviour
{
    [Header("Referencias")]
    public Camera minimapCamera;
    public RawImage minimapDisplay;   // Usa RawImage, no Image con material
    public GameObject minimapHUDRoot;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
        {
            // Desactivar el HUD COMPLETO, no solo la cámara
            minimapHUDRoot.SetActive(false);
            return;
        }

        // Solo llega acá el jugador local
        RenderTexture localRT = new RenderTexture(256, 256, 16);
        localRT.Create();

        minimapCamera.targetTexture = localRT;
        minimapDisplay.texture = localRT; // RawImage acepta la textura directamente
    }
    void OnDestroy()
    {
        // Limpiar la RenderTexture al destruirse
        if (minimapCamera != null && minimapCamera.targetTexture != null)
        {
            minimapCamera.targetTexture.Release();
        }
    }
}