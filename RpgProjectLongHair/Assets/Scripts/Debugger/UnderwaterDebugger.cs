using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Script de debugging para verificar el sistema de detección underwater
/// Adjuntar a la cámara junto con CameraUnderwaterDetector
/// </summary>
public class UnderwaterDebugger : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CameraUnderwaterDetector _detector;
    [SerializeField] private Volume _volume;

    [Header("UI Debug")]
    [SerializeField] private bool _showDebugUI = true;
    [SerializeField] private KeyCode _toggleKey = KeyCode.F3;

    private GUIStyle _labelStyle;
    private GUIStyle _headerStyle;

    private void Awake()
    {
        if (_detector == null)
            _detector = GetComponent<CameraUnderwaterDetector>();

        if (_volume == null)
            _volume = GetComponent<Volume>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
            _showDebugUI = !_showDebugUI;
    }

    private void OnGUI()
    {
        if (!_showDebugUI) return;

        // Inicializar estilos
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 14;
            _labelStyle.normal.textColor = Color.white;
            _labelStyle.padding = new RectOffset(5, 5, 2, 2);

            _headerStyle = new GUIStyle(_labelStyle);
            _headerStyle.fontSize = 16;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = Color.cyan;
        }

        // Fondo semi-transparente
        GUI.Box(new Rect(10, 10, 350, 220), "", GUI.skin.box);

        float y = 20;
        float lineHeight = 22;

        // Header
        GUI.Label(new Rect(20, y, 300, lineHeight), "UNDERWATER DETECTOR DEBUG", _headerStyle);
        y += lineHeight + 5;

        // Detector info
        if (_detector != null)
        {
            GUI.Label(new Rect(20, y, 300, lineHeight),
                $"Detector: {(_detector.enabled ? "Activo" : "Inactivo")}", _labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, 300, lineHeight),
                $"Underwater: {(_detector.IsUnderwater ? "SÍ" : "NO")}", _labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, 300, lineHeight),
                $"Weight Actual: {_detector.CurrentWeight:F2}", _labelStyle);
            y += lineHeight;
        }
        else
        {
            GUI.Label(new Rect(20, y, 300, lineHeight), "Detector NO encontrado", _labelStyle);
            y += lineHeight;
        }

        // Volume info
        if (_volume != null)
        {
            GUI.Label(new Rect(20, y, 300, lineHeight),
                $"Volume Global: {(_volume.isGlobal ? "SÍ" : "NO")}", _labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, 300, lineHeight),
                $"Volume Priority: {_volume.priority}", _labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, 300, lineHeight),
                $"Volume Weight: {_volume.weight:F2}", _labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, 300, lineHeight),
                $"Profile: {(_volume.profile != null ? _volume.profile.name : "NULL")}", _labelStyle);
            y += lineHeight;
        }
        else
        {
            GUI.Label(new Rect(20, y, 300, lineHeight), "Volume NO encontrado", _labelStyle);
            y += lineHeight;
        }

        // Posición de la cámara
        GUI.Label(new Rect(20, y, 300, lineHeight),
            $"Cámara Y: {transform.position.y:F2}", _labelStyle);
        y += lineHeight;

        // Instrucciones
        GUI.Label(new Rect(20, y, 300, lineHeight),
            $"[{_toggleKey}] Toggle Debug UI", _labelStyle);
    }

    private void OnDrawGizmos()
    {
        // Dibujar un pequeño indicador en la posición de la cámara
        Gizmos.color = _detector != null && _detector.IsUnderwater ?
            new Color(0f, 0.5f, 1f, 0.8f) : Color.green;

        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Línea vertical para ver altura
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 2f);
    }
}