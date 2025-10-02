using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;

public class PlayerRangeAttack : NetworkBehaviour
{
    [SerializeField] private RangedAttackData _attackData;
    [SerializeField] private Transform[] _spawnPoints;

    [Networked] private TickTimer _cooldownTimer { get; set; }

    private PlayerInput _playerInput;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
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

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!HasInputAuthority) return;

        Transform spawnPoint = GetClosestSpawnPoint();
        Vector3 shootDir = transform.forward;

        RPC_RequestShoot(spawnPoint.position, shootDir);
    }

    private Transform GetClosestSpawnPoint()
    {
        if (_spawnPoints.Length == 1) return _spawnPoints[0];

        Transform bestPoint = _spawnPoints[0];
        float bestDot = -1f;

        foreach (var point in _spawnPoints)
        {
            Vector3 dirToPoint = (point.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToPoint);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestPoint = point;
            }
        }

        return bestPoint;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestShoot(Vector3 spawnPos, Vector3 direction)
    {
        if (!_cooldownTimer.ExpiredOrNotRunning(Runner))
            return; 

        _cooldownTimer = TickTimer.CreateFromSeconds(Runner, _attackData.Cooldown);

        NetworkObject projObj = Runner.Spawn(
            _attackData.ProjectilePrefab,
            spawnPos,
            Quaternion.LookRotation(direction),
            Object.InputAuthority
        );

        var proj = projObj.GetComponent<Projectile>();
        if (proj != null)
            proj.InitProjectile(direction, _attackData, gameObject);
    }
}