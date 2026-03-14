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
            Debug.Log("[Chase] TargetPlayer es NULL, volviendo a Idle");
            _enemy.ChangeState(new EnemyIdleState(_enemy));
            return;
        }

        float dist = Vector3.Distance(_enemy.transform.position, _enemy.TargetPlayer.position);
        Debug.Log($"[Chase] dist={dist:F2} | agentEnabled={_enemy.Agent.enabled} | isOnNavMesh={_enemy.Agent.isOnNavMesh} | isStopped={_enemy.Agent.isStopped} | speed={_enemy.Agent.speed}");

        if (dist <= _enemy.MeleeAttackData.AttackRange)
        {
            _enemy.ChangeState(new EnemyAttackMeleeState(_enemy));
            return;
        }

        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
            _enemy.Agent.SetDestination(_enemy.TargetPlayer.position);
    }
}
