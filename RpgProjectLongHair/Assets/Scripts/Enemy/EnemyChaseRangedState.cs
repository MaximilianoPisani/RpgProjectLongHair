using UnityEngine;
public class EnemyChaseRangedState : IEnemyState
{
    private readonly EnemyRangedController _enemy;

    public EnemyChaseRangedState(EnemyRangedController enemy) => _enemy = enemy;

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
            _enemy.OnTargetDied();
            return;
        }

        float dist = Vector3.Distance(_enemy.transform.position, _enemy.TargetPlayer.position);

        if (dist <= _enemy.PreferredMaxRange)
        {
            _enemy.ChangeState(new EnemyAttackRangedState(_enemy));
            return;
        }

        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
            _enemy.Agent.SetDestination(_enemy.TargetPlayer.position);
    }
}