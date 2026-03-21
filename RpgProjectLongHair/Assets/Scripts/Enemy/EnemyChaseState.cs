using UnityEngine;

public class EnemyChaseState : IEnemyState
{
    private readonly EnemyController _enemy;

    public EnemyChaseState(EnemyController enemy)
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

        if (dist <= _enemy.MeleeAttackData.AttackRange)
        {
            _enemy.ChangeState(new EnemyAttackMeleeState(_enemy));
            return;
        }

        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
            _enemy.Agent.SetDestination(_enemy.TargetPlayer.position);
    }
}
