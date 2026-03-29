using UnityEngine;
using Fusion;

public class EnemyLockOnSystem : NetworkBehaviour
{
    [Header("Lock-On Settings")]
    [SerializeField] private float _searchRadius = 10f;
    [SerializeField] private float _lockOnAngle = 60f;
    [SerializeField] private LayerMask _enemyLayer;

    private LockOnCameraController _cameraController;
    private EnemyLockOnIndicator _currentIndicator;
    private PlayerWeaponHandler _weaponHandler;

    public Transform CurrentTarget { get; private set; }
    public bool IsLockedOn => CurrentTarget != null;

    private void Awake()
    {
        _weaponHandler = GetComponent<PlayerWeaponHandler>();
        if (_weaponHandler == null)
            Debug.LogError("[LockOn] PlayerWeaponHandler no encontrado en " + gameObject.name);
    }

    private LockOnCameraController GetCameraController()
    {
        if (_cameraController == null)
        {
            _cameraController = GetComponent<LockOnCameraController>();

            if (_cameraController == null)
                Debug.LogError("[LockOn] No se encontró LockOnCameraController en el player");
        }

        return _cameraController;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;

        if (!IsWeaponActive())
        {
            ClearLockOn();
            return;
        }

        if (GetInput(out NetworkInputData inputData) && inputData.LockOnPressed)
        {
            if (IsLockedOn)
                ClearLockOn();
            else
                TryLockOnToNearestEnemy();
        }


        ValidateCurrentTarget();
    }

    private bool IsWeaponActive()
    {
        return _weaponHandler != null && (_weaponHandler.IsMelee || _weaponHandler.IsRanged);
    }

    private void TryLockOnToNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _searchRadius, _enemyLayer);

        Transform bestTarget = null;
        float bestScore = Mathf.Infinity;
        Camera cam = Camera.main;

        foreach (var hit in hits)
        {
            if (hit.transform.root == transform.root) continue;

            Vector3 dirToEnemy = (hit.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToEnemy);
            if (angle > _lockOnAngle) continue;

            if (cam != null)
            {
                Vector3 screenPos = cam.WorldToScreenPoint(hit.transform.position);
                if (screenPos.z < 0) continue;
            }

            float distToPlayer = Vector3.Distance(transform.position, hit.transform.position);
            float score = angle * 1.5f + distToPlayer;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = hit.transform;
            }
        }

        if (bestTarget != null)
            SetLockOn(bestTarget);
    }

    private void SetLockOn(Transform target)
    {
        CurrentTarget = target;
        GetCameraController()?.SetTarget(target, transform);

        _currentIndicator = target.GetComponentInChildren<EnemyLockOnIndicator>(true);
        _currentIndicator?.SetVisible(true);

        Debug.Log($"[LockOn] Fijado en: {target.name}");
    }

    public void ClearLockOn()
    {
        if (!IsLockedOn) return;

        _currentIndicator?.SetVisible(false);
        _currentIndicator = null;

        CurrentTarget = null;
        GetCameraController()?.ClearTarget(transform);

        Debug.Log("[LockOn] Lock-on eliminado");
    }

    private void ValidateCurrentTarget()
    {
        if (!IsLockedOn) return;

        if (CurrentTarget == null || !CurrentTarget.gameObject.activeInHierarchy)
        {
            ClearLockOn();
            return;
        }

        float dist = Vector3.Distance(transform.position, CurrentTarget.position);
        if (dist > _searchRadius * 2f)
            ClearLockOn();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _searchRadius);
        if (IsLockedOn)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, CurrentTarget.position);
        }
    }
}