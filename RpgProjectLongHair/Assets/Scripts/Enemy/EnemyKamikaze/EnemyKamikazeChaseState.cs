using UnityEngine;

public class EnemyKamikazeChaseState : IEnemyState
{
    private readonly EnemyKamikazeController _enemy;

    public EnemyKamikazeChaseState(EnemyKamikazeController enemy) => _enemy = enemy;

    public void EnterState() { }
    public void ExitState() { }

    public void UpdateState()
    {
        if (!_enemy.Object.HasStateAuthority) return;

        if (_enemy.TargetPlayer == null)
        {
            _enemy.ChangeState(new EnemyKamikazeIdleState(_enemy));
            return;
        }

        float dist = Vector3.Distance(_enemy.transform.position, _enemy.TargetPlayer.position);

        if (dist <= _enemy.KamikazeData.AttackRange)
        {
            _enemy.ChangeState(new EnemyKamikazeExplodeState(_enemy));
            return;
        }

        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
            _enemy.Agent.SetDestination(_enemy.TargetPlayer.position);
    }
}