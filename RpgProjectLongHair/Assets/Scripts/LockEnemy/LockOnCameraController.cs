using UnityEngine;
using Unity.Cinemachine;

public class LockOnCameraController : MonoBehaviour
{
    [Header("Lock-On")]
    [SerializeField] private Vector3 _lockOnOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float _rotationSpeed = 5f;

    [Header("Camera Side Offset")]
    [SerializeField] private Vector3 _cameraSideOffset = new Vector3(2f, 0f, 0f);
    [SerializeField] private float _offsetSmooth = 5f;

    private CinemachineVirtualCamera _vCam;
    private CinemachinePOV _pov;

    private Transform _cameraRoot;
    private Vector3 _originalLocalPos;
    private Vector3 _targetOffset;

    private Transform _playerTransform;
    private Transform _lockTarget;

    private bool _isLocked;

    public void Initialize(CinemachineVirtualCamera vCam, Transform player)
    {
        _vCam = vCam;
        _playerTransform = player;

        _pov = _vCam.GetComponentInChildren<CinemachinePOV>();

        _cameraRoot = _vCam.transform.parent != null ? _vCam.transform.parent : _vCam.transform;

        _originalLocalPos = _cameraRoot.localPosition;

        Debug.Log($"[LockOnCam] Init OK | root:{_cameraRoot.name}");
    }

    public void SetTarget(Transform target, Transform player)
    {
        _lockTarget = target;
        _playerTransform = player;
        _isLocked = true;

        _targetOffset = _cameraSideOffset;

        Debug.Log("[LockOnCam] LOCK");
    }

    public void ClearTarget(Transform player)
    {
        _lockTarget = null;
        _isLocked = false;

        _targetOffset = Vector3.zero;

        Debug.Log("[LockOnCam] UNLOCK");
    }

    private void LateUpdate()
    {
        if (_cameraRoot != null)
        {
            Vector3 desiredPos = _originalLocalPos + (_isLocked ? _targetOffset : Vector3.zero);

            _cameraRoot.localPosition = Vector3.Lerp(
                _cameraRoot.localPosition,
                desiredPos,
                Time.deltaTime * _offsetSmooth
            );
        }

        if (!_isLocked || _lockTarget == null || _pov == null) return;

        Vector3 targetPos = _lockTarget.position + _lockOnOffset;
        Vector3 dir = (targetPos - _playerTransform.position).normalized;

        float targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float targetPitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg;

        _pov.m_HorizontalAxis.Value = Mathf.LerpAngle(
            _pov.m_HorizontalAxis.Value,
            targetYaw,
            _rotationSpeed * Time.deltaTime
        );

        _pov.m_VerticalAxis.Value = Mathf.Lerp(
            _pov.m_VerticalAxis.Value,
            targetPitch,
            _rotationSpeed * Time.deltaTime
        );
    }
}