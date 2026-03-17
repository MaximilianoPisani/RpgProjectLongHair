using UnityEngine;
using Unity.Cinemachine;
using Fusion;

public class LockOnCameraController : NetworkBehaviour
{
    [Header("Lock-On Camera")]
    [SerializeField] private Vector3 _lockOnOffset = new Vector3(0f, 1.5f, 0f);

    private CinemachineVirtualCamera _vCam;
    private CinemachineFramingTransposer _transposer;
    private Transform _lockTarget;
    private bool _isLocked;

    private float _originalScreenX;
    private float _originalScreenY;

    private GameObject _lookAtProxy;

    public void Initialize(CinemachineVirtualCamera vCam, Transform player)
    {
        _vCam = vCam;
        _transposer = _vCam?.GetCinemachineComponent<CinemachineFramingTransposer>();

        if (_transposer != null)
        {
            _originalScreenX = _transposer.m_ScreenX;
            _originalScreenY = _transposer.m_ScreenY;
        }

        _lookAtProxy = new GameObject("LockOnLookAtProxy");
        _lookAtProxy.hideFlags = HideFlags.HideInHierarchy;
    }

    public void SetTarget(Transform target, Transform player)
    {
        if (_vCam == null) return;

        _lockTarget = target;
        _isLocked = true;

        _lookAtProxy.transform.position = target.position + _lockOnOffset;

        _vCam.Follow = player;
        _vCam.LookAt = _lookAtProxy.transform; 

        if (_transposer != null)
        {
            _transposer.m_ScreenX = 0.35f;
            _transposer.m_ScreenY = 0.5f;
        }
    }

    public void ClearTarget(Transform player)
    {
        if (_vCam == null) return;

        _lockTarget = null;
        _isLocked = false;

        _vCam.Follow = player;
        _vCam.LookAt = null;

        if (_transposer != null)
        {
            _transposer.m_ScreenX = _originalScreenX;
            _transposer.m_ScreenY = _originalScreenY;
        }
    }

    private void LateUpdate()
    {
        if (!_isLocked || _lockTarget == null || _lookAtProxy == null) return;

        _lookAtProxy.transform.position = _lockTarget.position + _lockOnOffset;
    }

    private void OnDestroy()
    {
        if (_lookAtProxy != null)
            Destroy(_lookAtProxy);
    }
}