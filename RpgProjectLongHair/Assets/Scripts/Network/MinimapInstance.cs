using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class MinimapInstance : NetworkBehaviour
{
    public Camera minimapCamera;
    public Image minimapImage; // Mantenemos el componente Image que ya tienes
    public Material baseMaterial; // Arrastra tu "mat_minimap" aquí

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            // 1. Crear Textura
            RenderTexture localRT = new RenderTexture(256, 256, 16);
            localRT.Create();

            // 2. CREAR INSTANCIA DEL MATERIAL (La clave está aquí)
            // No uses el material original directamente en la UI.
            Material instancedMat = new Material(baseMaterial);

            // 3. Asignar la textura a esta copia privada del material
            instancedMat.mainTexture = localRT;

            // 4. Asignar todo
            minimapCamera.targetTexture = localRT;
            minimapImage.material = instancedMat; // ASIGNA LA COPIA, NO EL ORIGINAL
        }
        else
        {
            // Si no soy yo, asegúrate de que la cámara no esté activa
            if (minimapCamera != null) minimapCamera.gameObject.SetActive(false);
        }
    }
}