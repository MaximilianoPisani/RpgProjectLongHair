using UnityEngine;
using Fusion;

[RequireComponent(typeof(Collider))]
public class WaterTramoTrigger : NetworkBehaviour
{
    [Tooltip("Altura Y de la superficie del agua — ajustar al nivel visual del agua")]
    [SerializeField] private float _waterSurfaceLevel = 0f;

    [Tooltip("Layer del player para optimizar detección")]
    [SerializeField] private LayerMask _playerLayer = -1;

    private void Awake()
    {
        var collider = GetComponent<Collider>();
        collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo procesar en el cliente, el servidor no necesita postprocesado
        if (Runner != null && Runner.IsServer) return;

        // Opcional: verificar layer para optimizar
        if (_playerLayer != -1 && !_playerLayer.Contains(other.gameObject.layer))
            return;

        var detector = FindDetector(other);
        if (detector != null)
        {
            Debug.Log($"[WaterTrigger] Player entró al agua - Detector encontrado, registrando nivel Y={_waterSurfaceLevel}");
            detector.RegisterWaterSurface(_waterSurfaceLevel);
        }
        else
        {
            Debug.LogWarning($"[WaterTrigger] Player entró pero no se encontró CameraUnderwaterDetector en {other.name}");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (Runner != null && Runner.IsServer) return;
        if (_playerLayer != -1 && !_playerLayer.Contains(other.gameObject.layer)) return;

        var detector = FindDetector(other);
        detector?.ConfirmInsideZone();
    }

    private void OnTriggerExit(Collider other)
    {
        if (Runner != null && Runner.IsServer) return;

        if (_playerLayer != -1 && !_playerLayer.Contains(other.gameObject.layer))
            return;

        var detector = FindDetector(other);
        if (detector != null)
        {
            Debug.Log("[WaterTrigger] Player salió del agua - Desregistrando superficie");
            detector.UnregisterWaterSurface();
        }
    }

    private CameraUnderwaterDetector FindDetector(Collider other)
    {
        // 1. Buscar en el propio objeto (poco probable)
        var detector = other.GetComponent<CameraUnderwaterDetector>();
        if (detector != null) return detector;

        // 2. Buscar en el NetworkObject padre (lo más probable en Fusion)
        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj != null)
        {
            detector = netObj.GetComponentInChildren<CameraUnderwaterDetector>();
            if (detector != null)
            {
                Debug.Log($"[WaterTrigger] Detector encontrado en NetworkObject: {netObj.name}");
                return detector;
            }
        }

        // 3. Buscar en el transform root como último recurso
        var root = other.transform.root;
        detector = root.GetComponentInChildren<CameraUnderwaterDetector>();
        if (detector != null)
        {
            Debug.Log($"[WaterTrigger] Detector encontrado en root: {root.name}");
            return detector;
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var collider = GetComponent<Collider>();
        if (collider == null) return;

        Bounds bounds = collider.bounds;

        // Línea de superficie del agua
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f);
        Vector3 start = new Vector3(bounds.min.x, _waterSurfaceLevel, bounds.min.z);
        Vector3 end = new Vector3(bounds.max.x, _waterSurfaceLevel, bounds.max.z);

        // Dibujar línea en X
        Gizmos.DrawLine(start, end);

        // Dibujar línea en Z
        start = new Vector3(bounds.min.x, _waterSurfaceLevel, bounds.min.z);
        end = new Vector3(bounds.min.x, _waterSurfaceLevel, bounds.max.z);
        Gizmos.DrawLine(start, end);

        // Volumen del trigger semi-transparente
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.1f);
        Gizmos.DrawCube(bounds.center, bounds.size);

        // Indicador de superficie
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(new Vector3(bounds.center.x, _waterSurfaceLevel, bounds.center.z), 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        // Dibujar más visible cuando está seleccionado
        var collider = GetComponent<Collider>();
        if (collider == null) return;

        Bounds bounds = collider.bounds;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // Plano de agua completo
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);
        Vector3 waterPlaneSize = new Vector3(bounds.size.x, 0.01f, bounds.size.z);
        Vector3 waterPlaneCenter = new Vector3(bounds.center.x, _waterSurfaceLevel, bounds.center.z);
        Gizmos.DrawCube(waterPlaneCenter, waterPlaneSize);
    }
#endif
}

// Extension method helper
public static class LayerMaskExtensions
{
    public static bool Contains(this LayerMask mask, int layer)
    {
        return mask == (mask | (1 << layer));
    }
}