using UnityEngine;

public class EnemyLockOnIndicator : MonoBehaviour
{
    [SerializeField] private GameObject _indicatorRoot;

    private Transform _cameraTransform;

    private void Awake()
    {
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (_indicatorRoot == null || !_indicatorRoot.activeSelf) return;

        if (_cameraTransform == null)
            _cameraTransform = Camera.main?.transform;

        if (_cameraTransform != null)
        {
            _indicatorRoot.transform.rotation = _cameraTransform.rotation;
        }
    }

    public void SetVisible(bool visible)
    {
        if (_indicatorRoot != null)
            _indicatorRoot.SetActive(visible);
    }
}