using UnityEngine;

public class EnemyAttackMeleeState : IEnemyState
{
    private readonly EnemyController _enemy;
    private float _nextAttackTime;

    public EnemyAttackMeleeState(EnemyController enemy)
    {
        _enemy = enemy;
    }

    public void EnterState() { }
    public void ExitState() { }

    public void UpdateState()
    {
        if (!_enemy.Object.HasStateAuthority) return;

        if (_enemy.TargetPlayer == null)
        {
            _enemy.ChangeState(new EnemyIdleState(_enemy));
            return;
        }

        float dist = Vector3.Distance(_enemy.transform.position, _enemy.TargetPlayer.position);

        if (dist > _enemy.MeleeAttackData.AttackRange)
        {
            _enemy.ChangeState(new EnemyChaseState(_enemy));
            return;
        }

        if (_enemy.Runner.SimulationTime < _nextAttackTime)
            return;

        Collider[] hits = Physics.OverlapSphere(
            _enemy.AttackOrigin.position,
            _enemy.MeleeAttackData.HitRadius,
            _enemy.PlayerLayer
        );

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") && hit.TryGetComponent<PlayerHealth>(out var playerHealth))
            {
                playerHealth.TakeDamage(_enemy.MeleeAttackData.Damage);
                Debug.Log($"[Enemy] Hit player for {_enemy.MeleeAttackData.Damage} damage.");
                _nextAttackTime = _enemy.Runner.SimulationTime + _enemy.MeleeAttackData.Cooldown;
            }
        }
    }
}