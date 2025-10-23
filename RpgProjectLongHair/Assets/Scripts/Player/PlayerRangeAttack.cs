using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
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

    private void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!HasInputAuthority) return;
        if (_attackData == null || _attackData.ProjectilePrefab == null) return;

        Transform muzzle = GetBestSpawnPoint();
        if (muzzle == null) muzzle = transform;

        Vector3 spawnPos = muzzle.position;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        RPC_RequestShoot(spawnPos, forward);
    }

    private Transform GetBestSpawnPoint()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0) return null;
        if (_spawnPoints.Length == 1) return _spawnPoints[0];

        Transform best = _spawnPoints[0];
        float bestDot = -1f;
        Vector3 fwd = transform.forward;

        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            var t = _spawnPoints[i];
            float dot = Vector3.Dot(fwd, (t.position - transform.position).normalized);
            if (dot > bestDot) { bestDot = dot; best = t; }
        }
        return best;
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
           {
               proj.InitServer(direction, _attackData, info.Source, spawnPos);
           }
       }
      );
    }
}