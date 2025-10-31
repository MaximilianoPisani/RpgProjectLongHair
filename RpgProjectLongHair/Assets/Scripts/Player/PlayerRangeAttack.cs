using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerRangeAttack : NetworkBehaviour
{
    [SerializeField] private RangedAttackData _attackData;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private LayerMask _aimLayerMask = ~0;
    [SerializeField] private RectTransform _crosshair;
    [SerializeField] private Camera _camera;

    [Networked] private TickTimer _cooldownTimer { get; set; }

    private PlayerInput _playerInput;
    private Transform _cameraTransform;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
      
        if (_camera == null)
            _camera = Camera.main;
    }

    private void OnEnable()
    {
        if (_playerInput != null)
            _playerInput.actions["Attack1"].performed += OnAttack;
    }

    private void OnDisable()
    {
        if (_playerInput != null)
            _playerInput.actions["Attack1"].performed -= OnAttack;
    }

    private void Update()
    {
        if (HasInputAuthority && _cameraTransform == null && Camera.main != null)
            _cameraTransform = Camera.main.transform;
    }

    private void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!HasInputAuthority) return;
        if (_attackData == null || _attackData.ProjectilePrefab == null) return;

        if (_camera == null)
            _camera = Camera.main;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, _crosshair.position);

        Ray ray = _camera.ScreenPointToRay(screenPoint);

        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 1f);

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _aimLayerMask))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(1000f); 

        Vector3 muzzlePos = GetMuzzlePosition();

        Vector3 shootDir = (targetPoint - muzzlePos).normalized;

        Vector3 flatDir = new Vector3(shootDir.x, 0, shootDir.z);
        if (flatDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(flatDir);

        Debug.DrawLine(muzzlePos, targetPoint, Color.green, 1f);

        RPC_RequestShoot(muzzlePos, shootDir);
    }


    private Vector3 GetMuzzlePosition()
    {
        if (_spawnPoints != null && _spawnPoints.Length > 0 && _spawnPoints[0] != null)
            return _spawnPoints[0].position;
        return transform.position + transform.forward * 1.2f + Vector3.up * 1.2f;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestShoot(Vector3 spawnPos, Vector3 direction, RpcInfo info = default)
    {
        if (!Runner.IsServer) return;
        if (_attackData == null || _attackData.ProjectilePrefab == null) return;

        if (!_cooldownTimer.ExpiredOrNotRunning(Runner))
            return;

        float cd = (_attackData is AttackData ad) ? ad.Cooldown : 0f;
        _cooldownTimer = TickTimer.CreateFromSeconds(Runner, cd > 0f ? cd : 0f);

        Runner.Spawn(
            _attackData.ProjectilePrefab,
            spawnPos,
            Quaternion.LookRotation(direction.normalized),
            info.Source,
            (runner, spawned) =>
            {
                var proj = spawned.GetComponent<Projectile>();
                if (proj != null)
                    proj.InitServer(direction, _attackData, info.Source, spawnPos);
            }
        );
    }
}