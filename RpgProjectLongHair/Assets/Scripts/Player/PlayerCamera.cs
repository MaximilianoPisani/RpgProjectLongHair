using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -4f);

    [Header("Look Offset (Shoulder Camera)")]
    [SerializeField] private Vector3 lookOffset = new Vector3(1.8f, 2f, 0f);

    [Header("Orbit")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -26.1f;
    [SerializeField] private float maxPitch = 50f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayers = -1; // Todo por defecto
    [SerializeField] private float collisionPadding = 0.2f; // Espacio extra para evitar clip
    [SerializeField] private float smoothSpeed = 10f; // Velocidad de acercamiento/alejamiento
    [SerializeField] private float returnSpeed = 5f; // Velocidad de retorno (más lento)
    [SerializeField] private bool debugMode = false;

    private Transform _target;
    private float _yaw;
    private float _pitch = 15f;
    private Camera _camera;
    private Vector2 _nearPlaneSize;

    // Para suavizado
    private float _currentDistance;
    private float _targetDistance;

    public static PlayerCamera Local { get; private set; }
    public Transform Target => _target;
    public bool IsActive => _target != null;

    public void Init(Transform target)
    {
        _target = target;
        _yaw = target.eulerAngles.y;
        Local = this;

        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            _camera = GetComponentInChildren<Camera>();
        }

        CalculateNearPlaneSize();

        // Inicializar distancias
        _currentDistance = offset.magnitude;
        _targetDistance = _currentDistance;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        if (Local == this)
            Local = null;
    }

    private void CalculateNearPlaneSize()
    {
        if (_camera == null) return;

        float height = Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad / 2f) * _camera.nearClipPlane;
        float width = height * _camera.aspect;
        _nearPlaneSize = new Vector2(width, height);
    }

    private float CalculateCollisionDistance(Vector3 targetPosition, Quaternion rotation)
    {
        Vector3 desiredDirection = rotation * offset.normalized;
        float maxDistance = offset.magnitude;
        float minDistance = maxDistance;

        // Origen del raycast: posición del target (con un pequeño offset hacia arriba)
        Vector3 origin = targetPosition + Vector3.up * 0.1f;

        // Posición deseada de la cámara
        Vector3 desiredCameraPos = targetPosition + desiredDirection * maxDistance;

        // 4 esquinas del near plane en la posición DESEADA
        Vector3 right = rotation * Vector3.right * _nearPlaneSize.x;
        Vector3 up = rotation * Vector3.up * _nearPlaneSize.y;

        Vector3[] corners = new Vector3[]
        {
        desiredCameraPos - right + up,
        desiredCameraPos + right + up,
        desiredCameraPos - right - up,
        desiredCameraPos + right - up,
        desiredCameraPos   // Centro también
        };

        foreach (Vector3 corner in corners)
        {
            Vector3 rayDir = (corner - origin);
            float rayDist = rayDir.magnitude;
            rayDir.Normalize();

            RaycastHit hit;
            if (Physics.Raycast(origin, rayDir, out hit, rayDist, collisionLayers))
            {
                // Distancia al punto de hit, menos padding
                float hitDistance = hit.distance - collisionPadding;
                minDistance = Mathf.Min(minDistance, hitDistance);

                if (debugMode)
                    Debug.DrawLine(origin, hit.point, Color.red);
            }
            else if (debugMode)
            {
                Debug.DrawLine(origin, corner, Color.green);
            }
        }

        minDistance = Mathf.Max(minDistance, _camera.nearClipPlane + collisionPadding);
        return minDistance;
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        // Input de ratón
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            _yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        // Calcular rotación
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // Calcular distancia objetivo con colisiones
        float maxDistance = offset.magnitude;
        float collisionDistance = CalculateCollisionDistance(_target.position, rotation);
        _targetDistance = Mathf.Min(collisionDistance, maxDistance);

        // Suavizado de distancia (acercamiento rápido, alejamiento más lento)
        float speed = (_currentDistance > _targetDistance) ? smoothSpeed : returnSpeed;
        _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, Time.deltaTime * speed);

        // Aplicar offset escalado según la distancia actual
        Vector3 scaledOffset = offset.normalized * _currentDistance;
        transform.position = _target.position + rotation * scaledOffset;

        // Look target
        Vector3 lookTarget = _target.position + rotation * lookOffset;
        transform.LookAt(lookTarget);

        // Cambio de hombro
        if (Input.GetKeyDown(KeyCode.Q) && !RunnerManager.IsInventoryOpen)
        {
            lookOffset.x *= -1f;
        }

        // Debug visual
        if (debugMode)
        {
            Debug.DrawLine(_target.position, transform.position, Color.blue);
            Debug.DrawLine(_target.position, lookTarget, Color.yellow);
        }
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    public Quaternion GetHorizontalRotation()
    {
        return Quaternion.Euler(0f, _yaw, 0f);
    }

    // Método helper para ajustar layers en runtime si es necesario
    public void SetCollisionLayers(LayerMask layers)
    {
        collisionLayers = layers;
    }
}