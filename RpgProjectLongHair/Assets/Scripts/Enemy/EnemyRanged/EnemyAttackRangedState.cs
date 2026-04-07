using Fusion;
using UnityEngine;

public class EnemyAttackRangedState : IEnemyState
{
    private readonly EnemyRangedController _enemy;

    public EnemyAttackRangedState(EnemyRangedController enemy) => _enemy = enemy;

    public void EnterState() { }
    public void ExitState() { }

    public void UpdateState()
    {
        if (!_enemy.Object.HasStateAuthority) return;

        if (_enemy.TargetPlayer == null)
        {
            _enemy.ChangeState(new EnemyIdleRangedState(_enemy));
            return;
        }

        if (_enemy.TargetPlayer.TryGetComponent<PlayerHealth>(out var ph) && ph.IsDead)
        {
            _enemy.ChangeState(new EnemyIdleRangedState(_enemy));
            return;
        }

        float dist = Vector3.Distance(_enemy.transform.position, _enemy.TargetPlayer.position);

        if (dist > _enemy.PreferredMaxRange + 1f)
        {
            _enemy.ChangeState(new EnemyChaseRangedState(_enemy));
            return;
        }

        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
        {
            if (dist < _enemy.PreferredMinRange)
            {

                Vector3 retreatDir = (_enemy.transform.position - _enemy.TargetPlayer.position).normalized;
                Vector3 retreatTarget = _enemy.transform.position + retreatDir * 2f;
                _enemy.Agent.SetDestination(retreatTarget);
            }
            else
            {
                _enemy.Agent.SetDestination(_enemy.transform.position); 
            }
        }

        Vector3 dir = (_enemy.TargetPlayer.position - _enemy.transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            _enemy.transform.rotation = Quaternion.LookRotation(dir);

        if (_enemy.Runner.SimulationTime >= _enemy.NextRangedAttackTime)
        {
            _enemy.TriggerRangedAttackAnimation();

            FireProjectile();
            _enemy.NextRangedAttackTime = _enemy.Runner.SimulationTime
                + _enemy.RangedAttackData.Cooldown;
        }
    }

    private void FireProjectile()
    {
        var data = _enemy.RangedAttackData;
        if (data?.ProjectilePrefab == null) return;

        Vector3 spawnPos = _enemy.AttackOrigin != null
            ? _enemy.AttackOrigin.position
            : _enemy.transform.position + Vector3.up * 1.2f;

        Vector3 targetPos = GetPlayerChestPosition();
        Vector3 direction = (targetPos - spawnPos).normalized;

        _enemy.Runner.Spawn(
            data.ProjectilePrefab,
            spawnPos,
            Quaternion.LookRotation(direction),
            PlayerRef.None,
            (runner, spawned) =>
            {
                var proj = spawned.GetComponent<EnemyProjectile>();
                if (proj != null)
                    proj.InitServer(direction, data, spawnPos);
            }
        );
    }

    private Vector3 GetPlayerChestPosition()
    {
        if (_enemy.TargetPlayer.TryGetComponent<Collider>(out var col))
            return col.bounds.center;

        return _enemy.TargetPlayer.position + Vector3.up * 1.2f;
    }
}