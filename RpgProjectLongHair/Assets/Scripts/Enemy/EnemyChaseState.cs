using UnityEngine;
using UnityEngine.AI;


public class EnemyChaseState : IEnemyState
{
    private readonly EnemyController _enemy;

    public EnemyChaseState(EnemyController enemy)
    {
        _enemy = enemy;
    }

    public void EnterState() { }

    public void ExitState()
    {
        if (_enemy.Agent != null)
            _enemy.Agent.ResetPath();
    }

    public void UpdateState()
    {
        if (!_enemy.Object.HasStateAuthority) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var p in players)
        {
            float d = Vector3.Distance(_enemy.transform.position, p.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = p.transform;
            }
        }

        _enemy.SetTarget(closest);

        if (closest == null)
        {
            _enemy.ChangeState(new EnemyIdleState(_enemy));
            return;
        }

        if (minDist <= _enemy.MeleeAttackData.AttackRange)
        {
            _enemy.ChangeState(new EnemyAttackMeleeState(_enemy));
            return;
        }

        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
            _enemy.Agent.SetDestination(closest.position);
    }
}