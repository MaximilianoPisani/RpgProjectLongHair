using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -6f);
    [Header("Orbit")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 60f;

    private Transform _target;
    private float _yaw;
    private float _pitch = 15f;

    public static PlayerCamera Local { get; private set; }

    public Transform Target => _target;
    public bool IsActive => _target != null;

    public void Init(Transform target)
    {
        _target = target;
        _yaw = target.eulerAngles.y;
        Local = this; 
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        if (Local == this)
            Local = null;
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            _yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.position = _target.position + rotation * offset;
        transform.LookAt(_target.position + Vector3.up * 1.5f);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }
    }

    public Quaternion GetHorizontalRotation()
    {
        return Quaternion.Euler(0f, _yaw, 0f);
    }
}